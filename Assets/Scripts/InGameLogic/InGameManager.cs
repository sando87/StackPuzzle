﻿using System;
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
    public static InGameManager InstCurrent
    { get { if (mInstStage != null && mInstStage.gameObject.activeSelf) return mInstStage; else return mInstPVP_Player; } }

    private const string vgName = "VerticalGroup";

    public Sprite[] BackgroundImages;
    public SpriteRenderer BackgroundSprite;
    public GameObject[] ProductPrefabs;
    public GameObject FramePrefab1;
    public GameObject FramePrefab2;
    public GameObject ObstaclePrefab;
    public GameObject GroundPrefab;

    public GameObject ExplosionParticle;
    public GameObject StripeParticle;
    public GameObject LaserParticle;
    public AttackPoints AttackPointFrame;
    public GameObject AttackBullet;

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
    private float mStartTime = 0;
    private int mProductCount = 0;
    private System.Random mRandomSeed = null;
    private VerticalFrames[] mVerticalFrames = null;

    private LinkedList<PVPInfo> mNetMessages = new LinkedList<PVPInfo>();


    public Dictionary<int, Product> ProductIDs = new Dictionary<int, Product>();
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
    public bool IsIdle { get { return !mStopDropping && !mIsDropping && !mIsUserEventLock && !mIsFlushing && !mItemLooping && !mIsAutoMatching && mStageInfo != null && mProductCount == mStageInfo.XCount * mStageInfo.YCount; } }                     
    public int CountX { get { return mStageInfo.XCount; } }
    public int CountY { get { return mStageInfo.YCount; } }
    public int StageNum { get { return mStageInfo.Num; } }
    public float ColorCount { get { return mStageInfo.ColorCount; } }
    public int UserPk { get { return mUserInfo.userPk; } }
    public int UserScore { get { return mUserInfo.score; } }
    public float PlayTime { get { return Time.realtimeSinceStartup - mStartTime; } }
    public float LimitRate { get { return mStageInfo.TimeLimit > 0 ? PlayTime / mStageInfo.TimeLimit : Billboard.MoveCount / (float)mStageInfo.MoveLimit ; } }
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
    public Action<bool> EventFinishPre;
    public Action<int> EventCombo;
    public Action<int> EventRemainTime;
    public Action EventReduceLimit;
    public Action EventEnterIdle;

    private void Update()
    {
        if (mStageInfo == null)
            return;

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
            mStartTime = Time.realtimeSinceStartup;
            GetComponent<SwipeDetector>().EventSwipe = OnSwipe;
            GetComponent<SwipeDetector>().EventClick = OnClick;
            StartCoroutine(CheckFinishStageMode());
            mIsUserEventLock = false;
            if(mStageInfo.TimeLimit > 0)
                SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectCooltime);
        }));
    }
    public void StartGameInPVPPlayer(StageInfo info, UserInfo userInfo)
    {
        MenuInformBox.PopUp("START!!");
        StartGame(info, userInfo);

        mIsUserEventLock = true;
        StartCoroutine(UnityUtils.CallAfterSeconds(UserSetting.InfoBoxDisplayTime, () =>
        {
            mStartTime = Time.realtimeSinceStartup;
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
        mProductCount = info.XCount * info.YCount;

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
                frameObj.GetComponent<SpriteRenderer>().sortingLayerName = FieldType == GameFieldType.pvpOpponent ? "ProductOpp" : "Default";
                frameObj.transform.localPosition = localBasePos + localFramePos;
                mFrames[x, y] = frameObj.GetComponent<Frame>();
                mFrames[x, y].Initialize(this, x, y, info.GetCell(x, y).FrameCoverCount);
                mFrames[x, y].EventBreakCover = (frame) => {
                    Billboard.CoverCount++;
                    EventBreakTarget?.Invoke(frame.transform.position, StageGoalType.Cover);
                };
            }
        }

        InitFrameBorders();
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

            DestroySkillChain(pro);
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

        Product product = swipeObj.GetComponent<Product>();
        if (product.IsLocked || product.IsChocoBlock)
            return;

        Product targetProduct = null;
        switch (dir)
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
            mIsUserEventLock = true;
            Network_Swipe(product, dir);
            product.Swipe(targetProduct, () => {
                StartCoroutine(UpDownSizing(product.gameObject, 0.3f));
                StartCoroutine(UpDownSizing(targetProduct.gameObject, 0.3f));
                StartCoroutine(UnityUtils.CallAfterSeconds(0.3f, () =>
                {
                    mIsUserEventLock = false;
                    SwipeSkilledProducts(product, targetProduct);
                }));
            });
        }
        else
        {
            EventCombo?.Invoke(0);
            mIsUserEventLock = true;
            Network_Swipe(product, dir);
            product.Swipe(targetProduct, () => {
                mIsUserEventLock = false;
            });
        }

        SoundPlayer.Inst.PlaySoundEffect(ClipSound.Swipe);
        RemoveLimit();
    }

    #region MatchingLogic
    IEnumerator DoMatchingCycle(Product[] firstMatches)
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
            List<ProductInfo> breakIceProducts = new List<ProductInfo>();
            foreach (Product aroundPro in aroundProducts)
            {
                if (aroundPro.BreakChocoBlock(Billboard.CurrentCombo))
                    breakIceProducts.Add(new ProductInfo(aroundPro.Color, aroundPro.Color, ProductSkill.Nothing, aroundPro.ParentFrame.IndexX, aroundPro.ParentFrame.IndexY, aroundPro.InstanceID, aroundPro.InstanceID));
            }
            if (breakIceProducts.Count > 0)
                Network_BreakIce(breakIceProducts.ToArray());

            List<Product[]> nextMatches = FindMatchedProducts(aroundProducts.ToArray());
            if (nextMatches.Count <= 0)
                break;

            yield return new WaitForSeconds(UserSetting.ComboMatchInterval);

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

        yield return new WaitForSeconds(UserSetting.ComboMatchInterval);

        mStopDropping = false;
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
            newPro.EnableMasking(parentFrame.VertFrames.MaskOrder);
            nextProducts.Add(new ProductInfo(pro.Color, newPro.Color, ProductSkill.Nothing, parentFrame.IndexX, parentFrame.IndexY, pro.InstanceID, newPro.InstanceID));
        }
        mRequestDrop = true;
        Network_Destroy(nextProducts.ToArray(), ProductSkill.Nothing, withLaserEffect);
        mProductCount += destroyedProducts.Length;
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

        mProductCount -= validProducts.Length;
        StartCoroutine(DestroyProductDelay(validProducts, UserSetting.MatchReadyInterval, withLaserEffect));

        int preAttackCount = Billboard.CurrentScore / UserSetting.ScorePerAttack;
        int curAttackCount = (Billboard.CurrentScore + addedScore) / UserSetting.ScorePerAttack;
        Attack(curAttackCount - preAttackCount, validProducts[0].transform.position);

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
                newPro.EnableMasking(parentFrame.VertFrames.MaskOrder);
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

        mProductCount += mergeProducts.Length;
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

        mProductCount -= matches.Length;
        StartCoroutine(MergeProductDelay(matches, UserSetting.MatchReadyInterval, makeSkill));

        int preAttackCount = Billboard.CurrentScore / UserSetting.ScorePerAttack;
        int curAttackCount = (Billboard.CurrentScore + addedScore) / UserSetting.ScorePerAttack;
        Attack(curAttackCount - preAttackCount, matches[0].transform.position);

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
                StartCoroutine(DestroyProductsDropping());
        }

        mIsDropping = CountDroppingProducts() > 0;
    }
    IEnumerator DestroyProductsDropping()
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

            yield return new WaitForSeconds(UserSetting.AutoMatchInterval);
        }
        mIsAutoMatching = false;
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
    private int CountDroppingProducts()
    {
        int count = 0;
        foreach (VerticalFrames group in mVerticalFrames)
            count += group.Droppingcount;

        return count;
    }

    private void DestroySkillChain(Product target)
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
                    DestroySkillChain(pro);
        }
        else if (target.Skill == ProductSkill.Vertical)
        {
            CreateStripeEffect(target.transform.position, true);
            Product[] pros = ScanVerticalProducts(target);
            DestroyProducts(pros);
            foreach (Product pro in pros)
                if (pro != target && pro.Skill != ProductSkill.Nothing)
                    DestroySkillChain(pro);
        }
        else if (target.Skill == ProductSkill.Bomb)
        {
            CreateExplosionEffect(target.transform.position);
            Product[] pros = ScanAroundProducts(target, 1);
            DestroyProducts(pros);
            foreach (Product pro in pros)
                if (pro != target && pro.Skill != ProductSkill.Nothing)
                    DestroySkillChain(pro);
        }
        else if (target.Skill == ProductSkill.SameColor)
        {
            Product[] pros = FindSameColor(target);
            StartCoroutine(StartElectronicEffect(target.transform.position, pros,
                (pro) => {
                    DestroyProducts(new Product[1] { pro });
                }, null));
        }
    }
    private void SwipeSkilledProducts(Product main, Product sub)
    {
        if (main.Skill == ProductSkill.SameColor || sub.Skill == ProductSkill.SameColor)
        {
            DestroySkillWithSamecolor(main, sub);
        }
        else if (main.Skill == ProductSkill.Bomb && sub.Skill == ProductSkill.Bomb)
        {
            ShakeField(0.03f);
            DestroySkillBomb_Bomb(main, sub);
        }
        else if (main.Skill != ProductSkill.Bomb && sub.Skill != ProductSkill.Bomb)
        {
            ShakeField(0.03f);
            DestroySkillStripe_Stripe(main, sub);
        }
        else
        {
            ShakeField(0.03f);
            if (main.Skill == ProductSkill.Bomb)
                DestroySkillBomb_Stripe(main, sub);
            else
                DestroySkillBomb_Stripe(sub, main);
        }
    }
    private void DestroySkillBomb_Stripe(Product productbomb, Product productStripe)
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
                    DestroySkillChain(pro);
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
                    DestroySkillChain(pro);
        }

    }
    private void DestroySkillStripe_Stripe(Product productStripeA, Product productStripeB)
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
                DestroySkillChain(pro);
    }
    private void DestroySkillBomb_Bomb(Product productbombA, Product productbombB)
    {
        Product[] pros = ScanAroundProducts(productbombB, 2);
        CreateExplosionEffect(productbombB.transform.position);

        DestroyProducts(pros);
        foreach (Product pro in pros)
            if (pro != productbombA && pro != productbombB && pro.Skill != ProductSkill.Nothing)
                DestroySkillChain(pro);
    }
    private void DestroySkillWithSamecolor(Product productA, Product productB)
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
            StartCoroutine(StartElectronicEffect(sameColor.transform.position, pros,
                (pro) => {
                    pro.ChangeProductImage(UnityEngine.Random.Range(0, 2) == 0 ? ProductSkill.Horizontal : ProductSkill.Vertical);
                    netInfo.Add(new ProductInfo(pro.Color, pro.Color, pro.Skill, pro.ParentFrame.IndexX, pro.ParentFrame.IndexY, pro.InstanceID, pro.InstanceID));
                },
                () => {
                    Network_ChangeSkill(netInfo.ToArray());
                    StartCoroutine(DestroySkillSimpleLoop());
                }));
        }
        else if (another.Skill == ProductSkill.Bomb)
        {
            List<Product> randomProducts = ScanRandomProducts(5);
            randomProducts.Add(sameColor);
            Product[] pros = randomProducts.ToArray();

            List<ProductInfo> netInfo = new List<ProductInfo>();
            StartCoroutine(StartElectronicEffect(sameColor.transform.position, pros,
                (pro) => {
                    pro.ChangeProductImage(ProductSkill.Bomb);
                    netInfo.Add(new ProductInfo(pro.Color, pro.Color, pro.Skill, pro.ParentFrame.IndexX, pro.ParentFrame.IndexY, pro.InstanceID, pro.InstanceID));
                },
                () => {
                    Network_ChangeSkill(netInfo.ToArray());
                    StartCoroutine(DestroySkillSimpleLoop());
                }));
        }
        else if (another.Skill == ProductSkill.SameColor)
        {
            List<Product> randomProducts = ScanRandomProducts(5);
            Product[] pros = randomProducts.ToArray();

            List<ProductInfo> netInfo = new List<ProductInfo>();
            StartCoroutine(StartElectronicEffect(sameColor.transform.position, pros,
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
                    StartCoroutine(DestroyProductClimax());
                }));
        }
    }
    IEnumerator DestroySkillSimpleLoop()
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
                        DestroySkillNoChain(pro);
                        goto KeepLoop;
                    }
                }
            }

            if (IsAllProductIdle())
                break;

            KeepLoop:
            yield return new WaitForSeconds(UserSetting.SkillDestroyInterval);
        }
        mItemLooping = false;
    }
    IEnumerator DestroyProductClimax()
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
    private Product[] DestroySkillNoChain(Product target)
    {
        if (target.Skill == ProductSkill.Horizontal)
        {
            CreateStripeEffect(target.transform.position, false);
            Product[] scan = ScanHorizenProducts(target);
            return DestroyProducts(scan);
        }
        else if (target.Skill == ProductSkill.Vertical)
        {
            CreateStripeEffect(target.transform.position, true);
            Product[] scan = ScanVerticalProducts(target);
            return DestroyProducts(scan);
        }
        else if (target.Skill == ProductSkill.Bomb)
        {
            CreateExplosionEffect(target.transform.position);
            Product[] scan = ScanAroundProducts(target, 1);
            return DestroyProducts(scan);
        }
        else if (target.Skill == ProductSkill.SameColor)
        {
            Product[] sameProducts = FindSameColor(target);
            return DestroyProducts(sameProducts);
        }
        return null;
    }

    public void UseItemExtendsLimits()
    {
        if (mStageInfo.TimeLimit > 0)
            mStartTime += 10; //10초 연장
        else
            Billboard.MoveCount -= 5; //5번 이동 추가
    }
    public void UseItemBreakce(int count)
    {
        for(int y = CountY - 1; y >= 0; y--)
        {
            for (int x = CountX - 1; x >= 0; x--)
            {
                Product pro = mFrames[x, y].ChildProduct;
                if (pro == null || pro.IsLocked || !pro.IsChocoBlock)
                    continue;

                pro.BreakChocoBlock(Billboard.CurrentCombo);
                count--;
                if (count <= 0)
                    return;
            }
        }
    }
    public void UseItemMakeSkill1(int count)
    {
        Frame[] frames = GetRandomIdleFrames(count);
        foreach(Frame frame in frames)
        {
            Product pro = frame.ChildProduct;

            int ran = UnityEngine.Random.Range(0, 3);
            if (ran == 0)
                pro.ChangeProductImage(ProductSkill.Horizontal);
            else if (ran == 1)
                pro.ChangeProductImage(ProductSkill.Vertical);
            else
                pro.ChangeProductImage(ProductSkill.Bomb);
        }
    }
    public void UseItemMakeSkill2(int count)
    {
        Frame[] frames = GetRandomIdleFrames(count);
        foreach (Frame frame in frames)
        {
            Product pro = frame.ChildProduct;
            pro.ChangeProductImage(ProductSkill.SameColor);
        }
    }

    private void Attack(int count, Vector3 fromPos)
    {
        if (count <= 0)
            return;

        if (FieldType == GameFieldType.Stage)
            return;
        else if (FieldType == GameFieldType.pvpPlayer)
        {
            fromPos.z -= 1;
            GameObject[] objs = new GameObject[count];
            for (int i = 0; i < count; ++i)
            {
                objs[i] = GameObject.Instantiate(AttackBullet, fromPos, Quaternion.identity, transform);
                objs[i].transform.localScale = new Vector3(0.5f, 0.5f, 1.0f);
            }

            StartCoroutine(AnimateAttack(objs, AttackPointFrame.transform.position, (destObj) =>
            {
                Destroy(destObj);
                AttackPointFrame.AddPoints(-1);
            }));
        }
        else if (FieldType == GameFieldType.pvpOpponent)
        {
            fromPos.z -= 1;
            GameObject[] objs = new GameObject[count];
            for (int i = 0; i < count; ++i)
            {
                objs[i] = GameObject.Instantiate(AttackBullet, fromPos, Quaternion.identity, transform);
                objs[i].transform.localScale = new Vector3(0.5f, 0.5f, 1.0f);
            }

            StartCoroutine(AnimateAttack(objs, AttackPointFrame.transform.position, (destObj) =>
            {
                Destroy(destObj);
                AttackPointFrame.AddPoints(1);
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
    IEnumerator AnimateAttack(GameObject[] objs, Vector3 dest, Action<GameObject> EventEndEach)
    {
        float dragFactor = 0.02f;
        float destFactor = 0;
        Tuple<Vector2, float>[] startSpeed = new Tuple<Vector2, float>[objs.Length];
        for (int i = 0; i < objs.Length; ++i)
        {
            float rad = UnityEngine.Random.Range(225, 315) * Mathf.Deg2Rad;
            if (objs[i].transform.position.y > dest.y)
                rad += Mathf.PI;
            Vector2 force = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            startSpeed[i] = new Tuple<Vector2, float>(force, UnityEngine.Random.Range(30, 35));
        }

        while (true)
        {
            bool isFinished = true;
            for (int i = 0; i < objs.Length; ++i)
            {
                GameObject obj = objs[i];
                if (obj == null)
                    continue;

                isFinished = false;
                Vector3 curStartSpeed = startSpeed[i].Item1 * startSpeed[i].Item2;
                Vector3 dir = dest - obj.transform.position;
                dir.z = 0;
                dir.Normalize();
                Vector3 curSpeed = curStartSpeed + dir * destFactor;
                obj.transform.position += curSpeed * Time.deltaTime;
                obj.transform.Rotate(Vector3.forward, startSpeed[i].Item2 * 5.0f);

                float nextSpeed = startSpeed[i].Item2 - (startSpeed[i].Item2 * startSpeed[i].Item2 * dragFactor);
                nextSpeed = Mathf.Max(0, nextSpeed);
                startSpeed[i] = new Tuple<Vector2, float>(startSpeed[i].Item1, nextSpeed);

                Vector3 afterDir = dest - obj.transform.position;
                afterDir.z = 0;
                afterDir.Normalize();
                if (Vector3.Dot(afterDir, dir) < 0)
                {
                    EventEndEach?.Invoke(obj);
                    objs[i] = null;
                }

            }

            if (isFinished)
                break;

            destFactor += 0.7f;
            yield return null;
        }
    }
    IEnumerator CheckFlush()
    {
        while (true)
        {
            if (AttackPointFrame.Points > 0
                && Time.realtimeSinceStartup > AttackPointFrame.TouchedTime + UserSetting.ChocoFlushInterval
                && IsAllProductIdle())
            {
                int point = AttackPointFrame.Flush(UserSetting.FlushCount);

                List<Product> products = GetNextFlushTargets(point);
                Product[] rets = products.ToArray();
                Network_FlushAttacks(Serialize(rets));
                StartCoroutine(FlushObstacles(rets));
                if (products.Count < point)
                {
                    StartCoroutine("StartFinishing", false);
                    break;
                }

                yield return new WaitForSeconds(UserSetting.ChocoFlushInterval);
            }
            yield return null;
        }
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
        ShakeField(0.05f);
        mIsFlushing = false;
    }
    private List<Product> GetNextFlushTargets(int cnt)
    {
        List<Product> products = new List<Product>();
        for (int y = 0; y < CountY; ++y)
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

    IEnumerator StartFinishing(bool isSuccess)
    {
        mIsFinished = true;
        while (true)
        {
            if (IsIdle)
            {
                if (FieldType == GameFieldType.Stage)
                {
                    if (isSuccess)
                    {
                        EventFinishPre?.Invoke(isSuccess);
                        yield return new WaitForSeconds(1);

                        RewardRemainLimits();
                        while (!IsIdle)
                            yield return null; //마지막 보상 스킬들 루틴 끝날때까지 기달...

                        yield return new WaitForSeconds(0.5f);
                        MenuInformBox.PopUp("MISSION COMPLETE!!");
                    }
                    else if (mStageInfo.MoveLimit > 0)
                        MenuInformBox.PopUp("MOVE LIMITTED");
                    else if (mStageInfo.TimeLimit > 0)
                        MenuInformBox.PopUp("TIMEOUT");
                    else
                        MenuInformBox.PopUp("GAME OVER");
                }
                else
                {
                    if (isSuccess)
                        MenuInformBox.PopUp("YOU WIN");
                    else
                        MenuInformBox.PopUp("YOU LOSE");
                }

                yield return new WaitForSeconds(UserSetting.InfoBoxDisplayTime);

                EventFinish?.Invoke(isSuccess);
                CleanUpGame();
                break;
            }

            yield return null;
        }
    }
    IEnumerator CheckFinishStageMode()
    {
        while (true)
        {
            if (mStageInfo.TimeLimit > 0)
            {
                float remainTime = mStageInfo.TimeLimit - PlayTime;
                EventRemainTime?.Invoke((int)remainTime);
                if (remainTime < 0)
                {
                    StartCoroutine("StartFinishing", false);
                    break;
                }
            }

            if (IsAchieveGoals())
            {
                StartCoroutine("StartFinishing", true);
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
                if (Billboard.CurrentScore >= targetCount)
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
    private void RewardRemainLimits()
    {
        float totalLimits = 0;
        if (mStageInfo.TimeLimit > 0)
        {
            totalLimits = mStageInfo.TimeLimit - PlayTime;
        }
        else
        {
            totalLimits = mStageInfo.MoveLimit - Billboard.MoveCount;
        }

        int remains = Mathf.Min((int)totalLimits, mFrames.Length / 2);
        Frame[] frames = GetRandomIdleFrames(remains);
        Product[] pros = ToProducts(frames);
        float step = totalLimits / pros.Length;

        StartCoroutine(StartElectronicEffect(new Vector3(0, 3.5f, 0), pros,
            (pro) => {
                int ran = UnityEngine.Random.Range(0, 3);
                if (ran == 0)
                    pro.ChangeProductImage(ProductSkill.Horizontal);
                else if (ran == 1)
                    pro.ChangeProductImage(ProductSkill.Vertical);
                else
                    pro.ChangeProductImage(ProductSkill.Bomb);

                totalLimits -= step;
                int limit = Mathf.Max(0, (int)totalLimits);
                MenuInGame.Inst().Limit.text = limit.ToString();
            },
            () => {
                MenuInGame.Inst().Limit.text = "0";
                StartCoroutine(DestroySkillSimpleLoop());
            }));
    }

    #endregion


    #region Utility
    private void InitFrameBorders()
    {
        for (int x = 0; x < CountX; ++x)
        {
            for (int y = 0; y < CountY; ++y)
            {
                Frame curFrame = mFrames[x, y];
                if (curFrame.Empty)
                    continue;

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
                    if (vg == null)
                    {
                        vg = new GameObject().AddComponent<VerticalFrames>();
                        vg.name = vgName;
                        vg.transform.SetParent(transform);
                        vf.Add(vg);

                        GameObject ground = Instantiate(GroundPrefab, vg.transform);
                        ground.name = "ground";
                        ground.transform.position = curFrame.transform.position - new Vector3(0, GridSize, 0);
                        if (FieldType == GameFieldType.pvpOpponent)
                            ground.layer = LayerMask.NameToLayer("ProductOpp");
                    }
                    curFrame.transform.SetParent(vg.transform);
                }
            }
        }

        int maskOrder = 0;
        foreach (VerticalFrames group in vf)
        {
            group.init(maskOrder);
            maskOrder++;
        }

        mVerticalFrames = vf.ToArray();
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

                if (mFrames[x, y].Empty)
                    continue;

                Product pro = mFrames[x, y].ChildProduct;
                if (pro != null && !pro.IsLocked && !pro.IsChocoBlock)
                    rets.Add(pro);
            }
        }
        return rets.ToArray();
    }

    private Product[] ToProducts(Frame[] frames)
    {
        List<Product> products = new List<Product>();
        foreach (Frame frame in frames)
            if (frame.ChildProduct != null)
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
    public int NextMatchCount(Product pro, SwipeDirection dir)
    {
        Product target = pro.Dir(dir);
        if (target == null || target.Color == pro.Color || target.IsChocoBlock || target.Skill != ProductSkill.Nothing)
            return 0;

        List<Product> matches = new List<Product>();
        Product[] pros = target.GetAroundProducts(target.ParentFrame);
        foreach (Product each in pros)
        {
            if (each == pro)
                continue;

            each.SearchMatchedProducts(matches, pro.Color);
        }
        return matches.Count;
    }
    private ProductSkill CheckSkillable(Product[] matches)
    {
        if (matches.Length <= UserSetting.MatchCount + 1)
            return ProductSkill.Nothing;

        if (matches.Length >= UserSetting.MatchCount + 3)
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
        mPrevIdleState = true;
        mRequestDrop = false;
        mRandomSeed = null;
        mStartTime = 0;
        mProductCount = 0;

        ProductIDs.Clear();
        Billboard.Reset();
        mNetMessages.Clear();

        mVerticalFrames = null;
        mFrames = null;
        mUserInfo = null;
        mStageInfo = null;

        SwipeDetector comp = GetComponent<SwipeDetector>();
        if (comp != null)
        {
            comp.EventSwipe = null;
            comp.EventClick = null;
        }

        StopAllCoroutines();
        CancelInvoke();
    }
    private void RemoveLimit()
    {
        if (mStageInfo.MoveLimit > 0)
        {
            Billboard.MoveCount++;
            EventReduceLimit?.Invoke();
            if (Billboard.MoveCount >= mStageInfo.MoveLimit)
                StartCoroutine("StartFinishing", false);
        }
    }
    private Frame[] GetRandomIdleFrames(int count)
    {
        Dictionary<int, Frame> rets = new Dictionary<int, Frame>();
        int totalCount = CountX * CountY;
        int loopCount = 0;
        while (rets.Count < count && loopCount < totalCount)
        {
            loopCount++;
            int ranIdx = UnityEngine.Random.Range(0, totalCount);
            if (rets.ContainsKey(ranIdx))
                continue;

            int idxX = ranIdx % CountX;
            int idxY = ranIdx / CountX;
            Product pro = mFrames[idxX, idxY].ChildProduct;
            if (pro == null || pro.IsLocked || pro.IsChocoBlock || pro.Skill != ProductSkill.Nothing)
                continue;

            rets[ranIdx] = pro.ParentFrame;
        }

        return new List<Frame>(rets.Values).ToArray();
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
    private Frame FrameOfWorldPos(float worldPosX, float worldPosY)
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
    private void ShakeField(float intensity)
    {
        StopCoroutine("AnimShakeField");
        StartCoroutine("AnimShakeField", intensity);
    }
    IEnumerator AnimShakeField(float intensity)
    {
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
    }
    IEnumerator StartElectronicEffect(Vector3 _startPos, Product[] pros, Action<Product> eventTurn, Action eventEnd)
    {
        mItemLooping = true;
        Vector3 startPos = _startPos;
        while (true)
        {
            int idx = 0;
            bool isDone = true;
            Product cur = null;
            for (idx = 0; idx < pros.Length; ++idx)
            {
                if (pros[idx] == null)
                    continue;
                else if (pros[idx].IsLocked)
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

            if (cur != null)
            {
                CreateLaserEffect(startPos, cur.transform.position);
                eventTurn?.Invoke(cur);
                pros[idx] = null;
            }

            yield return new WaitForSeconds(0.1f);
        }

        mItemLooping = false;
        eventEnd?.Invoke();
    }
    IEnumerator UpDownSizing(GameObject obj, float duration)
    {
        float time = 0;
        Vector2 oriSize = obj.transform.localScale;
        Vector2 curSize = oriSize;
        while (time < duration)
        {
            float halfTime = duration / 2;
            float rate = (-1 / (halfTime * halfTime)) * (time - halfTime) * (time - halfTime) + 1;
            curSize = oriSize * (1 + rate);
            obj.transform.localScale = new Vector3(curSize.x, curSize.y, 1);
            yield return null;
            time += Time.deltaTime;
        }
        obj.transform.localScale = new Vector3(oriSize.x, oriSize.y, 1);
    }
    private void CreateSkillEffect(Product pro)
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
    private void CreateLaserEffect(Vector2 startPos, Vector2 destPos)
    {
        SoundPlayer.Inst.PlaySoundEffect(ClipSound.Skill3);
        Vector3 start = new Vector3(startPos.x, startPos.y, -4.0f);
        Vector3 dest = new Vector3(destPos.x, destPos.y, -4.0f);
        GameObject laserObj = GameObject.Instantiate(LaserParticle, start, Quaternion.identity, transform);
        laserObj.GetComponent<EffectLaser>().SetDestination(dest);
    }
    private void CreateStripeEffect(Vector2 startPos, bool isVertical)
    {
        SoundPlayer.Inst.PlaySoundEffect(ClipSound.Skill1);
        Vector3 start = new Vector3(startPos.x, startPos.y, -4.0f);
        GameObject obj = GameObject.Instantiate(StripeParticle, start, isVertical ? Quaternion.Euler(0, 0, 90) : Quaternion.identity, transform);
        Destroy(obj, 1.0f);
    }
    private void CreateExplosionEffect(Vector2 startPos)
    {
        SoundPlayer.Inst.PlaySoundEffect(ClipSound.Skill2);
        Vector3 start = new Vector3(startPos.x, startPos.y, -4.0f);
        GameObject obj = GameObject.Instantiate(ExplosionParticle, start, Quaternion.identity, transform);
        Destroy(obj, 1.0f);
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
                Opponent.StartCoroutine("StartFinishing", true);
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
                    pro.gameObject.layer = LayerMask.NameToLayer("ProductOpp");
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
                    if(body.skill == ProductSkill.Nothing)
                        EventCombo?.Invoke(0);

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
                    EventCombo?.Invoke(Billboard.CurrentCombo);

                    int score = body.combo * body.ArrayCount;

                    int preAttackCount = Billboard.CurrentScore / UserSetting.ScorePerAttack;
                    int curAttackCount = (Billboard.CurrentScore + score) / UserSetting.ScorePerAttack;

                    Billboard.CurrentScore += score;
                    Billboard.DestroyCount += body.ArrayCount;

                    Attack(curAttackCount - preAttackCount, products[0].transform.position);

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
                            newPro.gameObject.layer = LayerMask.NameToLayer("ProductOpp");
                            newPro.InstanceID = body.products[idx].nextInstID;
                            ProductIDs[newPro.InstanceID] = newPro;
                            //newPro.transform.SetParent(parentFrame.VertFrames.transform);
                            parentFrame.VertFrames.AddNewProduct(newPro);
                            newPro.EnableMasking(parentFrame.VertFrames.MaskOrder);
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
                                newPro.gameObject.layer = LayerMask.NameToLayer("ProductOpp");
                                newPro.InstanceID = body.products[idx].nextInstID;
                                ProductIDs[newPro.InstanceID] = newPro;
                                //newPro.transform.SetParent(parentFrame.VertFrames.transform);
                                parentFrame.VertFrames.AddNewProduct(newPro);
                                newPro.EnableMasking(parentFrame.VertFrames.MaskOrder);
                            }
                        }
                        mRequestDrop = true;
                        EventMatched?.Invoke(products.ToArray());
                    }

                    mNetMessages.RemoveFirst();
                }
            }
            else if (body.cmd == PVPCommand.BreakIce)
            {
                List<Product> products = new List<Product>();
                for (int i = 0; i < body.ArrayCount; ++i)
                {
                    ProductInfo info = body.products[i];
                    Product pro = ProductIDs[info.prvInstID];
                    if (pro != null && !pro.IsLocked && pro.Color == info.prvColor)
                        products.Add(pro);
                }

                if (products.Count == body.ArrayCount)
                {
                    foreach (Product pro in products)
                        pro.BreakChocoBlock(body.combo);

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
    private void Network_BreakIce(ProductInfo[] pros)
    {
        if (FieldType != GameFieldType.pvpPlayer)
            return;

        PVPInfo req = new PVPInfo();
        req.cmd = PVPCommand.BreakIce;
        req.oppUserPk = InstPVP_Opponent.UserPk;
        req.combo = Billboard.CurrentCombo;
        req.ArrayCount = pros.Length;
        Array.Copy(pros, req.products, pros.Length);
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