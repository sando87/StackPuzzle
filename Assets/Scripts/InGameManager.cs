using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum GameFieldType { Noting, Stage, pvpPlayer, pvpOpponent }
public enum InGameState { Noting, Running, Paused, Win, Lose }
public class InGameManager : MonoBehaviour
{
    private static InGameManager mInstStage = null;
    private static InGameManager mInstPVP_Player = null;
    private static InGameManager mInstPVP_Opponent = null;
    public static InGameManager InstStage
    { get { if (mInstStage == null) mInstStage = GameObject.Find("WorldSpace").transform.Find("GameScreen/GameField").GetComponent<InGameManager>(); return mInstStage; } }
    public static InGameManager InstPVP_Player
    { get { if (mInstPVP_Player == null) mInstPVP_Player = GameObject.Find("WorldSpace").transform.Find("BattleScreen/GameFieldMe").GetComponent<InGameManager>(); return mInstPVP_Player; } }
    public static InGameManager InstPVP_Opponent
    { get { if (mInstPVP_Opponent == null) mInstPVP_Opponent = GameObject.Find("WorldSpace").transform.Find("BattleScreen/GameFieldOpp").GetComponent<InGameManager>(); return mInstPVP_Opponent; } }

    public GameObject[] ProductPrefabs;
    public GameObject FramePrefab1;
    public GameObject FramePrefab2;
    public GameObject MaskPrefab;
    public GameObject ComboNumPrefab;
    public GameObject AttackPointPrefab;

    private Frame[,] mFrames = null;
    private StageInfo mStageInfo = null;
    private UserInfo mUserInfo = null;
    private bool mMoveLock = false;

    private Queue<ProductSkill> mNextSkills = new Queue<ProductSkill>();
    private List<Frame> mEmptyFrames = new List<Frame>();
    private LinkedList<PVPInfo> mNetMessages = new LinkedList<PVPInfo>();
    public InGameBillboard Billboard = new InGameBillboard();

    public GameFieldType FieldType { get {
            return this == mInstStage ? GameFieldType.Stage :
                (this == mInstPVP_Player ? GameFieldType.pvpPlayer :
                (this == mInstPVP_Opponent ? GameFieldType.pvpOpponent : GameFieldType.Noting)); }
    }
    public Frame GetFrame(int x, int y)
    {
        if (x < 0 || x >= mStageInfo.XCount || y < 0 || y >= mStageInfo.YCount)
            return null;
        if (mFrames[x, y].Empty)
            return null;
        return mFrames[x, y];
    }
    public bool IsIdle { get; private set; }
    public int CountX { get { return mStageInfo.XCount; } }
    public int CountY { get { return mStageInfo.YCount; } }
    public float ColorCount { get { return mStageInfo.ColorCount; } }
    public int UserPk { get { return mUserInfo.userPk; } }
    public AttackPoints AttackPoints { get; set; }
    public InGameManager Opponent { get { return FieldType == GameFieldType.pvpPlayer ? InstPVP_Opponent : InstPVP_Player; } }
    public InGameBillboard GetBillboard() { return Billboard; }
    

    public Action<Vector3, StageGoalType> EventBreakTarget;
    public Action<Product[]> EventDestroyed;
    public Action<bool> EventFinish;
    public Action EventReduceLimit;
    private Action EventEnterIdle;

