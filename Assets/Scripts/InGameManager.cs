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

    private Frame[,] mFrames = null;
    private StageInfo mStageInfo = null;

    private bool mIsIdle = true;
    private Queue<ProductSkill> mNextSkills = new Queue<ProductSkill>();
    private List<Frame> mDestroyes = new List<Frame>();

    public GameFieldType FieldType { get {
            return this == mInstStage ? GameFieldType.Stage :
                (this == mInstPVP_Player ? GameFieldType.pvpPlayer :
                (this == mInstPVP_Opponent ? GameFieldType.pvpOpponent : GameFieldType.Noting)); }
    }
    public bool IsIdle { get { return mIsIdle; } }
    public int CountX { get { return mStageInfo.XCount; } }
    public int CountY { get { return mStageInfo.YCount; } }
    public Frame[,] Frames { get { return mFrames; } }
    public bool MatchLock { get; set; }
    public bool Pause { get; set; }
    public Action<InGameBillboard, Product> EventOnChange;
    public Action EventOnIdle;

    public void StartGame(StageInfo info)
    {
        ResetGame();

        transform.parent.gameObject.SetActive(true);
        Pause = false;
        mStageInfo = info;

        //GameObject mask = Instantiate(MaskPrefab, transform);
        //mask.transform.localScale = new Vector3(info.XCount * 0.97f, info.YCount * 0.97f, 1);

        GetComponent<SwipeDetector>().EventSwipe = OnSwipe;
        GetComponent<SwipeDetector>().EventClick = OnClick;


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
                mFrames[x, y].Initialize(x, y, info.GetCell(x, y).FrameCoverCount);
                mFrames[x, y].GetFrame = GetFrame;
                mFrames[x, y].EventBreakCover = () => {
                    MenuInGame.Inst().ReduceGoalValue(mFrames[x, y].transform.position, StageGoalType.Cover);
                };
                Product pro = CreateNewProduct(mFrames[x, y]);
                pro.SetChocoBlock(info.GetCell(x, y).ProductChocoCount);
                pro.EventUnWrapChoco = () => {
                    MenuInGame.Inst().ReduceGoalValue(pro.transform.position, StageGoalType.Choco);
                };
            }
        }

        SecondaryInitFrames();
    }
    public void FinishGame(bool success)
    {
        if (success)
        {
            LOG.echo(SummaryToCSVString(true));
            int starCount = GetGrade();
            Stage currentStage = StageManager.Inst.GetStage(mStageInfo.Num);
            currentStage.UpdateStarCount(starCount);

            Stage nextStage = StageManager.Inst.GetStage(mStageInfo.Num + 1);
            if (nextStage != null)
                nextStage.UnLock();

            SoundPlayer.Inst.Player.Stop();
            SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectSuccess);
            MenuComplete.PopUp(mStageInfo.Num, starCount, MenuInGame.Inst().Score);
        }
        else
        {
            LOG.echo(SummaryToCSVString(false));
            SoundPlayer.Inst.Player.Stop();
            SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectGameOver);
            MenuFailed.PopUp(mStageInfo.Num, mStageInfo.GoalValue, mStageInfo.GoalTypeImage, MenuInGame.Inst().Score);
        }

        ResetGame();
        transform.parent.gameObject.SetActive(false);
    }


    public void OnClick(GameObject obj)
    {
    }
    public void OnSwipe(GameObject obj, SwipeDirection dir)
    {
        if (!IsIdle)
            return;

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
            mIsIdle = false;

            StartCoroutine(Utils.CallAfterSeconds(0.3f, () =>
            {
                bool isMatched = false;
                isMatched |= TryMatch(product);
                isMatched |= TryMatch(targetProduct);
                mIsIdle = isMatched ? false : true;
            }));
        }
    }
    private bool TryMatch(Product mainProduct)
    {
        List<Product> matches = new List<Product>();
        mainProduct.SearchMatchedProducts(matches, mainProduct.mColor);
        if (MatchLock || matches.Count < UserSetting.MatchCount)
            return false;

        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectMatched);

        MakeSkillProduct(matches.Count);

        bool isSameColorEnable = false;
        foreach (Product pro in matches)
        {
            if (pro.mSkill == ProductSkill.SameColor)
            {
                isSameColorEnable = true;
                break;
            }
        }

        List<Product> destroies = isSameColorEnable ? GetSameColorProducts(mainProduct.mColor) : matches;
        int currentCombo = MenuInGame.Inst().CurrentCombo;
        if (mainProduct.IsFirst && MenuInGame.Inst().NextCombo > 0)
        {
            currentCombo = MenuInGame.Inst().NextCombo;
            MenuInGame.Inst().NextCombo = 0;
        }

        int preScore = MenuInGame.Inst().Score;
        int addedScore = 0;
        float delay = 0;
        List<Frame> emptyFrames = new List<Frame>();
        foreach (Product pro in destroies)
        {
            addedScore += currentCombo + 1;
            pro.Combo = currentCombo + 1;
            delay = (pro.transform.position - mainProduct.transform.position).magnitude;
            delay = delay / 7.0f;
            pro.StartDestroy(gameObject, delay);
            emptyFrames.Add(pro.ParentFrame);
            AddScore(pro);
        }

        MenuInGame.Inst().CurrentCombo = currentCombo + 1;
        ReduceTargetScoreCombo(mainProduct, preScore, preScore + addedScore);

        StartCoroutine(Utils.CallAfterSeconds(1.0f, () =>
        {
            TryMatch(emptyFrames.ToArray());
        }));

        StartCoroutine(Utils.CallAfterSeconds(1.5f, () =>
        {
            CreateNewProducts(emptyFrames.ToArray());
        }));

        return true;
    }
    private bool TryMatch(Frame[] emptyFrames)
    {
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

        bool isMatched = false;
        foreach (Product pro in aroundProducts)
            isMatched |= TryMatch(pro);

        return isMatched ? true : false;
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
                mIsIdle = true;
        }));
    }




    public int GetGrade()
    {
        float totlaRemain = mStageInfo.MoveLimit;
        float currentRemin = MenuInGame.Inst().RemainLimit;
        if (totlaRemain < 5)
            return 3;
        else if (totlaRemain * 0.4f < currentRemin)
            return 3;
        else if (totlaRemain * 0.2f < currentRemin)
            return 2;
        else if (0 < currentRemin)
            return 1;

        return 0;
    }
    private void ReduceTargetScoreCombo(Product pro, int preScore, int nextScore)
    {
        if (mStageInfo.GoalTypeEnum == StageGoalType.Score)
        {
            int newStarCount = nextScore / UserSetting.ScorePerBar - preScore / UserSetting.ScorePerBar;
            for(int i = 0; i < newStarCount; ++i)
                MenuInGame.Inst().ReduceGoalValue(pro.transform.position, StageGoalType.Score);
        }
        else if (mStageInfo.GoalTypeEnum == StageGoalType.Combo)
        {
            string goalType = mStageInfo.GoalType;
            int targetCombo = int.Parse(goalType[goalType.Length - 1].ToString());
            int curCombo = MenuInGame.Inst().CurrentCombo;
            if (targetCombo == curCombo)
                MenuInGame.Inst().ReduceGoalValue(pro.transform.position, StageGoalType.Combo);
        }
    }
    private void BreakItemSkill(Product product)
    {
        if (product.mSkill == ProductSkill.OneMore)
        {
            MenuInGame.Inst().OneMoreCombo(product);
            MenuInGame.Inst().ReduceGoalValue(product.transform.position, StageGoalType.ItemOneMore);
        }
        else if (product.mSkill == ProductSkill.KeepCombo)
        {
            MenuInGame.Inst().KeepNextCombo(product);
            MenuInGame.Inst().ReduceGoalValue(product.transform.position, StageGoalType.ItemKeepCombo);
        }
        else if (product.mSkill == ProductSkill.SameColor)
        {
            MenuInGame.Inst().ReduceGoalValue(product.transform.position, StageGoalType.ItemSameColor);
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
        mDestroyes.Clear();

        mFrames = null;
        mStageInfo = null;
        Pause = false;
        MatchLock = false;
        mIsIdle = true;
    }
    public Frame GetFrame(int x, int y)
    {
        if (x < 0 || x >= mStageInfo.XCount || y < 0 || y >= mStageInfo.YCount)
            return null;
        if (mFrames[x, y].Empty)
            return null;
        return mFrames[x, y];
    }
    private void AddScore(Product product)
    {
        BreakItemSkill(product);
        MenuInGame.Inst().AddScore(product);
    }

    private void RemoveLimit()
    {
        MenuInGame.Inst().ReduceLimit();
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
            + MenuInGame.Inst().Score + ","
            + MenuInGame.Inst().RemainLimit +","
            + Purchases.CountHeart();

        return ret;
    }


}
