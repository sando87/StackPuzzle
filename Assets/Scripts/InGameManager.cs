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
    public static InGameManager Inst
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

    private Queue<ProductSkill> mNextSkills = new Queue<ProductSkill>();
    private List<Frame> mEmptyFrames = new List<Frame>();
    private LinkedList<Header> mNetMessages = new LinkedList<Header>();
    public InGameBillboard Billboard = new InGameBillboard();

    public GameFieldType FieldType { get {
            return this == mInstStage ? GameFieldType.Stage :
                (this == mInstPVP_Player ? GameFieldType.pvpPlayer :
                (this == mInstPVP_Opponent ? GameFieldType.pvpOpponent : GameFieldType.Noting)); }
    }
    public bool IsIdle { get; private set; }
    public int CountX { get { return mStageInfo.XCount; } }
    public int CountY { get { return mStageInfo.YCount; } }
    public bool MatchLock { get; set; }
    public bool Pause { get; set; }
    public AttackPoints AttackPoints { get; set; }
    public InGameBillboard GetBillboard() { return Billboard; }
    

    public Action<Vector3, StageGoalType> EventBreakTarget;
    public Action<Product[]> EventDestroyed;
    public Action<bool> EventFinish;
    public Action EventReduceLimit;
    private Action EventEnterIdle;

    public void StartGame(StageInfo info)
    {
        ResetGame();
        Vector3 pos = transform.position;

        transform.parent.gameObject.SetActive(true);
        Pause = false;
        mStageInfo = info;

        //GameObject mask = Instantiate(MaskPrefab, transform);
        //mask.transform.localScale = new Vector3(info.XCount * 0.97f, info.YCount * 0.97f, 1);

        GetComponent<SwipeDetector>().EventSwipe = OnSwipe;
        GetComponent<SwipeDetector>().EventClick = OnClick;
        EventEnterIdle = CheckTartgetLimit;

        if (FieldType == GameFieldType.pvpOpponent)
            StartCoroutine(ProcessNetMessages());


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
                Product pro = CreateNewProduct(mFrames[x, y]);
                pro.SetChocoBlock(info.GetCell(x, y).ProductChocoCount);
                pro.EventUnWrapChoco = () => {
                    Billboard.ChocoCount++;
                    EventBreakTarget?.Invoke(pro.transform.position, StageGoalType.Choco);
                };
            }
        }

        SecondaryInitFrames();

        GameObject ap = Instantiate(AttackPointPrefab, transform);
        ap.transform.localPosition = localBasePos + new Vector3(-gridSize + 0.2f, gridSize * CountY - 0.1f, 0);
        AttackPoints = ap.GetComponent<AttackPoints>();
    }
    public void FinishGame(bool success)
    {
        if (!transform.parent.gameObject.activeSelf)
            return;

        EventFinish?.Invoke(success);
        ResetGame();
        transform.parent.gameObject.SetActive(false);
    }


    public void OnClick(GameObject obj)
    {
        if (!IsIdle)
            return;

        Product pro = obj.GetComponent<Product>();
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
        RemoveLimit();
    }
    public void OnSwipe(GameObject obj, SwipeDirection dir)
    {
        Product product = obj.GetComponent<Product>();
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
            product.StartSwipe(targetProduct.GetComponentInParent<Frame>());
            targetProduct.StartSwipe(product.GetComponentInParent<Frame>());
        }
    }
    private void DestroyProducts(List<Product> matches)
    {
        Product mainProduct = matches[0];
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectMatched);

        int addedScore = 0;
        foreach (Product pro in matches)
        {
            addedScore += Billboard.CurrentCombo;
            pro.Combo = Billboard.CurrentCombo;
            pro.StartDestroy(gameObject);
            mEmptyFrames.Add(pro.ParentFrame);
            BreakItemSkill(pro);
        }

        ReduceTargetScoreCombo(mainProduct, Billboard.CurrentScore, Billboard.CurrentScore + addedScore);

        Billboard.CurrentScore += addedScore;
        Billboard.DestroyCount += matches.Count;

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
        }
        else
        {
            Billboard.CurrentCombo = 0;
        }

        StartCoroutine(Utils.CallAfterSeconds(0.5f, () =>
        {
            CreateNewProducts(emptyFrames);
        }));
    }
    void CreateNewProducts(Frame[] emptyFrames)
    {
        foreach(Frame frame in emptyFrames)
        {
            Product newProduct = CreateNewProduct(frame);
            newProduct.mAnimation.Play("swap");
        }

        StartCoroutine(Utils.CallAfterSeconds(1.0f, () =>
        {
            if (IsAllIdle())
            {
                IsIdle = true;
                EventEnterIdle?.Invoke();
            }
        }));
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
    void CheckTartgetLimit()
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
            FinishGame(true);
        else if (Billboard.MoveCount >= mStageInfo.MoveLimit)
            FinishGame(false);

        return;
    }
    public void HandlerNetworkMessage(Header responseMsg)
    {
        if (!gameObject.activeInHierarchy)
            return;
        if (responseMsg.Ack == 1)
            return;


        if (responseMsg.Cmd == NetCMD.EndGame)
        {
            mNetMessages.AddFirst(responseMsg);
        }
        else
        {
            mNetMessages.AddLast(responseMsg);
        }
    }
    IEnumerator ProcessNetMessages()
    {
        while (true)
        {
            yield return null;

            if (mNetMessages.Count == 0)
                continue;

            Header msg = mNetMessages.First.Value;
            if (msg.Cmd == NetCMD.EndGame)
            {
                EndGame res = msg.body as EndGame;
                FinishGame(!res.win);
            }
            else if (msg.Cmd == NetCMD.SendSwipe)
            {
                SwipeInfo res = msg.body as SwipeInfo;
                Product pro = mFrames[res.idxX, res.idxY].ChildProduct;
                if (res.isClick)
                    OnClick(pro.gameObject);
                else
                    OnSwipe(pro.gameObject, res.dir);

                mNetMessages.RemoveFirst();
            }
            else if (msg.Cmd == NetCMD.SendChoco)
            {
                ChocoInfo res = msg.body as ChocoInfo;
                AttackPoints.Pop(res.xIndicies.Length);
                for (int i = 0; i < res.xIndicies.Length; ++i)
                {
                    int idxX = res.xIndicies[i];
                    int idxY = res.yIndicies[i];
                    Product pro = GetFrame(idxX, idxY).ChildProduct;
                    pro.SetChocoBlock(1, true);
                }

                mNetMessages.RemoveFirst();
            }
        }
    }



    private void ReduceTargetScoreCombo(Product pro, int preScore, int nextScore)
    {
        if (mStageInfo.GoalTypeEnum == StageGoalType.Score)
        {
            int newStarCount = nextScore / UserSetting.ScorePerBar - preScore / UserSetting.ScorePerBar;
            for(int i = 0; i < newStarCount; ++i)
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
    public bool IsAllIdle()
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
    private Product CreateNewProduct(Frame parent, ProductSkill skill = ProductSkill.Nothing)
    {
        int typeIdx = RandomNextColor();
        GameObject obj = GameObject.Instantiate(ProductPrefabs[typeIdx], parent.transform, false);
        Product product = obj.GetComponent<Product>();
        product.transform.localPosition = new Vector3(0, 0, -1);
        product.ParentFrame = parent;
        product.ChangeSkilledProduct(skill);
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

        mNextSkills.Clear();
        mEmptyFrames.Clear();

        mFrames = null;
        mStageInfo = null;
        Pause = false;
        MatchLock = false;
        IsIdle = true;
        Billboard.Reset();
    }
    public Frame GetFrame(int x, int y)
    {
        if (x < 0 || x >= mStageInfo.XCount || y < 0 || y >= mStageInfo.YCount)
            return null;
        if (mFrames[x, y].Empty)
            return null;
        return mFrames[x, y];
    }

    private void RemoveLimit()
    {
        Billboard.MoveCount++;
        EventReduceLimit?.Invoke();
    }
    public string SummaryToCSVString(bool success)
    {
        //stageNum, XCount, YCount, ColorCount, GoalType, GoalValue, MoveLimit, Item(1-1-1-1), Success, CurScore, RemainLimit, HeartCount
        string ret = mStageInfo.Num + ","
            + CountX + ","
            + CountY + ","
            + mStageInfo.ColorCount + ","
            + mStageInfo.GoalType + ","
            + mStageInfo.GoalValue + ","
            + mStageInfo.MoveLimit + ","
            + StageInfo.ItemToString(mStageInfo.Items) + ","
            + success + ","
            + Billboard.CurrentScore + ","
            + Billboard.MoveCount + ","
            + Billboard.MaxCombo + ","
            + Purchases.CountHeart();

        return ret;
    }


}