    public void StartGame(StageInfo info, UserInfo userInfo)
    {
        ResetGame();
        Vector3 pos = transform.position;

        transform.parent.gameObject.SetActive(true);
        gameObject.SetActive(true);
        mStageInfo = info;
        mUserInfo = userInfo;

        if (FieldType == GameFieldType.Stage)
        {
            GetComponent<SwipeDetector>().EventSwipe = OnSwipe;
            GetComponent<SwipeDetector>().EventClick = OnClick;
            EventEnterIdle += CheckStageFinish;
        }
        else if (FieldType == GameFieldType.pvpPlayer)
        {
            GetComponent<SwipeDetector>().EventSwipe = OnSwipe;
            GetComponent<SwipeDetector>().EventClick = OnClick;
            EventEnterIdle += () =>
            {
                StartCoroutine("FlushAttacks");
                CheckPVPFinish();
            };
        }
        else if (FieldType == GameFieldType.pvpOpponent)
        {
            StartCoroutine(ProcessNetMessages());
        }


        float gridSize = UserSetting.GridSize;
        Vector3 localBasePos = new Vector3(-gridSize * info.XCount * 0.5f, -gridSize * info.YCount * 0.5f, 0);
        localBasePos.x += gridSize * 0.5f;
        localBasePos.y += gridSize * 0.5f;
        Vector3 localFramePos = new Vector3(0, 0, 0);
        mFrames = new Frame[info.XCount, info.YCount];
        for (int y = 0; y < info.YCount; y++)
        {
            for (int x = 0; x < info.XCount; x++)
            {
                GameObject frameObj = GameObject.Instantiate((x + y) % 2 == 0 ? FramePrefab1 : FramePrefab2, transform, false);
                localFramePos.x = gridSize * x;
                localFramePos.y = gridSize * y;
                frameObj.transform.localPosition = localBasePos + localFramePos;
                mFrames[x, y] = frameObj.GetComponent<Frame>();
                mFrames[x, y].Initialize(this, x, y, info.GetCell(x, y).FrameCoverCount);
                mFrames[x, y].EventBreakCover = () => {
                    Billboard.CoverCount++;
                    EventBreakTarget?.Invoke(mFrames[x, y].transform.position, StageGoalType.Cover);
                };
            }
        }

        SecondaryInitFrames();

        GameObject ap = Instantiate(AttackPointPrefab, transform);
        ap.transform.localPosition = localBasePos + new Vector3(-gridSize + 0.2f, gridSize * CountY - 0.1f, 0);
        AttackPoints = ap.GetComponent<AttackPoints>();
    }
    public void InitProducts()
    {
        List<Product> initProducts = new List<Product>();
        for (int y = 0; y < CountY; y++)
        {
            for (int x = 0; x < CountX; x++)
            {
                if (mFrames[x, y].Empty)
                    continue;

                Product pro = CreateNewProduct(mFrames[x, y]);
                pro.SetChocoBlock(mStageInfo.GetCell(x, y).ProductChocoCount);
                pro.EventUnWrapChoco = () => {
                    Billboard.ChocoCount++;
                    EventBreakTarget?.Invoke(pro.transform.position, StageGoalType.Choco);
                };
                initProducts.Add(pro);
            }
        }

        Network_StartGame(initProducts.ToArray());
    }
    public void FinishGame()
    {
        StopAllCoroutines();
        ResetGame();
        gameObject.SetActive(false);
        transform.parent.gameObject.SetActive(false);
    }


