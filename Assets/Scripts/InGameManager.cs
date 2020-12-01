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

    private const float durationDrop = 0.6f;
    private const float intervalMatch = 0.5f;
    private const float intervalDrop = 0.1f;
    private const float durationMerge = intervalMatch + intervalDrop;

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
    private bool mIsCycling = false;
    private bool mIsSwipping = false;
    private DateTime mStartTime = DateTime.Now;

    private Queue<ProductSkill> mNextSkills = new Queue<ProductSkill>();
    private LinkedList<PVPInfo> mNetMessages = new LinkedList<PVPInfo>();
    public InGameBillboard Billboard = new InGameBillboard();
    private List<Frame[]> mFrameDropGroup = new List<Frame[]>();

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
    public bool IsIdle { get { return !mIsCycling && !mIsSwipping; } }
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
    public Action<int> EventCombo;
    public Action<int> EventRemainTime;
    public Action EventReduceLimit;


    public void StartGame(StageInfo info, UserInfo userInfo)
    {
        ResetGame();
        Vector3 pos = transform.position;

        transform.parent.gameObject.SetActive(true);
        gameObject.SetActive(true);
        mStageInfo = info;
        mUserInfo = userInfo;
        mStartTime = DateTime.Now;

        if (FieldType == GameFieldType.Stage)
        {
            GetComponent<SwipeDetector>().EventSwipe = OnSwipe;
            GetComponent<SwipeDetector>().EventClick = OnClick;
            StartCoroutine(CheckFinishGame());
        }
        else if (FieldType == GameFieldType.pvpPlayer)
        {
            GetComponent<SwipeDetector>().EventSwipe = OnSwipe;
            GetComponent<SwipeDetector>().EventClick = OnClick;
            StartCoroutine(CheckFinishGame());
        }
        else if (FieldType == GameFieldType.pvpOpponent)
        {
            transform.localScale = new Vector3(UserSetting.BattleOppResize, UserSetting.BattleOppResize, 1);
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
        InitDropGroupFrames();

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
        ResetGame();
        gameObject.SetActive(false);
        transform.parent.gameObject.SetActive(false);
    }

    public void OnClick(GameObject clickedObj)
    {
        if (!IsIdle || mMoveLock)
            return;

        Product pro = clickedObj.GetComponent<Product>();
        List<Product[]> matches = FindMatchedProducts(new Product[1] { pro });
        if (matches.Count <= 0)
        {
            pro.mAnimation.Play("swap");
            return;
        }

        RemoveLimit();
        StartCoroutine(DoMatchingCycle(matches[0]));
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
            mIsSwipping = true;
            RemoveLimit();
            Network_Swipe(product, dir);
            targetProduct.StartSwipe(product.GetComponentInParent<Frame>(), null);
            product.StartSwipe(targetProduct.GetComponentInParent<Frame>(), () => {
                mIsSwipping = false;
            });
        }
    }

    #region Utility
    private IEnumerator DoMatchingCycle(Product[] firstMatches)
    {
        mIsCycling = true;
        Billboard.CurrentCombo = 1;
        EventCombo?.Invoke(Billboard.CurrentCombo);
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectMatched);

        //Frame[] emptyFrames = DestroyProductsWithSkill(firstMatches);
        Frame[] emptyFrames = DestroyProducts(firstMatches, ProductSkill.Nothing);
        while (true)
        {
            yield return new WaitForSeconds(intervalMatch);
            List<Product> aroundProducts = FindAroundProducts(emptyFrames);
            List<Product[]> nextMatches = FindMatchedProducts(aroundProducts.ToArray());
            if (nextMatches.Count <= 0)
                break;

            Billboard.CurrentCombo++;
            Billboard.MaxCombo = Math.Max(Billboard.CurrentCombo, Billboard.MaxCombo);
            Billboard.ComboCounter[Billboard.CurrentCombo]++;
            EventCombo?.Invoke(Billboard.CurrentCombo);
            SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectMatched);

            List<Frame> nextEmptyFrames = new List<Frame>();
            foreach (Product[] matches in nextMatches)
            {
                //Frame[] empties = DestroyProductsWithSkill(matches);
                Frame[] empties = DestroyProducts(matches, ProductSkill.Nothing);
                nextEmptyFrames.AddRange(empties);
            }
            emptyFrames = nextEmptyFrames.ToArray();
        }

        while (true)
        {
            yield return new WaitForSeconds(intervalDrop);
            Product[] droppedProducts = StartToDropAndCreate(durationDrop);
            yield return new WaitForSeconds(durationDrop);
            List<Product[]> matches = FindMatchedProducts(droppedProducts);
            if (matches.Count <= 0)
                break;

            EventCombo?.Invoke(Billboard.CurrentCombo);
            SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectMatched);

            foreach (Product[] pros in matches)
            {
                //Product[] skilledMatches = ApplySkillProducts(pros);
                //DestroyProducts(skilledMatches, ProductSkill.Nothing);
                DestroyProducts(pros, ProductSkill.Nothing);
            }
        }

        EventCombo?.Invoke(0);
        mIsCycling = false;
    }
    private Frame[] DestroyProducts(Product[] matches, ProductSkill makeSkill)
    {
        List<Frame> emptyFrames = new List<Frame>();
        Product mainProduct = matches[0];

        int addedScore = 0;
        foreach (Product pro in matches)
        {
            emptyFrames.Add(pro.ParentFrame);
            addedScore += Billboard.CurrentCombo;
            pro.Combo = Billboard.CurrentCombo;
            if (makeSkill == ProductSkill.Nothing)
                pro.StartDestroy(gameObject);
            else
                pro.StartMerge(mainProduct.ParentFrame, durationMerge, makeSkill);
        }

        if (FieldType == GameFieldType.Stage)
            ReduceTargetScoreCombo(mainProduct, Billboard.CurrentScore, Billboard.CurrentScore + addedScore);
        else
            Attack(addedScore, mainProduct.transform.position);

        Billboard.CurrentScore += addedScore;
        Billboard.DestroyCount += matches.Length;

        Network_Destroy(matches, makeSkill);
        EventDestroyed?.Invoke(matches);
        return emptyFrames.ToArray();
    }
    private Frame[] DestroyProductsWithSkill(Product[] matches)
    {
        List<Frame> emptyFrames = new List<Frame>();
        Product[] skilledMatches = ApplySkillProducts(matches);
        if (skilledMatches.Length != matches.Length)
        {
            Frame[] empties = DestroyProducts(skilledMatches, ProductSkill.Nothing);
            emptyFrames.AddRange(empties);
        }
        else
        {
            ProductSkill nextSkill = TryMakeSkillProduct(matches);
            Frame[] empties = DestroyProducts(matches, nextSkill);
            emptyFrames.AddRange(empties);
        }
        return emptyFrames.ToArray();
    }
    private Queue<Product> FindAliveProducts(Frame[] subFrames)
    {
        Queue<Product> aliveProducts = new Queue<Product>();
        foreach (Frame frame in subFrames)
        {
            Product pro = frame.ChildProduct;
            if (pro != null)
                aliveProducts.Enqueue(pro);
        }
        return aliveProducts;
    }
    private List<Product[]> FindMatchedProducts(Product[] targetProducts)
    {
        Dictionary<Product, int> matchedPro = new Dictionary<Product, int>();
        List<Product[]> list = new List<Product[]>();
        foreach (Product pro in targetProducts)
        {
            if (matchedPro.ContainsKey(pro))
                continue;

            List<Product> matches = new List<Product>();
            pro.SearchMatchedProducts(matches, pro.mColor);
            if (matches.Count >= UserSetting.MatchCount)
            {
                list.Add(matches.ToArray());
                foreach (Product sub in matches)
                    matchedPro[sub] = 1;
            }
        }
        return list;
    }
    private List<Product> FindAroundProducts(Frame[] emptyFrames)
    {
        Dictionary<Product, int> aroundProducts = new Dictionary<Product, int>();
        foreach (Frame frame in emptyFrames)
        {
            Frame[] aroundFrames = frame.GetAroundFrames();
            foreach (Frame sub in aroundFrames)
            {
                Product pro = sub.ChildProduct;
                if (pro != null && !pro.IsLocked())
                    aroundProducts[pro] = 1;
            }
        }
        return new List<Product>(aroundProducts.Keys);
    }
    private Product[] StartToDropAndCreate(float duration)
    {
        List<Product> droppedProducts = new List<Product>();
        List<Product> newProducts = new List<Product>();
        foreach (Frame[] frames in mFrameDropGroup)
        {
            Queue<Product> alivePros = FindAliveProducts(frames);
            int diffCount = frames.Length - alivePros.Count;
            if (diffCount <= 0)
                continue;

            foreach (Frame frame in frames)
            {
                if (alivePros.Count > 0)
                {
                    Product pro = alivePros.Dequeue();
                    if (frame.ChildProduct != pro)
                    {
                        pro.StartDropAnimate(frame, duration);
                        droppedProducts.Add(pro);
                    }
                }
                else
                {
                    Product pro = CreateNewProduct(frame);
                    float height = UserSetting.GridSize * diffCount;
                    pro.transform.localPosition = new Vector3(0, height, -1);
                    pro.StartDropAnimate(frame, duration);
                    droppedProducts.Add(pro);
                    newProducts.Add(pro);
                }
            }
        }

        Network_Create(newProducts.ToArray());

        return droppedProducts.ToArray();
    }
    private Product[] StartToDropAndCreateRemote(Dictionary<Frame, ProductColor> newProducts, float duration)
    {
        List<Product> droppedProducts = new List<Product>();
        foreach (Frame[] frames in mFrameDropGroup)
        {
            Queue<Product> alivePros = FindAliveProducts(frames);
            int diffCount = frames.Length - alivePros.Count;
            if (diffCount <= 0)
                continue;

            foreach (Frame frame in frames)
            {
                if (alivePros.Count > 0)
                {
                    Product pro = alivePros.Dequeue();
                    if (frame.ChildProduct != pro)
                    {
                        pro.StartDropAnimate(frame, duration);
                        droppedProducts.Add(pro);
                    }
                }
                else
                {
                    if (!newProducts.ContainsKey(frame))
                        LOG.warn("Not Found Remote New Product");

                    ProductColor proColor = newProducts[frame];
                    Product pro = CreateNewProduct(frame, proColor);
                    float height = UserSetting.GridSize * diffCount;
                    pro.transform.localPosition = new Vector3(0, height, -1);
                    pro.GetComponent<BoxCollider2D>().enabled = false;
                    pro.StartDropAnimate(frame, duration);
                    droppedProducts.Add(pro);
                }
            }
        }

        return droppedProducts.ToArray();
    }
    private bool IsReadyToNextDrop(Dictionary<Frame, ProductColor> newProducts)
    {
        int validCount = 0;
        foreach (Frame[] frames in mFrameDropGroup)
        {
            Queue<Product> alivePros = FindAliveProducts(frames);
            int diffCount = frames.Length - alivePros.Count;
            if (diffCount <= 0)
                continue;

            int x = frames[0].IndexX;
            for(int y = 0; y < diffCount; ++y)
            {
                Frame frame = frames[frames.Length - 1 - y];
                if (newProducts.ContainsKey(frame))
                    validCount++;
                else
                    return false;
            }
        }
        return validCount == newProducts.Count;
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
        if(FieldType == GameFieldType.pvpPlayer)
        {
            StopCoroutine("FlushAttacks");
            StartCoroutine("FlushAttacks");
        }
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
    private IEnumerator CheckFinishGame()
    {
        int preRemainSec = 0;
        while(true)
        {
            yield return null;

            if (mStageInfo.TimeLimit > 0)
            {
                int currentPlaySec = (int)new TimeSpan((DateTime.Now - mStartTime).Ticks).TotalSeconds;
                int remainSec = mStageInfo.TimeLimit - currentPlaySec;
                if (remainSec != preRemainSec && remainSec >= 0)
                {
                    preRemainSec = remainSec;
                    EventRemainTime?.Invoke(remainSec);
                }

                if(remainSec < 0 && IsIdle)
                {
                    EventFinish?.Invoke(false);
                    FinishGame();
                    break;
                }
            }
            else if(mStageInfo.MoveLimit > 0)
            {
                if (Billboard.MoveCount >= mStageInfo.MoveLimit && IsIdle)
                {
                    EventFinish?.Invoke(false);
                    FinishGame();
                    break;
                }
            }

            if (FieldType == GameFieldType.Stage)
                CheckStageFinish();
            else if (FieldType == GameFieldType.pvpPlayer)
                CheckPVPFinish();
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

        int counter = 0;
        foreach (Frame frame in mFrames)
        {
            if (frame.Empty)
                continue;

            counter++;
            Product pro = frame.ChildProduct;
            if (pro != null && pro.IsChocoBlock())
                counter--;
        }

        if(counter == 0)
        {
            EventFinish?.Invoke(false);
            FinishGame();
        }
        
        return;
    }
    private Product[] ApplySkillProducts(Product[] matches)
    {
        List<Product> result = new List<Product>();
        result.AddRange(matches);
        foreach (Product pro in matches)
        {
            if (pro.mSkill == ProductSkill.Nothing)
                continue;
            else if (pro.mSkill == ProductSkill.OneMore)
                FindSameRowProducts(pro, result);
            else if (pro.mSkill == ProductSkill.KeepCombo)
                FindSameColumnProducts(pro, result);
            else if (pro.mSkill == ProductSkill.SameColor)
                FindSameColorProducts(pro, result);
        }
        return result.ToArray();
    }
    private void FindSameRowProducts(Product target, List<Product> result)
    {
        int idxY = target.ParentFrame.IndexY;
        for(int x = 0; x < CountX; ++x)
        {
            Product pro = GetFrame(x, idxY).ChildProduct;
            if (pro != null && !pro.IsLocked() && !result.Contains(pro))
                result.Add(pro);
        }
    }
    private void FindSameColumnProducts(Product target, List<Product> result)
    {
        int idxX = target.ParentFrame.IndexX;
        for (int y = 0; y < CountY; ++y)
        {
            Product pro = GetFrame(idxX, y).ChildProduct;
            if (pro != null && !pro.IsLocked() && !result.Contains(pro))
                result.Add(pro);
        }
    }
    private void FindSameColorProducts(Product target, List<Product> result)
    {
        ProductColor color = target.mColor;
        foreach (Frame frame in mFrames)
        {
            Product pro = frame.ChildProduct;
            if (pro != null && pro.mColor == color && !pro.IsLocked() && !result.Contains(pro))
                result.Add(pro);
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
    private ProductSkill TryMakeSkillProduct(Product[] matches)
    {
        bool isHori = true;
        bool isVerti = true;
        foreach (Product pro in matches)
        {
            if (pro.mSkill != ProductSkill.Nothing)
                return ProductSkill.Nothing;

            isHori &= (matches[0].ParentFrame.IndexY == pro.ParentFrame.IndexY);
            isVerti &= (matches[0].ParentFrame.IndexX == pro.ParentFrame.IndexX);
        }

        if (matches.Length == 4 && isHori)
            return ProductSkill.OneMore;
        else if (matches.Length == 4 && isVerti)
            return ProductSkill.KeepCombo;
        else if (matches.Length == 5)
            return ProductSkill.SameColor;

        return ProductSkill.Nothing;
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
    private void InitDropGroupFrames()
    {
        List<Frame> frames = new List<Frame>();
        for (int x = 0; x < CountX; ++x)
        {
            for (int y = 0; y < CountY; ++y)
            {
                Frame curFrame = mFrames[x, y];
                if (curFrame.Empty)
                    continue;

                Frame up = curFrame.Up();
                if (up == null || up.Empty)
                {
                    frames.Add(curFrame);
                    mFrameDropGroup.Add(frames.ToArray());
                    frames.Clear();
                }
                else
                {
                    frames.Add(curFrame);
                }
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

        AttackPoints = null;
        mMoveLock = false;
        mIsCycling = false;
        mIsSwipping = false;

        Billboard.Reset();
        mNetMessages.Clear();
        mFrameDropGroup.Clear();
        mNextSkills.Clear();

        mFrames = null;
        mUserInfo = null;
        mStageInfo = null;

        StopAllCoroutines();
    }
    private void RemoveLimit()
    {
        if(mStageInfo.MoveLimit > 0)
        {
            Billboard.MoveCount++;
            mMoveLock = Billboard.MoveCount >= mStageInfo.MoveLimit;
            EventReduceLimit?.Invoke();
        }
    }
    public int NextMatchCount(Product pro, SwipeDirection dir)
    {
        Product target = pro.Dir(dir);
        if (target == null || target.mColor == pro.mColor)
            return 0;

        List<Product> matches = new List<Product>();
        Product[] pros = target.GetAroundProducts();
        foreach(Product each in pros)
        {
            if (each == pro)
                continue;

            each.SearchMatchedProducts(matches, pro.mColor);
        }
        return matches.Count;
    }
    #endregion

    #region Network
    public void HandlerNetworkMessage(Header head, byte[] body)
    {
        if (!gameObject.activeInHierarchy)
            return;
        if (head.Ack == 1)
            return;
        if (head.Cmd != NetCMD.PVP)
            return;

        PVPInfo resMsg = Utils.Deserialize<PVPInfo>(ref body);
        if (resMsg.cmd == PVPCommand.EndGame)
        {
            mNetMessages.AddFirst(resMsg);
        }
        else
        {
            mNetMessages.AddLast(resMsg);
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
            else if (body.cmd == PVPCommand.StartGame)
            {
                for (int i = 0; i < body.ArrayCount; ++i)
                {
                    ProductInfo info = body.products[i];
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
                for (int i = 0; i < body.ArrayCount; ++i)
                {
                    ProductInfo info = body.products[i];
                    Product pro = GetFrame(info.idxX, info.idxY).ChildProduct;
                    if (pro != null && !pro.IsLocked() && info.color == pro.mColor)
                        products.Add(pro);
                }

                if (products.Count != body.ArrayCount)
                    LOG.warn("Not Sync Destroy Products");
                else
                {
                    Billboard.CurrentCombo = body.combo;
                    DestroyProducts(products.ToArray(), body.skill);
                    mNetMessages.RemoveFirst();
                }
            }
            else if (body.cmd == PVPCommand.Create)
            {
                Dictionary<Frame, ProductColor> newProducts = new Dictionary<Frame, ProductColor>();
                for (int i = 0; i < body.ArrayCount; ++i)
                {
                    ProductInfo info = body.products[i];
                    Frame frame = GetFrame(info.idxX, info.idxY);
                    newProducts[frame] = info.color;
                }

                if(IsReadyToNextDrop(newProducts))
                {
                    StartToDropAndCreateRemote(newProducts, durationDrop);
                    mNetMessages.RemoveFirst();
                }
            }
            else if (body.cmd == PVPCommand.FlushAttacks)
            {
                AttackPoints.Pop(body.ArrayCount);
                for (int i = 0; i < body.ArrayCount; ++i)
                {
                    ProductInfo info = body.products[i];
                    Product pro = GetFrame(info.idxX, info.idxY).ChildProduct;
                    pro.SetChocoBlock(1, true);
                }

                mNetMessages.RemoveFirst();
            }
        }
    }
    private ProductInfo[] Serialize(Product[] pros)
    {
        ProductInfo[] infos = new ProductInfo[100];
        for (int i = 0; i < pros.Length; ++i)
        {
            infos[i].idxX = pros[i].ParentFrame.IndexX;
            infos[i].idxY = pros[i].ParentFrame.IndexY;
            infos[i].color = pros[i].mColor;
        }
        return infos;
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
        req.colorCount = mStageInfo.ColorCount;
        req.combo = 0;
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
        req.combo = pro.Combo;
        req.ArrayCount = 1;
        req.products[0].idxX = pro.ParentFrame.IndexX;
        req.products[0].idxY = pro.ParentFrame.IndexY;
        req.products[0].color = pro.mColor;
        NetClientApp.GetInstance().Request(NetCMD.PVP, req, null);
    }
    private void Network_Swipe(Product pro, SwipeDirection dir)
    {
        if (FieldType != GameFieldType.pvpPlayer)
            return;

        PVPInfo req = new PVPInfo();
        req.cmd = PVPCommand.Swipe;
        req.oppUserPk = InstPVP_Opponent.UserPk;
        req.combo = pro.Combo;
        req.ArrayCount = 1;
        req.dir = dir;
        req.products[0].idxX = pro.ParentFrame.IndexX;
        req.products[0].idxY = pro.ParentFrame.IndexY;
        req.products[0].color = pro.mColor;
        NetClientApp.GetInstance().Request(NetCMD.PVP, req, null);
    }
    private void Network_Destroy(Product[] pros, ProductSkill skill)
    {
        if (FieldType != GameFieldType.pvpPlayer)
            return;

        PVPInfo req = new PVPInfo();
        req.cmd = PVPCommand.Destroy;
        req.oppUserPk = InstPVP_Opponent.UserPk;
        req.combo = pros[0].Combo;
        req.skill = skill;
        req.ArrayCount = pros.Length;
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
        req.combo = 0;
        req.ArrayCount = pros.Length;
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
        req.combo = pros[0].Combo;
        req.ArrayCount = pros.Length;
        req.products = Serialize(pros);
        NetClientApp.GetInstance().Request(NetCMD.PVP, req, null);
    }
    #endregion
}
