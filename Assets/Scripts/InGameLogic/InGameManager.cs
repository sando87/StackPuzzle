using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

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
    public const string vgGround = "ground";

    public Sprite[] BackgroundImages;
    public Sprite[] PvpIceBlocks;
    public SpriteRenderer BackgroundSprite;
    public GameObject[] ProductPrefabs;
    public GameObject FramePrefab1;
    public GameObject FramePrefab2;
    public GameObject ObstaclePrefab;
    public GameObject ClosePrefab;
    public GameObject GroundPrefab;
    public GameObject SimpleSpritePrefab;
    public GameObject TrailingPrefab;
    public GameObject MissilePrefab;
    public GameObject MeteorPrefab;
    public Rocket LineRocketPrefab;
    public GameObject HammerPrefab;
    public GameObject HammerHitPrefab;

    public GameObject SmokeParticle;
    public GameObject ExplosionParticle;
    public GameObject StripeParticle;
    public GameObject LaserParticle;
    public AttackPoints AttackPointFrame;
    public GameObject AttackBullet;
    public GameObject ScoreTextDest;

    private Frame[,] mFrames = null;
    private StageInfo mStageInfo = null;
    private UserInfo mUserInfo = null;
    private bool mIsFinished = false;
    private bool mIsItemEffect = false;
    private bool mIsAutoMatching = false;
    private bool mItemLooping = false;
    private bool mIsLightningSkill = false;
    private bool mIsDropping = false;
    private bool mIsUserEventLock = false;
    private bool mIsFlushing = false;
    private bool mPrevIdleState = false;
    private int mDropLockCount = 0;
    public bool IsDroppable {get { return mDropLockCount == 0; } }
    private bool mUseCombo = false;
    private float mStartTime = 0;
    private float mSFXVolume = 1;
    private int mPVPTimerCounter = 0;
    private Vector3 mStartPos = Vector3.zero;
    private System.Random mRandomSeed = null;
    private int mStartRandomSeed = -1;
    private VerticalFrames[] mVerticalFrames = null;

    private bool mIsWorkingCycle = false;
    private List<DelayedCall> mWorkerList = new List<DelayedCall>();
    private LinkedList<PVPInfo> mNetMessages = new LinkedList<PVPInfo>();

    public float SFXVolume { get { return mSFXVolume; } }
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
    public bool IsIdle { get { return IsDroppable && !mIsDropping && !mIsUserEventLock && !mIsFlushing && !mItemLooping && !mIsAutoMatching && !mIsItemEffect && !mIsLightningSkill && mStageInfo != null && !mIsWorkingCycle; } }
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
    public MatchingLevel Difficulty { get { return mStageInfo.Difficulty; } }
    public Rect FieldWorldRect    {
        get {
            Rect rect = new Rect(Vector2.zero, new Vector2(GridSize * CountX, GridSize * CountY));
            rect.center = transform.position;
            return rect;
        }
    }


    public Action<Vector3, StageGoalType> EventBreakTarget;
    public Action<int> EventScore;
    public Action<int, float> EventReward;
    public Action<bool> EventFinish;
    public Action<bool> EventFinishPre;
    public Action<bool> EventFinishFirst;
    public Action<int> EventCombo;
    public Action<int> EventRemainTime;
    public Action EventReduceLimit;
    public Action EventEnterIdle;

    private void Update()
    {
        if (mStageInfo == null)
            return;

        if (IsIdle && !mPrevIdleState && IsAllProductIdle())
        {
            EventEnterIdle?.Invoke();
            if (mUseCombo && !mIsFinished)
            {
                mUseCombo = false;
                ComboReset();
            }
        }

        mPrevIdleState = IsIdle;
        //DropNextProducts();
    }
    public void StartGameInStageMode(StageInfo info, UserInfo userInfo)
    {
        MenuInformBox.PopUp("START!!");
        StartGame(info, userInfo);
        if (info.TimeLimit > 0)
            SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectCooltime, mSFXVolume);

        mIsUserEventLock = true;
        StartCoroutine(UnityUtils.CallAfterSeconds(UserSetting.InfoBoxDisplayTime, () =>
        {
            mStartTime = Time.realtimeSinceStartup;
            GetComponent<SwipeDetector>().EventSwipe = OnSwipe;
            GetComponent<SwipeDetector>().EventClick = OnClick;
            EventEnterIdle = CheckIsFinishedInStageMode;
            StartCoroutine(RefreshTimer());

            mIsUserEventLock = false;
            Animator anim = GetComponent<Animator>();
            if (anim != null)
                anim.enabled = false;
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
            StartCoroutine(CheckFinishPvpTimer());
            mIsUserEventLock = false;
            Animator anim = GetComponent<Animator>();
            if (anim != null)
                anim.enabled = false;
        }));
    }
    public void StartGameInPVPOpponent(StageInfo info, UserInfo userInfo)
    {
        StartGame(info, userInfo);

        mSFXVolume = 0.2f;
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
        mStartRandomSeed = info.RandomSeed == -1 ? (int)DateTime.Now.Ticks : info.RandomSeed;
        mRandomSeed = new System.Random(mStartRandomSeed);
        mStartPos = transform.position;

        int stageCountPerTheme = 20;
        int themeCount = 9;
        int backImgIdx = ((mStageInfo.Num - 1) % (stageCountPerTheme * themeCount)) / stageCountPerTheme;
        backImgIdx = Math.Min(backImgIdx, BackgroundImages.Length - 1);
        BackgroundSprite.sprite = BackgroundImages[backImgIdx];
        float scale = Camera.main.orthographicSize / 1.28f; //1.28f는 배경 이미지 height의 절반
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
                //frameObj.GetComponent<SpriteRenderer>().sortingLayerName = FieldType == GameFieldType.pvpOpponent ? "ProductOpp" : "Default";
                frameObj.transform.localPosition = localBasePos + localFramePos;
                mFrames[x, y] = frameObj.GetComponent<Frame>();
                StageInfoCell cellInfo = GetCellInversed(x, y);
                mFrames[x, y].Initialize(this, x, y, cellInfo.IsDisabled, cellInfo.CoverCount, cellInfo.BushCount);
                mFrames[x, y].EventBreakCover = (frame) => {
                    Billboard.CoverCount++;
                    EventBreakTarget?.Invoke(frame.transform.position, StageGoalType.Cover);
                };
                mFrames[x, y].EventBreakBush = (frame) => {
                    Billboard.BushCount++;
                    EventBreakTarget?.Invoke(frame.transform.position, StageGoalType.Bush);
                };
                mFrames[x, y].EventScoreText = (score) => {
                    EventScore?.Invoke(score);
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

                StageInfoCell cellInfo = GetCellInversed(x, y);
                int chocoCount = cellInfo.ChocoCount;
                if(chocoCount > 0)
                    pro.IcedBlock.SetDepth(chocoCount);

                pro.InitCap(cellInfo.CapCount);
                pro.EventUnWrapChoco = () => {
                    Billboard.ChocoCount++;
                    EventBreakTarget?.Invoke(pro.transform.position, StageGoalType.Choco);
                };
                pro.EventUnWrapCap = () => {
                    Billboard.CapCount++;
                    EventBreakTarget?.Invoke(pro.transform.position, StageGoalType.Cap);
                };
                initProducts.Add(pro);
            }
        }

        Network_StartGame(Serialize(initProducts.ToArray()));
    }
    private StageInfoCell GetCellInversed(int xIdx, int yIdx)
    {
        return mStageInfo.GetCell(xIdx, mStageInfo.YCount - 1 - yIdx);
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
        if (pro.ParentFrame.IsCovered)
            return;

        if(pro.Skill != ProductSkill.Nothing)
        {
            Network_Click(pro);
            mUseCombo = true;
            RemoveLimit();
            CastSkillProduct(pro);
            //DestroyProducts(new Product[] { pro });
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
                Network_Click(pro);
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
        if (product.IsLocked || product.IsChocoBlock || product.ParentFrame.IsCovered)
            return;

        Product targetProduct = null;
        switch (dir)
        {
            case SwipeDirection.UP: targetProduct = product.Up(); break;
            case SwipeDirection.DOWN: targetProduct = product.Down(); break;
            case SwipeDirection.LEFT: targetProduct = product.Left(); break;
            case SwipeDirection.RIGHT: targetProduct = product.Right(); break;
        }

        if (targetProduct == null || targetProduct.IsLocked || targetProduct.IsChocoBlock || targetProduct.ParentFrame.IsCovered)
            return;

        if (product.Skill != ProductSkill.Nothing && targetProduct.Skill != ProductSkill.Nothing)
        {
            mUseCombo = true;
            mIsUserEventLock = true;
            bool isSameColor = product.Skill == ProductSkill.SameColor || targetProduct.Skill == ProductSkill.SameColor;
            Network_Swipe(product, dir);
            product.Swipe(targetProduct, () => {
                if (isSameColor)
                {
                    mIsUserEventLock = false;
                    DestroySkillWithSamecolor(product, targetProduct);
                }
                else
                {
                    StartCoroutine(UpDownSizing(product.gameObject, 0.3f));
                    StartCoroutine(UpDownSizing(targetProduct.gameObject, 0.3f));
                    StartCoroutine(UnityUtils.CallAfterSeconds(0.3f, () =>
                    {
                        mIsUserEventLock = false;
                        ShakeField(0.03f);
                        DestroySkillNormal_Normal(product, targetProduct);
                    }));
                }
            });
        }
        else if (product.Skill != ProductSkill.Nothing || targetProduct.Skill != ProductSkill.Nothing)
        {
            mUseCombo = true;
            mIsUserEventLock = true;
            Network_Swipe(product, dir);
            product.Swipe(targetProduct, () =>
            {
                mIsUserEventLock = false;
                CastSkillProduct(product.Skill != ProductSkill.Nothing ? product : targetProduct);
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

        SoundPlayer.Inst.PlaySoundEffect(ClipSound.Swipe, mSFXVolume);
        RemoveLimit();
    }

    #region MatchingLogic
    IEnumerator DoMatchingCycle(Product[] firstMatches)
    {
        mDropLockCount++;
        ComboReset();

        List<Product[]> matchableGroups = new List<Product[]>();
        matchableGroups.Add(firstMatches);

        while(true)
        {
            foreach(Product[] pros in matchableGroups)
            {
                LockToMatch(pros);
            }

            yield return new WaitForSeconds(0.3f);

            // 주변 블럭 리스트 얻어옴
            List<Product> aroundPros = new List<Product>();
            foreach (Product[] pros in matchableGroups)
            {
                FindAroundProducts(pros, aroundPros);
            }
            Product[] aroundProducts = aroundPros.ToArray();

            //주변 블럭 정보 가져왔으면 실제로 터트리는 기능 수행
            foreach (Product[] pros in matchableGroups)
            {
                DoMatchProducts(pros);
            }
            matchableGroups.Clear();

            //터질때 주변 장해물 파괴
            BreakObstacles(aroundProducts);

            //터질때 주변에 매칭가능하면 연쇄하여 파괴하며 콤보 올라감
            matchableGroups = FindMatchedProducts(aroundProducts);
            if (matchableGroups.Count > 0)
            {
                // 콤보 올리고 기타 정보도 수정
                ComboUp();
                Billboard.MaxCombo = Math.Max(Billboard.CurrentCombo, Billboard.MaxCombo);
                Billboard.ComboCounter[Billboard.CurrentCombo]++;

                SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectMatched, mSFXVolume);
            }
            else
            {
                //주변 매칭가능한 블럭들이 없으면 종료
                break;
            }
        }

        StartToDrop();
        mDropLockCount--;
    }

    private void DoMatchProducts(Product[] pros)
    {
        ProductSkill nextSkill = CheckSkillable(pros);
        if (nextSkill == ProductSkill.Nothing)
        {
            foreach (Product destPro in pros)
            {
                destPro.DestroyImmediately(Billboard.CurrentCombo);
            }
        }
        else
        {
            foreach (Product destPro in pros)
            {
                destPro.MergeImImmediately(pros[0], nextSkill);
            }
        }
    }
    private void LockToMatch(Product[] pros)
    {
        ProductSkill nextSkill = CheckSkillable(pros);
        if (nextSkill == ProductSkill.Nothing)
        {
            foreach (Product destPro in pros)
            {
                destPro.ReadyForDestroy(Billboard.CurrentCombo);
            }
        }
        else
        {
            foreach (Product destPro in pros)
            {
                destPro.ReadyForMerge(Billboard.CurrentCombo);
            }
        }

        AcquireScore(Billboard.CurrentCombo * pros.Length, pros[0].transform.position);
    }
    private void BreakObstacles(Product[] pros)
    {
        foreach (Product pro in pros)
        {
            if (IsObstacled(pro.ParentFrame))
            {
                BreakObstacle(pro.ParentFrame);
            }
        }

    }

    private void AcquireScore(int score, Vector3 position)
    {
        DoAttack(Billboard.CurrentScore, Billboard.CurrentScore + score, position);

        Billboard.CurrentScore += score;

        EventBreakTarget?.Invoke(Vector3.zero, StageGoalType.Score);
    }
    private void DoAttack(int fromScore, int toScore, Vector3 position)
    {
        int spa = Mathf.Max(10, UserSetting.ScorePerAttack - (10 * mPVPTimerCounter));
        int preAttackCount = fromScore / spa;
        int curAttackCount = toScore / spa;
        Attack(curAttackCount - preAttackCount, position);
    }

    public enum DelayedCallRet { Keep, Done }
    public class DelayedCall
    {
        // tick 은 모두 0.1f 초마다 1씩 증가
        public int tickTotalCount = 0;
        public int tickCurrentCount = 0;
        public int tickDelayRef = 0;
        public int tickStepRef = 0;
        public int callCount = 0;

        // bool Function(int param); 형태
        // 반환값이 true이면 worker중지, 입력 int값은 callcount를 인자로 넣어줌
        public System.Func<int, DelayedCallRet> callback = null;

        public DelayedCall(int delayTick, int stepTick, System.Func<int, DelayedCallRet> _callback)
        {
            this.tickTotalCount = 0;
            this.tickCurrentCount = 0;
            this.tickDelayRef = delayTick;
            this.tickStepRef = stepTick;
            this.callCount = 0;
            this.callback = _callback;
        }
    }


    // delayTick은 처음 callback함수가 실행되기까지 지연시간(tick당 0.1초)
    // stepTick은 callback함수가 실행되는 간격(tick당 0.1초)
    private void AddWorker(int delayTick, int stepTick, System.Func<int, DelayedCallRet> callback)
    {
        mWorkerList.Add(new DelayedCall(delayTick, stepTick, callback));
        if(!mIsWorkingCycle)
        {
            StartCoroutine(DoWorkerCycle());
        }
    }

    private void StartToDrop()
    {
        AddWorker(0, 1, (tick) => { return DelayedCallRet.Done; });
    }
    

    IEnumerator DoWorkerCycle()
    {
        mIsUserEventLock = true;
        mIsWorkingCycle = true;
        
        yield return new WaitForSeconds(0.1f);

        while(mWorkerList.Count > 0)
        {
            DelayedCall[] workers = mWorkerList.ToArray();
            foreach (DelayedCall worker in workers)
            {
                worker.tickTotalCount++;

                // 초기 딜레이 시간보다 클때까지 기다림
                if(worker.tickTotalCount >= worker.tickDelayRef)
                {
                    // 함수 호출 간격마다 callback 호출
                    if(worker.tickCurrentCount == 0)
                    {
                        DelayedCallRet ret = worker.callback(worker.callCount);
                        worker.callCount++;
                        if (ret == DelayedCallRet.Done)
                        {
                            mWorkerList.Remove(worker);
                        }
                    }
                    worker.tickCurrentCount = (worker.tickCurrentCount + 1) % worker.tickStepRef;
                }
                
            }

            // 드랍 프로세스 시작
            Product[] droppingPros = DoDropProcess();
            if(droppingPros.Length > 0)
            {
                // 떨어지는 동안의 delay 후
                AddWorker(3, 3, (tick) =>
                {
                    // EndDrop 처리
                    foreach (Product droppingPro in droppingPros)
                        droppingPro.DropEnd();

                    // 매치가능한 블럭들이 있으면 재매칭 수행
                    TryMatchAfterDrop(droppingPros);
                    return DelayedCallRet.Done;
                });
            }

            yield return new WaitForSeconds(0.1f);
        }

        mIsWorkingCycle = false;
        mIsUserEventLock = false;
    }

    private Product[] DoDropProcess()
    {
        List<Product> pros = new List<Product>();
        foreach (VerticalFrames vf in mVerticalFrames)
        {
            if (vf.IsDroppable())
            {
                // 비어있는 프레임에 새로운 블럭 생성
                CreateNewProducts(vf);

                // 떨어지기 시작
                Product[] droppingPros = vf.StartToDrop();

                if (droppingPros != null && droppingPros.Length > 0)
                {
                    pros.AddRange(droppingPros);
                }
            }
        }
        return pros.ToArray();
    }
    private Product[] CreateNewProducts(VerticalFrames vf)
    {
        List<Product> newPros = new List<Product>();
        foreach(Frame frame in vf.Frames)
        {
            if(frame.ChildProduct == null)
            {
                Product newPro = CreateNewProduct();
                vf.AddNewProduct(newPro);
                newPro.EnableMasking(vf.MaskOrder);
            }
        }
        return newPros.ToArray();
    }
    private bool TryMatchAfterDrop(Product[] droppingPros)
    {
        //매칭 가능한 블록들이 있는지 찾는 기능 수행
        List<Product[]> matches = FindMatchedProducts(droppingPros, UserSetting.MatchCount + 1);
        if (matches.Count > 0)
        {
            // 매치가능한 블럭들이 있다면 매치 수행

            // 깜빡이는 연출 효과
            foreach (Product[] pros in matches)
            {
                LockToMatch(pros);
            }

            //0.3초뒤에 재매칭
            AddWorker(3, 3, (int cnt) =>
            {
                // 실제 블럭이 파괴되는 로직 수행
                foreach (Product[] pros in matches)
                {
                    DoMatchProducts(pros);
                }
                return DelayedCallRet.Done;
            });
            return true;
        }
        return false;
    }

    private void TryDestroy(Product pro, bool isDelay)
    {
        if(IsObstacled(pro.ParentFrame))
        {
            BreakObstacle(pro.ParentFrame);
        }
        else if(pro.IsSkillable)
        {
            CastSkillProduct(pro);
        }
        else
        {
            if(isDelay)
            {
                AcquireScore(Billboard.CurrentCombo, pro.transform.position);
                pro.ReadyForDestroy(Billboard.CurrentCombo);
                AddWorker(3, 3, (tick) =>
                {
                    pro.DestroyImmediately(Billboard.CurrentCombo);
                    return DelayedCallRet.Done;
                });
            }
            else
            {
                AcquireScore(Billboard.CurrentCombo, pro.transform.position);
                pro.DestroyImmediately(Billboard.CurrentCombo);
            }
        }
    }
    private void CastSkillProduct(Product target)
    {
        if (target.SkillCasted)
            return;

        if (target.Skill == ProductSkill.Horizontal)
        {
            CastHorizontalProduct(target);
        }
        else if (target.Skill == ProductSkill.Vertical)
        {
            CastVerticalProduct(target);
        }
        else if (target.Skill == ProductSkill.Bomb)
        {
            CastBombProduct(target);
        }
        else if (target.Skill == ProductSkill.SameColor)
        {
            CastRainbowProduct(target);
        }
        else if (target.Skill == ProductSkill.Hammer)
        {
            CastHammerProduct(target);
        }
    }

    private void CastHorizontalProduct(Product pro)
    {
        pro.SkillCasted = true;

        Frame startFrame = pro.ParentFrame;
        Vector3 startPosition = startFrame.transform.position;
        Vector3 rightEndPosition = startFrame.MostRight().transform.position;
        Vector3 leftEndPosition = startFrame.MostLeft().transform.position;
        float maxDistance = Mathf.Max(startPosition.x - leftEndPosition.x, rightEndPosition.x - startPosition.x);
        maxDistance += GridSize;
        rightEndPosition.x = startPosition.x + maxDistance;
        leftEndPosition.x = startPosition.x - maxDistance;
        float speed = GridSize / 0.1f;
        float duration = maxDistance / speed;

        CreateRocketEffect(startPosition, rightEndPosition, duration);
        CreateRocketEffect(startPosition, leftEndPosition, duration);

        TryDestroy(pro, false);

        AddWorker(1, 1, (cnt) =>
        {
            int offIdx = cnt + 1;
            Frame leftFrame = startFrame.Left(offIdx);
            if (leftFrame != null)
            {
                Product leftPro = leftFrame.ChildProduct;
                if (leftPro != null && !leftPro.IsLocked)
                {
                    TryDestroy(leftPro, false);
                }
            }

            Frame rightFrame = startFrame.Right(offIdx);
            if (rightFrame != null)
            {
                Product rightPro = rightFrame.ChildProduct;
                if (rightPro != null && !rightPro.IsLocked)
                {
                    TryDestroy(rightPro, false);
                }
            }

            if (startFrame.IndexX - offIdx < 0 && startFrame.IndexX + offIdx >= CountX)
            {
                return DelayedCallRet.Done;
            }

            return DelayedCallRet.Keep;
        });
    }
    private void CastHorizontalProduct(Frame frame)
    {
        Frame startFrame = frame;
        Product pro = frame.ChildProduct;
        Vector3 startPosition = startFrame.transform.position;
        Vector3 rightEndPosition = startFrame.MostRight().transform.position;
        Vector3 leftEndPosition = startFrame.MostLeft().transform.position;
        float maxDistance = Mathf.Max(startPosition.x - leftEndPosition.x, rightEndPosition.x - startPosition.x);
        maxDistance += GridSize;
        rightEndPosition.x = startPosition.x + maxDistance;
        leftEndPosition.x = startPosition.x - maxDistance;
        float speed = GridSize / 0.1f;
        float duration = maxDistance / speed;

        CreateRocketEffect(startPosition, rightEndPosition, duration);
        CreateRocketEffect(startPosition, leftEndPosition, duration);

        if(pro != null && !pro.IsLocked)
            TryDestroy(pro, false);

        AddWorker(1, 1, (cnt) =>
        {
            int offIdx = cnt + 1;
            Frame leftFrame = startFrame.Left(offIdx);
            if (leftFrame != null)
            {
                Product leftPro = leftFrame.ChildProduct;
                if (leftPro != null && !leftPro.IsLocked)
                {
                    TryDestroy(leftPro, false);
                }
            }

            Frame rightFrame = startFrame.Right(offIdx);
            if (rightFrame != null)
            {
                Product rightPro = rightFrame.ChildProduct;
                if (rightPro != null && !rightPro.IsLocked)
                {
                    TryDestroy(rightPro, false);
                }
            }

            if (startFrame.IndexX - offIdx < 0 && startFrame.IndexX + offIdx >= CountX)
            {
                return DelayedCallRet.Done;
            }

            return DelayedCallRet.Keep;
        });
    }
    private void CastVerticalProduct(Product pro)
    {
        pro.SkillCasted = true;
        Frame startFrame = pro.ParentFrame;
        startFrame.VertFrames.HoldCount++;

        Vector3 startPosition = startFrame.transform.position;
        Vector3 topEndPosition = startFrame.MostUp().transform.position;
        Vector3 bottomEndPosition = startFrame.MostDown().transform.position;
        float maxDistance = Mathf.Max(startPosition.y - bottomEndPosition.y, topEndPosition.y - startPosition.y);
        maxDistance += GridSize;
        topEndPosition.y = startPosition.y + maxDistance;
        bottomEndPosition.y = startPosition.y - maxDistance;
        float speed = GridSize / 0.1f;
        float duration = maxDistance / speed;

        CreateRocketEffect(startPosition, topEndPosition, duration);
        CreateRocketEffect(startPosition, bottomEndPosition, duration);

        TryDestroy(pro, false);

        AddWorker(1, 1, (cnt) =>
        {
            int offIdx = cnt + 1;
            Frame upFrame = startFrame.Up(offIdx);
            if (upFrame != null)
            {
                Product upPro = upFrame.ChildProduct;
                if (upPro != null && !upPro.IsLocked)
                {
                    TryDestroy(upPro, false);
                }
            }

            Frame downFrame = startFrame.Down(offIdx);
            if (downFrame != null)
            {
                Product downPro = downFrame.ChildProduct;
                if (downPro != null && !downPro.IsLocked)
                {
                    TryDestroy(downPro, false);
                }
            }

            if (startFrame.IndexY - offIdx < 0 && startFrame.IndexY + offIdx >= CountY)
            {
                startFrame.VertFrames.HoldCount--;
                return DelayedCallRet.Done;
            }

            return DelayedCallRet.Keep;
        });
    }
    private void CastBombProduct(Product pro)
    {
        pro.SkillCasted = true;
        pro.Animation.Play("destroy");
        AddWorker(3, 3, (cnt) =>
        {
            CreateExplosionEffect(pro.transform.position);
            Product[] arPros = ScanAroundProducts(pro, 1);
            TryDestroy(pro, false);
            foreach(Product arPro in arPros)
            {
                TryDestroy(arPro, true);
            }
            return DelayedCallRet.Done;
        });
    }
    private void CastRainbowProduct(Product pro)
    {
        pro.SkillCasted = true;
        pro.Animation.Play("destroy");
        Vector3 startPos = pro.transform.position;
        Product[] samePros = null;
        AddWorker(3, 1, (idx) =>
        {
            if(samePros == null)
            {
                ShakeField(0.05f);
                SoundPlayer.Inst.PlaySoundEffect(ClipSound.Skill2, mSFXVolume);
                samePros = FindNormalSameColor(pro.Color);
                TryDestroy(pro, false);
                if(samePros == null)
                {
                    return DelayedCallRet.Done;
                }
            }

            if(idx < samePros.Length)
            {
                Product curPro = samePros[idx];
                if(curPro != null && !curPro.IsLocked)
                {
                    CreateLaserEffect(startPos, curPro.transform.position);
                    TryDestroy(curPro, true);
                }
                
                return DelayedCallRet.Keep;
            }
            else
            {
                return DelayedCallRet.Done;
            }
        });
    }
    private void CastHammerProduct(Product pro)
    {
        pro.SkillCasted = true;
        pro.Animation.Play("destroy");

        Frame nextTarget = null;
        AddWorker(3, 9, (cnt) =>
        {
            if(cnt == 0)
            {
                // 망치가 날아갈 목적지 찾고
                nextTarget = FindHammerTarget();
                // 날아가는 연출
                CreateHammerEffect(ProductSkill.Hammer, pro.transform.position, nextTarget.transform.position, 0.9f);

                TryDestroy(pro, false);
                return DelayedCallRet.Keep;
            }
            else
            {
                Product destPro = nextTarget.ChildProduct;
                if(destPro != null && !destPro.IsLocked)
                {
                    TryDestroy(destPro, true);
                }
                return DelayedCallRet.Done;
            }
        });
    }

    private void CastSkillBomb_Stripe(Product productbomb, Product productStripe)
    {
        if (productStripe.Skill == ProductSkill.Horizontal)
        {
            productbomb.SkillCasted = true;
            productStripe.SkillCasted = true;

            Frame startFrame = productStripe.ParentFrame;
            Vector3 startPosition = startFrame.transform.position;
            Vector3 rightEndPosition = startFrame.MostRight().transform.position;
            Vector3 leftEndPosition = startFrame.MostLeft().transform.position;
            float maxDistance = Mathf.Max(startPosition.x - leftEndPosition.x, rightEndPosition.x - startPosition.x);
            maxDistance += GridSize;
            rightEndPosition.x = startPosition.x + maxDistance;
            leftEndPosition.x = startPosition.x - maxDistance;
            float speed = GridSize / 0.1f;
            float duration = maxDistance / speed;

            GameObject rocketR = CreateRocketEffect(startPosition, rightEndPosition, duration, 0.3f);
            GameObject rocketL = CreateRocketEffect(startPosition, leftEndPosition, duration, 0.3f);

            rocketR.transform.DOScale(2, 0.3f);
            rocketL.transform.DOScale(2, 0.3f);

            AddWorker(3, 1, (cnt) =>
            {
                if(cnt == 0)
                {
                    TryDestroy(productbomb, false);
                    TryDestroy(productStripe, false);
                }

                int offIdx = cnt;
                Frame leftFrame = startFrame.Left(offIdx);
                if (leftFrame != null)
                {
                    Product[] arPros = ScanAroundProducts(leftFrame, 1);
                    foreach (Product arPro in arPros)
                    {
                        TryDestroy(arPro, false);
                    }
                }

                Frame rightFrame = startFrame.Right(offIdx);
                if (rightFrame != null)
                {
                    Product[] arPros = ScanAroundProducts(rightFrame, 1);
                    foreach (Product arPro in arPros)
                    {
                        TryDestroy(arPro, false);
                    }
                }

                if (startFrame.IndexX - offIdx < 0 && startFrame.IndexX + offIdx >= CountX)
                {
                    return DelayedCallRet.Done;
                }

                return DelayedCallRet.Keep;
            });
        }
        else if (productStripe.Skill == ProductSkill.Vertical)
        {
            productbomb.SkillCasted = true;
            productStripe.SkillCasted = true;

            Frame startFrame = productStripe.ParentFrame;
            startFrame.VertFrames.HoldCount++;
            if (startFrame.IndexX > 0) startFrame.Left().VertFrames.HoldCount++;
            if (startFrame.IndexX < CountX - 1) startFrame.Right().VertFrames.HoldCount++;

            Vector3 startPosition = startFrame.transform.position;
            Vector3 topEndPosition = startFrame.MostUp().transform.position;
            Vector3 bottomEndPosition = startFrame.MostDown().transform.position;
            float maxDistance = Mathf.Max(startPosition.y - bottomEndPosition.y, topEndPosition.y - startPosition.y);
            maxDistance += GridSize;
            topEndPosition.y = startPosition.y + maxDistance;
            bottomEndPosition.y = startPosition.y - maxDistance;
            float speed = GridSize / 0.1f;
            float duration = maxDistance / speed;

            GameObject rocketT = CreateRocketEffect(startPosition, topEndPosition, duration, 0.3f);
            GameObject rocketB = CreateRocketEffect(startPosition, bottomEndPosition, duration, 0.3f);

            rocketT.transform.DOScale(2, 0.3f);
            rocketB.transform.DOScale(2, 0.3f);

            AddWorker(3, 1, (cnt) =>
            {
                if (cnt == 0)
                {
                    TryDestroy(productbomb, false);
                    TryDestroy(productStripe, false);
                }

                int offIdx = cnt;
                Frame upFrame = startFrame.Up(offIdx);
                if (upFrame != null)
                {
                    Product[] arPros = ScanAroundProducts(upFrame, 1);
                    foreach (Product arPro in arPros)
                    {
                        TryDestroy(arPro, false);
                    }
                }

                Frame downFrame = startFrame.Down(offIdx);
                if (downFrame != null)
                {
                    Product[] arPros = ScanAroundProducts(downFrame, 1);
                    foreach (Product arPro in arPros)
                    {
                        TryDestroy(arPro, false);
                    }
                }

                if (startFrame.IndexY - offIdx < 0 && startFrame.IndexY + offIdx >= CountY)
                {
                    startFrame.VertFrames.HoldCount--;
                    if (startFrame.IndexX > 0) startFrame.Left().VertFrames.HoldCount--;
                    if (startFrame.IndexX < CountX - 1) startFrame.Right().VertFrames.HoldCount--;
                    return DelayedCallRet.Done;
                }

                return DelayedCallRet.Keep;
            });
        }
    }
    private void CastSkillStripe_Stripe(Product productStripeA, Product productStripeB)
    {
        CastSkillProduct(productStripeA);
        CastSkillProduct(productStripeB);
    }
    private void CastSkillHammer_Bomb(Product productHammer, Product productBomb)
    {
        productHammer.SkillCasted = true;
        productBomb.SkillCasted = true;

        Frame nextTarget = null;
        AddWorker(1, 9, (cnt) =>
        {
            if (cnt == 0)
            {
                // 망치가 날아갈 목적지 찾고
                nextTarget = FindHammerTarget();
                // 날아가는 연출
                CreateHammerEffect(ProductSkill.Bomb, productBomb.transform.position, nextTarget.transform.position, 0.9f);

                TryDestroy(productHammer, false);
                TryDestroy(productBomb, false);
                return DelayedCallRet.Keep;
            }
            else
            {
                Product destPro = nextTarget.ChildProduct;
                if(destPro != null && !destPro.IsLocked)
                {
                    CreateExplosionEffect(destPro.transform.position);
                    Product[] arPros = ScanAroundProducts(destPro, 1);
                    TryDestroy(destPro, true);
                    foreach (Product arPro in arPros)
                    {
                        TryDestroy(arPro, true);
                    }
                }

                return DelayedCallRet.Done;
            }
        });
    }
    private void CastSkillHammer_Hori(Product productHammer, Product productHori)
    {
        productHammer.SkillCasted = true;
        productHori.SkillCasted = true;

        Frame nextTarget = null;
        AddWorker(1, 9, (cnt) =>
        {
            if (cnt == 0)
            {
                // 망치가 날아갈 목적지 찾고
                nextTarget = FindHammerTarget();
                // 날아가는 연출
                CreateHammerEffect(ProductSkill.Horizontal, productHori.transform.position, nextTarget.transform.position, 0.9f);

                TryDestroy(productHammer, false);
                TryDestroy(productHori, false);
                return DelayedCallRet.Keep;
            }
            else
            {
                Product destPro = nextTarget.ChildProduct;
                if (destPro != null && !destPro.IsLocked)
                {
                    CastHorizontalProduct(destPro);
                }
                return DelayedCallRet.Done;
            }
        });
    }
    private void CastSkillHammer_Vert(Product productHammer, Product productVert)
    {
        productHammer.SkillCasted = true;
        productVert.SkillCasted = true;

        Frame nextTarget = null;
        AddWorker(1, 9, (cnt) =>
        {
            if (cnt == 0)
            {
                // 망치가 날아갈 목적지 찾고
                nextTarget = FindHammerTarget();
                // 날아가는 연출
                CreateHammerEffect(ProductSkill.Vertical, productVert.transform.position, nextTarget.transform.position, 0.9f);

                TryDestroy(productHammer, false);
                TryDestroy(productVert, false);
                return DelayedCallRet.Keep;
            }
            else
            {
                Product destPro = nextTarget.ChildProduct;
                if (destPro != null && !destPro.IsLocked)
                {
                    CastVerticalProduct(destPro);
                }
                return DelayedCallRet.Done;
            }
        });
    }
    private void CastSkillHammer_Hammer(Product productHammerA, Product productHammerB)
    {
        productHammerA.SkillCasted = true;
        productHammerB.SkillCasted = true;
        productHammerA.Animation.Play("destroy");
        productHammerB.Animation.Play("destroy");

        List<Frame> nextTargets = new List<Frame>();
        AddWorker(3, 9, (cnt) =>
        {
            if (cnt == 0)
            {
                for (int i = 0; i < 3; ++i)
                {
                    // 도착블럭 찾고
                    Frame target = FindHammerTarget();
                    // 날아가는 연출
                    CreateHammerEffect(ProductSkill.Hammer, productHammerA.transform.position, target.transform.position, 0.9f);
                    nextTargets.Add(target);
                }
                for (int i = 0; i < 3; ++i)
                {
                    // 도착블럭 찾고
                    Frame target = FindHammerTarget();
                    // 날아가는 연출
                    CreateHammerEffect(ProductSkill.Hammer, productHammerB.transform.position, target.transform.position, 0.9f);
                    nextTargets.Add(target);
                }

                TryDestroy(productHammerA, false);
                TryDestroy(productHammerB, false);
                return DelayedCallRet.Keep;
            }
            else
            {
                foreach(Frame frame in nextTargets)
                {
                    Product destPro = frame.ChildProduct;
                    if (destPro != null && !destPro.IsLocked)
                    {
                        TryDestroy(destPro, true);
                    }
                }
                
                return DelayedCallRet.Done;
            }
        });
    }

    private void StartSingleSkillLoop()
    {
        AddWorker(0, 2, (cnt) =>
        {
            Product targetSkill = FindNextSingleSkill();
            if(targetSkill != null)
            {
                CastSkillProduct(targetSkill);
                return DelayedCallRet.Keep;
            }
            else
            {
                if (IsAllProductIdle())
                {
                    return DelayedCallRet.Done;
                }
            }
            
            return DelayedCallRet.Keep;
        });
    }
    private void StartRainbowSkillLoop()
    {
        AddWorker(0, 3, (cnt) =>
        {
            Product target = FindRainbowSkill();
            if(target != null)
            {
                target.SkillCasted = true;
                Product[] pros = FindNormalSameColor(target.Color);
                foreach(Product pro in pros)
                {
                    TryDestroy(pro, true);
                }

                TryDestroy(target, true);
                return DelayedCallRet.Keep;
            }
            else
            {
                StartSingleSkillLoop();
                return DelayedCallRet.Done;
            }
        });
    }

    private Product FindNextSingleSkill()
    {
        for (int y = CountY - 1; y >= 0; --y)
        {
            for (int x = 0; x < CountX; ++x)
            {
                Frame frame = mFrames[x, y];
                Product pro = frame.ChildProduct;
                if (pro != null && pro.IsSkillable && !pro.IsLocked && !pro.IsObstacled())
                {
                    return pro;
                }
            }
        }
        return null;
    }
    private Product FindRainbowSkill()
    {
        for (int y = CountY - 1; y >= 0; --y)
        {
            for (int x = 0; x < CountX; ++x)
            {
                Frame frame = mFrames[x, y];
                Product pro = frame.ChildProduct;
                if (pro != null && pro.IsSkillable && !pro.IsLocked && !pro.IsObstacled() && pro.Skill == ProductSkill.SameColor)
                {
                    return pro;
                }
            }
        }
        return null;
    }














    IEnumerator DestroyProductDelay(Product[] destroyedProducts, float delay, bool withLaserEffect, int timerCounter)
    {
        if(delay > 0)
        {
            yield return new WaitForSeconds(delay);
        }

        SoundPlayer.Inst.PlaySoundEffect(ClipSound.Match, mSFXVolume);
        //SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectBreakFruit, mSFXVolume);
        List<ProductInfo> nextProducts = new List<ProductInfo>();
        foreach (Product pro in destroyedProducts)
        {
            Frame parentFrame = pro.ParentFrame;
            pro.DestroyImmediately(Billboard.CurrentCombo);
            Product newPro = CreateNewProduct();
            parentFrame.VertFrames.AddNewProduct(newPro);
            newPro.EnableMasking(parentFrame.VertFrames.MaskOrder);
            //nextProducts.Add(new ProductInfo(pro.Color, newPro.Color, ProductSkill.Nothing, parentFrame.IndexX, parentFrame.IndexY, pro.InstanceID, newPro.InstanceID));
        }
        //Network_Destroy(nextProducts.ToArray(), ProductSkill.Nothing, withLaserEffect, timerCounter);
    }
    public Product[] DestroyProducts(Product[] matches, float delay = UserSetting.MatchReadyInterval, bool ignoreSkill = false)
    {
        if (matches == null || matches.Length <= 0)
            return matches;

        List<Product> rets = new List<Product>();

        int addedScore = 0;
        foreach (Product pro in matches)
        {
            if(IsObstacled(pro.ParentFrame))
            {
                BreakObstacle(pro.ParentFrame);
                continue;
            }

            if(pro.IsLocked)
            {
                continue;
            }

            if(pro.Skill != ProductSkill.Nothing && !ignoreSkill)
            {
                BreakSkillProduct(pro);
            }

            if (pro.ReadyForDestroy(Billboard.CurrentCombo))
            {
                addedScore += Billboard.CurrentCombo;
                rets.Add(pro);
            }
        }

        Product[] validProducts = rets.ToArray();
        if (validProducts.Length <= 0)
            return validProducts;

        StartCoroutine(DestroyProductDelay(validProducts, delay, false, mPVPTimerCounter));

        int spa = Mathf.Max(10, UserSetting.ScorePerAttack - (10 * mPVPTimerCounter));
        int preAttackCount = Billboard.CurrentScore / spa;
        int curAttackCount = (Billboard.CurrentScore + addedScore) / spa;
        Attack(curAttackCount - preAttackCount, validProducts[0].transform.position);

        Billboard.CurrentScore += addedScore;
        Billboard.DestroyCount += validProducts.Length;
        EventBreakTarget?.Invoke(validProducts[0].transform.position, StageGoalType.Score);

        return validProducts;
    }
    IEnumerator MergeProductDelay(Product[] mergeProducts, float delay, ProductSkill skill, int timerCounter)
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

        Network_Destroy(nextProducts.ToArray(), skill, false, timerCounter);

        if (skill == ProductSkill.SameColor)
            SoundPlayer.Inst.PlaySoundEffect(ClipSound.Merge3, mSFXVolume);
        else if (skill == ProductSkill.Bomb)
            SoundPlayer.Inst.PlaySoundEffect(ClipSound.Merge2, mSFXVolume);
        else
            SoundPlayer.Inst.PlaySoundEffect(ClipSound.Merge1, mSFXVolume);

    }
    private Product[] MergeProducts(Product[] matches, ProductSkill makeSkill)
    {
        Frame mainFrame = matches[0].ParentFrame;

        List<Product> rets = new List<Product>();

        int addedScore = 0;
        foreach (Product pro in matches)
        {
            Frame curFrame = pro.ParentFrame;
            if(pro.ReadyForMerge(Billboard.CurrentCombo))
            {
                addedScore += Billboard.CurrentCombo;
                rets.Add(pro);
            }
        }

        Product[] validProducts = rets.ToArray();
        if (validProducts.Length <= 0)
            return validProducts;

        StartCoroutine(MergeProductDelay(validProducts, UserSetting.MatchReadyInterval, makeSkill, mPVPTimerCounter));

        int spa = Mathf.Max(10, UserSetting.ScorePerAttack - (10 * mPVPTimerCounter));
        int preAttackCount = Billboard.CurrentScore / spa;
        int curAttackCount = (Billboard.CurrentScore + addedScore) / spa;
        Attack(curAttackCount - preAttackCount, validProducts[0].transform.position);

        Billboard.CurrentScore += addedScore;
        Billboard.DestroyCount += validProducts.Length;
        EventBreakTarget?.Invoke(mainFrame.transform.position, StageGoalType.Score);

        SoundPlayer.Inst.PlaySoundEffect(ClipSound.Match, mSFXVolume);
        return validProducts;
    }


    IEnumerator DetachProduct(Product product)
    {

        //SoundPlayer.Inst.PlaySoundEffect(ClipSound.Match, mSFXVolume);
        //SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectBreakFruit, mSFXVolume);
        Frame parentFrame = product.ParentFrame;
        product.DetachFromField();
        Product newPro = CreateNewProduct();
        parentFrame.VertFrames.AddNewProduct(newPro);
        newPro.EnableMasking(parentFrame.VertFrames.MaskOrder);

        yield return new WaitForSeconds(0);
    }

    private void DropNextProducts()
    {
        if(IsDroppable)
        {
            StartToDropProducts();
        }

        mIsDropping = CountDroppingProducts() > 0;
    }
    // IEnumerator DestroyProductsDropping()
    // {
    //     int continuousMatchedCount = 0;
    //     mIsAutoMatching = true;
    //     while (true)
    //     {
    //         bool isAllIdle = true;
    //         Product[] matchableProducts = null;
    //         foreach (Frame frame in mFrames)
    //         {
    //             if (frame.Empty)
    //                 continue;

    //             if (frame.ChildProduct == null || frame.ChildProduct.IsLocked)
    //             {
    //                 isAllIdle = false;
    //                 continue;
    //             }

    //             List<Product[]> matches = FindMatchedProducts(new Product[1] { frame.ChildProduct }, UserSetting.MatchCount + 1);
    //             if (matches.Count <= 0)
    //                 continue;

    //             isAllIdle = false;
    //             matchableProducts = matches[0];
    //             break;
    //         }


    //         if (isAllIdle)
    //         {
    //             break;
    //         }
    //         else if (matchableProducts != null)
    //         {
    //             continuousMatchedCount++;
    //             ProductSkill nextSkill = CheckSkillable(matchableProducts);
    //             if (nextSkill == ProductSkill.Nothing)
    //                 DestroyProducts(matchableProducts);
    //             else
    //             {
    //                 //밸런스 조정을 위한 장치
    //                 if(continuousMatchedCount > 40)
    //                     DestroyProducts(matchableProducts);
    //                 else if (continuousMatchedCount > 25 && nextSkill == ProductSkill.SameColor)
    //                     DestroyProducts(matchableProducts);
    //                 else
    //                     MergeProducts(matchableProducts, nextSkill);
    //             }
                    
    //         }

    //         yield return new WaitForSeconds(UserSetting.AutoMatchInterval);
    //     }
    //     mIsAutoMatching = false;
    // }
    private int StartToDropProducts()
    {
        foreach(VerticalFrames group in mVerticalFrames)
        {
            Product[] droppingPros = group.StartToDrop();
            if(droppingPros != null && droppingPros.Length > 0)
            {
                StartCoroutine(MatchProductsAfterDrop(droppingPros));
            }
        }

        return 0;
    }
    private IEnumerator MatchProductsAfterDrop(Product[] droppingProducts)
    {
        //모든 Product가 다 떨어질때까지 기다림..
        while (true)
        {
            int droppedProCount = 0;
            foreach (Product pro in droppingProducts)
            {
                if (pro.IsDropping)
                    break;
                else
                    droppedProCount++;
            }

            if (droppingProducts.Length == droppedProCount)
                break;
            else
                yield return null;
        }

        //다 떨어지고 나면 떨어진 Products들로 다시 Matching 시도
        List<Product[]> matches = FindMatchedProducts(droppingProducts, UserSetting.MatchCount + 1);
        if (matches.Count > 0)
        {
            foreach(Product[] group in matches)
            {
                ProductSkill nextSkill = CheckSkillable(group);
                if (nextSkill == ProductSkill.Nothing)
                    DestroyProducts(group);
                else
                {
                    MergeProducts(group, nextSkill);
                }

                List<Product> aroundProducts = new List<Product>();
                FindAroundProducts(group, aroundProducts);
                foreach (Product aroundPro in aroundProducts)
                {
                    if (IsObstacled(aroundPro.ParentFrame))
                    {
                        BreakObstacle(aroundPro.ParentFrame);
                    }
                }
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

    private IEnumerator BreakHorizontalProduct(Frame frame, float delay = UserSetting.MatchReadyInterval)
    {
        Vector3 startPosition = frame.transform.position;
        Vector3 rightEndPosition = frame.MostRight().transform.position;
        Vector3 leftEndPosition = frame.MostLeft().transform.position;
        float maxDistance = Mathf.Max(startPosition.x - leftEndPosition.x, rightEndPosition.x - startPosition.x);
        maxDistance += GridSize;

        //mDropLockCount++;
        if(delay > 0)
        {
            mIsUserEventLock = true;
            if(frame.ChildProduct != null)
            {
                frame.ChildProduct.transform.DOShakePosition(delay, 0.1f);
            }

            yield return new WaitForSeconds(delay);
            mIsUserEventLock = false;
        }


        Rocket rocketR = Instantiate(LineRocketPrefab, transform);
        rocketR.IngameMgr = this;
        rocketR.transform.position = startPosition;
        rocketR.EventExplosion = (onFrame) =>
        {
            Product child = onFrame.ChildProduct;
            if (child != null && !child.IsLocked)
            {
                DestroyProducts(new Product[] { child }, 0);
            }
        };
        rocketR.transform.DOMoveX(startPosition.x + maxDistance, 0.5f).SetEase(Ease.InQuad).OnComplete(() =>
        {
            Destroy(rocketR.gameObject);
        });

        Rocket rocketL = Instantiate(LineRocketPrefab, transform);
        rocketL.IngameMgr = this;
        rocketL.transform.position = startPosition;
        rocketL.transform.rotation = Quaternion.Euler(0, 0, 180);
        rocketL.EventExplosion = (onFrame) =>
        {
            Product child = onFrame.ChildProduct;
            if (child != null && !child.IsLocked)
            {
                DestroyProducts(new Product[] { child }, 0);
            }
        };
        rocketL.transform.DOMoveX(startPosition.x - maxDistance, 0.5f).SetEase(Ease.InQuad).OnComplete(() =>
        {
            Destroy(rocketL.gameObject);
        });

        //yield return new WaitUntil(() => rocketL == null && rocketR == null);
        //mDropLockCount--;
    }
    private IEnumerator BreakVerticalProduct(Frame frame, float delay = UserSetting.MatchReadyInterval)
    {
        Vector3 startPosition = frame.transform.position;
        Vector3 topEndPosition = frame.MostUp().transform.position;
        Vector3 bottomEndPosition = frame.MostDown().transform.position;
        float maxDistance = Mathf.Max(startPosition.y - bottomEndPosition.y, topEndPosition.y - startPosition.y);
        maxDistance += GridSize;

        frame.VertFrames.HoldCount++;

        if (delay > 0)
        {
            mIsUserEventLock = true;
            if(frame.ChildProduct != null)
            {
                frame.ChildProduct.transform.DOShakePosition(delay, 0.1f);
            }
            yield return new WaitForSeconds(delay);
            mIsUserEventLock = false;
        }

        Rocket rocketT = Instantiate(LineRocketPrefab, transform);
        rocketT.IngameMgr = this;
        rocketT.transform.position = startPosition;
        rocketT.transform.rotation = Quaternion.Euler(0, 0, 90);
        rocketT.EventExplosion = (onFrame) =>
        {
            Product child = onFrame.ChildProduct;
            if (child != null && !child.IsLocked)
            {
                DestroyProducts(new Product[] { child }, 0);
            }
        };
        rocketT.transform.DOMoveY(startPosition.y + maxDistance, 0.5f).SetEase(Ease.InQuad).OnComplete(() =>
        {
            Destroy(rocketT.gameObject);
        });

        Rocket rocketB = Instantiate(LineRocketPrefab, transform);
        rocketB.IngameMgr = this;
        rocketB.transform.position = startPosition;
        rocketB.transform.rotation = Quaternion.Euler(0, 0, 270);
        rocketB.EventExplosion = (onFrame) =>
        {
            Product child = onFrame.ChildProduct;
            if (child != null && !child.IsLocked)
            {
                DestroyProducts(new Product[] { child }, 0);
            }
        };
        rocketB.transform.DOMoveY(startPosition.y - maxDistance, 0.5f).SetEase(Ease.InQuad).OnComplete(() =>
        {
            Destroy(rocketB.gameObject);
        });

        yield return new WaitUntil(() => rocketT == null && rocketB == null);
        frame.VertFrames.HoldCount--;
    }
    private IEnumerator BreakBombProduct(Frame frame, float delay = UserSetting.MatchReadyInterval)
    {
        if (delay > 0)
        {
            mIsUserEventLock = true;
            if(frame.ChildProduct != null)
            {
                frame.ChildProduct.Animation.Play("destroy");
            }
            yield return new WaitForSeconds(delay);
            mIsUserEventLock = false;
        }

        CreateExplosionEffect(frame.transform.position);
        Product[] pros = ScanAroundProducts(frame.ChildProduct, 1);
        DestroyProducts(pros);
    }
    private IEnumerator BreakHammerProduct(Product pro, float delay = UserSetting.MatchReadyInterval)
    {
        if (delay > 0)
        {
            mIsUserEventLock = true;
            pro.Animation.Play("destroy");
            yield return new WaitForSeconds(delay);
            mIsUserEventLock = false;
        }

        GameObject hammerObj = Instantiate(HammerPrefab, transform);
        hammerObj.transform.position = pro.transform.position;

        float duration = 0.8f;
        Frame nextTarget = FindHammerTarget();
        float topPosY = hammerObj.transform.position.y + 3;
        hammerObj.transform.DOMoveX(nextTarget.transform.position.x, duration).SetEase(Ease.Linear);
        hammerObj.transform.DORotate(new Vector3(0, 0, 720), duration, RotateMode.FastBeyond360);

        hammerObj.transform.DOMoveY(topPosY, duration * 0.5f).SetEase(Ease.OutQuad);
        yield return new WaitForSeconds(duration * 0.5f);
        hammerObj.transform.DOMoveY(nextTarget.transform.position.y, duration * 0.5f).SetEase(Ease.InQuad);
        yield return new WaitForSeconds(duration * 0.5f);
        Destroy(hammerObj);
        if(nextTarget.ChildProduct != null)
        {
            DestroyProducts(new Product[1] { nextTarget.ChildProduct });
        }
    }
    private IEnumerator BreakRainbowProduct(Product pro, float delay = UserSetting.MatchReadyInterval)
    {
        if (delay > 0)
        {
            mIsUserEventLock = true;
            pro.Animation.Play("destroy");
            yield return new WaitForSeconds(delay);
            mIsUserEventLock = false;
        }

        ShakeField(0.05f);
        Product[] pros = FindNormalSameColor(pro.Color);
        StartCoroutine(StartElectronicEffect(pro.transform.position, pros, (target) =>
            {
                DestroyProducts(new Product[1] { target });
            }, null)
        );
    }
    private void BreakSkillProduct(Product target, float delay = UserSetting.MatchReadyInterval)
    {
        if(target.SkillCasted)
            return;

        target.SkillCasted = true;
        if (target.Skill == ProductSkill.Horizontal)
        {
            StartCoroutine(BreakHorizontalProduct(target.ParentFrame, delay));
        }
        else if (target.Skill == ProductSkill.Vertical)
        {
            StartCoroutine(BreakVerticalProduct(target.ParentFrame, delay));
        }
        else if (target.Skill == ProductSkill.Bomb)
        {
            StartCoroutine(BreakBombProduct(target.ParentFrame, delay));
        }
        else if (target.Skill == ProductSkill.SameColor)
        {
            StartCoroutine(BreakRainbowProduct(target, delay));
        }
        else if (target.Skill == ProductSkill.Hammer)
        {
            StartCoroutine(BreakHammerProduct(target, delay));
        }
    }

    private void DestroySkillNormal_Normal(Product main, Product sub)
    {
        if (main.Skill == ProductSkill.Horizontal)
        {
            switch (sub.Skill)
            {
                case ProductSkill.Horizontal: CastSkillStripe_Stripe(main, sub); break;
                case ProductSkill.Vertical: CastSkillStripe_Stripe(main, sub); break;
                case ProductSkill.Bomb: CastSkillBomb_Stripe(sub, main); break;
                case ProductSkill.Hammer: CastSkillHammer_Hori(sub, main); break;
            }
        }
        else if (main.Skill == ProductSkill.Vertical)
        {
            switch (sub.Skill)
            {
                case ProductSkill.Horizontal: CastSkillStripe_Stripe(main, sub); break;
                case ProductSkill.Vertical: CastSkillStripe_Stripe(main, sub); break;
                case ProductSkill.Bomb: CastSkillBomb_Stripe(sub, main); break;
                case ProductSkill.Hammer: CastSkillHammer_Vert(sub, main); break;
            }
        }
        else if (main.Skill == ProductSkill.Bomb)
        {
            switch (sub.Skill)
            {
                case ProductSkill.Horizontal: CastSkillBomb_Stripe(main, sub); break;
                case ProductSkill.Vertical: CastSkillBomb_Stripe(main, sub); break;
                case ProductSkill.Bomb: StartCoroutine(DestroySkillBomb_Bomb(main, sub)); break;
                case ProductSkill.Hammer: CastSkillHammer_Bomb(sub, main); break;
            }
        }
        else if (main.Skill == ProductSkill.Hammer)
        {
            switch (sub.Skill)
            {
                case ProductSkill.Horizontal: CastSkillHammer_Hori(main, sub); break;
                case ProductSkill.Vertical: CastSkillHammer_Vert(main, sub); break;
                case ProductSkill.Bomb: CastSkillHammer_Bomb(main, sub); break;
                case ProductSkill.Hammer: CastSkillHammer_Hammer(main, sub); break;
            }
        }
    }
    private IEnumerator DestroySkillBomb_Stripe(Product productbomb, Product productStripe)
    {
        if (productStripe.Skill == ProductSkill.Horizontal)
        {
            mDropLockCount++;
            Vector3 startPosition = productStripe.transform.position;
            Vector3 rightEndPosition = productStripe.ParentFrame.MostRight().transform.position;
            Vector3 leftEndPosition = productStripe.ParentFrame.MostLeft().transform.position;
            float maxDistance = Mathf.Max(startPosition.x - leftEndPosition.x, rightEndPosition.x - startPosition.x);
            maxDistance += GridSize;

            Rocket rocketR = Instantiate(LineRocketPrefab, transform);
            rocketR.IngameMgr = this;
            rocketR.transform.position = startPosition;
            rocketR.transform.DOScale(2, UserSetting.AutoMatchInterval);
            rocketR.IsBig = true;
            rocketR.EventExplosion = (frame) =>
            {
                Product child = frame.ChildProduct;
                if (child != null && !child.IsLocked)
                {
                    DestroyProducts(new Product[] { child }, 0);
                }
            };

            Rocket rocketL = Instantiate(LineRocketPrefab, transform);
            rocketL.IngameMgr = this;
            rocketL.transform.position = startPosition;
            rocketL.transform.rotation = Quaternion.Euler(0, 0, 180);
            rocketL.transform.DOScale(2, UserSetting.AutoMatchInterval);
            rocketL.IsBig = true;
            rocketL.EventExplosion = (frame) =>
            {
                Product child = frame.ChildProduct;
                if (child != null && !child.IsLocked)
                {
                    DestroyProducts(new Product[] { child }, 0);
                }
            };

            mIsUserEventLock = true;
            yield return new WaitForSeconds(UserSetting.AutoMatchInterval);
            mIsUserEventLock = false;

            rocketR.transform.DOMoveX(startPosition.x + maxDistance, 0.3f).SetEase(Ease.InQuad).OnComplete(() =>
            {
                Destroy(rocketR.gameObject);
            });

            rocketL.transform.DOMoveX(startPosition.x - maxDistance, 0.3f).SetEase(Ease.InQuad).OnComplete(() =>
            {
                Destroy(rocketL.gameObject);
            });

            yield return new WaitUntil(() => rocketL == null && rocketR == null);
            mDropLockCount--;
        }
        else if (productStripe.Skill == ProductSkill.Vertical)
        {
            mDropLockCount++;
            Vector3 startPosition = productStripe.transform.position;
            Vector3 topEndPosition = productStripe.ParentFrame.MostUp().transform.position;
            Vector3 bottomEndPosition = productStripe.ParentFrame.MostDown().transform.position;
            float maxDistance = Mathf.Max(startPosition.y - bottomEndPosition.y, topEndPosition.y - startPosition.y);
            maxDistance += GridSize;

            Rocket rocketT = Instantiate(LineRocketPrefab, transform);
            rocketT.IngameMgr = this;
            rocketT.transform.position = startPosition;
            rocketT.transform.rotation = Quaternion.Euler(0, 0, 90);
            rocketT.transform.DOScale(2, UserSetting.AutoMatchInterval);
            rocketT.IsBig = true;
            rocketT.EventExplosion = (frame) =>
            {
                Product child = frame.ChildProduct;
                if (child != null && !child.IsLocked)
                {
                    DestroyProducts(new Product[] { child }, 0);
                }
            };

            Rocket rocketB = Instantiate(LineRocketPrefab, transform);
            rocketB.IngameMgr = this;
            rocketB.transform.position = startPosition;
            rocketB.transform.rotation = Quaternion.Euler(0, 0, 270);
            rocketB.transform.DOScale(2, UserSetting.AutoMatchInterval);
            rocketB.IsBig = true;
            rocketB.EventExplosion = (frame) =>
            {
                Product child = frame.ChildProduct;
                if (child != null && !child.IsLocked)
                {
                    DestroyProducts(new Product[] { child }, 0);
                }
            };

            mIsUserEventLock = true;
            yield return new WaitForSeconds(UserSetting.AutoMatchInterval);
            mIsUserEventLock = false;

            rocketT.transform.DOMoveY(startPosition.y + maxDistance, 0.3f).SetEase(Ease.InQuad).OnComplete(() =>
            {
                Destroy(rocketT.gameObject);
            });

            rocketB.transform.DOMoveY(startPosition.y - maxDistance, 0.3f).SetEase(Ease.InQuad).OnComplete(() =>
            {
                Destroy(rocketB.gameObject);
            });

            yield return new WaitUntil(() => rocketT == null && rocketB == null);
            mDropLockCount--;
        }
    }
    private void DestroySkillStripe_Stripe(Product productStripeA, Product productStripeB)
    {
        DestroyProducts(new Product[2] { productStripeA, productStripeB });
    }
    private IEnumerator DestroySkillBomb_Bomb(Product productbombA, Product productbombB)
    {
        mIsUserEventLock = true;
        Vector3 startPos = productbombA.transform.position;
        productbombA.SkillCasted = true;
        productbombB.SkillCasted = true;

        List<Product> targetsA = new List<Product>();
        List<Product> targetsB = new List<Product>();
        for (int x = 0; x < CountX; ++x)
        {
            for (int y = CountY - 1; y >= 0; --y)
            {
                Frame frame = mFrames[x, y];
                Product pro = frame.ChildProduct;
                if (pro != null && !pro.IsLocked && pro.Skill == ProductSkill.Nothing)
                {
                    if (y % 2 == 0)
                        targetsA.Add(pro);
                    else
                        targetsB.Add(pro);
                }
            }
        }

        ShakeField(0.05f);

        StartCoroutine(StartElectronicEffect(productbombA.transform.position, targetsA.ToArray(),
            (pro) => {
                TryDestroy(pro, false);
            }, 
            () => {
                targetsA.Clear();
            }));


        StartCoroutine(StartElectronicEffect(productbombB.transform.position, targetsB.ToArray(),
            (pro) =>
            {
                TryDestroy(pro, false);
            },
            () =>
            {
                targetsB.Clear();
            }));

        yield return new WaitUntil(() => { return targetsA.Count == 0 && targetsB.Count == 0; });
        TryDestroy(productbombA, false);
        TryDestroy(productbombB, false);

        mIsUserEventLock = false;
        StartToDrop();
    }
    private IEnumerator DestroySkillHammer_Bomb(Product productHammer, Product productBomb)
    {
        //mIsUserEventLock = true;
        GameObject bombObj = Instantiate(SimpleSpritePrefab, transform);
        bombObj.transform.position = productBomb.transform.position;
        bombObj.transform.localScale = new Vector3(0.6f, 0.6f, 1);
        bombObj.GetComponent<SpriteRenderer>().sprite = ProductSkill.Bomb.GetSprite();
        //yield return new WaitForSeconds(UserSetting.MatchReadyInterval);
        //mIsUserEventLock = false;

        DestroyProducts(new Product[] { productHammer, productBomb }, 0, true);

        float duration = 0.8f;
        Frame nextTarget = FindHammerTarget();
        yield return StartCoroutine(ThrowOver(bombObj.transform, nextTarget.transform.position.y, duration));
        Destroy(bombObj);

        StartCoroutine(BreakBombProduct(nextTarget, 0));
    }
    private IEnumerator DestroySkillHammer_Hori(Product productHammer, Product productHori)
    {
        //mIsUserEventLock = true;
        GameObject horiObj = Instantiate(SimpleSpritePrefab, transform);
        horiObj.transform.position = productHori.transform.position;
        horiObj.transform.localScale = new Vector3(0.6f, 0.6f, 1);
        horiObj.GetComponent<SpriteRenderer>().sprite = ProductSkill.Horizontal.GetSprite();
        //yield return new WaitForSeconds(UserSetting.MatchReadyInterval);
        //mIsUserEventLock = false;

        DestroyProducts(new Product[] { productHammer, productHori }, 0, true);

        float duration = 0.8f;
        Frame nextTarget = FindHammerTarget();
        yield return StartCoroutine(ThrowOver(horiObj.transform, nextTarget.transform.position.y, duration));
        Destroy(horiObj);

        StartCoroutine(BreakHorizontalProduct(nextTarget, 0));
    }
    private IEnumerator DestroySkillHammer_Vert(Product productHammer, Product productVert)
    {
        //mIsUserEventLock = true;
        GameObject horiObj = Instantiate(SimpleSpritePrefab, transform);
        horiObj.transform.position = productVert.transform.position;
        horiObj.transform.localScale = new Vector3(0.6f, 0.6f, 1);
        horiObj.GetComponent<SpriteRenderer>().sprite = ProductSkill.Horizontal.GetSprite();
        //yield return new WaitForSeconds(UserSetting.MatchReadyInterval);
        //mIsUserEventLock = false;

        DestroyProducts(new Product[] { productHammer, productVert }, 0, true);

        float duration = 0.8f;
        Frame nextTarget = FindHammerTarget();
        yield return StartCoroutine(ThrowOver(horiObj.transform, nextTarget.transform.position.y, duration));
        Destroy(horiObj);

        StartCoroutine(BreakVerticalProduct(nextTarget, 0));
    }
    private IEnumerator DestroySkillHammer_Hammer(Product productHammerA, Product productHammerB)
    {
        GameObject horiObjA = Instantiate(SimpleSpritePrefab, transform);
        horiObjA.transform.position = productHammerA.transform.position;
        horiObjA.transform.localScale = new Vector3(0.6f, 0.6f, 1);
        horiObjA.GetComponent<SpriteRenderer>().sprite = ProductSkill.Hammer.GetSprite();

        GameObject horiObjB = Instantiate(SimpleSpritePrefab, transform);
        horiObjB.transform.position = productHammerA.transform.position;
        horiObjB.transform.localScale = new Vector3(0.6f, 0.6f, 1);
        horiObjB.GetComponent<SpriteRenderer>().sprite = ProductSkill.Hammer.GetSprite();

        GameObject horiObjC = Instantiate(SimpleSpritePrefab, transform);
        horiObjC.transform.position = productHammerA.transform.position;
        horiObjC.transform.localScale = new Vector3(0.6f, 0.6f, 1);
        horiObjC.GetComponent<SpriteRenderer>().sprite = ProductSkill.Hammer.GetSprite();

        DestroyProducts(new Product[] { productHammerA, productHammerB }, 0, true);

        float duration = 0.8f;
        Frame nextTarget = FindHammerTarget();

        StartCoroutine(ThrowOver(horiObjA.transform, nextTarget.transform.position.y, duration));
        yield return new WaitForSeconds(0.3f);
        StartCoroutine(ThrowOver(horiObjB.transform, nextTarget.transform.position.y, duration));
        yield return new WaitForSeconds(0.3f);
        StartCoroutine(ThrowOver(horiObjC.transform, nextTarget.transform.position.y, duration));
        
        yield return new WaitForSeconds(0.2f);
        Destroy(horiObjA);
        if (nextTarget.ChildProduct != null)
        {
            DestroyProducts(new Product[1] { nextTarget.ChildProduct });
        }
        yield return new WaitForSeconds(0.3f);
        Destroy(horiObjB);
        if (nextTarget.ChildProduct != null)
        {
            DestroyProducts(new Product[1] { nextTarget.ChildProduct });
        }
        yield return new WaitForSeconds(0.3f);
        Destroy(horiObjC);
        if (nextTarget.ChildProduct != null)
        {
            DestroyProducts(new Product[1] { nextTarget.ChildProduct });
        }
    }

    private void DestroySkillWithSamecolor(Product productA, Product productB)
    {
        Product sameColor = productA.Skill == ProductSkill.SameColor ? productA : productB;
        Product another = productA.Skill == ProductSkill.SameColor ? productB : productA;
        if (sameColor.Skill != ProductSkill.SameColor)
            return;

        if (another.Skill == ProductSkill.Horizontal || another.Skill == ProductSkill.Vertical)
        {
            List<Product> randomProducts = ScanRandomProducts(7);
            randomProducts.Add(sameColor);
            Product[] pros = randomProducts.ToArray();

            List<ProductInfo> netInfo = new List<ProductInfo>();
            StartCoroutine(CreateProductBullets(sameColor, 0.2f, ProductSkill.Horizontal, pros,
                (pro) => {
                    netInfo.Add(new ProductInfo(pro.Color, pro.Color, pro.Skill, pro.ParentFrame.IndexX, pro.ParentFrame.IndexY, pro.InstanceID, pro.InstanceID));
                },
                () => {
                    Network_ChangeSkill(netInfo.ToArray());
                    StartSingleSkillLoop();
                }));
        }
        else if (another.Skill == ProductSkill.Bomb)
        {
            List<Product> randomProducts = ScanRandomProducts(7);
            randomProducts.Add(sameColor);
            Product[] pros = randomProducts.ToArray();

            List<ProductInfo> netInfo = new List<ProductInfo>();
            StartCoroutine(CreateProductBullets(sameColor, 0.2f, ProductSkill.Bomb, pros,
                (pro) => {
                    netInfo.Add(new ProductInfo(pro.Color, pro.Color, pro.Skill, pro.ParentFrame.IndexX, pro.ParentFrame.IndexY, pro.InstanceID, pro.InstanceID));
                },
                () => {
                    Network_ChangeSkill(netInfo.ToArray());
                    StartSingleSkillLoop();
                }));
        }
        else if (another.Skill == ProductSkill.Hammer)
        {
            List<Product> randomProducts = ScanRandomProducts(7);
            randomProducts.Add(sameColor);
            Product[] pros = randomProducts.ToArray();

            List<ProductInfo> netInfo = new List<ProductInfo>();
            StartCoroutine(CreateProductBullets(sameColor, 0.2f, ProductSkill.Hammer, pros,
                (pro) =>
                {
                    netInfo.Add(new ProductInfo(pro.Color, pro.Color, pro.Skill, pro.ParentFrame.IndexX, pro.ParentFrame.IndexY, pro.InstanceID, pro.InstanceID));
                },
                () =>
                {
                    Network_ChangeSkill(netInfo.ToArray());
                    StartSingleSkillLoop();
                }));
        }
        else if (another.Skill == ProductSkill.SameColor)
        {
            List<Product> randomProducts = ScanRandomProducts(9);
            Product[] pros = randomProducts.ToArray();
            Frame[] frames = ToFrames(pros);

            SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectBreakSameSkill, mSFXVolume);
            ShakeField(0.1f);

            List<ProductInfo> netInfo = new List<ProductInfo>();
            StartCoroutine(CreateDirectBeamAtOnce(TrailingPrefab, sameColor.transform.position, frames,
                (frame) => {
                    Product pro = frame.ChildProduct;
                    pro.ChangeProductImage(ProductSkill.SameColor);
                    pro.FlashProduct();
                    netInfo.Add(new ProductInfo(pro.Color, pro.Color, pro.Skill, pro.ParentFrame.IndexX, pro.ParentFrame.IndexY, pro.InstanceID, pro.InstanceID));
                },
                () => {
                    //SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectBreakSameSkill2, mSFXVolume);
                    Network_ChangeSkill(netInfo.ToArray());
                    StartRainbowSkillLoop();
                    //StartCoroutine(DestroySameProductLoop());
                }));
        }
    }
    IEnumerator CreateProductBullets(Product startPro, float interval, ProductSkill skillType, Product[] destPros, Action<Product> eventEach, Action eventEnd)
    {
        mItemLooping = true;
        Vector3 startWorldPos = startPro.transform.position;
        int count = 0;
        foreach (Product pro in destPros)
        {
            startPro.Animation.Play("flinch");
            SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectEndGameReward, mSFXVolume);
            GameObject effect = Instantiate(SimpleSpritePrefab, startWorldPos, Quaternion.identity, transform);
            effect.transform.localScale = new Vector3(0.75f, 0.75f, 1);

            ProductSkill skillIndex = ProductSkill.Nothing;
            if(skillType == ProductSkill.Bomb || skillType == ProductSkill.Hammer)
                skillIndex = skillType;
            else
                skillIndex = mRandomSeed.Next() % 2 == 0 ? ProductSkill.Horizontal : ProductSkill.Vertical;
            effect.GetComponent<SpriteRenderer>().sprite = skillIndex.GetSprite();

            StartCoroutine(UnityUtils.MoveLinear(effect, pro.transform.position, 0.3f, 50.0f, () =>
            {
                count++;
                Destroy(effect);
                pro.ChangeProductImage(skillIndex);
                eventEach?.Invoke(pro);
            }));
            yield return new WaitForSeconds(interval);
        }

        while (count < destPros.Length)
            yield return null;

        mItemLooping = false;
        eventEnd?.Invoke();
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
                    if (pro != null && pro.Skill != ProductSkill.Nothing && !pro.IsLocked && !pro.IsObstacled())
                    {
                        DestroyProducts(new Product[1] { pro });

                        goto KeepLoop;
                    }
                }
            }

            if (!mIsAutoMatching && IsAllProductIdle())
                break;

            KeepLoop:
            yield return new WaitForSeconds(UserSetting.SkillDestroyInterval);
        }
        mItemLooping = false;
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
        //else if (target.Skill == ProductSkill.SameColor)
        //{
        //    Product[] sameProducts = FindSameColor(target);
        //    return DestroyProducts(sameProducts);
        //}
        return null;
    }
    IEnumerator CreateDirectBeamAtOnce(GameObject prefab, Vector3 startWorldPos, Frame[] frames, Action<Frame> eventEachEnd, Action eventEnd)
    {
        mItemLooping = true;
        int endCount = 0;
        foreach (Frame frame in frames)
        {
            GameObject projectail = Instantiate(prefab, startWorldPos, Quaternion.identity, transform);
            StartCoroutine(UnityUtils.MoveDecelerate(projectail, frame.transform.position, 0.1f, () =>
            {
                Destroy(projectail, 1.0f);
                endCount++;
                eventEachEnd?.Invoke(frame);
            }));
        }

        while (endCount < frames.Length)
            yield return null;

        mItemLooping = false;
        eventEnd?.Invoke();
    }
    IEnumerator DestroySameProductLoop()
    {
        mItemLooping = true;
        List<Product> sameSkills = new List<Product>();
        foreach (Frame frame in mFrames)
            if (frame.ChildProduct != null && frame.ChildProduct.Skill == ProductSkill.SameColor)
                sameSkills.Add(frame.ChildProduct);

        while(true)
        {
            Product target = null;
            foreach (Product sameSkill in sameSkills)
            {
                if (sameSkill != null && !sameSkill.IsLocked)
                {
                    target = sameSkill;
                    break;
                }
            }

            if (target == null)
                break;

            Product[] pros = FindNormalSameColor(target.Color);
            DestroyProducts(pros, UserSetting.MatchReadyInterval, true);
            yield return new WaitForSeconds(UserSetting.SameSkillInterval);
        }
        mItemLooping = false;
        StartCoroutine(DestroySkillSimpleLoop());
    }

    public void UseItemExtendsLimits(Vector3 startWorldPos, Vector3 destWorldPos)
    {
        mIsItemEffect = true;
        Network_UseItem(new ProductInfo[0], PurchaseItemType.ExtendLimit);
        GameObject missile = GameObject.Instantiate(TrailingPrefab, startWorldPos, Quaternion.identity, transform);
        StartCoroutine(UnityUtils.MoveDecelerate(missile, destWorldPos, 0.3f, () =>
        {
            mIsItemEffect = false;
            if (mStageInfo.TimeLimit > 0)
            {
                mStartTime += 10; //10초 연장
                float remainTime = mStageInfo.TimeLimit - PlayTime;
                EventRemainTime?.Invoke((int)remainTime);
            }
            else
            {
                Billboard.MoveCount -= 5; //5번 이동 추가
                EventReduceLimit?.Invoke();
            }
        }));
    }
    public void UseItemBreakce(Vector3 startWorldPos, int count)
    {
        mIsItemEffect = true;
        Product[] icedBlocks = GetTopIceBlocks(count);

        List<ProductInfo> netPros = new List<ProductInfo>();
        foreach (Product target in icedBlocks)
            netPros.Add(new ProductInfo(target.Color, target.Color, ProductSkill.Nothing, target.ParentFrame.IndexX, target.ParentFrame.IndexY, target.InstanceID, target.InstanceID));
        Network_UseItem(netPros.ToArray(), PurchaseItemType.RemoveIce);

        StartCoroutine(CreateMagnetMissiles(startWorldPos, icedBlocks,
            (pro) =>
            {
                if(pro.ParentFrame.IsObstacled())
                    pro.ParentFrame.BreakObstacle();
                else if(pro.IsObstacled())
                    pro.BreakObstacle();
            },
            () =>
            {
                mIsItemEffect = false;
            }));
    }
    public void UseItemMakeSkill1(Vector3 startWorldPos, int count)
    {
        mIsItemEffect = true;
        Frame[] idleFrames = GetRandomIdleFrames(count);
        Product[] idlePros = ToProducts(idleFrames);

        List<ProductInfo> netPros = new List<ProductInfo>();
        foreach (Product target in idlePros)
            netPros.Add(new ProductInfo(target.Color, target.Color, ProductSkill.Nothing, target.ParentFrame.IndexX, target.ParentFrame.IndexY, target.InstanceID, target.InstanceID));
        Network_UseItem(netPros.ToArray(), PurchaseItemType.MakeSkill1);

        List<ProductInfo> netInfo = new List<ProductInfo>();
        StartCoroutine(CreateMagnetTrails(TrailingPrefab, startWorldPos, idlePros,
            (pro) =>
            {
                int ran = mRandomSeed.Next() % 3;
                if (ran == 0)
                    pro.ChangeProductImage(ProductSkill.Horizontal);
                else if (ran == 1)
                    pro.ChangeProductImage(ProductSkill.Vertical);
                else
                    pro.ChangeProductImage(ProductSkill.Bomb);

                netInfo.Add(new ProductInfo(pro.Color, pro.Color, pro.Skill, pro.ParentFrame.IndexX, pro.ParentFrame.IndexY, pro.InstanceID, pro.InstanceID));
                pro.FlashProduct();
            },
            () =>
            {
                Network_ChangeSkill(netInfo.ToArray());
                mIsItemEffect = false;
            }));
    }
    public void UseItemMakeSkill2(Vector3 startWorldPos, int count)
    {
        mIsItemEffect = true;
        Frame[] idleFrames = GetRandomIdleFrames(count);
        Product[] idlePros = ToProducts(idleFrames);

        List<ProductInfo> netPros = new List<ProductInfo>();
        foreach (Product target in idlePros)
            netPros.Add(new ProductInfo(target.Color, target.Color, ProductSkill.Nothing, target.ParentFrame.IndexX, target.ParentFrame.IndexY, target.InstanceID, target.InstanceID));
        Network_UseItem(netPros.ToArray(), PurchaseItemType.MakeSkill2);

        List<ProductInfo> netInfo = new List<ProductInfo>();
        StartCoroutine(CreateMagnetTrails(TrailingPrefab, idlePros,
            (pro) =>
            {
                pro.ChangeProductImage(ProductSkill.SameColor);
                netInfo.Add(new ProductInfo(pro.Color, pro.Color, pro.Skill, pro.ParentFrame.IndexX, pro.ParentFrame.IndexY, pro.InstanceID, pro.InstanceID));
                pro.FlashProduct();
            },
            () =>
            {
                Network_ChangeSkill(netInfo.ToArray());
                mIsItemEffect = false;
            }));
    }
    public void UseItemMatch(Vector3 startWorldPos)
    {
        List<Product[]> matchesGroup = FindMatchedAllProducts();
        List<Frame> frames = new List<Frame>();
        foreach (Product[] matchedPros in matchesGroup)
            frames.Add(matchedPros[0].ParentFrame);

        List<ProductInfo> netPros = new List<ProductInfo>();
        foreach (Frame target in frames)
            netPros.Add(new ProductInfo(target.ChildProduct.Color, target.ChildProduct.Color, ProductSkill.Nothing, target.IndexX, target.IndexY, target.ChildProduct.InstanceID, target.ChildProduct.InstanceID));
        Network_UseItem(netPros.ToArray(), PurchaseItemType.MakeCombo);

        mIsItemEffect = true;
        mDropLockCount++;
        StartCoroutine(CreateDirectBeamInterval(TrailingPrefab, startWorldPos, 0.2f, frames.ToArray(),
            (frame) =>
            {
                ComboUp();
                Product pro = frame.ChildProduct;
                List<Product> rets = new List<Product>();
                pro.SearchMatchedProducts(rets, pro.Color);
                DestroyProducts(rets.ToArray());
            },
            () =>
            {
                mDropLockCount--;
                mIsItemEffect = false;
            }));
    }
    public void UseItemMeteor(int count)
    {
        mIsItemEffect = true;
        Frame[] idleFrames = GetRandomIdleFrames(count);

        List<ProductInfo> netPros = new List<ProductInfo>();
        foreach (Frame target in idleFrames)
            netPros.Add(new ProductInfo(target.ChildProduct.Color, target.ChildProduct.Color, ProductSkill.Nothing, target.IndexX, target.IndexY, target.ChildProduct.InstanceID, target.ChildProduct.InstanceID));
        Network_UseItem(netPros.ToArray(), PurchaseItemType.Meteor);

        StartCoroutine(CreateMeteor(idleFrames, 
            (frame) => {
                ShakeField(0.05f);
                Product[] scan = ScanAroundProducts(frame, 1);
                List<Product> rets = new List<Product>();
                foreach (Product pro in scan)
                    if (!pro.IsLocked)
                        rets.Add(pro);
                DestroyProducts(rets.ToArray());
            },
            () =>
            {
                mIsItemEffect = false;
            }));
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

            SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectAttackPVP, mSFXVolume);

            Vector3 destPos = AttackPointFrame.Points > 0 ? AttackPointFrame.transform.position : InstPVP_Opponent.AttackPointFrame.transform.position;
            StartCoroutine(AnimateAttack(objs, destPos, (destObj) =>
            {
                Destroy(destObj);

                if(AttackPointFrame.Points > 0)
                    AttackPointFrame.AddPoints(-1);
                else
                    InstPVP_Opponent.AttackPointFrame.AddPoints(1);
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

            SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectAttackPVP, mSFXVolume);
            Vector3 destPos = AttackPointFrame.Points > 0 ? AttackPointFrame.transform.position : InstPVP_Player.AttackPointFrame.transform.position;
            StartCoroutine(AnimateAttack(objs, destPos, (destObj) =>
            {
                Destroy(destObj);

                if (AttackPointFrame.Points > 0)
                    AttackPointFrame.AddPoints(-1);
                else
                    InstPVP_Player.AttackPointFrame.AddPoints(1);
            }));
        }
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
        yield return null;
        float dragFactor = 0.015f;
        float destFactor = 0;
        Tuple<Vector2, float>[] startSpeed = new Tuple<Vector2, float>[objs.Length];
        for (int i = 0; i < objs.Length; ++i)
        {
            float rad = UnityEngine.Random.Range(195, 345) * Mathf.Deg2Rad;
            if (objs[i].transform.position.y > dest.y)
                rad += Mathf.PI;
            Vector2 force = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            startSpeed[i] = new Tuple<Vector2, float>(force, UnityEngine.Random.Range(35, 45));
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
                && IsIdle)
            {
                int point = AttackPointFrame.Flush(UserSetting.FlushCount);

                List<Product> products = GetNextFlushTargets(point);
                Product[] rets = products.ToArray();
                Network_FlushAttacks(Serialize(rets), 1);
                StartCoroutine(FlushObstacles(rets, 3));
                if (products.Count < point)
                {
                    StartFinish(false);
                    break;
                }

                yield return new WaitForSeconds(UserSetting.ChocoFlushInterval);
            }
            yield return null;
        }
    }
    IEnumerator FlushObstacles(Product[] targets, int blockLevel, Action eventEnd = null)
    {
        while (!IsIdle)
            yield return null;

        mIsFlushing = true;
        float duration = 0.5f;
        foreach (Product target in targets)
        {
            Vector3 startPos = target.ParentFrame.VertFrames.TopFrame.transform.position;
            startPos.y += GridSize;
            startPos.z = target.transform.position.z - 0.5f;
            GameObject obj = Instantiate(ObstaclePrefab, startPos, Quaternion.identity, transform);
            obj.transform.DOMoveY(target.transform.position.y, duration).SetEase(Ease.InQuad)
            .OnComplete(() =>
            {
                target.IcedBlock.SetDepth(blockLevel);
                Destroy(obj);
            });
        }

        yield return new WaitForSeconds(duration);
        ShakeField(0.05f);
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectDropIce, mSFXVolume);

        eventEnd?.Invoke();
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

    private void StartFinish(bool isSuccess)
    {
        if(!mIsFinished)
        {
            mIsFinished = true;
            EventFinishFirst?.Invoke(isSuccess);
            StartCoroutine("_StartFinishing", isSuccess);
        }
    }
    IEnumerator _StartFinishing(bool isSuccess)
    {
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

                        yield return StartCoroutine(LoopRewardProducts());

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
    IEnumerator RefreshTimer()
    {
        while (mStageInfo.TimeLimit > 0 && !mIsFinished)
        {
            float remainTime = mStageInfo.TimeLimit - PlayTime;
            EventRemainTime?.Invoke((int)remainTime);
            yield return new WaitForSeconds(1);
        }
    }
    private void CheckIsFinishedInStageMode()
    {
        if (IsAchieveGoals())
        {
            StartFinish(true);
            return;
        }

        if (mStageInfo.TimeLimit > 0)
        {
            float remainTime = mStageInfo.TimeLimit - PlayTime;
            if (remainTime <= 0)
                StartFinish(false);
        }
        else if (mStageInfo.MoveLimit > 0)
        {
            if (Billboard.MoveCount >= mStageInfo.MoveLimit)
                StartFinish(false);
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
            case StageGoalType.Cap:
                if (Billboard.CapCount >= targetCount)
                    isSuccess = true;
                break;
            case StageGoalType.Bush:
                if (Billboard.BushCount >= targetCount)
                    isSuccess = true;
                break;
        }

        return isSuccess;
    }
    private int RewardCount()
    {
        if (mStageInfo.TimeLimit > 0)
        {
            float rate = PlayTime / mStageInfo.TimeLimit;
            rate = 1 - Mathf.Clamp(rate, 0, 1);
            int remains = (int)(rate * mFrames.Length);
            return remains;
        }
        else
        {
            int remains = mStageInfo.MoveLimit - Billboard.MoveCount;
            return remains < 0 ? 0 : remains;
        }
    }
    private int GetProductCount()
    {
        return GetComponentsInChildren<Product>().Length;
    }
    IEnumerator LoopRewardProducts()
    {
        int rewardedCount = 0;
        float interval = 0.15f;
        int remains = RewardCount();
        remains = Mathf.Min(remains, (int)(GetProductCount() * 0.4f));
        EventReward?.Invoke(remains, interval);
        Vector3 startWorldPos = MenuInGame.Inst().Limit.transform.position;
        for(int i = 0; i < remains; ++i)
        {
            SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectEndGameReward, mSFXVolume);
            GameObject effect = Instantiate(SimpleSpritePrefab, startWorldPos, Quaternion.identity, transform);
            effect.transform.localScale = new Vector3(0.5f, 0.5f, 1);
            StartCoroutine(RewardEach(effect, () => { rewardedCount++; }));
            yield return new WaitForSeconds(interval);
        }

        while (remains != rewardedCount)
            yield return null;
    }
    IEnumerator RewardEach(GameObject obj, Action eventEnd)
    {
        ProductSkill skillIndex = (ProductSkill)((mRandomSeed.Next() % 3) + 1);
        obj.GetComponent<SpriteRenderer>().sprite = skillIndex.GetSprite();

        while (true)
        {
            Frame[] next = GetRandomIdleFrames(1);
            if(next == null || next.Length <= 0)
            {
                yield return null;
            }
            else
            {
                Frame frame = next[0];
                yield return StartCoroutine(UnityUtils.MoveLinear(obj, frame.transform.position, 0.5f, 50.0f));

                Product pro = frame.ChildProduct;
                if (pro != null && !pro.IsLocked && !pro.IsChocoBlock)
                {
                    pro.ChangeProductImage(skillIndex);
                    eventEnd?.Invoke();
                    Destroy(obj);
                    if (!mItemLooping)
                        StartCoroutine(DestroySkillSimpleLoop());
                    break;
                }
                else
                {
                    yield return StartCoroutine(UnityUtils.ReSizing(obj, 0.4f, new Vector2(1, 1)));
                    yield return new WaitForSeconds(0.3f);
                    yield return StartCoroutine(UnityUtils.ReSizing(obj, 0.2f, new Vector2(0.6f, 0.6f)));
                }
            }
        }
    }

    IEnumerator CheckFinishPvpTimer()
    {
        mPVPTimerCounter = 0;
        float currentTimelimit = mStageInfo.TimeLimit;
        while (true)
        {
            float remain = currentTimelimit - PlayTime;
            if (remain <= 0)
            {
                while(!IsIdle)
                {
                    yield return null;
                }

                mPVPTimerCounter++;
                currentTimelimit += mStageInfo.TimeLimit;
                remain = currentTimelimit - PlayTime;

                Network_SyncTimer((int)remain);
                SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectCooltime);
                MenuBattle.Inst().AnimTimeoutEffect(mPVPTimerCounter);
            }

            EventRemainTime?.Invoke((int)remain);
            if (IsNoMoreMatchableProducts()) //더이상 움직일 수 있는 블럭이 없을 경우 실패
            {
                yield return new WaitForSeconds(1);
                StartFinish(false);
            }
            if(Opponent.mIsFinished) //상대방이 죽거나 종료된 상태이면 승리
            {
                yield return new WaitForSeconds(1);
                StartFinish(true);
            }
            yield return new WaitForSeconds(1);
        }
    }
    IEnumerator CloseProducts(Product[] nextPros)
    {
        mIsFlushing = true;
        List<Tuple<GameObject, Product>> obstacles = new List<Tuple<GameObject, Product>>();
        foreach (Product target in nextPros)
        {
            Vector3 startPos = target.ParentFrame.VertFrames.TopFrame.transform.position;
            startPos.y += GridSize;
            startPos.z = target.transform.position.z - 0.5f;
            GameObject obj = Instantiate(ClosePrefab, startPos, Quaternion.identity, transform);
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
                if (destProduct.IsClosed || obstacle == null)
                    continue;

                isDone = false;
                float deltaY = vel * Time.deltaTime;
                if (obstacle.transform.position.y - deltaY <= destProduct.transform.position.y)
                {
                    destProduct.IcedBlock.SetDepth(99);
                    SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectDropIce, mSFXVolume);
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
    private List<Product> GetNextCloseProducts(int cnt)
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
                if (pro == null || pro.IsClosed)
                    continue;

                products.Add(pro);
                if (products.Count >= cnt)
                    return products;
            }
        }
        return products;
    }
    private Frame[] GetVaildFrames(int yIndex)
    {
        List<Frame> rets = new List<Frame>();
        for (int x = 0; x < CountX; ++x)
            if (!mFrames[x, yIndex].Empty)
                rets.Add(mFrames[x, yIndex]);
        return rets.ToArray();
    }
    private IEnumerator StartToPushStone(int refYIdx, int remainTime)
    {
        mIsFlushing = true;
        int count = 0;

        List<ProductInfo> nextClosePros = new List<ProductInfo>();

        foreach (VerticalFrames vf in mVerticalFrames)
        {
            if(vf.BottomFrame.IndexY <= refYIdx && refYIdx <= vf.TopFrame.IndexY)
            {
                count++;
                Product stonePro = CreateNewProduct();
                stonePro.IcedBlock.SetDepth(99);

                nextClosePros.Add(new ProductInfo(stonePro.Color, stonePro.Color, ProductSkill.Nothing, vf.BottomFrame.IndexX, vf.BottomFrame.IndexY, stonePro.InstanceID, stonePro.InstanceID));

                StartCoroutine(vf.PushUpStone(stonePro, () =>
                {
                    count--;
                }));
            }
        }

        //Network_CloseProducts(nextClosePros.ToArray(), remainTime);

        while (count > 0)
            yield return null;

        mIsFlushing = false;
    }
    private IEnumerator StartToPushStoneOpp(ProductInfo[] pros, int count)
    {
        mIsFlushing = true;
        int completeCount = 0;

        for(int i = 0; i < count; ++i)
        {
            ProductInfo info = pros[i];
            VerticalFrames vf = mFrames[info.idxX, info.idxY].VertFrames;
            Product stonePro = CreateNewProduct();
            stonePro.IcedBlock.SetDepth(99);
            stonePro.InstanceID = info.nextInstID;

            StartCoroutine(vf.PushUpStone(stonePro, () =>
            {
                completeCount++;
            }));
        }

        while (count > completeCount)
            yield return null;

        mIsFlushing = false;
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
        float oneBlockScale = FieldType == GameFieldType.pvpOpponent ? UserSetting.BattleOppResize : 1.0f;
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
                        ground.name = vgGround;
                        ground.transform.position = curFrame.transform.position - new Vector3(0, GridSize, 0);
                        ground.transform.localScale = new Vector3(oneBlockScale, oneBlockScale, 1);
                        if (FieldType == GameFieldType.pvpOpponent)
                            ground.layer = LayerMask.NameToLayer("ProductOpp");
                    }
                    curFrame.transform.SetParent(vg.transform);
                }
            }
        }

        foreach (VerticalFrames group in vf)
        {
            group.init(0, oneBlockScale);
        }

        mVerticalFrames = vf.ToArray();
    }

    private Product CreateNewProduct(Frame parent, ProductColor color = ProductColor.None, int instanceID = 0)
    {
        int typeIdx = color == ProductColor.None ? RandomNextColor() : (int)color - 1;
        GameObject obj = GameObject.Instantiate(ProductPrefabs[typeIdx], parent.transform, false);
        Product product = obj.GetComponent<Product>();
        product.Manager = this;
        product.transform.localPosition = new Vector3(0, 0, -1);
        product.AttachTo(parent);
        product.InstanceID = instanceID == 0 ? product.GetInstanceID() : instanceID;
        ProductIDs[product.InstanceID] = product;
        return product;
    }
    private Product CreateNewProduct(ProductColor color = ProductColor.None, int instanceID = 0)
    {
        int typeIdx = color == ProductColor.None ? RandomNextColor() : (int)color - 1;
        GameObject obj = Instantiate(ProductPrefabs[typeIdx], transform);
        Product product = obj.GetComponent<Product>();
        product.Manager = this;
        product.InstanceID = instanceID == 0 ? product.GetInstanceID() : instanceID;
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
                if (pro != null && !pro.IsLocked && pro != target)
                    rets.Add(pro);
            }
        }
        return rets.ToArray();
    }
    private Product[] ScanAroundProducts(Frame frame, int round)
    {
        List<Product> rets = new List<Product>();
        Frame frameOf = frame;
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
                if (pro != null && !pro.IsLocked)
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

    private void ComboUp()
    {
        Billboard.CurrentCombo++;
        EventCombo?.Invoke(Billboard.CurrentCombo);
    }
    private void ComboReset()
    {
        Billboard.CurrentCombo = 1;
        EventCombo?.Invoke(Billboard.CurrentCombo);
    }
    private Product[] GetTopIceBlocks(int count)
    {
        List<Product> icedProducts = new List<Product>();
        for (int y = CountY - 1; y >= 0; y--)
        {
            for (int x = CountX - 1; x >= 0; x--)
            {
                Product pro = mFrames[x, y].ChildProduct;
                if (pro == null || pro.IsLocked || !pro.IsChocoBlock)
                    continue;

                icedProducts.Add(pro);
                if (icedProducts.Count >= count)
                    return icedProducts.ToArray();
            }
        }

        return icedProducts.ToArray();
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
        {
            return ProductSkill.Nothing;
        }
        else if (matches.Length <= UserSetting.MatchCount + 3)
        {
            ProductSkill skill = ProductSkill.Nothing;
            int ran = mRandomSeed.Next() % 4;
            if (ran == 0)
                skill = ProductSkill.Horizontal;
            else if (ran == 1)
                skill = ProductSkill.Vertical;
            else if (ran == 2)
                skill = ProductSkill.Bomb;
            else if (ran == 3)
                skill = ProductSkill.Hammer;

            return skill;
        }
        else
        {
            return ProductSkill.SameColor;
        }
    }
    private int GetChocoCount()
    {
        int count = 0;
        foreach(Frame frame in mFrames)
        {
            Product pro = frame.ChildProduct;
            if (pro != null && pro.IsChocoBlock)
                count++;
        }
        return count;
    }
    private int RandomNextColor()
    {
        return mRandomSeed.Next() % (int)mStageInfo.ColorCount;

        float colorCount = mStageInfo.ColorCount;
        if (FieldType == GameFieldType.pvpPlayer)
        {
            //pvp 에서 역전을 위한 장치 (방해블럭이 많을수록 colorCount값이 낮아진다)
            float rate = GetChocoCount() / (mFrames.Length * 0.5f);
            rate = Mathf.Min(rate, 1.0f);
            colorCount = colorCount - rate;
        }

        int range = (int)(colorCount * 10.0f);
        int ran = mRandomSeed.Next(range);
        return ran / 10;
    }
    private void ResetGame()
    {
        int cnt = transform.childCount;
        for (int i = 0; i < cnt; ++i)
            Destroy(transform.GetChild(i).gameObject);

        EventBreakTarget = null;
        EventScore = null;
        EventFinish = null;
        EventReduceLimit = null;

        AttackPointFrame.ResetPoints();
        mIsFinished = false;
        mIsItemEffect = false;
        mIsDropping = false;
        mItemLooping = false;
        mIsLightningSkill = false;
        mIsUserEventLock = false;
        mIsFlushing = false;
        mIsAutoMatching = false;
        mPrevIdleState = true;
        mDropLockCount = 0;
        mRandomSeed = null;
        mStartTime = 0;
        mSFXVolume = 1;
        mPVPTimerCounter = 0;
        mUseCombo = false;
        mIsWorkingCycle = false;
        mStartRandomSeed = -1;
        mWorkerList.Clear();

        ProductIDs.Clear();
        Billboard.Reset(this);
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
        }
    }
    private bool IsNoMoreMatchableProducts()
    {
        if (!IsIdle)
            return false;

        Dictionary<ProductColor, int> pros = new Dictionary<ProductColor, int>();
        foreach (Frame frame in mFrames)
        {
            Product pro = frame.ChildProduct;
            if (pro == null || pro.IsChocoBlock || pro.IsCapped)
                continue;

            if (pro.Skill != ProductSkill.Nothing)
                return false;

            if (pros.ContainsKey(pro.Color))
                pros[pro.Color]++;
            else
                pros[pro.Color] = 1;

            if (pros[pro.Color] >= UserSetting.MatchCount)
                return false;
        }
        return true;
    }
    private Frame[] GetRandomIdleFrames(int count)
    {
        Dictionary<int, Frame> rets = new Dictionary<int, Frame>();
        int totalCount = CountX * CountY;
        int loopCount = 0;
        while (rets.Count < count && loopCount < totalCount)
        {
            loopCount++;
            int ranIdx = mRandomSeed.Next() % totalCount;
            if (rets.ContainsKey(ranIdx))
                continue;

            int idxX = ranIdx % CountX;
            int idxY = ranIdx / CountX;
            Product pro = mFrames[idxX, idxY].ChildProduct;
            if (pro == null || pro.IsLocked || pro.Skill != ProductSkill.Nothing)
                continue;

            rets[ranIdx] = pro.ParentFrame;
        }

        return new List<Frame>(rets.Values).ToArray();
    }
    private Product[] FindNormalSameColor(ProductColor color)
    {
        List<Product> pros = new List<Product>();
        for (int y = CountY - 1; y >= 0; --y)
        {
            for (int x = 0; x < CountX; ++x)
            {
                Frame frame = mFrames[x, y];
                Product pro = frame.ChildProduct;
                if (pro != null && !pro.IsLocked && pro.Skill == ProductSkill.Nothing && pro.Color == color)
                {
                    pros.Add(pro);
                }
            }
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
    private List<Product[]> FindMatchedAllProducts(int matchCount = UserSetting.MatchCount)
    {
        Dictionary<Product, int> matchedPro = new Dictionary<Product, int>();
        List<Product[]> list = new List<Product[]>();
        foreach (Frame frame in mFrames)
        {
            Product pro = frame.ChildProduct;
            if (pro == null || pro.IsLocked || pro.IsChocoBlock || pro.Skill != ProductSkill.Nothing)
                continue;

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
    private void FindAroundProducts(Product[] targets, List<Product> rets)
    {
        foreach (Product target in targets)
        {
            Frame[] aroundFrames = target.ParentFrame.GetAroundFrames();
            foreach (Frame sub in aroundFrames)
            {
                Product arPro = sub.ChildProduct;
                if (arPro != null && !arPro.IsLocked)
                {
                    if(!rets.Contains(arPro))
                    {
                        rets.Add(arPro);
                    }
                }
            }
        }
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
            curIdx = (mRandomSeed.Next() % step) + curIdx;
            if (curIdx >= totalCount)
                break;

            int idxX = curIdx % CountX;
            int idxY = curIdx / CountX;
            Product pro = mFrames[idxX, idxY].ChildProduct;
            if (pro != null && !pro.IsLocked && !pro.IsObstacled() && !pro.ParentFrame.IsObstacled() && pro.Skill == ProductSkill.Nothing)
                rets.Add(pro);
        }
        return rets;
    }
    private void ShakeField(float intensity)
    {
        transform.position = mStartPos;
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
        SoundPlayer.Inst.PlaySoundEffect(ClipSound.Skill2, mSFXVolume);
        mIsLightningSkill = true;
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

        mIsLightningSkill = false;
        eventEnd?.Invoke();
    }
    IEnumerator UpDownSizing(GameObject obj, float duration)
    {
        float time = 0;
        Vector3 localPos = obj.transform.localPosition;
        localPos.z -= 0.01f;
        obj.transform.localPosition = localPos;
        Vector2 oriSize = obj.transform.localScale;
        Vector2 curSize = oriSize;
        while (time < duration)
        {
            float halfTime = duration / 2;
            float rate = (-1 / (halfTime * halfTime)) * (time - halfTime) * (time - halfTime) + 2;
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
        SoundPlayer.Inst.PlaySoundEffect(ClipSound.Skill3, mSFXVolume);
        Vector3 start = new Vector3(startPos.x, startPos.y, -4.0f);
        Vector3 dest = new Vector3(destPos.x, destPos.y, -4.0f);
        GameObject laserObj = GameObject.Instantiate(LaserParticle, start, Quaternion.identity, transform);
        laserObj.GetComponent<EffectLaser>().SetDestination(dest);
    }
    private void CreateStripeEffect(Vector2 startPos, bool isVertical)
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectBreakStripe1, mSFXVolume);
        Vector3 start = new Vector3(startPos.x, startPos.y, -4.0f);
        GameObject obj = GameObject.Instantiate(StripeParticle, start, isVertical ? Quaternion.Euler(0, 0, 90) : Quaternion.identity, transform);
        Destroy(obj, 1.0f);
    }
    private void CreateExplosionEffect(Vector2 startPos)
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectBreakBomb1, mSFXVolume);
        Vector3 start = new Vector3(startPos.x, startPos.y, -4.0f);
        GameObject obj = GameObject.Instantiate(ExplosionParticle, start, Quaternion.identity, transform);
        Destroy(obj, 1.0f);
    }
    private void CreateSmokeEffect(Vector2 startPos)
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectSmoke, mSFXVolume);
        Vector3 start = new Vector3(startPos.x, startPos.y, -4.0f);
        GameObject obj = GameObject.Instantiate(SmokeParticle, start, Quaternion.identity, transform);
        Destroy(obj, 1.0f);
    }
    private void CreateHammerEffect(ProductSkill skillType, Vector2 startPos, Vector2 endPos, float duration)
    {
        GameObject hammerObj = Instantiate(SimpleSpritePrefab, transform);
        hammerObj.transform.position = startPos;
        hammerObj.transform.localScale = new Vector3(0.6f, 0.6f, 1);
        hammerObj.GetComponent<SpriteRenderer>().sprite = skillType.GetSprite();

        StartCoroutine(ThrowOver(hammerObj.transform, endPos.y, duration));
        hammerObj.transform.DORotate(new Vector3(0, 0, 720), duration, RotateMode.FastBeyond360);
        hammerObj.transform.DOMoveX(endPos.x, duration).SetEase(Ease.Linear)
        .OnComplete(() =>
        {
            CreateHammerHit(endPos);
            Destroy(hammerObj);
        });
    }
    private IEnumerator ThrowOver(Transform target, float endY, float duration)
    {
        float startY = target.transform.position.y;
        // 목표지점까지 포물선을 그리며 도달한다
        float t = 0;
        float gravity = -40.0f; // 중력가속도
        //초기 투사체 발사 속도를 구하는 2차 방정식의 해
        // v = -gt + vs
        // y = -(g/2)t^2 + vst + ys
        // vs = (ye - ys + (g/2)t^2) / t
        float velocityStart = (endY - startY - gravity * 0.5f * duration * duration) / duration;
        while(true)
        {
            Vector3 pos = target.transform.position;
            velocityStart += gravity * Time.deltaTime;
            pos.y += velocityStart * Time.deltaTime;
            target.transform.position = pos;
            
            t += Time.deltaTime;
            if(duration <= t)
                break;
            else
                yield return null;
        }

        Vector3 curPos = target.transform.position;
        curPos.y = endY;
        target.transform.position = curPos;
    }
    private void CreateHammerHit(Vector3 startPos)
    {
        GameObject hammerHit = Instantiate(HammerHitPrefab, startPos, Quaternion.identity, transform);
        hammerHit.GetComponent<SpriteRenderer>().DOFade(0, 0.2f).SetDelay(0.3f);
        hammerHit.transform.DOScale(0.7f, 0.5f).From(0.4f).SetEase(Ease.OutQuart)
        .OnComplete(() => Destroy(hammerHit));
    }
    private GameObject CreateRocketEffect(Vector2 startPos, Vector2 destPos, float duration, float delay = 0)
    {
        Vector3 lookDir = new Vector3(destPos.x - startPos.x, destPos.y - startPos.y, 0);
        lookDir.Normalize();

        GameObject projectail = Instantiate(MissilePrefab, startPos, Quaternion.identity, transform);
        projectail.transform.right = lookDir;
        projectail.transform.DOMove(new Vector2(destPos.x, destPos.y), duration).SetEase(Ease.Linear).SetDelay(delay)
        .OnComplete(() =>
        {
            projectail.GetComponent<SpriteRenderer>().enabled = false;
            projectail.transform.GetChild(1).gameObject.SetActive(true);
            Destroy(projectail, 1.0f);
        });

        return projectail;
    }
    IEnumerator CreateSmokeInterval(float interval, Frame[] frames, Action<Frame> eventEachEnd, Action eventEnd)
    {
        mItemLooping = true;
        foreach (Frame frame in frames)
        {
            CreateSmokeEffect(frame.transform.position);
            eventEachEnd?.Invoke(frame);
            yield return new WaitForSeconds(interval);
        }

        mItemLooping = false;
        eventEnd?.Invoke();
    }
    IEnumerator CreateDirectBeamInterval(GameObject prefab, Vector3 startWorldPos, float interval, Frame[] frames, Action<Frame> eventEachEnd, Action eventEnd)
    {
        mItemLooping = true;
        int endCount = 0;
        foreach (Frame frame in frames)
        {
            SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectStartBeam, mSFXVolume);
            GameObject projectail = Instantiate(prefab, startWorldPos, Quaternion.identity, transform);
            StartCoroutine(UnityUtils.MoveDecelerate(projectail, frame.transform.position, 1.0f, () =>
            {
                Destroy(projectail, 1.0f);
                endCount++;
                eventEachEnd?.Invoke(frame);
            }));
            yield return new WaitForSeconds(interval);
        }

        while (endCount < frames.Length)
            yield return null;

        mItemLooping = false;
        eventEnd?.Invoke();
    }
    IEnumerator CreateMagnetTrails(GameObject prefab, Vector3 startWorldPos, Product[] objs, Action<Product> eventEachEnd, Action eventEnd)
    {
        mItemLooping = true;
        float left = 1;
        int endCount = 0;
        foreach(Product obj in objs)
        {
            SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectStartBeam, mSFXVolume);
            float rad = UnityEngine.Random.Range(-10, 45) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            dir *= left;
            GameObject projectail = Instantiate(prefab, startWorldPos, Quaternion.identity, transform);
            StartCoroutine(AnimateMagnet(projectail, dir, obj.transform.position, 30, () =>
            {
                SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectEndBeam, mSFXVolume);
                Destroy(projectail, 1.0f);
                endCount++;
                eventEachEnd?.Invoke(obj);
            }));
            yield return new WaitForSeconds(0.1f);
            left *= -1;
        }

        while (endCount < objs.Length)
            yield return null;

        mItemLooping = false;
        eventEnd?.Invoke();
    }
    IEnumerator CreateMagnetTrails(GameObject prefab, Product[] objs, Action<Product> eventEachEnd, Action eventEnd)
    {
        mItemLooping = true;
        int endCount = 0;
        foreach (Product obj in objs)
        {
            SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectStartBeam, mSFXVolume);
            float rad = UnityEngine.Random.Range(0, 360) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0);
            Vector3 startPos = dir * UnityEngine.Random.Range(1.3f, 1.8f) + transform.position;
            GameObject projectail = Instantiate(prefab, startPos, Quaternion.identity, transform);
            //projectail.transform.GetChild(1).gameObject.SetActive(false);
            StartCoroutine(AnimateMagnet(projectail, dir, obj.transform.position, 30, () =>
            {
                SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectEndBeam, mSFXVolume);
                Destroy(projectail, 1.0f);
                endCount++;
                eventEachEnd?.Invoke(obj);
            }));
            yield return new WaitForSeconds(0.1f);
        }

        while (endCount < objs.Length)
            yield return null;

        mItemLooping = false;
        eventEnd?.Invoke();
    }
    IEnumerator CreateMagnetMissiles(Vector3 startWorldPos, Product[] objs, Action<Product> eventEachEnd, Action eventEnd)
    {
        mItemLooping = true;
        float left = 1;
        int endCount = 0;
        foreach (Product obj in objs)
        {
            SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectLaunchMissile, mSFXVolume);
            float rad = UnityEngine.Random.Range(-10, 45) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            dir *= left;
            GameObject projectail = Instantiate(MissilePrefab, startWorldPos, Quaternion.identity, transform);
            StartCoroutine(AnimateMagnet(projectail, dir, obj.transform.position, 30, () =>
            {
                projectail.GetComponent<SpriteRenderer>().enabled = false;
                projectail.transform.GetChild(1).gameObject.SetActive(true);
                Destroy(projectail, 1.0f);
                endCount++;
                eventEachEnd?.Invoke(obj);
            }));
            yield return new WaitForSeconds(0.1f);
            left *= -1;
        }

        while (endCount < objs.Length)
            yield return null;

        mItemLooping = false;
        eventEnd?.Invoke();
    }
    IEnumerator CreateMeteor(Frame[] frames, Action<Frame> eventEachEnd, Action eventEnd)
    {
        mItemLooping = true;
        int endCount = 0;
        foreach (Frame frame in frames)
        {
            Vector3 destPos = frame.transform.position;
            Vector3 startPos = destPos + new Vector3(7, 7, 0);
            GameObject meteor = Instantiate(MeteorPrefab, startPos, Quaternion.identity, transform);
            StartCoroutine(UnityUtils.MoveAccelerate(meteor, destPos, 0.5f, () =>
            {
                SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectLightBomb, mSFXVolume);
                meteor.transform.GetChild(1).gameObject.SetActive(true);
                meteor.transform.GetChild(2).gameObject.SetActive(true);
                //meteor.GetComponentInChildren<Animator>().StartPlayback();
                Destroy(meteor, 1.0f);

                endCount++;
                eventEachEnd?.Invoke(frame);
            }));

            yield return new WaitForSeconds(0.3f);
        }

        while (endCount < frames.Length)
            yield return null;

        mItemLooping = false;
        eventEnd?.Invoke();
    }
    IEnumerator AnimateMagnet(GameObject obj, Vector2 startDir, Vector3 dest, float power, Action eventEnd)
    {
        yield return null;
        float dragFactor = 0.02f;
        float destFactor = 0;
        startDir.Normalize();
        Tuple<Vector2, float> startSpeed = new Tuple<Vector2, float>(startDir, UnityEngine.Random.Range(power, power + 5));

        while (true)
        {
            Vector3 curStartSpeed = startSpeed.Item1 * startSpeed.Item2;
            Vector3 dir = dest - obj.transform.position;
            dir.z = 0;
            dir.Normalize();
            Vector3 curSpeed = curStartSpeed + dir * destFactor;
            obj.transform.position += curSpeed * Time.deltaTime;
            curSpeed.Normalize();
            float deg = Mathf.Acos(Vector3.Dot(Vector3.right, curSpeed)) * Mathf.Rad2Deg;
            obj.transform.localRotation = Quaternion.Euler(0, 0, deg);

            float nextSpeed = startSpeed.Item2 - (startSpeed.Item2 * startSpeed.Item2 * dragFactor);
            nextSpeed = Mathf.Max(0, nextSpeed);
            startSpeed = new Tuple<Vector2, float>(startSpeed.Item1, nextSpeed);

            Vector3 afterDir = dest - obj.transform.position;
            afterDir.z = 0;
            afterDir.Normalize();
            if (Vector3.Dot(afterDir, dir) < 0)
            {
                obj.transform.SetPosition2D(new Vector2(dest.x, dest.y));
                eventEnd?.Invoke();
                break;
            }

            destFactor += 0.7f;
            yield return null;
        }
    }
    IEnumerator AnimateThrowSide(GameObject obj, Vector3 dest, Action eventEnd = null)
    {
        float time = 0;
        float duration = 0.6f;
        Vector3 startPos = obj.transform.position;
        Vector3 dir = dest - startPos;
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
            obj.transform.Rotate(axisZ, 5.0f);

            time += Time.deltaTime;
            yield return null;
        }

        eventEnd?.Invoke();
    }
    private Frame FindHammerTarget()
    {
        Frame scenaryFrame = null;
        int randomStartIndex = mRandomSeed.Next() % mFrames.Length;
        for (int i = 0; i < mFrames.Length; ++i)
        {
            int ranIdx = (i + randomStartIndex) % mFrames.Length;
            int idxX = ranIdx % CountX;
            int idxY = ranIdx / CountX;
            Frame frame = Frame(idxX, idxY);
            if(frame.ChildProduct != null && frame.ChildProduct.IsObstacled())
            {
                return frame;
            }

            if(scenaryFrame == null)
            {
                if(frame.ChildProduct != null)
                {
                    scenaryFrame = frame;
                }
            }
        }

        return scenaryFrame;
    }
    private bool IsObstacled(Frame frame)
    {
        if(frame.IsObstacled())
            return true;

        if(frame.ChildProduct != null)
        {
            if(frame.ChildProduct.IsObstacled())
                return true;
        }

        return false;
    }
    private void BreakObstacle(Frame frame)
    {
        if (frame.IsObstacled())
        {
            frame.BreakObstacle();
            return;
        }

        if (frame.ChildProduct != null)
        {
            if (frame.ChildProduct.IsObstacled())
            {
                frame.ChildProduct.BreakObstacle();
            }
        }
    }

    #endregion

    #region Network
    public void HandlerNetworkMessage(Header head, byte[] body)
    {
        if (!gameObject.activeInHierarchy)
            return;
        if (head.Ack == 1)
            return;

        if(head.Cmd == NetCMD.EndPVP)
        {
            mIsFinished = true;
            return;
        }

        if (head.Cmd == NetCMD.PVP)
        {
            PVPInfo resMsg = new PVPInfo();
            resMsg.Deserialize(body);
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
            if (body.cmd == PVPCommand.StartGame)
            {
                int randomSeed = body.remainTime;
                mRandomSeed = new System.Random(randomSeed);
                for (int i = 0; i < body.ArrayCount; ++i)
                {
                    ProductInfo info = body.pros[i];
                    Frame frame = mFrames[info.idxX, info.idxY];
                    Product pro = CreateNewProduct(frame);
                    pro.IcedBlock.SetDepth(0);
                    pro.gameObject.layer = LayerMask.NameToLayer("ProductOpp");
                }

                mNetMessages.RemoveFirst();
            }
            else if (body.cmd == PVPCommand.Click)
            {
                if (IsIdle && IsAllProductIdle())
                {
                    Product pro = mFrames[body.pros[0].idxX, body.pros[0].idxY].ChildProduct;
                    OnClick(pro.gameObject);

                    mNetMessages.RemoveFirst();
                }
            }
            else if (body.cmd == PVPCommand.Swipe)
            {
                if(IsIdle && IsAllProductIdle())
                {
                    Product pro = mFrames[body.pros[0].idxX, body.pros[0].idxY].ChildProduct;
                    OnSwipe(pro.gameObject, body.dir);

                    mNetMessages.RemoveFirst();
                }
            }
            else if (body.cmd == PVPCommand.FlushAttacks)
            {
                if (IsIdle && IsAllProductIdle() && AttackPointFrame.Points >= body.ArrayCount)
                {
                    int point = AttackPointFrame.Flush(body.ArrayCount);
                    List<Product> products = GetNextFlushTargets(point);
                    Product[] rets = products.ToArray();
                    if(body.ArrayCount != rets.Length)
                    {
                        LOG.warn("body: " + body.ArrayCount);
                        LOG.warn("point,ret: " + point + "," + rets.Length);
                    }
                        
                    StartCoroutine(FlushObstacles(rets, 3));

                    mNetMessages.RemoveFirst();
                }
            }
            else if (body.cmd == PVPCommand.SyncTimer)
            {
                if (IsIdle && IsAllProductIdle())
                {
                    mPVPTimerCounter = body.combo;
                    EventRemainTime?.Invoke(body.remainTime);

                    mNetMessages.RemoveFirst();
                }
                
            }
            
            /*
            else if (body.cmd == PVPCommand.Destroy)
            {
                List<Product> products = new List<Product>();
                for (int i = 0; i < body.ArrayCount; ++i)
                {
                    ProductInfo info = body.pros[i];
                    Product pro = ProductIDs[info.prvInstID];
                    if (pro != null && !pro.IsLocked && pro.Color == info.prvColor)
                        products.Add(pro);
                }

                if (products.Count == body.ArrayCount)
                {
                    Billboard.CurrentCombo = body.combo;
                    EventCombo?.Invoke(Billboard.CurrentCombo);

                    int score = body.combo * body.ArrayCount;

                    int pvpTimerCounter = body.remainTime;
                    int spa = Mathf.Max(10, UserSetting.ScorePerAttack - (10 * pvpTimerCounter));
                    int preAttackCount = Billboard.CurrentScore / spa;
                    int curAttackCount = (Billboard.CurrentScore + score) / spa;

                    Billboard.CurrentScore += score;
                    Billboard.DestroyCount += body.ArrayCount;
                    EventBreakTarget?.Invoke(transform.position, StageGoalType.Score);

                    Attack(curAttackCount - preAttackCount, products[0].transform.position);

                    if (body.skill == ProductSkill.Nothing)
                    {
                        for (int idx = 0; idx < body.ArrayCount; ++idx)
                        {
                            if (body.withLaserEffect)
                            {
                                if (idx == 0)
                                    CreateLaserEffect(transform.position, products[idx].transform.position);
                            }
                            else
                            {
                                CreateSkillEffect(products[idx]);
                            }
                            Frame parentFrame = products[idx].ParentFrame;
                            products[idx].Combo = body.combo;
                            products[idx].DestroyImmediately();
                            Product newPro = CreateNewProduct(body.pros[idx].nextColor, body.pros[idx].nextInstID);
                            newPro.gameObject.layer = LayerMask.NameToLayer("ProductOpp");
                            //newPro.transform.SetParent(parentFrame.VertFrames.transform);
                            parentFrame.VertFrames.AddNewProduct(newPro);
                            newPro.EnableMasking(parentFrame.VertFrames.MaskOrder);
                        }
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
                                Product newPro = CreateNewProduct(body.pros[idx].nextColor, body.pros[idx].nextInstID);
                                newPro.gameObject.layer = LayerMask.NameToLayer("ProductOpp");
                                //newPro.transform.SetParent(parentFrame.VertFrames.transform);
                                parentFrame.VertFrames.AddNewProduct(newPro);
                                newPro.EnableMasking(parentFrame.VertFrames.MaskOrder);
                            }
                        }
                    }

                    mNetMessages.RemoveFirst();
                }
            }
            else if (body.cmd == PVPCommand.BreakIce)
            {
                List<Product> products = new List<Product>();
                for (int i = 0; i < body.ArrayCount; ++i)
                {
                    ProductInfo info = body.pros[i];
                    Product pro = ProductIDs[info.prvInstID];
                    if (pro != null && !pro.IsLocked && pro.Color == info.prvColor && pro.IsChocoBlock)
                        products.Add(pro);
                }

                // if (products.Count == body.ArrayCount)
                // {
                //     foreach (Product pro in products)
                //         pro.BreakChocoBlock(body.combo);

                //     mNetMessages.RemoveFirst();
                // }
            }
            else if (body.cmd == PVPCommand.CloseProducts)
            {
                if (IsIdle && IsAllProductIdle())
                {
                    EventRemainTime?.Invoke(body.remainTime);
                    if(body.ArrayCount > 0)
                    {
                        StartCoroutine(StartToPushStoneOpp(body.pros, body.ArrayCount));
                    }

                    mNetMessages.RemoveFirst();
                }
            }
            else if (body.cmd == PVPCommand.UseItem)
            {
                List<Product> products = new List<Product>();
                for (int i = 0; i < body.ArrayCount; ++i)
                {
                    ProductInfo info = body.pros[i];
                    Product pro = ProductIDs[info.prvInstID];
                    if (pro != null && !pro.IsLocked && pro.Color == info.prvColor)
                        products.Add(pro);
                }

                if (products.Count == body.ArrayCount)
                {
                    CastItemEffectOnOpponent(products.ToArray(), body.item);
                    mNetMessages.RemoveFirst();
                }
            }
            else if (body.cmd == PVPCommand.DropPause)
            {
                mDropLockCount++;
                mNetMessages.RemoveFirst();
            }
            else if (body.cmd == PVPCommand.DropResume)
            {
                mDropLockCount--;
                mNetMessages.RemoveFirst();
            }
            else if (body.cmd == PVPCommand.ChangeSkill)
            {
                List<Product> products = new List<Product>();
                for (int i = 0; i < body.ArrayCount; ++i)
                {
                    ProductInfo info = body.pros[i];
                    Product pro = mFrames[info.idxX, info.idxY].ChildProduct;
                    if (pro != null && !pro.IsLocked)
                        products.Add(pro);
                }

                if (products.Count == body.ArrayCount)
                {
                    for (int i = 0; i < body.ArrayCount; ++i)
                        products[i].ChangeProductImage(body.pros[i].skill);

                    mNetMessages.RemoveFirst();
                }

            }
            */
        }
    }
    private void CastItemEffectOnOpponent(Product[] pros, PurchaseItemType item)
    {
        ItemButton[] itemSlots = MenuBattle.Inst().OpponentItemSlots;
        Vector3 startWorldPos = itemSlots[0].transform.position;
        foreach (ItemButton slot in itemSlots)
        {
            if (slot.GetItem() == item)
            {
                startWorldPos = slot.transform.position;
                slot.SetEnable(false);
                break;
            }
        }

        if (item == PurchaseItemType.ExtendLimit)
        {
            Vector3 destWorldPos = MenuBattle.Inst().OpponentLimit.transform.position;
            GameObject missile = Instantiate(TrailingPrefab, startWorldPos, Quaternion.identity, transform);
            StartCoroutine(UnityUtils.MoveDecelerate(missile, destWorldPos, 0.3f, () =>
            {
                if (mStageInfo.TimeLimit > 0)
                {
                    int remainSec = MenuBattle.StringToSec(MenuBattle.Inst().OpponentLimit.text);
                    remainSec += 10; //10초 연장
                    EventRemainTime?.Invoke(remainSec);
                }
            }));
        }
        else if (item == PurchaseItemType.RemoveIce)
        {
            StartCoroutine(CreateMagnetMissiles(startWorldPos, pros, null, null));
        }
        else if (item == PurchaseItemType.MakeSkill1)
        {
            StartCoroutine(CreateMagnetTrails(TrailingPrefab, startWorldPos, pros, null, null));
        }
        else if (item == PurchaseItemType.MakeSkill2)
        {
            StartCoroutine(CreateMagnetTrails(TrailingPrefab, pros, null, null));
        }
        else if (item == PurchaseItemType.MakeCombo)
        {
            List<Frame> frames = new List<Frame>();
            foreach (Product pro in pros)
                frames.Add(pro.ParentFrame);

            StartCoroutine(CreateDirectBeamInterval(TrailingPrefab, startWorldPos, 0.2f, frames.ToArray(), null, null));
        }
        else if (item == PurchaseItemType.Meteor)
        {
            List<Frame> frames = new List<Frame>();
            foreach (Product pro in pros)
                frames.Add(pro.ParentFrame);

            StartCoroutine(CreateMeteor(frames.ToArray(), (frame) => { ShakeField(0.05f); }, null));
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
        if (FieldType != GameFieldType.pvpPlayer || mIsFinished)
            return;

        PVPInfo req = new PVPInfo();
        req.cmd = PVPCommand.StartGame;
        req.oppUserPk = InstPVP_Opponent.UserPk;
        req.XCount = CountX;
        req.YCount = CountY;
        req.colorCount = mStageInfo.ColorCount;
        req.combo = 0;
        req.remainTime = mStartRandomSeed;
        req.pros = pros;

        if (!NetClientApp.GetInstance().Request(NetCMD.PVP, req, Network_PVPAck))
            StartFinish(false);
    }
    private void Network_Click(Product pro)
    {
        if (FieldType != GameFieldType.pvpPlayer || mIsFinished)
            return;

        PVPInfo req = new PVPInfo();
        req.cmd = PVPCommand.Click;
        req.oppUserPk = InstPVP_Opponent.UserPk;
        req.combo = pro.Combo;
        req.pros = new ProductInfo[1];
        req.pros[0].idxX = pro.ParentFrame.IndexX;
        req.pros[0].idxY = pro.ParentFrame.IndexY;
        req.pros[0].prvColor = pro.Color;
        if (!NetClientApp.GetInstance().Request(NetCMD.PVP, req, Network_PVPAck))
            StartFinish(false);
    }
    private void Network_Swipe(Product pro, SwipeDirection dir)
    {
        if (FieldType != GameFieldType.pvpPlayer || mIsFinished)
            return;

        PVPInfo req = new PVPInfo();
        req.cmd = PVPCommand.Swipe;
        req.oppUserPk = InstPVP_Opponent.UserPk;
        req.combo = pro.Combo;
        req.dir = dir;
        req.pros = new ProductInfo[1];
        req.pros[0].idxX = pro.ParentFrame.IndexX;
        req.pros[0].idxY = pro.ParentFrame.IndexY;
        req.pros[0].prvColor = pro.Color;
        if (!NetClientApp.GetInstance().Request(NetCMD.PVP, req, Network_PVPAck))
            StartFinish(false);
    }
    private void Network_Destroy(ProductInfo[] pros, ProductSkill skill, bool withLaserEffect, int timerCounter)
    {
        return;
        if (FieldType != GameFieldType.pvpPlayer || mIsFinished)
            return;

        PVPInfo req = new PVPInfo();
        req.cmd = PVPCommand.Destroy;
        req.oppUserPk = InstPVP_Opponent.UserPk;
        req.combo = Billboard.CurrentCombo;
        req.skill = skill;
        req.remainTime = timerCounter;
        req.withLaserEffect = withLaserEffect;
        req.pros = pros;

        if (!NetClientApp.GetInstance().Request(NetCMD.PVP, req, Network_PVPAck))
            StartFinish(false);
    }
    private void Network_BreakIce(ProductInfo[] pros)
    {
        return;
        if (FieldType != GameFieldType.pvpPlayer || mIsFinished)
            return;

        PVPInfo req = new PVPInfo();
        req.cmd = PVPCommand.BreakIce;
        req.oppUserPk = InstPVP_Opponent.UserPk;
        req.combo = Billboard.CurrentCombo;
        req.pros = pros;

        if (!NetClientApp.GetInstance().Request(NetCMD.PVP, req, Network_PVPAck))
            StartFinish(false);
    }
    private void Network_UseItem(ProductInfo[] pros, PurchaseItemType item)
    {
        return;
        if (FieldType != GameFieldType.pvpPlayer || mIsFinished)
            return;

        PVPInfo req = new PVPInfo();
        req.cmd = PVPCommand.UseItem;
        req.oppUserPk = InstPVP_Opponent.UserPk;
        req.combo = Billboard.CurrentCombo;
        req.item = item;
        req.pros = pros;

        if (!NetClientApp.GetInstance().Request(NetCMD.PVP, req, Network_PVPAck))
            StartFinish(false);
    }
    private void Network_ChangeSkill(ProductInfo[] pros)
    {
        return;
        if (FieldType != GameFieldType.pvpPlayer || mIsFinished)
            return;

        PVPInfo req = new PVPInfo();
        req.cmd = PVPCommand.ChangeSkill;
        req.oppUserPk = InstPVP_Opponent.UserPk;
        req.pros = pros;

        if (!NetClientApp.GetInstance().Request(NetCMD.PVP, req, Network_PVPAck))
            StartFinish(false);
    }
    private void Network_FlushAttacks(ProductInfo[] pros, int level)
    {
        if (FieldType != GameFieldType.pvpPlayer || mIsFinished)
            return;

        PVPInfo req = new PVPInfo();
        req.cmd = PVPCommand.FlushAttacks;
        req.oppUserPk = InstPVP_Opponent.UserPk;
        req.combo = level;
        req.pros = pros;

        if (!NetClientApp.GetInstance().Request(NetCMD.PVP, req, Network_PVPAck))
            StartFinish(false);
    }
    private void Network_SyncTimer(int remainSec)
    {
        if (FieldType != GameFieldType.pvpPlayer || mIsFinished)
            return;

        PVPInfo req = new PVPInfo();
        req.cmd = PVPCommand.SyncTimer;
        req.oppUserPk = InstPVP_Opponent.UserPk;
        req.remainTime = remainSec;
        req.combo = mPVPTimerCounter;
        if (!NetClientApp.GetInstance().Request(NetCMD.PVP, req, Network_PVPAck))
            StartFinish(false);
    }
    private void Network_PVPAck(byte[] _resBody)
    {
        UserInfo oppUser = Utils.Deserialize<UserInfo>(ref _resBody);
        if (oppUser.userPk < 0)
            StartFinish(true);
    }
    #endregion
}

