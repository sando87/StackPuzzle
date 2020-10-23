using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum InGameState { Noting, Running, Paused, Win, Lose }
public class InGameManager : MonoBehaviour
{
    private static InGameManager mInst = null;
    public static InGameManager Inst
    {
        get
        {
            if (mInst == null)
                mInst = GameObject.Find("WorldSpace").transform.Find("GameScreen/GameField").GetComponent<InGameManager>();
            return mInst;
        }
    }

    public GameObject[] ProductPrefabs;
    public GameObject FramePrefab1;
    public GameObject FramePrefab2;
    public GameObject MaskPrefab;

    private Frame[,] mFrames = null;
    private StageInfo mStageInfo = null;

    private int mIdleCounter = -1;
    private ProductColor mSkipColor = ProductColor.None;
    private Queue<ProductSkill> mNextSkills = new Queue<ProductSkill>();
    private Dictionary<int,Frame> mDestroyes = new Dictionary<int, Frame>();

    public bool IsIdle { get { return mIdleCounter < 0; } }
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
        SoundPlayer.Inst.PlayBackMusic(SoundPlayer.Inst.BackMusicInGame);

        //GameObject mask = Instantiate(MaskPrefab, transform);
        //mask.transform.localScale = new Vector3(info.XCount * 0.97f, info.YCount * 0.97f, 1);

        GetComponent<SwipeDetector>().EventSwipe = OnSwipe;

        StartCoroutine(CheckIdle());
        StartCoroutine(CreateNextProducts());

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
            int starCount = 3;
            Stage currentStage = StageManager.Inst.GetStage(mStageInfo.Num);
            currentStage.UpdateStarCount(starCount);

            Stage nextStage = StageManager.Inst.GetStage(mStageInfo.Num + 1);
            if (nextStage != null)
                nextStage.UnLock();