    public void OnClick(GameObject clickedObj)
    {
        if (!IsIdle || mMoveLock)
            return;

        Product pro = clickedObj.GetComponent<Product>();
        List<Product> matches = new List<Product>();
        pro.SearchMatchedProducts(matches, pro.mColor);
        if (matches.Count < UserSetting.MatchCount)
        {
            pro.mAnimation.Play("swap");
            return;
        }

        IsIdle = false;
        DestroyProducts(matches);
        StartCoroutine(TryDestroyAroundEmpty(1.0f));
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectMatched);
        RemoveLimit();
    }
    public void OnSwipe(GameObject swipeObj, SwipeDirection dir)
    {
        if (mMoveLock)
            return;

        Product product = swipeObj.GetComponent<Product>();
        Product targetProduct = null;
        switch (dir)
        {
            case SwipeDirection.UP: targetProduct = product.Up(); break;
            case SwipeDirection.DOWN: targetProduct = product.Down(); break;
            case SwipeDirection.LEFT: targetProduct = product.Left(); break;
            case SwipeDirection.RIGHT: targetProduct = product.Right(); break;
        }

        if (targetProduct != null && !product.IsLocked() && !targetProduct.IsLocked())
        {
            RemoveLimit();
            Network_Swipe(product, dir);
            product.StartSwipe(targetProduct.GetComponentInParent<Frame>());
            targetProduct.StartSwipe(product.GetComponentInParent<Frame>());

            IsIdle = false;
            StartCoroutine("CheckIdle", 0.1f);
        }
    }
    private void DestroyProducts(List<Product> matches)
    {
        Product mainProduct = matches[0];

        int addedScore = 0;
        foreach (Product pro in matches)
        {
            addedScore += Billboard.CurrentCombo;
            pro.Combo = Billboard.CurrentCombo;
            pro.StartDestroy(gameObject);
            mEmptyFrames.Add(pro.ParentFrame);
            BreakItemSkill(pro);
        }

        if (FieldType == GameFieldType.Stage)
            ReduceTargetScoreCombo(mainProduct, Billboard.CurrentScore, Billboard.CurrentScore + addedScore);
        else
            Attack(addedScore, mainProduct.transform.position);

        Billboard.CurrentScore += addedScore;
        Billboard.DestroyCount += matches.Count;

        Network_Destroy(matches.ToArray());
        EventDestroyed?.Invoke(matches.ToArray());
    }
    private IEnumerator TryDestroyAroundEmpty(float delay)
    {
        yield return new WaitForSeconds(delay);
        Frame[] emptyFrames = mEmptyFrames.ToArray();
        mEmptyFrames.Clear();

        List<Product> aroundProducts = new List<Product>();
        foreach (Frame frame in emptyFrames)
        {
            Frame[] aroundFrames = frame.GetAroundFrames();
            foreach (Frame sub in aroundFrames)
            {
                Product pro = sub.ChildProduct;
                if (pro != null && !pro.IsLocked())
                    aroundProducts.Add(pro);
            }
        }

        if (IsMatched(aroundProducts))
        {
            Billboard.CurrentCombo++;
            Billboard.MaxCombo = Math.Max(Billboard.CurrentCombo, Billboard.MaxCombo);
            Billboard.ComboCounter[Billboard.CurrentCombo]++;

            List<Product> matches = new List<Product>();
            foreach (Product pro in aroundProducts)
            {
                matches.Clear();
                pro.SearchMatchedProducts(matches, pro.mColor);
                if (matches.Count >= UserSetting.MatchCount)
                    DestroyProducts(matches);
            }

            StartCoroutine(TryDestroyAroundEmpty(delay));
            SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectMatched);
        }
        else
        {
            Billboard.CurrentCombo = 0;
        }

        StartCoroutine(UnityUtils.CallAfterSeconds(0.5f, () =>
        {
            CreateNewProducts(emptyFrames);
        }));
    }
    private void CreateNewProducts(Frame[] emptyFrames)
    {
        List<Product> newProducts = new List<Product>();
        foreach(Frame frame in emptyFrames)
        {
            Product newProduct = CreateNewProduct(frame);
            newProduct.mAnimation.Play("swap");
            newProducts.Add(newProduct);
        }
        Network_Create(newProducts.ToArray());

        StartCoroutine("CheckIdle", 1.0f);
    }
    IEnumerator CheckIdle(float delay)
    {
        yield return new WaitForSeconds(delay);
        while (true)
        {
            if(IsAllIdle())
            {
                IsIdle = true;
                EventEnterIdle?.Invoke();
                break;
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    private void ReduceTargetScoreCombo(Product pro, int preScore, int nextScore)
    {
        if (mStageInfo.GoalTypeEnum == StageGoalType.Score)
        {
            int newStarCount = nextScore / UserSetting.ScorePerBar - preScore / UserSetting.ScorePerBar;
            for (int i = 0; i < newStarCount; ++i)
                EventBreakTarget?.Invoke(pro.transform.position, StageGoalType.Score);

        }
        else if (mStageInfo.GoalTypeEnum == StageGoalType.Combo)
        {
            string goalType = mStageInfo.GoalType;
            int targetCombo = int.Parse(goalType[goalType.Length - 1].ToString());
            int curCombo = Billboard.CurrentCombo;
            if (targetCombo == curCombo)
                EventBreakTarget?.Invoke(pro.transform.position, StageGoalType.Combo);
        }
    }

    private void Attack(int score, Vector3 fromPos)
    {
        int point = score / UserSetting.AttackScore;
        if (point <= 0)
            return;

        int remainPt = AttackPoints.Count;
        if (remainPt <= 0)
        {
            Opponent.Damaged(point, fromPos);
        }
        else
        {
            AttackPoints.Add(-point, fromPos);
        }
    }
    private void Damaged(int point, Vector3 fromPos)
    {
        AttackPoints.Add(point, fromPos);
        StartCoroutine("FlushAttacks");
    }
    private IEnumerator FlushAttacks()
    {
        while (AttackPoints.Count > 0)
        {
            if (AttackPoints.IsReady && IsIdle)
            {
                int cnt = AttackPoints.Pop(UserSetting.FlushCount);
                List<Product> products = GetNextFlushTargets(cnt);
                Network_FlushAttacks(products.ToArray());
                foreach (Product pro in products)
                    pro.SetChocoBlock(1, true);

                yield return new WaitForSeconds(2.0f);
            }
            else
                yield return null;

        }
    }
    private List<Product> GetNextFlushTargets(int cnt)
    {
        float xCenter = (CountX - 1.0f) * 0.5f;
        float yCenter = (CountY - 1.0f) * 0.5f;
        List<Product> products = new List<Product>();
        for (int y = 0; y < CountY; ++y)
        {
            for (int x = 0; x < CountX; ++x)
            {
                Product pro = mFrames[x, y].ChildProduct;
                if (pro == null || pro.IsChocoBlock())
                    continue;

                float distX = Math.Abs(xCenter - x);
                float distY = Math.Abs(yCenter - y);
                float max = Math.Max(distX, distY);
                float weight = max + UnityEngine.Random.Range(-0.4f, 0.4f);
                pro.Weight = weight;
                products.Add(pro);
            }
        }

        products.Sort((lsb, msb) =>
        {
            return lsb.Weight - msb.Weight > 0 ? -1 : 1;
        });
        return (products.Count < cnt) ? products : products.GetRange(0, cnt);
    }

    public void HandlerNetworkMessage(Header responseMsg)
    {
        if (!gameObject.activeInHierarchy)
            return;
        if (responseMsg.Ack == 1)
            return;
        if (responseMsg.Cmd != NetCMD.PVP)
            return;

        byte[] msgbody = (byte[])responseMsg.bodyByteBuffer;
        PVPInfo body = Utils.Deserialize<PVPInfo>(ref msgbody);
        if (body.cmd == PVPCommand.EndGame)
        {
            mNetMessages.AddFirst(body);
        }
        else
        {
            mNetMessages.AddLast(body);
        }
    }
    IEnumerator ProcessNetMessages()
    {
        while (true)
        {
            yield return null;

            if (mNetMessages.Count == 0)
                continue;

            PVPInfo body = mNetMessages.First.Value;
            if (body.cmd == PVPCommand.EndGame)
            {
                EventFinish?.Invoke(body.success);
                FinishGame();
            }
            else if(body.cmd == PVPCommand.StartGame)
            {
                foreach(ProductInfo info in body.products)
                {
                    Frame frame = GetFrame(info.idxX, info.idxY);
                    Product pro = CreateNewProduct(frame, info.color);
                    pro.GetComponent<BoxCollider2D>().enabled = false;
                    pro.SetChocoBlock(0);
                    pro.EventUnWrapChoco = () => {
                        Billboard.ChocoCount++;
                        EventBreakTarget?.Invoke(pro.transform.position, StageGoalType.Choco);
                    };
                }
                mNetMessages.RemoveFirst();
            }
            else if (body.cmd == PVPCommand.Click)
            {
                //if(IsIdle)
                //{
                //    Product pro = GetFrame(body.products[0].idxX, body.products[0].idxY).ChildProduct;
                //    OnClick(pro.gameObject);
                //    mNetMessages.RemoveFirst();
                //}
                mNetMessages.RemoveFirst();
            }
            else if (body.cmd == PVPCommand.Swipe)
            {
                Product pro = GetFrame(body.products[0].idxX, body.products[0].idxY).ChildProduct;
                OnSwipe(pro.gameObject, body.dir);
                mNetMessages.RemoveFirst();
            }
            else if (body.cmd == PVPCommand.Destroy)
            {
                List<Product> products = new List<Product>();
                foreach (ProductInfo info in body.products)
                {
                    Product pro = GetFrame(info.idxX, info.idxY).ChildProduct;
                    if(pro != null && !pro.IsLocked() && info.color == pro.mColor)
                        products.Add(pro);
                }

                
                if(products.Count == body.products.Length)
                {
                    Billboard.CurrentCombo = body.combo;
                    DestroyProducts(products);
                    mEmptyFrames.Clear();
                    mNetMessages.RemoveFirst();
                }
            }
            else if (body.cmd == PVPCommand.Create)
            {
                foreach (ProductInfo info in body.products)
                {
                    Frame frame = GetFrame(info.idxX, info.idxY);
                    Product newProduct = CreateNewProduct(frame, info.color);
                    newProduct.GetComponent<BoxCollider2D>().enabled = false;
                    newProduct.mAnimation.Play("swap");
                }
                mNetMessages.RemoveFirst();
            }
            else if (body.cmd == PVPCommand.FlushAttacks)
            {
                AttackPoints.Pop(body.products.Length);
                foreach (ProductInfo info in body.products)
                {
                    Product pro = GetFrame(info.idxX, info.idxY).ChildProduct;
                    pro.SetChocoBlock(1, true);
                }

                mNetMessages.RemoveFirst();
            }
        }
    }


    private void CheckStageFinish()
    {
        bool isSuccess = false;
        int targetCount = mStageInfo.GoalValue;
        int comboTypeCount = mStageInfo.ComboTypeCount();

        switch (mStageInfo.GoalTypeEnum)
        {
            case StageGoalType.Score:
                if (Billboard.CurrentScore >= targetCount * UserSetting.ScorePerBar)
                    isSuccess = true;
                break;
            case StageGoalType.Combo:
                if (Billboard.ComboCounter[comboTypeCount] >= targetCount)
                    isSuccess = true;
                break;
            case StageGoalType.ItemOneMore:
                if (Billboard.ItemOneMoreCount >= targetCount)
                    isSuccess = true;
                break;
            case StageGoalType.ItemKeepCombo:
                if (Billboard.ItemKeepComboCount >= targetCount)
                    isSuccess = true;
                break;
            case StageGoalType.ItemSameColor:
                if (Billboard.ItemSameColorCount >= targetCount)
                    isSuccess = true;
                break;
            case StageGoalType.Cover:
                if (Billboard.CoverCount >= targetCount)
                    isSuccess = true;
                break;
            case StageGoalType.Choco:
                if (Billboard.ChocoCount >= targetCount)
                    isSuccess = true;
                break;
        }

        if (isSuccess)
        {
            EventFinish?.Invoke(true);
            FinishGame();
        }
        else if (Billboard.MoveCount >= mStageInfo.MoveLimit)
        {
            EventFinish?.Invoke(false);
            FinishGame();
        }

        return;
    }
    private void CheckPVPFinish()
    {
        if (Opponent.AttackPoints.Count > 200)
        {
            EventFinish?.Invoke(true);
            FinishGame();
            return;
        }

        Dictionary<ProductColor, int> colorCount = new Dictionary<ProductColor, int>();
        foreach (Frame frame in mFrames)
        {
            Product pro = frame.ChildProduct;
            if (pro == null || pro.IsChocoBlock())
                continue;

            if (colorCount.ContainsKey(pro.mColor))
                colorCount[pro.mColor] += 1;
            else
                colorCount[pro.mColor] = 1;

            if (colorCount[pro.mColor] >= UserSetting.MatchCount)
                return;
        }

        EventFinish?.Invoke(false);
        FinishGame();
        return;
    }



    private void BreakItemSkill(Product product)
    {
        if (product.mSkill == ProductSkill.OneMore)
        {
            Billboard.ItemOneMoreCount++;
            EventBreakTarget?.Invoke(product.transform.position, StageGoalType.ItemOneMore);
        }
        else if (product.mSkill == ProductSkill.KeepCombo)
        {
            Billboard.KeepCombo = Math.Max(product.Combo, Billboard.KeepCombo);
            Billboard.ItemKeepComboCount++;
            EventBreakTarget?.Invoke(product.transform.position, StageGoalType.ItemKeepCombo);
        }
        else if (product.mSkill == ProductSkill.SameColor)
        {
            Billboard.ItemSameColorCount++;
            EventBreakTarget?.Invoke(product.transform.position, StageGoalType.ItemSameColor);
        }
    }
    private List<Product> GetSameColorProducts(ProductColor color)
    {
        List<Product> list = new List<Product>();
        foreach (Frame frame in mFrames)
        {
            if (frame.Empty || frame.ChildProduct == null || frame.ChildProduct.IsLocked() || frame.ChildProduct.IsChocoBlock())
                continue;
            if (frame.ChildProduct.mColor != color)
                continue;
            list.Add(frame.ChildProduct);
        }
        return list;
    }
    private void MakeSkillProduct(int matchedCount)
    {
        if (matchedCount <= UserSetting.MatchCount)
            return;

        ProductSkill skill = ProductSkill.Nothing;
        if (mStageInfo.Items.ContainsKey(matchedCount))
            skill = mStageInfo.Items[matchedCount];
        else if (mStageInfo.Items.ContainsKey(-1))
            skill = mStageInfo.Items[-1];

        if (skill == ProductSkill.Nothing)
            return;

        mNextSkills.Enqueue(skill);
    }
    private bool IsAllIdle()
    {
        foreach(Frame frame in mFrames)
        {
            if (frame.Empty)
                continue;
            if (frame.ChildProduct == null || frame.ChildProduct.IsLocked())
                return false;
        }
        return true;
    }
    private bool IsMatched(List<Product> products)
    {
        List<Product> matches = new List<Product>();
        foreach (Product pro in products)
        {
            matches.Clear();
            if (pro.IsMatchable(matches, pro.mColor))
                return true;
        }
        return false;
    }
    private void SecondaryInitFrames()
    {
        for(int x = 0; x < CountX; ++x)
        {
            SpriteMask mask = null;
            int maskOrder = 0;
            for (int y = 0; y < CountY; ++y)
            {
                Frame curFrame = mFrames[x, y];
                if (curFrame.Empty)
                    continue;

                Frame subTopFrame = SubTopFrame(curFrame);
                if (curFrame.Down() == null)
                {
                    int height = subTopFrame.IndexY - curFrame.IndexY + 1;
                    Vector3 centerPos = (curFrame.transform.position + subTopFrame.transform.position) * 0.5f;
                    mask = CreateMask(centerPos, height, maskOrder);
                    maskOrder++;
                }
                curFrame.SetSubTopFrame(subTopFrame);
                curFrame.SetSpriteMask(mask);

                if (curFrame.Left() == null) curFrame.ShowBorder(0);
                if (curFrame.Right() == null) curFrame.ShowBorder(1);
                if (curFrame.Up() == null) curFrame.ShowBorder(2);
                if (curFrame.Down() == null) curFrame.ShowBorder(3);
            }
        }
    }
    private Frame SubTopFrame(Frame baseFrame)
    {
        Frame curFrame = baseFrame;
        while (true)
        {
            if (curFrame.Up() == null)
                break;
            else
                curFrame = curFrame.Up();
        }
        return curFrame;
    }
    private SpriteMask CreateMask(Vector3 pos, float height, int layerOrder)
    {
        GameObject maskObj = Instantiate(MaskPrefab, transform);
        maskObj.transform.position = pos;
        maskObj.transform.localScale = new Vector3(1, height, 1);

        SpriteMask mask = maskObj.GetComponent<SpriteMask>();
        mask.isCustomRangeActive = true;
        mask.frontSortingOrder = layerOrder + 1;
        mask.backSortingOrder = layerOrder;
        return mask;
    }
    private Product CreateNewProduct(Frame parent, ProductColor color = ProductColor.None)
    {
        int typeIdx = color == ProductColor.None ? RandomNextColor() : (int)color - 1;
        GameObject obj = GameObject.Instantiate(ProductPrefabs[typeIdx], parent.transform, false);
        Product product = obj.GetComponent<Product>();
        product.transform.localPosition = new Vector3(0, 0, -1);
        product.ParentFrame = parent;
        return product;
    }
    private int RandomNextColor()
    {
        int count = (int)(mStageInfo.ColorCount + 0.99f);
        float remain = mStageInfo.ColorCount - (int)mStageInfo.ColorCount;
        int idx = UnityEngine.Random.Range(0, count);
        if (remain > 0 && idx == count - 1)
        {
            if (remain <= UnityEngine.Random.Range(0, 10) * 0.1f)
                idx = UnityEngine.Random.Range(0, count - 1);
        }
        return idx;
    }
    private void ResetGame()
    {
        int cnt = transform.childCount;
        for (int i = 0; i < cnt; ++i)
            Destroy(transform.GetChild(i).gameObject);

        EventBreakTarget = null;
        EventDestroyed = null;
        EventFinish = null;
        EventReduceLimit = null;
        EventEnterIdle = null;

        AttackPoints = null;
        mMoveLock = false;
        IsIdle = true;

        Billboard.Reset();
        mNetMessages.Clear();
        mEmptyFrames.Clear();
        mNextSkills.Clear();

        mFrames = null;
        mUserInfo = null;
        mStageInfo = null;
    }
    private void RemoveLimit()
    {
        Billboard.MoveCount++;
        mMoveLock = Billboard.MoveCount >= mStageInfo.MoveLimit;
        EventReduceLimit?.Invoke();
    }

    private ProductInfo[] Serialize(Product[] pros)
    {
        List<ProductInfo> list = new List<ProductInfo>();
        foreach (Product pro in pros)
            list.Add(new ProductInfo(pro.ParentFrame.IndexX, pro.ParentFrame.IndexY, pro.mColor));
        return list.ToArray();
    }
    private void Network_StartGame(Product[] pros)
    {
        if (FieldType != GameFieldType.pvpPlayer)
            return;

        PVPInfo req = new PVPInfo();
        req.cmd = PVPCommand.StartGame;
        req.oppUserPk = InstPVP_Opponent.UserPk;
        req.XCount = CountX;
        req.YCount = CountY;
        req.combo = pros[0].Combo;
        req.colorCount = mStageInfo.ColorCount;
        req.ArrayCount = pros.Length;
        req.products = Serialize(pros);

        NetClientApp.GetInstance().Request(NetCMD.PVP, req, null);
    }
    private void Network_Click(Product pro)
    {
        if (FieldType != GameFieldType.pvpPlayer)
            return;

        PVPInfo req = new PVPInfo();
        req.cmd = PVPCommand.Click;
        req.oppUserPk = InstPVP_Opponent.UserPk;
        req.products = new ProductInfo[1];
        req.products[0] = new ProductInfo(pro.ParentFrame.IndexX, pro.ParentFrame.IndexY, pro.mColor);
        NetClientApp.GetInstance().Request(NetCMD.PVP, req, null);
    }
    private void Network_Swipe(Product pro, SwipeDirection dir)
    {
        if (FieldType != GameFieldType.pvpPlayer)
            return;

        PVPInfo req = new PVPInfo();
        req.cmd = PVPCommand.Swipe;
        req.oppUserPk = InstPVP_Opponent.UserPk;
        req.dir = dir;
        req.products = new ProductInfo[1];
        req.products[0] = new ProductInfo(pro.ParentFrame.IndexX, pro.ParentFrame.IndexY, pro.mColor);
        NetClientApp.GetInstance().Request(NetCMD.PVP, req, null);
    }
    private void Network_Destroy(Product[] pros)
    {
        if (FieldType != GameFieldType.pvpPlayer)
            return;

        PVPInfo req = new PVPInfo();
        req.cmd = PVPCommand.Destroy;
        req.oppUserPk = InstPVP_Opponent.UserPk;
        req.products = Serialize(pros);
        NetClientApp.GetInstance().Request(NetCMD.PVP, req, null);
    }
    private void Network_Create(Product[] pros)
    {
        if (FieldType != GameFieldType.pvpPlayer)
            return;

        PVPInfo req = new PVPInfo();
        req.cmd = PVPCommand.Create;
        req.oppUserPk = InstPVP_Opponent.UserPk;
        req.products = Serialize(pros);
        NetClientApp.GetInstance().Request(NetCMD.PVP, req, null);
    }
    private void Network_FlushAttacks(Product[] pros)
    {
        if (FieldType != GameFieldType.pvpPlayer)
            return;

        PVPInfo req = new PVPInfo();
        req.cmd = PVPCommand.FlushAttacks;
        req.oppUserPk = InstPVP_Opponent.UserPk;
        req.products = Serialize(pros);
        NetClientApp.GetInstance().Request(NetCMD.PVP, req, null);
    }

}
