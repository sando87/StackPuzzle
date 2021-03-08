﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using SkillPair = System.Tuple<PVPCommand, UnityEngine.Sprite>;

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
    public static InGameManager InstCurrent
    { get { if (mInstStage != null && mInstStage.gameObject.activeSelf) return mInstStage; else return mInstPVP_Player; } }

    private const float durationDrop = 0.6f;
    private const float intervalMatch = 0.5f;
    private const float intervalDrop = 0.1f;
    private const float durationMerge = intervalMatch + intervalDrop;
    private const string vgName = "VerticalGroup";

    public Sprite[] BackgroundImages;
    public SpriteRenderer BackgroundSprite;
    public GameObject[] ProductPrefabs;
    public GameObject FramePrefab1;
    public GameObject FramePrefab2;
    public GameObject MaskPrefab;
    public GameObject ComboNumPrefab;
    public GameObject AttackPointPrefab;
    public GameObject ObstaclePrefab;
    public GameObject GroundPrefab;

    public GameObject ExplosionParticle;
    public GameObject MergeParticle;
    public GameObject StripeParticle;
    public GameObject SparkParticle;
    public GameObject LaserParticle;
    public GameObject BombParticle;
    public GameObject ShieldParticle;
    public GameObject ScoreBuffParticle;
    public GameObject CloudPrefab;
    public GameObject UpsideDownParticle;
    public GameObject RemoveBadEffectParticle;
    public AttackPoints AttackPointFrame;
    public GameObject AttackBullet;

    public GameObject[] SkillSlots;

    private Frame[,] mFrames = null;
    private StageInfo mStageInfo = null;
    private UserInfo mUserInfo = null;
    private bool mIsFinished = false;
    private bool mIsAutoMatching = false;
    private bool mStopDropping = false;
    private bool mItemLooping = false;
    private bool mIsDropping = false;
    private bool mIsUserEventLock = false;
    private bool mIsFlushing = false;
    private bool mPrevIdleState = false;
    private bool mRequestDrop = false;
    private System.Random mRandomSeed = null;
    private VerticalFrames[] mVerticalFrames = null;

    private LinkedList<PVPInfo> mNetMessages = new LinkedList<PVPInfo>();


    public Dictionary<int, Product> ProductIDs = new Dictionary<int, Product>();
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
    public bool IsValidIndex(int idxX, int idxY)
    {
        if (idxX < 0 || idxX >= mStageInfo.XCount || idxY < 0 || idxY >= mStageInfo.YCount)
            return false;
        return true;
    }
    public Frame Frame(int x, int y) { return mFrames[x, y]; }
    public Frame CenterFrame { get { return mFrames[CountX / 2, CountY / 2]; } }
    public GameObject ShieldSlot { get { return SkillSlots[0]; } }
    public GameObject ScoreBuffSlot { get { return SkillSlots[1]; } }
    public GameObject UpsideDownSlot { get { return SkillSlots[2]; } }
    public bool IsIdle { get { return !mStopDropping && !mIsDropping && !mIsUserEventLock && !mIsFlushing && !mItemLooping && !mIsAutoMatching; } }
    public int CountX { get { return mStageInfo.XCount; } }
    public int CountY { get { return mStageInfo.YCount; } }
    public int StageNum { get { return mStageInfo.Num; } }
    public float ColorCount { get { return mStageInfo.ColorCount; } }
    public int UserPk { get { return mUserInfo.userPk; } }
    public int UserScore { get { return mUserInfo.score; } }
    public UserInfo UserInfo { get { return mUserInfo; } }
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
    public Action<Product[]> EventMatched;
    public Action<bool> EventFinish;
    public Action<int> EventCombo;
    public Action<int> EventRemainTime;
    public Action EventReduceLimit;
    public Action EventEnterIdle;

    private void Update()
    {
        if (IsIdle && !mPrevIdleState)
            EventEnterIdle?.Invoke();

        mPrevIdleState = IsIdle;
        DropNextProducts();
    }
    public void StartGameInStageMode(StageInfo info, UserInfo userInfo)
    {
        MenuInformBox.PopUp("START!!");
        StartGame(info, userInfo);

        mIsUserEventLock = true;
        StartCoroutine(UnityUtils.CallAfterSeconds(UserSetting.InfoBoxDisplayTime, () =>
        {
            GetComponent<SwipeDetector>().EventSwipe = OnSwipe;
            GetComponent<SwipeDetector>().EventClick = OnClick;
            StartCoroutine(CheckFinishStageMode());
            mIsUserEventLock = false;
        }));
    }
    public void StartGameInPVPPlayer(StageInfo info, UserInfo userInfo)
    {
        MenuInformBox.PopUp("START!!");
        StartGame(info, userInfo);

        mIsUserEventLock = true;
        StartCoroutine(UnityUtils.CallAfterSeconds(UserSetting.InfoBoxDisplayTime, () =>
        {
            GetComponent<SwipeDetector>().EventSwipe = OnSwipe;
            GetComponent<SwipeDetector>().EventClick = OnClick;
            StartCoroutine(CheckFlush());
            mIsUserEventLock = false;
        }));
    }
    public void StartGameInPVPOpponent(StageInfo info, UserInfo userInfo)
    {
        StartGame(info, userInfo);

        transform.localScale = new Vector3(UserSetting.BattleOppResize, UserSetting.BattleOppResize, 1);
        StartCoroutine(ProcessNetMessages());
    }
    public void StartGame(StageInfo info, UserInfo userInfo)
    {
        ResetGame();
        Vector3 pos = transform.position;

        transform.parent.gameObject.SetActive(true);
        gameObject.SetActive(true);
        mStageInfo = info;
        mUserInfo = userInfo;
        mRandomSeed = new System.Random(info.RandomSeed == -1 ? (int)DateTime.Now.Ticks : info.RandomSeed);

        int stageCountPerTheme = 20;
        int themeCount = 9;
        int backImgIdx = (mStageInfo.Num % (stageCountPerTheme * themeCount)) / stageCountPerTheme;
        backImgIdx = Math.Min(backImgIdx, BackgroundImages.Length - 1);
        BackgroundSprite.sprite = BackgroundImages[backImgIdx];
        float scale = Camera.main.orthographicSize / 10.24f; //10.24f는 배경 이미지 height의 절반
        BackgroundSprite.transform.localScale = new Vector3(scale, scale, 1);

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

        AttackPointFrame.ResetPoints();
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
    public void CleanUpGame()
    {
        ResetGame();
        gameObject.SetActive(false);
        transform.parent.gameObject.SetActive(false);
    }

    public void OnClick(GameObject clickedObj)
    {
        if (!IsIdle || mIsFinished || !IsAllProductIdle())
            return;

        Product pro = clickedObj.GetComponent<Product>();
        if(pro.Skill != ProductSkill.Nothing)
        {
            pro.Animation.Play("swap");

            //CreateSparkEffect(pro.transform.position);

            DestroySkill(pro);
        }
        else
        {
            List<Product[]> matches = FindMatchedProducts(new Product[1] { pro });
            if (matches.Count <= 0)
            {
                pro.Animation.Play("swap");
            }
            else
            {
                StartCoroutine(DoMatchingCycle(matches[0]));
                RemoveLimit();
            }
        }
    }
    public void OnSwipe(GameObject swipeObj, SwipeDirection dir)
    {
        if (!IsIdle || mIsFinished || !IsAllProductIdle())
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
        if (product.IsLocked || product.IsChocoBlock)
            return;

        Product targetProduct = null;
        switch (fixedDir)
        {
            case SwipeDirection.UP: targetProduct = product.Up(); break;
            case SwipeDirection.DOWN: targetProduct = product.Down(); break;
            case SwipeDirection.LEFT: targetProduct = product.Left(); break;
            case SwipeDirection.RIGHT: targetProduct = product.Right(); break;
        }

        if (targetProduct == null || targetProduct.IsLocked || targetProduct.IsChocoBlock)
            return;

        if (product.Skill != ProductSkill.Nothing && targetProduct.Skill != ProductSkill.Nothing)
        {
            //mItemLooping = true;
            //CreateMergeEffect(product, targetProduct);
            //product.SkillMerge(targetProduct, () => {
            //    mItemLooping = false;
            //    SwipeSkilledProducts(product, targetProduct);
            //});


            mIsUserEventLock = true;
            Network_Swipe(product, dir);
            //CreateMergeEffect(product, targetProduct);
            product.Swipe(targetProduct, () => {
                mIsUserEventLock = false;
                SwipeSkilledProducts(product, targetProduct);
            });
        }
        else
        {
            mIsUserEventLock = true;
            Network_Swipe(product, dir);
            product.Swipe(targetProduct, () => {
                mIsUserEventLock = false;
            });
        }

        SoundPlayer.Inst.PlaySoundEffect(ClipSound.Swipe);
        RemoveLimit();
    }

    #region Utility
    private IEnumerator DoMatchingCycle(Product[] firstMatches)
    {
        mStopDropping = true;
        Billboard.CurrentCombo = 1;
        EventCombo?.Invoke(Billboard.CurrentCombo);

        List<Frame> nextScanFrames = new List<Frame>();
        nextScanFrames.AddRange(ToFrames(firstMatches));
        ProductSkill nextSkill = CheckSkillable(firstMatches);
        if (nextSkill == ProductSkill.Nothing)
            DestroyProducts(firstMatches);
        else
            MergeProducts(firstMatches, nextSkill);

        while (true)
        {
            List<Product> aroundProducts = FindAroundProducts(nextScanFrames.ToArray());
            List<Product[]> nextMatches = FindMatchedProducts(aroundProducts.ToArray());
            if (nextMatches.Count <= 0)
                break;

            yield return new WaitForSeconds(intervalMatch);

            Billboard.CurrentCombo++;
            Billboard.MaxCombo = Math.Max(Billboard.CurrentCombo, Billboard.MaxCombo);
            Billboard.ComboCounter[Billboard.CurrentCombo]++;
            EventCombo?.Invoke(Billboard.CurrentCombo);
            SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectMatched);

            nextScanFrames.Clear();
            foreach (Product[] matches in nextMatches)
            {
                nextScanFrames.AddRange(ToFrames(matches));
                nextSkill = CheckSkillable(matches);
                if (nextSkill == ProductSkill.Nothing)
                    DestroyProducts(matches);
                else
                    MergeProducts(matches, nextSkill);
            }
        }

        yield return new WaitForSeconds(0.2f);

        EventCombo?.Invoke(0);
        mStopDropping = false;
    }
    private Product[] ToProducts(Frame[] frames)
    {
        List<Product> products = new List<Product>();
        foreach (Frame frame in frames)
            if(frame.ChildProduct != null)
                products.Add(frame.ChildProduct);
        return products.ToArray();
    }
    private Frame[] ToFrames(Product[] products)
    {
        List<Frame> frames = new List<Frame>();
        foreach (Product pro in products)
            frames.Add(pro.ParentFrame);
        return frames.ToArray();
    }
    IEnumerator DestroyProductDelay(Product[] destroyedProducts, float delay, bool withLaserEffect)
    {
        yield return new WaitForSeconds(delay);

        List<ProductInfo> nextProducts = new List<ProductInfo>();
        foreach (Product pro in destroyedProducts)
        {
            Frame parentFrame = pro.ParentFrame;
            pro.DestroyImmediately();
            Product newPro = CreateNewProduct();
            parentFrame.VertFrames.AddNewProduct(newPro);
            nextProducts.Add(new ProductInfo(pro.Color, newPro.Color, ProductSkill.Nothing, parentFrame.IndexX, parentFrame.IndexY, pro.InstanceID, newPro.InstanceID));
        }
        mRequestDrop = true;
        Network_Destroy(nextProducts.ToArray(), ProductSkill.Nothing, withLaserEffect);
    }
    private Product[] DestroyProducts(Product[] matches, bool withLaserEffect = false)
    {
        if (matches == null || matches.Length <= 0)
            return matches;

        List<Product> rets = new List<Product>();

        int addedScore = 0;
        foreach (Product pro in matches)
        {
            if (pro.ReadyForDestroy(Billboard.CurrentCombo))
            {
                addedScore += Billboard.CurrentCombo;
                rets.Add(pro);
            }
        }

        Product[] validProducts = rets.ToArray();
        if (validProducts.Length <= 0)
            return validProducts;

        StartCoroutine(DestroyProductDelay(validProducts, 0.2f, withLaserEffect));

        Attack(addedScore, validProducts[0].transform.position);
        //if (FieldType == GameFieldType.Stage)
        //    ReduceTargetScoreCombo(mainProduct, Billboard.CurrentScore, Billboard.CurrentScore + addedScore);
        //else
        //    Attack(addedScore, mainProduct.transform.position);

        Billboard.CurrentScore += addedScore;
        Billboard.DestroyCount += validProducts.Length;

        SoundPlayer.Inst.PlaySoundEffect(ClipSound.Match);
        EventMatched?.Invoke(validProducts);
        return validProducts;
    }
    IEnumerator MergeProductDelay(Product[] mergeProducts, float delay, ProductSkill skill)
    {
        yield return new WaitForSeconds(delay);

        List<ProductInfo> nextProducts = new List<ProductInfo>();
        foreach (Product pro in mergeProducts)
        {
            Frame parentFrame = pro.ParentFrame;
            pro.MergeImImmediately(mergeProducts[0], skill);

            if(pro == mergeProducts[0])
            {
                nextProducts.Add(new ProductInfo(pro.Color, pro.Color, ProductSkill.Nothing, parentFrame.IndexX, parentFrame.IndexY, pro.InstanceID, pro.InstanceID));
            }
            else
            {
                Product newPro = CreateNewProduct();
                parentFrame.VertFrames.AddNewProduct(newPro);
                nextProducts.Add(new ProductInfo(pro.Color, newPro.Color, ProductSkill.Nothing, parentFrame.IndexX, parentFrame.IndexY, pro.InstanceID, newPro.InstanceID));
            }
        }

        mRequestDrop = true;
        Network_Destroy(nextProducts.ToArray(), skill, false);

        if (skill == ProductSkill.SameColor)
            SoundPlayer.Inst.PlaySoundEffect(ClipSound.Merge3);
        else if (skill == ProductSkill.Bomb)
            SoundPlayer.Inst.PlaySoundEffect(ClipSound.Merge2);
        else
            SoundPlayer.Inst.PlaySoundEffect(ClipSound.Merge1);

    }
    private void MergeProducts(Product[] matches, ProductSkill makeSkill)
    {
        Frame mainFrame = matches[0].ParentFrame;

        int addedScore = 0;
        foreach (Product pro in matches)
        {
            Frame curFrame = pro.ParentFrame;
            pro.ReadyForMerge(Billboard.CurrentCombo);
            addedScore += Billboard.CurrentCombo;
        }

        StartCoroutine(MergeProductDelay(matches, 0.2f, makeSkill));

        Attack(addedScore, matches[0].transform.position);
        //if (FieldType == GameFieldType.Stage)
        //    ReduceTargetScoreCombo(mainProduct, Billboard.CurrentScore, Billboard.CurrentScore + addedScore);
        //else
        //    Attack(addedScore, mainProduct.transform.position);

        Billboard.CurrentScore += addedScore;
        Billboard.DestroyCount += matches.Length;

        SoundPlayer.Inst.PlaySoundEffect(ClipSound.Match);
        EventMatched?.Invoke(matches);
    }
    private void DropNextProducts()
    {
        if (mStopDropping)
            return;

        if(mRequestDrop)
        {
            mRequestDrop = false;
            StartToDropProducts();

            if (!mIsAutoMatching && FieldType != GameFieldType.pvpOpponent)
                StartCoroutine(StartAutoMatchFlow());
        }

        mIsDropping = CountDroppingProducts() > 0;
    }
    private int StartToDropProducts()
    {
        List<ProductInfo> droppedPros = new List<ProductInfo>();
        foreach(VerticalFrames group in mVerticalFrames)
        {
            Product[] prosA = group.StartToDropFloatingProducts();
            foreach(Product pro in prosA)
                droppedPros.Add(new ProductInfo(pro.Color, pro.Color, ProductSkill.Nothing, 0, 0, pro.InstanceID, pro.InstanceID));

            Product[] prosB = group.StartToDropNewProducts();
            foreach (Product pro in prosB)
                droppedPros.Add(new ProductInfo(pro.Color, pro.Color, ProductSkill.Nothing, 0, 0, pro.InstanceID, pro.InstanceID));
        }

        //Network_Drop(droppedPros.ToArray());

        return droppedPros.Count;
    }
    private void DropProductsManually(Product[] pros)
    {
        foreach(Product pro in pros)
        {
            if (pro.ParentFrame != null)
                pro.Drop();
            else
            {
                VerticalFrames group = pro.VertFrames;
                group.AddNDropNewProduct(pro);
            }
        }
    }
    private int CountDroppingProducts()
    {
        int count = 0;
        foreach (VerticalFrames group in mVerticalFrames)
            count += group.Droppingcount;

        return count;
    }
    private Product[] ScanHorizenProducts(Product target, int range = 0)
    {
        if (target == null)
            return null;

        List<Product> rets = new List<Product>();
        Frame frameOf = target.ParentFrame != null ? target.ParentFrame : FrameOfWorldPos(target.transform.position.x, target.transform.position.y);
        int idxY = frameOf.IndexY;
        for (int x = 0; x < CountX; ++x)
        {
            Product pro = mFrames[x, idxY].ChildProduct;
            if (pro != null && !pro.IsLocked && !pro.IsChocoBlock)
                rets.Add(pro);
        }

        if (range >= 1 && frameOf.IndexY + 1 < CountY)
        {
            int idxYUp = frameOf.IndexY + 1;
            for (int x = 0; x < CountX; ++x)
            {
                Product pro = mFrames[x, idxYUp].ChildProduct;
                if (pro != null && !pro.IsLocked && !pro.IsChocoBlock)
                    rets.Add(pro);
            }
        }

        if (range >= 2 && frameOf.IndexY - 1 >= 0)
        {
            int idxYDown = frameOf.IndexY - 1;
            for (int x = 0; x < CountX; ++x)
            {
                Product pro = mFrames[x, idxYDown].ChildProduct;
                if (pro != null && !pro.IsLocked && !pro.IsChocoBlock)
                    rets.Add(pro);
            }
        }

        return rets.ToArray();
    }
    private Product[] ScanVerticalProducts(Product target, int range = 0)
    {
        if (target == null)
            return null;

        List<Product> rets = new List<Product>();
        Frame frameOf = target.ParentFrame != null ? target.ParentFrame : FrameOfWorldPos(target.transform.position.x, target.transform.position.y);
        int idxX = frameOf.IndexX;
        for (int y = 0; y < CountY; ++y)
        {
            Product pro = mFrames[idxX, y].ChildProduct;
            if (pro != null && !pro.IsLocked && !pro.IsChocoBlock)
                rets.Add(pro);
        }

        if (range >= 1 && frameOf.IndexX + 1 < CountX)
        {
            int idxXRight = frameOf.IndexX + 1;
            for (int y = 0; y < CountY; ++y)
            {
                Product pro = mFrames[idxXRight, y].ChildProduct;
                if (pro != null && !pro.IsLocked && !pro.IsChocoBlock)
                    rets.Add(pro);
            }
        }

        if (range >= 2 && frameOf.IndexX - 1 >= 0)
        {
            int idxXLeft = frameOf.IndexX - 1;
            for (int y = 0; y < CountY; ++y)
            {
                Product pro = mFrames[idxXLeft, y].ChildProduct;
                if (pro != null && !pro.IsLocked && !pro.IsChocoBlock)
                    rets.Add(pro);
            }
        }

        return rets.ToArray();
    }
    private Product[] ScanAroundProducts(Product target, int round)
    {
        if (target == null)
            return null;

        List<Product> rets = new List<Product>();
        Frame frameOf = target.ParentFrame != null ? target.ParentFrame : FrameOfWorldPos(target.transform.position.x, target.transform.position.y);
        int idxX = frameOf.IndexX;
        int idxY = frameOf.IndexY;
        for (int y = idxY - round; y < idxY + round + 1; ++y)
        {
            for (int x = idxX - round; x < idxX + round + 1; ++x)
            {
                if (!IsValidIndex(x, y))
                    continue;

                if(mFrames[x, y].Empty)
                    continue;

                Product pro = mFrames[x, y].ChildProduct;
                if (pro != null && !pro.IsLocked && !pro.IsChocoBlock)
                    rets.Add(pro);
            }
        }
        return rets.ToArray();
    }

    public Product[] DestroySkillProduct(Product productA, Product productB = null)
    {
        if (productB == null)
        {
            if (productA.Skill == ProductSkill.Horizontal)
            {
                CreateStripeEffect(productA.transform.position, false);
                Product[] scan = ScanHorizenProducts(productA);
                return DestroyProducts(scan);
            }
            else if (productA.Skill == ProductSkill.Vertical)
            {
                CreateStripeEffect(productA.transform.position, true);
                Product[] scan = ScanVerticalProducts(productA);
                return DestroyProducts(scan);
            }
            else if (productA.Skill == ProductSkill.Bomb)
            {
                CreateExplosionEffect(productA.transform.position);
                Product[] scan = ScanAroundProducts(productA, 1);
                return DestroyProducts(scan);
            }
            else if (productA.Skill == ProductSkill.SameColor)
            {
                Product[] sameProducts = FindSameColor(productA);
                return DestroyProducts(sameProducts);
            }
        }
        else
        {
            if (productA.Skill == ProductSkill.Bomb)
            {
                List<Product> pros = new List<Product>();
                if (productB.Skill == ProductSkill.Horizontal)
                {
                    List<Product> list = new List<Product>();
                    list.AddRange(ScanHorizenProducts(productB));
                    CreateStripeEffect(productB.transform.position, false);

                    Product productB_up = productB.Up();
                    if (productB_up != null)
                    {
                        list.AddRange(ScanHorizenProducts(productB_up));
                        CreateStripeEffect(productB_up.transform.position, false);
                    }

                    Product productB_down = productB.Down();
                    if (productB_down != null)
                    {
                        list.AddRange(ScanHorizenProducts(productB_down));
                        CreateStripeEffect(productB_down.transform.position, false);
                    }

                    return DestroyProducts(list.ToArray());
                }
                else if (productB.Skill == ProductSkill.Vertical)
                {

                    List<Product> list = new List<Product>();
                    list.AddRange(ScanVerticalProducts(productB));
                    CreateStripeEffect(productB.transform.position, true);

                    Product productB_left = productB.Left();
                    if (productB_left != null)
                    {
                        list.AddRange(ScanVerticalProducts(productB_left));
                        CreateStripeEffect(productB_left.transform.position, true);
                    }

                    Product productB_right = productB.Right();
                    if (productB_right != null)
                    {
                        list.AddRange(ScanVerticalProducts(productB_right));
                        CreateStripeEffect(productB_right.transform.position, true);
                    }

                    return DestroyProducts(list.ToArray());
                }
                else if (productB.Skill == ProductSkill.Bomb)
                {
                    Product[] scan = ScanAroundProducts(productB, 2);
                    CreateExplosionEffect(productB.transform.position);
                    return DestroyProducts(scan);
                }
            }
            else if (productA.Skill == ProductSkill.Horizontal || productA.Skill == ProductSkill.Vertical)
            {
                List<Product> scan = new List<Product>();
                scan.AddRange(ScanHorizenProducts(productB));
                scan.AddRange(ScanVerticalProducts(productB));
                CreateStripeEffect(productB.transform.position, false);
                CreateStripeEffect(productB.transform.position, true);
                return DestroyProducts(scan.ToArray());
            }
        }
        return null;
    }
    public void DestroySameColorBoth(Product productA, Product productB)
    {
        DestroyProducts(new Product[2] { productA, productB });

        if (productA.Skill == ProductSkill.SameColor)
        {
            if (productB.Skill == ProductSkill.Horizontal || productB.Skill == ProductSkill.Vertical)
            {
                List<Product> randomProducts = ScanRandomProducts(5);
                List<ProductInfo> netInfo = new List<ProductInfo>();
                foreach (Product pro in randomProducts)
                {
                    CreateLaserEffect(productA.transform.position, pro.transform.position);
                    pro.ChangeProductImage(UnityEngine.Random.Range(0, 2) == 0 ? ProductSkill.Horizontal : ProductSkill.Vertical);
                    netInfo.Add(new ProductInfo(pro.Color, pro.Color, pro.Skill, pro.ParentFrame.IndexX, pro.ParentFrame.IndexY, 0, 0));
                }
                Network_ChangeSkill(netInfo.ToArray());
                StartCoroutine(LoopBreakAllSkill());
            }
            else if (productB.Skill == ProductSkill.Bomb)
            {
                List<Product> randomProducts = ScanRandomProducts(5);
                List<ProductInfo> netInfo = new List<ProductInfo>();
                foreach (Product pro in randomProducts)
                {
                    CreateLaserEffect(productA.transform.position, pro.transform.position);
                    pro.ChangeProductImage(ProductSkill.Bomb);
                    netInfo.Add(new ProductInfo(pro.Color, pro.Color, pro.Skill, pro.ParentFrame.IndexX, pro.ParentFrame.IndexY, 0, 0));
                }
                Network_ChangeSkill(netInfo.ToArray());
                StartCoroutine(LoopBreakAllSkill());
            }
            else if (productB.Skill == ProductSkill.SameColor)
            {
                StartCoroutine(LoopSameColorSkill(productA.transform.position));
            }
        }
    }
    IEnumerator LoopBreakSkill(Product[] skillProducts)
    {
        mStopDropping = true;
        List<Product> skilledProducts = new List<Product>();
        skilledProducts.AddRange(skillProducts);

        while (true)
        {
            yield return new WaitForSeconds(0.4f);

            Product[] targets = skilledProducts.ToArray();
            skilledProducts.Clear();
            foreach (Product pro in targets)
            {
                Product[] destroyes = DestroySkillProduct(pro);
                foreach (Product des in destroyes)
                {
                    if (des.Skill != ProductSkill.Nothing && des != pro)
                        skilledProducts.Add(des);
                }
            }

            if (skilledProducts.Count <= 0)
                break;
        }

        yield return new WaitForSeconds(0.2f);
        mStopDropping = false;
    }
    IEnumerator LoopSameColorSkill(Vector3 startPos)
    {
        mItemLooping = true;
        while (true)
        {
            if (mIsDropping)
            {
                yield return null;
                continue;
            }

            bool end = true;
            foreach (Frame frame in mFrames)
            {
                if (frame.Empty)
                    continue;

                Product pro = frame.ChildProduct;
                if (pro != null && !pro.IsLocked)
                {
                    List<Product[]> matches = FindMatchedProducts(new Product[1] { pro });
                    if (matches.Count > 0)
                    {
                        Vector3 destPos = matches[0][0].transform.position;
                        CreateLaserEffect(startPos, destPos);
                        DestroyProducts(matches[0], true);
                        end = false;
                    }
                }
            }

            if (end)
                break;
            else
                yield return new WaitForSeconds(0.3f);
        }
        mItemLooping = false;
    }



    private void SwipeSkilledProducts(Product main, Product sub)
    {
        if (main.Skill == ProductSkill.SameColor || sub.Skill == ProductSkill.SameColor)
        {
            DestroySkillWithSamecolor(main, sub);
        }
        else if (main.Skill == ProductSkill.Bomb && sub.Skill == ProductSkill.Bomb)
        {
            DestroySkillBomb_Bomb(main, sub);
        }
        else if (main.Skill != ProductSkill.Bomb && sub.Skill != ProductSkill.Bomb)
        {
            DestroySkillStripe_Stripe(main, sub);
        }
        else
        {
            if (main.Skill == ProductSkill.Bomb)
                DestroySkillBomb_Stripe(main, sub);
            else
                DestroySkillBomb_Stripe(sub, main);
        }
    }
    IEnumerator LoopBreakAllSkill()
    {
        mItemLooping = true;
        while (true)
        {
            for (int y = CountY - 1; y >= 0; --y)
            {
                for (int x = 0; x < CountX; ++x)
                {
                    Frame frame = mFrames[x, y];
                    Product pro = frame.ChildProduct;
                    if (pro != null && pro.Skill != ProductSkill.Nothing && !pro.IsLocked)
                    {
                        DestroySkillProduct(pro);
                        goto KeepLoop;
                    }
                }
            }

            break;

        KeepLoop:
            yield return new WaitForSeconds(0.4f);
        }
        mItemLooping = false;
    }
    IEnumerator StartAutoMatchFlow()
    {
        mIsAutoMatching = true;
        while (true)
        {
            bool isAllIdle = true;
            Product[] matchableProducts = null;
            foreach (Frame frame in mFrames)
            {
                if (frame.Empty)
                    continue;

                if (frame.ChildProduct == null || frame.ChildProduct.IsLocked)
                {
                    isAllIdle = false;
                    continue;
                }
                    
                List<Product[]> matches = FindMatchedProducts(new Product[1] { frame.ChildProduct });
                if (matches.Count <= 0)
                    continue;

                isAllIdle = false;
                matchableProducts = matches[0];
                break;
            }

            
            if (isAllIdle)
            {
                break;
            }
            else if (matchableProducts != null)
            {
                ProductSkill nextSkill = CheckSkillable(matchableProducts);
                if (nextSkill == ProductSkill.Nothing)
                    DestroyProducts(matchableProducts);
                else
                    MergeProducts(matchableProducts, nextSkill);
            }
            
            yield return new WaitForSeconds(0.3f);
        }
        mIsAutoMatching = false;
    }
    IEnumerator StartAutoMatchFlow2()
    {
        mIsAutoMatching = true;
        float time = 0;
        float interval = 0.3f;
        float limitTime = 5.0f;
        int matchCount = UserSetting.MatchCount;
        while (true)
        {
            time += Time.deltaTime;
            bool isAllIdle = true;
            Product[] matchableProducts = null;
            foreach (Frame frame in mFrames)
            {
                if (frame.Empty)
                    continue;

                if (frame.ChildProduct == null || frame.ChildProduct.IsLocked)
                {
                    isAllIdle = false;
                    continue;
                }

                List<Product[]> matches = FindMatchedProducts(new Product[1] { frame.ChildProduct }, matchCount);
                if (matches.Count <= 0)
                    continue;

                matchableProducts = matches[0];
                isAllIdle = false;
                break;
            }

            if (time > limitTime || isAllIdle)
            {
                time = 0;
                interval -= 0.1f;
                matchCount--;
                limitTime -= 2.0f;
                if (matchCount <= 1)
                    break;
            }
            else if (matchableProducts != null)
            {
                ProductSkill nextSkill = CheckSkillable(matchableProducts);
                if (nextSkill == ProductSkill.Nothing)
                    DestroyProducts(matchableProducts);
                else
                    MergeProducts(matchableProducts, nextSkill);
            }

            yield return new WaitForSeconds(interval);
        }
        mIsAutoMatching = false;
    }
    IEnumerator StartElectronicEffect(Product main, Product[] pros, Action<Product> eventTurn, Action eventEnd)
    {
        mItemLooping = true;
        Vector3 startPos = main.transform.position;
        while (true)
        {
            int idx = 0;
            bool isDone = true;
            Product cur = null;
            for(idx = 0; idx < pros.Length; ++idx)
            {
                if (pros[idx] == null)
                    continue;
                else if(pros[idx].IsLocked)
                {
                    isDone = false;
                    continue;
                }
                else
                {
                    cur = pros[idx];
                    break;
                }
            }

            if (isDone && cur == null)
                break;

            if(cur != null)
            {
                CreateLaserEffect(startPos, cur.transform.position);
                eventTurn?.Invoke(cur);
                pros[idx] = null;
            }

            yield return new WaitForSeconds(0.1f);
        }

        eventEnd?.Invoke();
        mItemLooping = false;
    }
    public void DestroySkillOneshot(Product productA)
    {
        if (productA.Skill == ProductSkill.Horizontal)
        {
            CreateStripeEffect(productA.transform.position, false);
            Product[] scan = ScanHorizenProducts(productA);
            DestroyProducts(scan);
        }
        else if (productA.Skill == ProductSkill.Vertical)
        {
            CreateStripeEffect(productA.transform.position, true);
            Product[] scan = ScanVerticalProducts(productA);
            DestroyProducts(scan);
        }
        else if (productA.Skill == ProductSkill.Bomb)
        {
            CreateExplosionEffect(productA.transform.position);
            Product[] scan = ScanAroundProducts(productA, 1);
            DestroyProducts(scan);
        }
        else if (productA.Skill == ProductSkill.SameColor)
        {
            Product[] sameProducts = FindSameColor(productA);
            DestroyProducts(sameProducts);
        }
    }
    private void DestroySkill(Product target)
    {
        if (target.SkillCasted)
            return;

        target.SkillCasted = true;
        if (target.Skill == ProductSkill.Horizontal)
        {
            CreateStripeEffect(target.transform.position, false);
            Product[] pros = ScanHorizenProducts(target);
            DestroyProducts(pros);
            foreach (Product pro in pros)
                if (pro != target && pro.Skill != ProductSkill.Nothing)
                    DestroySkill(pro);
        }
        else if (target.Skill == ProductSkill.Vertical)
        {
            CreateStripeEffect(target.transform.position, true);
            Product[] pros = ScanVerticalProducts(target);
            DestroyProducts(pros);
            foreach (Product pro in pros)
                if (pro != target && pro.Skill != ProductSkill.Nothing)
                    DestroySkill(pro);
        }
        else if (target.Skill == ProductSkill.Bomb)
        {
            CreateExplosionEffect(target.transform.position);
            Product[] pros = ScanAroundProducts(target, 1);
            DestroyProducts(pros);
            foreach (Product pro in pros)
                if (pro != target && pro.Skill != ProductSkill.Nothing)
                    DestroySkill(pro);
        }
        else if (target.Skill == ProductSkill.SameColor)
        {
            Product[] pros = FindSameColor(target);
            StartCoroutine(StartElectronicEffect(target, pros,
                (pro) => {
                DestroyProducts(new Product[1] { pro });
            }, null));
        }
    }
    public void DestroySkillWithSamecolor(Product productA, Product productB)
    {
        Product sameColor = productA.Skill == ProductSkill.SameColor ? productA : productB;
        Product another = productA.Skill == ProductSkill.SameColor ? productB : productA;
        if (sameColor.Skill != ProductSkill.SameColor)
            return;

        if (another.Skill == ProductSkill.Horizontal || another.Skill == ProductSkill.Vertical)
        {
            List<Product> randomProducts = ScanRandomProducts(5);
            randomProducts.Add(sameColor);
            Product[] pros = randomProducts.ToArray();

            List<ProductInfo> netInfo = new List<ProductInfo>();
            StartCoroutine(StartElectronicEffect(sameColor, pros, 
                (pro) => {
                    pro.ChangeProductImage(UnityEngine.Random.Range(0, 2) == 0 ? ProductSkill.Horizontal : ProductSkill.Vertical);
                    netInfo.Add(new ProductInfo(pro.Color, pro.Color, pro.Skill, pro.ParentFrame.IndexX, pro.ParentFrame.IndexY, pro.InstanceID, pro.InstanceID));
                },
                () => {
                    Network_ChangeSkill(netInfo.ToArray());
                    StartCoroutine(LoopBreakAllSkill());
                }));
        }
        else if (another.Skill == ProductSkill.Bomb)
        {
            List<Product> randomProducts = ScanRandomProducts(5);
            randomProducts.Add(sameColor);
            Product[] pros = randomProducts.ToArray();

            List<ProductInfo> netInfo = new List<ProductInfo>();
            StartCoroutine(StartElectronicEffect(sameColor, pros,
                (pro) => {
                    pro.ChangeProductImage(ProductSkill.Bomb);
                    netInfo.Add(new ProductInfo(pro.Color, pro.Color, pro.Skill, pro.ParentFrame.IndexX, pro.ParentFrame.IndexY, pro.InstanceID, pro.InstanceID));
                },
                () => {
                    Network_ChangeSkill(netInfo.ToArray());
                    StartCoroutine(LoopBreakAllSkill());
                }));
        }
        else if (another.Skill == ProductSkill.SameColor)
        {
            List<Product> randomProducts = ScanRandomProducts(5);
            Product[] pros = randomProducts.ToArray();

            List<ProductInfo> netInfo = new List<ProductInfo>();
            StartCoroutine(StartElectronicEffect(sameColor, pros,
                (pro) => {
                    pro.ChangeProductImage(ProductSkill.SameColor);
                    netInfo.Add(new ProductInfo(pro.Color, pro.Color, pro.Skill, pro.ParentFrame.IndexX, pro.ParentFrame.IndexY, pro.InstanceID, pro.InstanceID));
                },
                () => {
                    Network_ChangeSkill(netInfo.ToArray());
                    List<Product> allSkillProducts = new List<Product>();
                    foreach (Frame frame in mFrames)
                        if (frame.ChildProduct != null && !frame.ChildProduct.IsLocked && frame.ChildProduct.Skill != ProductSkill.Nothing)
                            allSkillProducts.Add(frame.ChildProduct);

                    DestroyProducts(allSkillProducts.ToArray());
                    StartCoroutine(StartAutoMatchFlow2());
                }));
        }
    }
    public void DestroySkillBomb_Stripe(Product productbomb, Product productStripe)
    {
        if (productStripe.Skill == ProductSkill.Horizontal)
        {
            List<Product> pros = new List<Product>();
            pros.AddRange(ScanHorizenProducts(productStripe));
            CreateStripeEffect(productStripe.transform.position, false);

            Product productB_up = productStripe.Up();
            if (productB_up != null)
            {
                pros.AddRange(ScanHorizenProducts(productB_up));
                CreateStripeEffect(productB_up.transform.position, false);
            }

            Product productB_down = productStripe.Down();
            if (productB_down != null)
            {
                pros.AddRange(ScanHorizenProducts(productB_down));
                CreateStripeEffect(productB_down.transform.position, false);
            }

            DestroyProducts(pros.ToArray());
            foreach (Product pro in pros)
                if (pro != productbomb && pro != productStripe && pro.Skill != ProductSkill.Nothing)
                    DestroySkill(pro);
        }
        else if (productStripe.Skill == ProductSkill.Vertical)
        {
            List<Product> pros = new List<Product>();
            pros.AddRange(ScanVerticalProducts(productStripe));
            CreateStripeEffect(productStripe.transform.position, true);

            Product productB_left = productStripe.Left();
            if (productB_left != null)
            {
                pros.AddRange(ScanVerticalProducts(productB_left));
                CreateStripeEffect(productB_left.transform.position, true);
            }

            Product productB_right = productStripe.Right();
            if (productB_right != null)
            {
                pros.AddRange(ScanVerticalProducts(productB_right));
                CreateStripeEffect(productB_right.transform.position, true);
            }

            DestroyProducts(pros.ToArray());
            foreach (Product pro in pros)
                if (pro != productbomb && pro != productStripe && pro.Skill != ProductSkill.Nothing)
                    DestroySkill(pro);
        }

    }
    public void DestroySkillStripe_Stripe(Product productStripeA, Product productStripeB)
    {

        List<Product> pros = new List<Product>();
        pros.AddRange(ScanHorizenProducts(productStripeA));
        pros.AddRange(ScanVerticalProducts(productStripeA));
        pros.AddRange(ScanHorizenProducts(productStripeB));
        pros.AddRange(ScanVerticalProducts(productStripeB));
        CreateStripeEffect(productStripeA.transform.position, false);
        CreateStripeEffect(productStripeA.transform.position, true);
        CreateStripeEffect(productStripeB.transform.position, false);
        CreateStripeEffect(productStripeB.transform.position, true);

        DestroyProducts(pros.ToArray());
        foreach (Product pro in pros)
            if (pro != productStripeA && pro != productStripeB && pro.Skill != ProductSkill.Nothing)
                DestroySkill(pro);
    }
    public void DestroySkillBomb_Bomb(Product productbombA, Product productbombB)
    {
        Product[] pros = ScanAroundProducts(productbombB, 2);
        CreateExplosionEffect(productbombB.transform.position);

        DestroyProducts(pros);
        foreach (Product pro in pros)
            if (pro != productbombA && pro != productbombB && pro.Skill != ProductSkill.Nothing)
                DestroySkill(pro);
    }


    private Product[] FindSameColor(Product target)
    {
        List<Product> pros = new List<Product>();
        pros.Add(target);
        foreach (Frame frame in mFrames)
        {
            Product pro = frame.ChildProduct;
            if (pro == null || pro.IsLocked || pro.IsChocoBlock || pro.Skill != ProductSkill.Nothing)
                continue;

            if(pro.Color == target.Color)
                pros.Add(pro);
        }
        return pros.ToArray();
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
    private List<Product[]> FindMatchedProducts(Product[] targetProducts, int matchCount = UserSetting.MatchCount)
    {
        Dictionary<Product, int> matchedPro = new Dictionary<Product, int>();
        List<Product[]> list = new List<Product[]>();
        foreach (Product pro in targetProducts)
        {
            if (matchedPro.ContainsKey(pro))
                continue;

            List<Product> matches = new List<Product>();
            pro.SearchMatchedProducts(matches, pro.Color);
            if (matches.Count >= matchCount)
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
                if (pro != null && !pro.IsLocked)
                    aroundProducts[pro] = 1;
            }
        }
        return new List<Product>(aroundProducts.Keys);
    }
    public Frame FrameOfWorldPos(float worldPosX, float worldPosY)
    {
        Rect worldRect = FieldWorldRect;
        if (worldPosX < worldRect.xMin || worldPosY < worldRect.yMin || worldRect.xMax < worldPosX || worldRect.yMax < worldPosY)
            return null;

        float idxX = (worldPosX - worldRect.xMin) / GridSize;
        float idxY = (worldPosY - worldRect.yMin) / GridSize;
        return mFrames[(int)idxX, (int)idxY];
    }
    private List<Product> ScanRandomProducts(int step)
    {
        List<Product> rets = new List<Product>();
        int totalCount = CountX * CountY;
        int curIdx = -1;
        while (true)
        {
            curIdx++;
            curIdx = UnityEngine.Random.Range(curIdx, curIdx + step);
            if (curIdx >= totalCount)
                break;

            int idxX = curIdx % CountX;
            int idxY = curIdx / CountX;
            Product pro = mFrames[idxX, idxY].ChildProduct;
            if (pro != null && !pro.IsLocked && !pro.IsChocoBlock && pro.Skill == ProductSkill.Nothing)
                rets.Add(pro);
        }
        return rets;
    }
    private void Attack(int score, Vector3 fromPos)
    {
        int point = score / UserSetting.AttackScore;
        if (point <= 0)
            return;

        if (FieldType == GameFieldType.Stage)
            return;
        else if (FieldType == GameFieldType.pvpPlayer)
        {
            fromPos.z -= 1;
            GameObject obj = GameObject.Instantiate(AttackBullet, fromPos, Quaternion.identity, transform);
            obj.transform.localScale = new Vector3(0.5f, 0.5f, 1.0f);
            StartCoroutine(AnimateThrowOver(obj, () =>
            {
                Destroy(obj);
                AttackPointFrame.AddPoints(-point);
            }));
        }
        else if (FieldType == GameFieldType.pvpOpponent)
        {
            fromPos.z -= 1;
            GameObject obj = GameObject.Instantiate(AttackBullet, fromPos, Quaternion.identity, transform);
            obj.transform.localScale = new Vector3(0.5f, 0.5f, 1.0f);
            StartCoroutine(AnimateThrowOver(obj, () => {
                Destroy(obj);
                AttackPointFrame.AddPoints(point);
            }));
        }
    }
    IEnumerator AnimateThrowSide(GameObject obj, Action action = null)
    {
        float time = 0;
        float duration = 1.0f;
        Vector3 startPos = obj.transform.position;
        Vector3 destPos = AttackPointFrame.transform.position;
        Vector3 dir = destPos - startPos;
        Vector3 offset = Vector3.zero;
        Vector3 axisZ = new Vector3(0, 0, 1);
        float slopeY = dir.y / (duration * duration);
        float slopeX = -dir.x / (duration * duration);
        while (time < duration)
        {
            float nowT = time - duration;
            offset.x = slopeX * nowT * nowT + dir.x;
            offset.y = slopeY * time * time;
            obj.transform.position = startPos + offset;
            obj.transform.Rotate(axisZ, (offset - dir).magnitude);

            time += Time.deltaTime;
            yield return null;
        }

        action?.Invoke();
    }
    IEnumerator AnimateThrowOver(GameObject obj, Action action = null)
    {
        float time = 0;
        float duration = 1.0f;
        Vector3 startPos = obj.transform.position;
        Vector3 destPos = AttackPointFrame.transform.position;
        Vector3 dir = destPos - startPos;
        Vector3 offset = Vector3.zero;
        Vector3 axisZ = new Vector3(0, 0, 1);
        float slopeY = -dir.y / (duration * duration);
        float slopeX = -dir.x / (duration * duration);
        while (time < duration)
        {
            float nowT = time - duration;
            offset.x = slopeX * nowT * nowT + dir.x;
            offset.y = slopeY * nowT * nowT + dir.y;
            obj.transform.position = startPos + offset;
            obj.transform.Rotate(axisZ, (offset - dir).magnitude);

            time += Time.deltaTime;
            yield return null;
        }

        action?.Invoke();
    }
    private IEnumerator CheckFlush()
    {
        while (true)
        {
            if(AttackPointFrame.Points > 0
                && Time.realtimeSinceStartup > AttackPointFrame.TouchedTime + UserSetting.ChocoFlushInterval
                && IsAllProductIdle() )
            {
                int point = AttackPointFrame.Flush(UserSetting.FlushCount);

                List<Product> products = GetNextFlushTargets(point);
                Product[] rets = products.ToArray();
                Network_FlushAttacks(Serialize(rets));
                StartCoroutine(FlushObstacles(rets));
                if (products.Count < point)
                {
                    StartCoroutine("StartFinishing");
                    break;
                }

                yield return new WaitForSeconds(UserSetting.ChocoFlushInterval);
            }
            yield return null;
        }
    }
    private IEnumerator StartFinishing()
    {
        mIsFinished = true;
        while(true)
        {
            if (IsIdle)
            {
                bool success = false;
                if(FieldType == GameFieldType.Stage)
                {
                    if(IsAchieveGoals())
                    {
                        MenuInformBox.PopUp("MISSION COMPLETE!!");
                        success = true;
                    }
                    else if(mStageInfo.MoveLimit > 0)
                        MenuInformBox.PopUp("MOVE LIMITTED");
                    else if (mStageInfo.TimeLimit > 0)
                        MenuInformBox.PopUp("TIMEOUT");
                    else
                        MenuInformBox.PopUp("GAME OVER");
                }
                else
                {
                    if(Opponent.mIsFinished)
                    {
                        MenuInformBox.PopUp("YOU WIN");
                        success = true;
                    }
                    else
                        MenuInformBox.PopUp("YOU LOSE");
                }

                yield return new WaitForSeconds(UserSetting.InfoBoxDisplayTime);

                EventFinish?.Invoke(success);
                CleanUpGame();
                break;
            }

            yield return null;
        }
    }
    private IEnumerator CheckFinishStageMode()
    {
        float time = 0;
        while(true)
        {
            if(mStageInfo.TimeLimit > 0)
            {
                int prevSec = (int)time;
                time += Time.deltaTime;
                int curSec = (int)time;
                int remainTime = mStageInfo.TimeLimit - curSec;
                if (remainTime < 0)
                {
                    StartCoroutine("StartFinishing");
                    break;
                }
                else if (prevSec == 0 || prevSec != curSec)
                    EventRemainTime?.Invoke(remainTime);

            }

            if (IsAchieveGoals())
            {
                StartCoroutine("StartFinishing");
                break;
            }

            yield return null;
        }
    }
    private bool IsAchieveGoals()
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

        return isSuccess;
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
                if (pro == null || pro.IsChocoBlock)
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
    private ProductSkill CheckSkillable(Product[] matches)
    {
        if (matches.Length <= UserSetting.MatchCount + 1)
            return ProductSkill.Nothing;

        if (matches.Length >= UserSetting.MatchCount + 4)
            return ProductSkill.SameColor;

        ProductSkill skill = ProductSkill.Nothing;
        int ran = UnityEngine.Random.Range(0, 3);
        if (ran == 0)
            skill = ProductSkill.Horizontal;
        else if (ran == 1)
            skill = ProductSkill.Vertical;
        else
            skill = ProductSkill.Bomb;

        return skill;
    }
    private List<Product> GetSameColorProducts(ProductColor color)
    {
        List<Product> list = new List<Product>();
        foreach (Frame frame in mFrames)
        {
            if (frame.Empty || frame.ChildProduct == null || frame.ChildProduct.IsLocked || frame.ChildProduct.IsChocoBlock)
                continue;
            if (frame.ChildProduct.Color != color)
                continue;
            list.Add(frame.ChildProduct);
        }
        return list;
    }
    private bool IsDropFinish()
    {
        for (int x = 0; x < CountX; ++x)
        {
            Frame frame = mFrames[x, CountY - 1];
            if (frame.Empty)
                continue;
            if (frame.ChildProduct == null)
                return false;
        }
        return true;
    }
    public bool IsAllProductIdle()
    {
        for (int y = CountY - 1; y >= 0; --y)
        {
            for (int x = 0; x < CountX; ++x)
            {
                if (mFrames[x, y].Empty)
                    continue;
                if (mFrames[x, y].ChildProduct == null || mFrames[x, y].ChildProduct.IsLocked)
                    return false;
            }
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
                curFrame.SetSpriteMask(mask);

                if (curFrame.Left() == null || curFrame.Left().Empty) curFrame.ShowBorder(0);
                if (curFrame.Right() == null || curFrame.Right().Empty) curFrame.ShowBorder(1);
                if (curFrame.Up() == null || curFrame.Up().Empty) curFrame.ShowBorder(2);
                if (curFrame.Down() == null || curFrame.Down().Empty) curFrame.ShowBorder(3);
            }
        }
    }
    private void InitDropGroupFrames()
    {
        List<VerticalFrames> vf = new List<VerticalFrames>();
        VerticalFrames vg = null;
        for (int x = 0; x < CountX; ++x)
        {
            vg = null;
            for (int y = 0; y < CountY; ++y)
            {
                Frame curFrame = mFrames[x, y];
                if (curFrame.Empty)
                {
                    vg = null;
                    continue;
                }
                else
                {
                    if(vg == null)
                    {
                        vg = new GameObject().AddComponent<VerticalFrames>();
                        vg.name = vgName;
                        vg.transform.SetParent(transform);
                        vf.Add(vg);

                        GameObject ground = Instantiate(GroundPrefab, vg.transform);
                        ground.name = "ground";
                        ground.transform.position = curFrame.transform.position - new Vector3(0, GridSize, 0);
                    }
                    curFrame.transform.SetParent(vg.transform);
                }
            }
        }

        foreach (VerticalFrames group in vf)
            group.init();

        mVerticalFrames = vf.ToArray();
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
        product.Manager = this;
        product.transform.localPosition = new Vector3(0, 0, -1);
        product.AttachTo(parent);
        product.InstanceID = product.GetInstanceID();
        ProductIDs[product.InstanceID] = product;
        return product;
    }
    private Product CreateNewProduct(ProductColor color = ProductColor.None)
    {
        int typeIdx = color == ProductColor.None ? RandomNextColor() : (int)color - 1;
        GameObject obj = Instantiate(ProductPrefabs[typeIdx], transform);
        Product product = obj.GetComponent<Product>();
        product.Manager = this;
        product.InstanceID = product.GetInstanceID();
        ProductIDs[product.InstanceID] = product;
        return product;
    }
    IEnumerator FlushObstacles(Product[] targets)
    {
        mIsFlushing = true;
        List<Tuple<GameObject, Product>> obstacles = new List<Tuple<GameObject, Product>>();
        foreach (Product target in targets)
        {
            Vector3 startPos = target.ParentFrame.VertFrames.TopFrame.transform.position;
            startPos.y += GridSize;
            startPos.z = target.transform.position.z - 0.5f;
            GameObject obj = Instantiate(ObstaclePrefab, startPos, Quaternion.identity, transform);
            obstacles.Add(new Tuple<GameObject, Product>(obj, target));
        }

        float vel = 0;
        while (true)
        {
            vel += 2;
            bool isDone = true;
            foreach (var each in obstacles)
            {
                GameObject obstacle = each.Item1;
                Product destProduct = each.Item2;
                if (destProduct.IsChocoBlock || obstacle == null)
                    continue;

                isDone = false;
                float deltaY = vel * Time.deltaTime;
                if (obstacle.transform.position.y - deltaY <= destProduct.transform.position.y)
                {
                    destProduct.SetChocoBlock(1);
                    SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectDropIce);
                    Destroy(obstacle);
                }
                else
                {
                    obstacle.transform.position -= new Vector3(0, deltaY, 0);
                }
            }

            if (isDone)
                break;
            else
                yield return null;
        }
        ShakeField();
        mIsFlushing = false;
    }
    private int RandomNextColor()
    {
        return mRandomSeed.Next((int)mStageInfo.ColorCount);
        //int count = (int)(mStageInfo.ColorCount + 0.99f);
        //float remain = mStageInfo.ColorCount - (int)mStageInfo.ColorCount;
        //int idx = UnityEngine.Random.Range(0, count);
        //if (remain > 0 && idx == count - 1)
        //{
        //    if (remain <= UnityEngine.Random.Range(0, 10) * 0.1f)
        //        idx = UnityEngine.Random.Range(0, count - 1);
        //}
        //return idx;
    }
    private void ResetGame()
    {
        int cnt = transform.childCount;
        for (int i = 0; i < cnt; ++i)
            Destroy(transform.GetChild(i).gameObject);

        foreach (GameObject skill in SkillSlots)
            skill.SetActive(false);

        EventBreakTarget = null;
        EventMatched = null;
        EventFinish = null;
        EventReduceLimit = null;

        AttackPointFrame.ResetPoints();
        mIsFinished = false;
        mIsDropping = false;
        mStopDropping = false;
        mItemLooping = false;
        mIsUserEventLock = false;
        mIsFlushing = false;
        mIsAutoMatching = false;
        mPrevIdleState = IsIdle;
        mRequestDrop = false;
        mRandomSeed = null;

        ProductIDs.Clear();
        Billboard.Reset();
        mNetMessages.Clear();

        mVerticalFrames = null;
        mFrames = null;
        mUserInfo = null;
        mStageInfo = null;

        SwipeDetector comp = GetComponent<SwipeDetector>();
        if(comp != null)
        {
            comp.EventSwipe = null;
            comp.EventClick = null;
        }

        StopAllCoroutines();
        CancelInvoke();
    }
    private void RemoveLimit()
    {
        if(mStageInfo.MoveLimit > 0)
        {
            Billboard.MoveCount++;
            EventReduceLimit?.Invoke();
            if (Billboard.MoveCount >= mStageInfo.MoveLimit)
                StartCoroutine("StartFinishing");
        }
    }
    public void ShakeField()
    {
        StopCoroutine("AnimShakeField");
        StartCoroutine("AnimShakeField", 0.05f);
    }
    IEnumerator AnimShakeField(float intensity)
    {
        GetComponent<Animator>().enabled = false;
        float dist = intensity;
        Vector3 startPos = transform.position;
        Vector3 dir = new Vector3(-1, -1, 0);
        dir.Normalize();
        while (dist > 0.01f)
        {
            transform.position = startPos + (dist * dir);
            dist *= 0.7f;
            dir *= -1;
            yield return new WaitForSeconds(0.1f);
        }
        transform.position = startPos;
        GetComponent<Animator>().enabled = true;
    }
    public int NextMatchCount(Product pro, SwipeDirection dir)
    {
        Product target = pro.Dir(dir);
        if (target == null || target.Color == pro.Color || target.IsChocoBlock || target.Skill != ProductSkill.Nothing)
            return 0;

        List<Product> matches = new List<Product>();
        Product[] pros = target.GetAroundProducts(target.ParentFrame);
        foreach(Product each in pros)
        {
            if (each == pro)
                continue;

            each.SearchMatchedProducts(matches, pro.Color);
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
            if (pro == null || pro.IsLocked)
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
            SkillPair skillPair = SkillMapping[(int)pros[0].Color];
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
    void RemoveBadEffects(Vector3 startPos)
    {
        foreach (Frame frame in mFrames)
        {
            Product pro = frame.ChildProduct;
            if (pro != null && pro.IsChocoBlock)
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
    void CreateSkillEffect(Product pro)
    {
        switch(pro.Skill)
        {
            case ProductSkill.Horizontal:
                CreateStripeEffect(pro.transform.position, false);
                break;
            case ProductSkill.Vertical:
                CreateStripeEffect(pro.transform.position, true);
                break;
            case ProductSkill.Bomb:
                CreateExplosionEffect(pro.transform.position);
                break;
            case ProductSkill.SameColor:
            case ProductSkill.Nothing:
            default:
                break;
        }
    }
    void CreateMergeEffect(Product productA, Product productB)
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectBadEffect);
        Vector3 pos = (productA.transform.position + productB.transform.position) * 0.5f;
        pos.z = -4.0f;
        GameObject obj = GameObject.Instantiate(MergeParticle, pos, Quaternion.identity, transform);
        obj.GetComponent<EffectMerge>().SetProucts(productA, productB);
    }
    void CreateSparkEffect(Vector2 startPos)
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectBadEffect);
        Vector3 start = new Vector3(startPos.x, startPos.y, -4.0f);
        GameObject obj = GameObject.Instantiate(SparkParticle, start, Quaternion.identity, transform);
        Destroy(obj, 1.0f);
    }
    void CreateLaserEffect(Vector2 startPos, Vector2 destPos)
    {
        SoundPlayer.Inst.PlaySoundEffect(ClipSound.Skill3);
        Vector3 start = new Vector3(startPos.x, startPos.y, -4.0f);
        Vector3 dest = new Vector3(destPos.x, destPos.y, -4.0f);
        GameObject laserObj = GameObject.Instantiate(LaserParticle, start, Quaternion.identity, transform);
        laserObj.GetComponent<EffectLaser>().SetDestination(dest);
    }
    void CreateStripeEffect(Vector2 startPos, bool isVertical)
    {
        SoundPlayer.Inst.PlaySoundEffect(ClipSound.Skill1);
        Vector3 start = new Vector3(startPos.x, startPos.y, -4.0f);
        GameObject obj = GameObject.Instantiate(StripeParticle, start, isVertical ? Quaternion.Euler(0, 0, 90) : Quaternion.identity, transform);
        Destroy(obj, 1.0f);
    }
    void CreateExplosionEffect(Vector2 startPos)
    {
        SoundPlayer.Inst.PlaySoundEffect(ClipSound.Skill2);
        Vector3 start = new Vector3(startPos.x, startPos.y, -4.0f);
        GameObject obj = GameObject.Instantiate(ExplosionParticle, start, Quaternion.identity, transform);
        Destroy(obj, 1.0f);
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
                mIsFinished = true;
                Opponent.StartCoroutine("StartFinishing");
                mNetMessages.RemoveFirst();
            }
            else if (body.cmd == PVPCommand.StartGame)
            {
                for (int i = 0; i < body.ArrayCount; ++i)
                {
                    ProductInfo info = body.products[i];
                    Frame frame = mFrames[info.idxX, info.idxY];
                    Product pro = CreateNewProduct(frame, info.nextColor);
                    pro.InstanceID = info.nextInstID;
                    ProductIDs[pro.InstanceID] = pro;
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
                mNetMessages.RemoveFirst();
            }
            else if (body.cmd == PVPCommand.Swipe)
            {
                if(IsAllProductIdle())
                {
                    Product pro = mFrames[body.products[0].idxX, body.products[0].idxY].ChildProduct;
                    Product target = pro.Dir(body.dir);

                    mIsUserEventLock = true;
                    pro.Swipe(target, () => {
                        mIsUserEventLock = false;
                    });

                    mNetMessages.RemoveFirst();
                }
            }
            else if (body.cmd == PVPCommand.Destroy)
            {
                List<Product> products = new List<Product>();
                for (int i = 0; i < body.ArrayCount; ++i)
                {
                    ProductInfo info = body.products[i];
                    Product pro = ProductIDs[info.prvInstID];
                    if (pro != null && !pro.IsLocked && pro.Color == info.prvColor)
                        products.Add(pro);
                }
                
                if (products.Count != body.ArrayCount)
                    LOG.warn("Not Sync Destroy Products");
                else
                {
                    Billboard.CurrentCombo = body.combo;

                    int score = body.combo * body.ArrayCount;
                    Billboard.CurrentScore += score;
                    Billboard.DestroyCount += body.ArrayCount;

                    Attack(score, products[0].transform.position);

                    if (body.skill == ProductSkill.Nothing)
                    {
                        for (int idx = 0; idx < body.ArrayCount; ++idx)
                        {
                            if (body.withLaserEffect)
                            {
                                if(idx == 0)
                                    CreateLaserEffect(transform.position, products[idx].transform.position);
                            }
                            else
                            {
                                CreateSkillEffect(products[idx]);
                            }
                            Frame parentFrame = products[idx].ParentFrame;
                            products[idx].Combo = body.combo;
                            products[idx].DestroyImmediately();
                            Product newPro = CreateNewProduct(body.products[idx].nextColor);
                            newPro.InstanceID = body.products[idx].nextInstID;
                            ProductIDs[newPro.InstanceID] = newPro;
                            //newPro.transform.SetParent(parentFrame.VertFrames.transform);
                            parentFrame.VertFrames.AddNewProduct(newPro);
                        }
                        mRequestDrop = true;
                        EventMatched?.Invoke(products.ToArray());
                    }
                    else
                    {
                        for (int idx = 0; idx < body.ArrayCount; ++idx)
                        {
                            Frame parentFrame = products[idx].ParentFrame;
                            products[idx].Combo = body.combo;
                            products[idx].MergeImImmediately(products[0], body.skill);
                            if (idx != 0)
                            {
                                Product newPro = CreateNewProduct(body.products[idx].nextColor);
                                newPro.InstanceID = body.products[idx].nextInstID;
                                ProductIDs[newPro.InstanceID] = newPro;
                                //newPro.transform.SetParent(parentFrame.VertFrames.transform);
                                parentFrame.VertFrames.AddNewProduct(newPro);
                            }
                        }
                        mRequestDrop = true;
                        EventMatched?.Invoke(products.ToArray());
                    }

                    mNetMessages.RemoveFirst();
                }
            }
            else if (body.cmd == PVPCommand.DropPause)
            {
                mStopDropping = true;
                mNetMessages.RemoveFirst();
            }
            else if (body.cmd == PVPCommand.DropResume)
            {
                mStopDropping = false;
                mNetMessages.RemoveFirst();
            }
            else if (body.cmd == PVPCommand.ChangeSkill)
            {
                List<Product> products = new List<Product>();
                for (int i = 0; i < body.ArrayCount; ++i)
                {
                    ProductInfo info = body.products[i];
                    Product pro = mFrames[info.idxX, info.idxY].ChildProduct;
                    if (pro != null && !pro.IsLocked)
                        products.Add(pro);
                }

                if (products.Count == body.ArrayCount)
                {
                    for (int i = 0; i < body.ArrayCount; ++i)
                        products[i].ChangeProductImage(body.products[i].skill);

                    mNetMessages.RemoveFirst();
                }

            }
            else if (body.cmd == PVPCommand.FlushAttacks)
            {
                if(IsAllProductIdle())
                {
                    AttackPointFrame.Flush(body.ArrayCount);
                    List<Product> rets = new List<Product>();
                    for (int i = 0; i < body.ArrayCount; ++i)
                    {
                        ProductInfo info = body.products[i];
                        Product pro = mFrames[info.idxX, info.idxY].ChildProduct;
                        rets.Add(pro);
                    }
                    StartCoroutine(FlushObstacles(rets.ToArray()));

                    mNetMessages.RemoveFirst();
                }
            }
        }
    }
    private ProductInfo[] Serialize(Product[] pros)
    {
        List<ProductInfo> infos = new List<ProductInfo>();
        for (int i = 0; i < pros.Length; ++i)
        {
            ProductInfo info = new ProductInfo();
            info.prvInstID = pros[i].InstanceID;
            info.nextInstID = pros[i].InstanceID;
            info.idxX = pros[i].ParentFrame.IndexX;
            info.idxY = pros[i].ParentFrame.IndexY;
            info.skill = pros[i].Skill;
            info.nextColor = pros[i].Color;
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
            info.skill = ProductSkill.Nothing;
            info.nextColor = ProductColor.None;
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
        req.products[0].prvColor = pro.Color;
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
        req.products[0].prvColor = pro.Color;
        NetClientApp.GetInstance().Request(NetCMD.PVP, req, null);
    }
    private void Network_Destroy(ProductInfo[] pros, ProductSkill skill, bool withLaserEffect)
    {
        if (FieldType != GameFieldType.pvpPlayer)
            return;

        PVPInfo req = new PVPInfo();
        req.cmd = PVPCommand.Destroy;
        req.oppUserPk = InstPVP_Opponent.UserPk;
        req.combo = Billboard.CurrentCombo;
        req.skill = skill;
        req.withLaserEffect = withLaserEffect;
        req.ArrayCount = pros.Length;
        Array.Copy(pros, req.products, pros.Length);
        NetClientApp.GetInstance().Request(NetCMD.PVP, req, null);
    }
    private void Network_Drop(bool dropPause)
    {
        if (FieldType != GameFieldType.pvpPlayer)
            return;

        PVPInfo req = new PVPInfo();
        req.cmd = dropPause ? PVPCommand.DropPause : PVPCommand.DropResume;
        req.oppUserPk = InstPVP_Opponent.UserPk;
        NetClientApp.GetInstance().Request(NetCMD.PVP, req, null);
    }
    private void Network_ChangeSkill(ProductInfo[] pros)
    {
        if (FieldType != GameFieldType.pvpPlayer)
            return;

        PVPInfo req = new PVPInfo();
        req.cmd = PVPCommand.ChangeSkill;
        req.oppUserPk = InstPVP_Opponent.UserPk;
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
