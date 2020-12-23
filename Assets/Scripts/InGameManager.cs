using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using SkillPair = System.Tuple<PVPCommand, UnityEngine.Sprite>;

public class VerticalFrames
{
    public Frame[] Frames;
    public LinkedList<Product> NewProducts = new LinkedList<Product>();
}

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

    public GameObject LaserParticle;
    public GameObject BombParticle;
    public GameObject ShieldParticle;
    public GameObject ScoreBuffParticle;
    public GameObject CloudPrefab;
    public GameObject UpsideDownParticle;
    public GameObject RemoveBadEffectParticle;

    public GameObject[] SkillSlots;

    private Frame[,] mFrames = null;
    private StageInfo mStageInfo = null;
    private UserInfo mUserInfo = null;
    private bool mMoveLock = false;
    private bool mIsCycling = false;
    private bool mIsSwipping = false;
    private DateTime mStartTime = DateTime.Now;
    private bool mRemoveBadEffectsCoolTime = false;

    private Queue<ProductSkill> mNextSkills = new Queue<ProductSkill>();
    private LinkedList<PVPInfo> mNetMessages = new LinkedList<PVPInfo>();
    private VerticalFrames[] mFrameDropGroup = null;


    public SkillPair[] SkillMapping = new SkillPair[7]
    {
        new SkillPair(PVPCommand.Undef, null),
        new SkillPair(PVPCommand.Undef, null),
        new SkillPair(PVPCommand.Undef, null),
        new SkillPair(PVPCommand.Undef, null),
        new SkillPair(PVPCommand.Undef, null),
        new SkillPair(PVPCommand.Undef, null),
        new SkillPair(PVPCommand.Undef, null)
    };
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
    public Frame CenterFrame { get { return mFrames[CountX / 2, CountY / 2]; } }
    public GameObject ShieldSlot { get { return SkillSlots[0]; } }
    public GameObject ScoreBuffSlot { get { return SkillSlots[1]; } }
    public GameObject UpsideDownSlot { get { return SkillSlots[2]; } }
    public bool IsIdle { get { return !mIsCycling && !mIsSwipping; } }
    public int CountX { get { return mStageInfo.XCount; } }
    public int CountY { get { return mStageInfo.YCount; } }
    public float ColorCount { get { return mStageInfo.ColorCount; } }
    public int UserPk { get { return mUserInfo.userPk; } }
    public AttackPoints AttackPoints { get; set; }
    public InGameManager Opponent { get { return FieldType == GameFieldType.pvpPlayer ? InstPVP_Opponent : InstPVP_Player; } }
    public InGameBillboard GetBillboard() { return Billboard; }
    public float GridSize { get { return UserSetting.GridSize * transform.localScale.x; } }
    public Rect FieldWorldRect    {
        get {
            Rect rect = new Rect(Vector2.zero, new Vector2(GridSize * CountX, GridSize * CountY));
            rect.center = transform.position;
            return rect;
        }
    }


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

        Network_StartGame(Serialize(initProducts.ToArray()));
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

        SwipeDirection fixedDir = dir;
        if (UpsideDownSlot.activeSelf)
        {
            switch (dir)
            {
                case SwipeDirection.UP: fixedDir = SwipeDirection.DOWN; break;
                case SwipeDirection.DOWN: fixedDir = SwipeDirection.UP; break;
                case SwipeDirection.LEFT: fixedDir = SwipeDirection.RIGHT; break;
                case SwipeDirection.RIGHT: fixedDir = SwipeDirection.LEFT; break;
            }
        }

        Product product = swipeObj.GetComponent<Product>();
        Product targetProduct = null;
        switch (fixedDir)
        {
            case SwipeDirection.UP: targetProduct = product.Up(); break;
            case SwipeDirection.DOWN: targetProduct = product.Down(); break;
            case SwipeDirection.LEFT: targetProduct = product.Left(); break;
            case SwipeDirection.RIGHT: targetProduct = product.Right(); break;
        }

        if (targetProduct != null && !product.IsLocked() && !targetProduct.IsLocked() && !product.IsIced && !targetProduct.IsIced)
        {
            mIsSwipping = true;
            RemoveLimit();
            Network_Swipe(product, dir);
            product.Swipe(targetProduct, () => {
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

        List<Product> skilledProducts = new List<Product>();
        ProductSkill nextSkill = CheckSkillable(firstMatches, skilledProducts);
        Frame[] emptyFrames = DestroyProducts(firstMatches, nextSkill);

        List<Product[]> matchedProductsGroup = new List<Product[]>();
        matchedProductsGroup.Add(firstMatches);
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

            matchedProductsGroup.Clear();
            List<Frame> nextEmptyFrames = new List<Frame>();
            foreach (Product[] matches in nextMatches)
            {
                //Frame[] empties = DestroyProductsWithSkill(matches);
                nextSkill = CheckSkillable(matches, skilledProducts);
                Frame[] empties = DestroyProducts(matches, nextSkill);
                matchedProductsGroup.Add(matches);
                nextEmptyFrames.AddRange(empties);
            }
            emptyFrames = nextEmptyFrames.ToArray();
        }

        CastSkill(matchedProductsGroup);

        if (skilledProducts.Count >= 4)
        {
            yield return new WaitForSeconds(intervalDrop + durationDrop);
            DestroyProducts(skilledProducts.ToArray(), ProductSkill.Nothing);
            yield return new WaitForSeconds(intervalDrop + durationDrop);
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
                    DestroyProducts(pros, ProductSkill.Nothing);
            }
        }
        else
        {
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
                    DestroyProducts(pros, ProductSkill.Nothing);
            }
        }

        EventCombo?.Invoke(0);
        mIsCycling = false;
    }
    private IEnumerator DoDropCycle()
    {
        mIsCycling = true;
        Billboard.CurrentCombo = 1;
        EventCombo?.Invoke(Billboard.CurrentCombo);
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectMatched);

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
                DestroyProducts(pros, ProductSkill.Nothing);
        }

        EventCombo?.Invoke(0);
        mIsCycling = false;
    }
    private Product[] FindSkilledProducts()
    {
        List<Product> pros = new List<Product>();
        foreach(Frame frame in mFrames)
        {
            Product pro = frame.ChildProduct;
            if (pro != null && pro.mSkill != ProductSkill.Nothing)
                pros.Add(pro);
        }
        return pros.ToArray();
    }
    private Product[] ScanProducts(Product skilledProduct)
    {
        List<Product> results = new List<Product>();
        results.Add(skilledProduct);

        if (skilledProduct.mSkill == ProductSkill.OneMore) //hori
        {
            for(int i = 0; i < CountX; ++i)
            {
                Frame frame = GetFrame(i, skilledProduct.ParentFrame.IndexY);
                if (frame == null)
                    continue;
                Product pro = frame.ChildProduct;
                if (pro != null && pro.mSkill == ProductSkill.Nothing && !pro.IsChocoBlock())
                    results.Add(pro);
            }
        }
        else if (skilledProduct.mSkill == ProductSkill.KeepCombo) //vert
        {
            for (int i = 0; i < CountY; ++i)
            {
                Frame frame = GetFrame(skilledProduct.ParentFrame.IndexX, i);
                if (frame == null)
                    continue;
                Product pro = frame.ChildProduct;
                if (pro != null && pro.mSkill == ProductSkill.Nothing && !pro.IsChocoBlock())
                    results.Add(pro);
            }
        }
        else if (skilledProduct.mSkill == ProductSkill.SameColor) //bomb
        {
            int idxX = skilledProduct.ParentFrame.IndexX;
            int idxY = skilledProduct.ParentFrame.IndexY;
            for (int y = idxY - 1; y < idxY + 2; ++y)
            {
                for (int x = idxX - 1; x < idxX + 2; ++x)
                {
                    Frame frame = GetFrame(x, y);
                    if (frame == null)
                        continue;
                    Product pro = frame.ChildProduct;
                    if (pro != null && pro.mSkill == ProductSkill.Nothing && !pro.IsChocoBlock())
                        results.Add(pro);
                }
            }
        }

        return results.ToArray();
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
            {
                if (pro == mainProduct)
                    pro.StartMakeSkill(durationMerge, makeSkill);
                else
                    pro.StartMerge(mainProduct.ParentFrame, durationMerge);
            }
        }

        //if (FieldType == GameFieldType.Stage)
        //    ReduceTargetScoreCombo(mainProduct, Billboard.CurrentScore, Billboard.CurrentScore + addedScore);
        //else
        //    Attack(addedScore, mainProduct.transform.position);

        if (ScoreBuffSlot.activeSelf)
            addedScore = (int)(addedScore * 1.2f);

        Billboard.CurrentScore += addedScore;
        Billboard.DestroyCount += matches.Length;

        Network_Destroy(Serialize(matches), makeSkill);
        EventDestroyed?.Invoke(matches);
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
    public Frame GetFrame(float worldPosX, float worldPosY)
    {
        Rect worldRect = FieldWorldRect;
        if (worldPosX < worldRect.xMin || worldPosY < worldRect.yMin || worldRect.xMax < worldPosX || worldRect.yMax < worldPosY)
            return null;

        float idxX = (worldPosX - worldRect.xMin) / GridSize;
        float idxY = (worldPosY - worldRect.yMin) / GridSize;
        return mFrames[(int)idxX, (int)idxY];
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

        Network_Create(Serialize(newProducts.ToArray()));

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
                Network_FlushAttacks(Serialize(products.ToArray()));
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
        EventRemainTime?.Invoke(mStageInfo.TimeLimit);

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
                    bool isWin = Billboard.CurrentScore > Opponent.Billboard.CurrentScore;
                    EventFinish?.Invoke(isWin);
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
        List<Product> products = new List<Product>();
        for(int y = 0; y < CountY; ++y)
        {
            for (int x = 0; x < CountX; ++x)
            {
                Frame frame = mFrames[x, y];
                if (frame.Empty)
                    continue;
                Product pro = frame.ChildProduct;
                if (pro == null || pro.IsChocoBlock())
                    continue;

                products.Add(pro);
                if (products.Count >= cnt)
                    return products;
            }
        }
        return products;
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
    private ProductSkill CheckSkillable(Product[] matches, List<Product> skilledProducts)
    {
        return ProductSkill.Nothing;
        if (matches.Length <= UserSetting.MatchCount)
            return ProductSkill.Nothing;

        //bool isHori = true;
        //bool isVerti = true;
        //foreach (Product pro in matches)
        //{
        //    if (pro.mSkill != ProductSkill.Nothing)
        //        return ProductSkill.Nothing;
        //
        //    isHori &= (matches[0].ParentFrame.IndexY == pro.ParentFrame.IndexY);
        //    isVerti &= (matches[0].ParentFrame.IndexX == pro.ParentFrame.IndexX);
        //}

        ProductSkill skill = ProductSkill.Nothing;
        int ran = UnityEngine.Random.Range(0, 3);
        if (ran == 0)
            skill = ProductSkill.OneMore;
        else if (ran == 1)
            skill = ProductSkill.KeepCombo;
        else
            skill = ProductSkill.SameColor;

        skilledProducts.Add(matches[0]);
        return skill;
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
        List<VerticalFrames> groups = new List<VerticalFrames>();
        VerticalFrames group = new VerticalFrames();
        for (int x = 0; x < CountX; ++x)
        {
            List<Frame> frames = new List<Frame>();
            for (int y = 0; y < CountY; ++y)
            {
                Frame curFrame = mFrames[x, y];
                if (curFrame.Empty)
                    continue;

                Frame up = curFrame.Up();
                if (up == null || up.Empty)
                {
                    curFrame.VertFrames = group;
                    frames.Add(curFrame);

                    group.Frames = frames.ToArray();
                    groups.Add(group);
                    frames.Clear();
                    group = new VerticalFrames();
                }
                else
                {
                    curFrame.VertFrames = group;
                    frames.Add(curFrame);
                }
            }
        }

        mFrameDropGroup = groups.ToArray();
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

        SkillPair skill = SkillMapping[(int)product.mColor];
        if(skill.Item1 != PVPCommand.Undef)
            product.Renderer.sprite = skill.Item2;

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

        foreach (GameObject skill in SkillSlots)
            skill.SetActive(false);

        EventBreakTarget = null;
        EventDestroyed = null;
        EventFinish = null;
        EventReduceLimit = null;

        AttackPoints = null;
        mMoveLock = false;
        mIsCycling = false;
        mIsSwipping = false;
        mRemoveBadEffectsCoolTime = false;

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
    private Frame[] GetRandomIdleFrames(int count)
    {
        Dictionary<int, Frame> rets = new Dictionary<int, Frame>();
        int totalCount = CountX * CountY;
        int loopCount = 0;
        while(rets.Count < count && loopCount < totalCount)
        {
            loopCount++;
            int ranIdx = UnityEngine.Random.Range(0, totalCount);
            if (rets.ContainsKey(ranIdx))
                continue;

            int idxX = ranIdx % CountX;
            int idxY = ranIdx / CountX;
            Product pro = mFrames[idxX, idxY].ChildProduct;
            if (pro == null || pro.IsLocked())
                continue;

            rets[ranIdx] = pro.ParentFrame;
        }

        return new List<Frame>(rets.Values).ToArray();
    }
    void CastSkill(List<Product[]> nextProducts)
    {
        if (Billboard.CurrentCombo > 3)
            return;

        foreach(Product[] pros in nextProducts)
        {
            SkillPair skillPair = SkillMapping[(int)pros[0].mColor];
            PVPCommand skill = skillPair.Item1;
            if (skill == PVPCommand.Undef)
                continue;

            switch(skill)
            {
                case PVPCommand.SkillBomb: CastSkillBomb(pros); break;
                case PVPCommand.SkillIce: CastSkillice(pros); break;
                case PVPCommand.SkillShield: CastSkillShield(pros); break;
                case PVPCommand.SkillScoreBuff: CastSkillScoreBuff(pros); break;
                case PVPCommand.SkillCloud: CastSkillCloud(pros); break;
                case PVPCommand.SkillUpsideDown: CastSkillUpsideDown(pros); break;
                case PVPCommand.SkillRemoveBadEffects: CastSkillRemoveBadEffects(pros); break;
                default: break;
            }
        }
    }
    void CastSkillBomb(Product[] matches)
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectBadEffect);

        Vector3 startPos = matches[0].transform.position;
        Frame[] targetFrames = Opponent.GetRandomIdleFrames(Billboard.CurrentCombo * 2);
        foreach (Frame frame in targetFrames)
            CreateLaserEffect(startPos, frame.transform.position);

        if (!Opponent.DefenseShield(targetFrames))
        {
            foreach (Frame frame in targetFrames)
                CreateParticle(BombParticle, frame.transform.position);
        }

        Network_Skill(PVPCommand.SkillBomb, Serialize(targetFrames), matches[0].ParentFrame);
    }
    void CastSkillice(Product[] matches)
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectBadEffect);

        Vector3 pos = matches[0].transform.position;
        Frame[] targetFrames = Opponent.GetRandomIdleFrames(Billboard.CurrentCombo * 2);
        foreach (Frame frame in targetFrames)
            CreateLaserEffect(pos, frame.transform.position);

        Opponent.DefenseShield(targetFrames);

        Network_Skill(PVPCommand.SkillIce, Serialize(targetFrames), matches[0].ParentFrame);
    }
    void CastSkillShield(Product[] matches)
    {
        if (ShieldSlot.activeSelf)
            return;

        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectBadEffect);

        Vector3 pos = matches[0].transform.position;
        CreateLaserEffect(pos, ShieldSlot.transform.position);
        CreateParticle(ShieldParticle, pos);
        ShieldSlot.SetActive(true);

        Network_Skill(PVPCommand.SkillShield, Serialize(new List<Product>().ToArray()), matches[0].ParentFrame);
    }
    bool DefenseShield(Frame[] frames)
    {
        if (!ShieldSlot.activeSelf)
            return false;

        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectGoodEffect);

        ShieldSlot.SetActive(false);
        foreach (Frame frame in frames)
            CreateParticle(ShieldParticle, frame.transform.position);

        return true;
    }
    bool DefenseShield(Product[] pros)
    {
        if (!ShieldSlot.activeSelf)
            return false;

        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectGoodEffect);

        ShieldSlot.SetActive(false);
        foreach (Product pro in pros)
            CreateParticle(ShieldParticle, pro.transform.position);

        return true;
    }
    void CastSkillScoreBuff(Product[] matches)
    {
        if (ScoreBuffSlot.activeSelf)
            return;

        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectGoodEffect);

        Vector3 pos = matches[0].transform.position;
        CreateLaserEffect(pos, ScoreBuffSlot.transform.position);
        CreateParticle(ScoreBuffParticle, pos);
        ScoreBuffSlot.SetActive(true);
        StartCoroutine(UnityUtils.CallAfterSeconds(Billboard.CurrentCombo * 4, () =>
        {
            ScoreBuffSlot.SetActive(false);
        }));

        Network_Skill(PVPCommand.SkillScoreBuff, Serialize(new List<Product>().ToArray()), matches[0].ParentFrame);
    }
    void CastSkillChangeProducts(Product[] matches)
    {
        Vector3 pos = matches[0].transform.position;
        Frame[] targetFrames = Opponent.GetRandomIdleFrames(3);
        foreach (Frame frame in targetFrames)
            CreateLaserEffect(pos, frame.transform.position);

        Network_Skill(PVPCommand.SkillChangeProducts, Serialize(targetFrames), matches[0].ParentFrame);
    }
    void CastSkillCloud(Product[] matches)
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectBadEffect);

        Vector3 pos = matches[0].transform.position;
        Frame[] targetFrames = Opponent.GetRandomIdleFrames(3);
        for(int i = 0; i < targetFrames.Length; ++i)
        {
            targetFrames[i] = Opponent.mFrames[0, targetFrames[i].IndexY];
            CreateLaserEffect(pos, targetFrames[i].transform.position);
        }

        if(!Opponent.DefenseShield(targetFrames))
            Opponent.CreateCloud(targetFrames, Billboard.CurrentCombo);

        Network_Skill(PVPCommand.SkillCloud, Serialize(targetFrames), matches[0].ParentFrame);
    }
    void CreateCloud(Frame[] frames, float size)
    {
        foreach(Frame frame in frames)
        {
            Vector3 pos = frame.transform.position;
            pos.z -= 2;
            GameObject cloudObj = Instantiate(CloudPrefab, pos, Quaternion.identity, transform);
            cloudObj.transform.localScale = new Vector3(size, size, 1);
            cloudObj.GetComponent<EffectCloud>().LimitWorldPosX = mFrames[CountX - 1, 0].transform.position.x;
        }
    }
    void CastSkillUpsideDown(Product[] matches)
    {
        if (Opponent.UpsideDownSlot.activeSelf)
        {
            SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectCooltime);
            return;
        }

        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectBadEffect);

        Vector3 pos = matches[0].transform.position;
        Frame destFrame = Opponent.CenterFrame;
        CreateLaserEffect(pos, destFrame.transform.position);
        
        if (!Opponent.DefenseShield(new Frame[1] { destFrame }))
        {
            CreateParticle(UpsideDownParticle, destFrame.transform.position);
            Opponent.UpsideDownSlot.SetActive(true);
            Opponent.StartCoroutine(UnityUtils.CallAfterSeconds(Billboard.CurrentCombo * 3, () =>
            {
                Opponent.UpsideDownSlot.SetActive(false);
            }));
        }

        Network_Skill(PVPCommand.SkillUpsideDown, Serialize(new List<Product>().ToArray()), matches[0].ParentFrame);
    }
    void CastSkillRemoveBadEffects(Product[] matches)
    {
        if (mRemoveBadEffectsCoolTime)
        {
            SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectCooltime);
            return;
        }

        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectGoodEffect);

        Vector3 pos = matches[0].transform.position;
        CreateParticle(RemoveBadEffectParticle, pos);
        RemoveBadEffects(pos);

        mRemoveBadEffectsCoolTime = true;
        StartCoroutine(UnityUtils.CallAfterSeconds(8.0f, () =>
        {
            mRemoveBadEffectsCoolTime = false;
        }));

        Network_Skill(PVPCommand.SkillRemoveBadEffects, Serialize(new List<Product>().ToArray()), matches[0].ParentFrame);
    }
    void RemoveBadEffects(Vector3 startPos)
    {
        foreach (Frame frame in mFrames)
        {
            Product pro = frame.ChildProduct;
            if (pro != null && pro.IsChocoBlock())
            {
                CreateLaserEffect(startPos, pro.ParentFrame.transform.position);
                pro.BreakChocoBlock(100);
            }
        }

        EffectCloud[] clouds = GetComponentsInChildren<EffectCloud>();
        foreach (EffectCloud cloud in clouds)
        {
            CreateLaserEffect(startPos, cloud.transform.position);
            Destroy(cloud.gameObject);
        }

        if(UpsideDownSlot.activeSelf)
        {
            CreateLaserEffect(startPos, UpsideDownSlot.transform.position);
            UpsideDownSlot.SetActive(false);
        }
    }
    void CreateLaserEffect(Vector3 startPos, Vector3 destPos)
    {
        startPos.z -= 1;
        GameObject laserObj = GameObject.Instantiate(LaserParticle, startPos, Quaternion.identity, transform);
        laserObj.GetComponent<EffectLaser>().SetDestination(destPos);
    }
    void CreateParticle(GameObject prefab, Vector3 worldPos)
    {
        worldPos.z -= 1;
        Instantiate(prefab, worldPos, Quaternion.identity, transform);
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
                if(IsAllIdle())
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
            else if (body.cmd == PVPCommand.SkillBomb)
            {
                SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectBadEffect);
                Vector3 startPos = GetFrame(body.idxX, body.idxY).transform.position;
                List<Product> rets = new List<Product>();
                for (int i = 0; i < body.ArrayCount; ++i)
                {
                    ProductInfo info = body.products[i];
                    Frame frame = Opponent.GetFrame(info.idxX, info.idxY);
                    CreateLaserEffect(startPos, frame.transform.position);
                    Product pro = frame.ChildProduct;
                    if (pro != null && !pro.IsLocked())
                        rets.Add(pro);
                }

                if (!Opponent.DefenseShield(rets.ToArray()))
                {
                    foreach(Product pro in rets)
                        CreateParticle(BombParticle, pro.transform.position);

                    Opponent.DestroyProducts(rets.ToArray(), ProductSkill.Nothing);
                    if (!Opponent.mIsCycling)
                        Opponent.StartCoroutine(Opponent.DoDropCycle());
                }

                mNetMessages.RemoveFirst();
            }
            else if (body.cmd == PVPCommand.SkillIce)
            {
                SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectBadEffect);
                Vector3 startPos = GetFrame(body.idxX, body.idxY).transform.position;
                List<Product> rets = new List<Product>();
                for (int i = 0; i < body.ArrayCount; ++i)
                {
                    ProductInfo info = body.products[i];
                    Frame frame = Opponent.GetFrame(info.idxX, info.idxY);
                    CreateLaserEffect(startPos, frame.transform.position);
                    Product pro = frame.ChildProduct;
                    if (pro != null && !pro.IsLocked())
                        rets.Add(pro);
                }

                if (!Opponent.DefenseShield(rets.ToArray()))
                {
                    foreach (Product pro in rets)
                    {
                        pro.SetIce(true);
                        pro.StartCoroutine(UnityUtils.CallAfterSeconds(5.0f, () => {
                            pro.SetIce(false);
                        }));
                    }

                    Network_Skill(PVPCommand.SkillIceRes, Serialize(rets.ToArray()));
                }

                mNetMessages.RemoveFirst();
            }
            else if (body.cmd == PVPCommand.SkillIceRes)
            {
                for (int i = 0; i < body.ArrayCount; ++i)
                {
                    ProductInfo info = body.products[i];
                    Product pro = GetFrame(info.idxX, info.idxY).ChildProduct;
                    if (pro != null && !pro.IsLocked() && info.color == pro.mColor)
                    {
                        pro.SetIce(true);
                        pro.StartCoroutine(UnityUtils.CallAfterSeconds(5.0f, () => {
                            pro.SetIce(false);
                        }));
                    }
                }

                mNetMessages.RemoveFirst();
            }
            else if (body.cmd == PVPCommand.SkillShield)
            {
                Vector3 startPos = GetFrame(body.idxX, body.idxY).transform.position;
                CreateLaserEffect(startPos, ShieldSlot.transform.position);
                CreateParticle(ShieldParticle, startPos);
                ShieldSlot.SetActive(true);

                mNetMessages.RemoveFirst();
            }
            else if (body.cmd == PVPCommand.SkillScoreBuff)
            {
                Vector3 startPos = GetFrame(body.idxX, body.idxY).transform.position;
                CreateLaserEffect(startPos, ScoreBuffSlot.transform.position);
                CreateParticle(ScoreBuffParticle, startPos);
                ScoreBuffSlot.SetActive(true);
                StartCoroutine(UnityUtils.CallAfterSeconds(body.combo * 4, () =>
                {
                    ScoreBuffSlot.SetActive(false);
                }));

                mNetMessages.RemoveFirst();
            }
            else if (body.cmd == PVPCommand.SkillChangeProducts)
            {
                mNetMessages.RemoveFirst();
            }
            else if (body.cmd == PVPCommand.SkillCloud)
            {
                SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectBadEffect);
                Vector3 startPos = GetFrame(body.idxX, body.idxY).transform.position;
                List<Frame> frames = new List<Frame>();
                for (int i = 0; i < body.ArrayCount; ++i)
                {
                    ProductInfo info = body.products[i];
                    Frame frame = Opponent.GetFrame(info.idxX, info.idxY);
                    CreateLaserEffect(startPos, frame.transform.position);
                    frames.Add(frame);
                }

                if (!Opponent.DefenseShield(frames.ToArray()))
                {
                    Opponent.CreateCloud(frames.ToArray(), body.combo);
                }

                mNetMessages.RemoveFirst();
            }
            else if (body.cmd == PVPCommand.SkillUpsideDown)
            {
                SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectBadEffect);
                Vector3 startPos = GetFrame(body.idxX, body.idxY).transform.position;
                Frame destFrame = Opponent.CenterFrame;
                CreateLaserEffect(startPos, destFrame.transform.position);

                if (!Opponent.DefenseShield(new Frame[1] { destFrame }))
                {
                    CreateParticle(UpsideDownParticle, destFrame.transform.position);
                    Opponent.UpsideDownSlot.SetActive(true);
                    Opponent.StartCoroutine(UnityUtils.CallAfterSeconds(body.combo * 4, () =>
                    {
                        Opponent.UpsideDownSlot.SetActive(false);
                    }));
                }

                mNetMessages.RemoveFirst();
            }
            else if (body.cmd == PVPCommand.SkillRemoveBadEffects)
            {
                Vector3 startPos = GetFrame(body.idxX, body.idxY).transform.position;
                CreateParticle(RemoveBadEffectParticle, startPos);
                RemoveBadEffects(startPos);
                mNetMessages.RemoveFirst();
            }
        }
    }
    private ProductInfo[] Serialize(Product[] pros)
    {
        List<ProductInfo> infos = new List<ProductInfo>();
        for (int i = 0; i < pros.Length; ++i)
        {
            ProductInfo info = new ProductInfo();
            info.idxX = pros[i].ParentFrame.IndexX;
            info.idxY = pros[i].ParentFrame.IndexY;
            info.color = pros[i].mColor;
            infos.Add(info);
        }
        return infos.ToArray();
    }
    private ProductInfo[] Serialize(Frame[] frames)
    {
        List<ProductInfo> infos = new List<ProductInfo>();
        for (int i = 0; i < frames.Length; ++i)
        {
            ProductInfo info = new ProductInfo();
            info.idxX = frames[i].IndexX;
            info.idxY = frames[i].IndexY;
            info.color = ProductColor.None;
            infos.Add(info);
        }
        return infos.ToArray();
    }
    private void Network_StartGame(ProductInfo[] pros)
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
        Array.Copy(pros, req.products, pros.Length);

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
    private void Network_Destroy(ProductInfo[] pros, ProductSkill skill)
    {
        if (FieldType != GameFieldType.pvpPlayer)
            return;

        PVPInfo req = new PVPInfo();
        req.cmd = PVPCommand.Destroy;
        req.oppUserPk = InstPVP_Opponent.UserPk;
        req.combo = Billboard.CurrentCombo;
        req.skill = skill;
        req.ArrayCount = pros.Length;
        Array.Copy(pros, req.products, pros.Length);
        NetClientApp.GetInstance().Request(NetCMD.PVP, req, null);
    }
    private void Network_Create(ProductInfo[] pros)
    {
        if (FieldType != GameFieldType.pvpPlayer)
            return;

        PVPInfo req = new PVPInfo();
        req.cmd = PVPCommand.Create;
        req.oppUserPk = InstPVP_Opponent.UserPk;
        req.combo = 0;
        req.ArrayCount = pros.Length;
        Array.Copy(pros, req.products, pros.Length);
        NetClientApp.GetInstance().Request(NetCMD.PVP, req, null);
    }
    private void Network_FlushAttacks(ProductInfo[] pros)
    {
        if (FieldType != GameFieldType.pvpPlayer)
            return;

        PVPInfo req = new PVPInfo();
        req.cmd = PVPCommand.FlushAttacks;
        req.oppUserPk = InstPVP_Opponent.UserPk;
        req.combo = Billboard.CurrentCombo;
        req.ArrayCount = pros.Length;
        Array.Copy(pros, req.products, pros.Length);
        NetClientApp.GetInstance().Request(NetCMD.PVP, req, null);
    }
    private void Network_Skill(PVPCommand skill, ProductInfo[] infos, Frame startFrame = null)
    {
        PVPInfo req = new PVPInfo();
        req.cmd = skill;
        req.oppUserPk = InstPVP_Opponent.UserPk;
        req.ArrayCount = infos.Length;
        req.combo = Billboard.CurrentCombo;
        Array.Copy(infos, req.products, infos.Length);
        req.idxX = startFrame == null ? 0 : startFrame.IndexX;
        req.idxY = startFrame == null ? 0 : startFrame.IndexY;
        NetClientApp.GetInstance().Request(NetCMD.PVP, req, null);
    }
    #endregion
}