            SoundPlayer.Inst.Player.Stop();
            MenuComplete.PopUp(mStageInfo.Num, starCount, MenuInGame.Inst().CurrentCombo);
        }
        else
        {
            LOG.echo(SummaryToCSVString(false));
            SoundPlayer.Inst.Player.Stop();
            MenuFailed.PopUp(mStageInfo.Num, mStageInfo.GoalValue, MenuInGame.Inst().CurrentCombo);
        }

        ResetGame();
        transform.parent.gameObject.SetActive(false);
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
            mIdleCounter = 2;
        }
    }
    private void OnMatch(List<Product> matches)
    {
        mIdleCounter--;
        if (MatchLock || matches.Count < UserSetting.MatchCount)
            return;

        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectMatched);

        MakeSkillProduct(matches.Count);

        bool isSameColorEnable = false;
        foreach (Product pro in matches)
        {
            if (pro.mSkill == ProductSkill.BreakSameColor)
            {
                isSameColorEnable = true;
                break;
            }
        }

        Product mainProduct = matches[0];
        List<Product> destroies = isSameColorEnable ? GetSameColorProducts(mainProduct.mColor) : matches;
        int currentCombo = mainProduct.IsSwipe ? MenuInGame.Inst().UseNextCombo() : MenuInGame.Inst().CurrentCombo;
        foreach (Product pro in destroies)
        {
            pro.Combo = currentCombo + 1;
            pro.StartDestroy();
            AddScore(pro);
        }

        mainProduct.StartFlash(matches);
    }
    private void BreakItemSkill(Product product)
    {
        if (product.mSkill == ProductSkill.MatchOneMore)
        {
            MenuInGame.Inst().OneMoreCombo(product);
            MenuInGame.Inst().ReduceGoalValue(product.transform.position, StageGoalType.ItemOneMore);
        }
        else if (product.mSkill == ProductSkill.KeepCombo)
        {
            MenuInGame.Inst().KeepNextCombo(product);
            MenuInGame.Inst().ReduceGoalValue(product.transform.position, StageGoalType.ItemKeepCombo);
        }
        else if (product.mSkill == ProductSkill.BreakSameColor)
        {
            MenuInGame.Inst().ReduceGoalValue(product.transform.position, StageGoalType.ItemSameColor);
        }
    }
    private List<Product> GetSameColorProducts(ProductColor color)
    {
        List<Product> list = new List<Product>();
        foreach (Frame frame in mFrames)
        {
            if (frame.Empty || frame.ChildProduct == null || frame.ChildProduct.IsLocked())
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

        switch (matchedCount)
        {
            case 5:
                if(mStageInfo.ItemOneMore)
                    mNextSkills.Enqueue(ProductSkill.MatchOneMore);
                break;
            case 6:
                if (mStageInfo.ItemKeepCombo)
                    mNextSkills.Enqueue(ProductSkill.KeepCombo);
                break;
            case 7:
                if (mStageInfo.ItemSameColor)
                    mNextSkills.Enqueue(ProductSkill.BreakSameColor);
                break;
            default:
                if (mStageInfo.ItemSameColor)
                    mNextSkills.Enqueue(ProductSkill.BreakSameColor);
                break;
        }
    }
    private void OnDestroyProduct(Product pro)
    {
        int idxX = pro.ParentFrame.IndexX;
        if (!mDestroyes.ContainsKey(idxX))
            mDestroyes[idxX] = pro.ParentFrame;
        else
        {
            if (pro.ParentFrame.IndexY < mDestroyes[idxX].IndexY)
                mDestroyes[idxX] = pro.ParentFrame;
        }
    }

    private Frame[] NextBaseFrames(Frame baseFrame)
    {
        List<Frame> bases = new List<Frame>();
        int idxX = baseFrame.IndexX;
        int curIdxY = baseFrame.IndexY;
        bool pushed = false;
        while(curIdxY < CountY)
        {
            Frame frame = mFrames[idxX, curIdxY];
            if (frame.Empty)
                pushed = false;
            else if (!pushed && frame.ChildProduct == null)
            {
                bases.Add(frame);
                pushed = true;
            }
            curIdxY++;
        }
        return bases.ToArray();
    }

    private void StartNextProducts(Frame baseFrame)
    {
        Frame curFrame = baseFrame;
        Frame validFrame = curFrame;
        while (curFrame != null)
        {
            Product pro = NextUpProductFrom(validFrame);
            if (pro == null)
                break;

            validFrame = pro.ParentFrame;
            pro.StartDropAnimate(curFrame, pro.ParentFrame.IndexY - curFrame.IndexY, curFrame == baseFrame);

            curFrame = curFrame.Up();
        }

        Frame topFrame = SubTopFrame(curFrame);
        int emptyCount = topFrame.IndexY - curFrame.IndexY + 1;
        while (curFrame != null)
        {
            Product pro = CreateNewProduct(curFrame, mNextSkills.Count > 0 ? mNextSkills.Dequeue() : ProductSkill.Nothing);
            pro.StartDropAnimate(curFrame, emptyCount, curFrame == baseFrame);

            curFrame = curFrame.Up();
        }
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
    private IEnumerator CheckIdle()
    {
        while(true)
        {
            if(mIdleCounter == 0)
            {
                if (IsAllIdle())
                {
                    mIdleCounter = -1; //set Idle enable
                    MenuInGame.Inst().CurrentCombo = 0;
                    EventOnIdle?.Invoke();
                }
                else
                {
                    mIdleCounter = 1;
                    MenuInGame.Inst().CurrentCombo++;
                }
            }
            yield return null;
        }
    }

    private IEnumerator CreateNextProducts()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.1f);

            int comborableCounter = 0;
            for (int x = 0; x < CountX; ++x)
            {
                if (!mDestroyes.ContainsKey(x))
                    continue;

                Frame[] baseFrames = NextBaseFrames(mDestroyes[x]);
                if (baseFrames.Length <= 0)
                    continue;

                comborableCounter += baseFrames.Length;
                foreach (Frame baseFrame in baseFrames)
                    StartNextProducts(baseFrame);

                mDestroyes.Remove(x);
            }

            if (comborableCounter > 0)
                mIdleCounter = comborableCounter;
        }
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
        int colorCount = Math.Min(mStageInfo.ColorCount, ProductPrefabs.Length);
        int typeIdx = UnityEngine.Random.Range(0, colorCount);
        if (mSkipColor != ProductColor.None && ProductPrefabs[typeIdx].GetComponent<Product>().mColor == mSkipColor)
        {
            int nextIdx = UnityEngine.Random.Range(1, colorCount);
            typeIdx = (typeIdx + nextIdx) % colorCount;
        }
        GameObject obj = GameObject.Instantiate(ProductPrefabs[typeIdx], parent.transform, false);
        Product product = obj.GetComponent<Product>();
        product.transform.localPosition = new Vector3(0, 0, -1);
        product.ParentFrame = parent;
        product.EventMatched = OnMatch;
        product.EventDestroyed = OnDestroyProduct;
        product.ChangeSkilledProduct(skill);
        return product;
    }

    private void ResetGame()
    {
        int cnt = transform.childCount;
        for (int i = 0; i < cnt; ++i)
            Destroy(transform.GetChild(i).gameObject);

        mFrames = null;
        mStageInfo = null;
        Pause = false;
        mSkipColor = ProductColor.None;
        MatchLock = false;
    }
    private Product NextUpProductFrom(Frame frame)
    {
        Frame curFrame = frame;
        while (curFrame != null)
        {
            if (curFrame.ChildProduct != null)
                return curFrame.ChildProduct;

            curFrame = curFrame.Up();
        }
        return null;
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
        //stageNum, XCount, YCount, ColorCount, GoalType, GoalValue, MoveLimit, Item(1-1-1-1), Success, CurScore, MaxCombo, MoveCount, Item(4-2-5-0)
        string ret = mStageInfo.Num + ","
            + CountX + ","
            + CountY + ","
            + mStageInfo.ColorCount + ","
            + mStageInfo.GoalType + ","
            + mStageInfo.GoalValue + ","
            + mStageInfo.MoveLimit + ","
            + "Item("
            + mStageInfo.ItemOneMore + "-"
            + mStageInfo.ItemKeepCombo + "-"
            + mStageInfo.ItemSameColor + "-"
            + mStageInfo.ItemReduceColor + "),"
            + success + ",";

        return ret;
    }


}
