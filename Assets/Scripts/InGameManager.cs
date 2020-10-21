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

    private bool mIsIdle = false;
    private Product mSwipedProductA = null;
    private Product mSwipedProductB = null;
    private InGameBillboard mBillboard = new InGameBillboard();
    private ProductColor mSkipColor = ProductColor.None;
    private Queue<ProductSkill> mNextSkills = new Queue<ProductSkill>();
    private Dictionary<int,Frame> mDestroyes = new Dictionary<int, Frame>();

    public bool IsIdle { get { return mIsIdle; } }
    public int CountX { get { return mStageInfo.XCount; } }
    public int CountY { get { return mStageInfo.YCount; } }
    public Frame[,] Frames { get { return mFrames; } }
    public bool MatchLock { get; set; }
    public bool Pause { get; set; }
    public Action<InGameBillboard, Product> EventOnChange;

    public void StartGame(StageInfo info)
    {
        ResetGame();

        transform.parent.gameObject.SetActive(true);
        Pause = false;
        mStageInfo = info;
        mBillboard.RemainLimit = info.MoveLimit;
        SoundPlayer.Inst.PlayBackMusic(SoundPlayer.Inst.BackMusicInGame);

        //GameObject mask = Instantiate(MaskPrefab, transform);
        //mask.transform.localScale = new Vector3(info.XCount * 0.97f, info.YCount * 0.97f, 1);

        GetComponent<SwipeDetector>().EventSwipe = OnSwipe;

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
                mFrames[x, y].EventBreakCover = () => { mBillboard.CoverCount++; };
                Product pro = CreateNewProduct(mFrames[x, y]);
                pro.SetChocoBlock(info.GetCell(x, y).ProductChocoCount);
                pro.EventUnWrapChoco = () => { mBillboard.ChocoCount++; };
            }
        }

        SecondaryInitFrames();
    }
    public void FinishGame(bool success)
    {
        if (success)
        {
            LOG.echo(SummaryToCSVString(true));
            int starCount = mBillboard.GetStarCount(mStageInfo);
            Stage currentStage = StageManager.Inst.GetStage(mStageInfo.Num);
            currentStage.UpdateStarCount(starCount);

            Stage nextStage = StageManager.Inst.GetStage(mStageInfo.Num + 1);
            if (nextStage != null)
                nextStage.UnLock();

            SoundPlayer.Inst.Player.Stop();
            MenuComplete.PopUp(mStageInfo.Num, starCount, mBillboard.CurrentScore);
        }
        else
        {
            LOG.echo(SummaryToCSVString(false));
            SoundPlayer.Inst.Player.Stop();
            MenuFailed.PopUp(mStageInfo.Num, mStageInfo.GoalValue, mBillboard.CurrentScore);
        }

        ResetGame();
        transform.parent.gameObject.SetActive(false);
    }

    public void OnSwipe(GameObject obj, SwipeDirection dir)
    {
        if (!mIsIdle)
            return;

        if (mBillboard.CheckState(mStageInfo) != InGameState.Running)
            return;

        mIsIdle = false;
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
            product.StartSwipe(targetProduct.GetComponentInParent<Frame>(), mBillboard.KeepCombo);
            targetProduct.StartSwipe(product.GetComponentInParent<Frame>(), mBillboard.KeepCombo);
            mSwipedProductA = product;
            mSwipedProductB = targetProduct;
        }
    }
    private void OnMatch(List<Product> matches)
    {
        mIsIdle = true;
        if (MatchLock || matches.Count < UserSetting.MatchCount)
            return;

        mIsIdle = false;
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectMatched);

        Product mainProduct = matches[0];
        if (mainProduct == mSwipedProductA || mainProduct == mSwipedProductB)
        {
            mBillboard.KeepCombo = 0;
            EventOnChange?.Invoke(mBillboard, null);
        }

        //mainProduct.BackupSkillToFrame(matches.Count, mSkipColor != ProductColor.None);
        MakeSkillProduct(matches.Count);

        int additionalCombo = 0;
        bool isSameColorEnable = false;
        foreach (Product pro in matches)
        {
            if (pro.mSkill == ProductSkill.MatchOneMore)
            {
                additionalCombo++;
                mBillboard.ItemOneMoreCount++;
            }
            else if (pro.mSkill == ProductSkill.BreakSameColor)
            {
                isSameColorEnable = true;
                mBillboard.ItemSameColorCount++;
            }
                
        }

        List<Product> destroies = isSameColorEnable ? GetSameColorProducts(mainProduct.mColor) : matches;
        int currentCombo = mainProduct.Combo;
        foreach (Product pro in destroies)
        {
            pro.Combo = currentCombo + 1 + additionalCombo;
            pro.StartDestroy();
            AddScore(pro);
        }

        mainProduct.StartFlash(matches);
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

    private IEnumerator CreateNextProducts()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.1f);

            for (int x = 0; x < CountX; ++x)
            {
                if (!mDestroyes.ContainsKey(x))
                    continue;

                Frame[] baseFrames = NextBaseFrames(mDestroyes[x]);
                if (baseFrames.Length <= 0)
                {
                    LOG.trace();
                    continue;
                }


                foreach (Frame baseFrame in baseFrames)
                    StartNextProducts(baseFrame);

                mDestroyes.Remove(x);
            }
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
        mSwipedProductA = null;
        mSwipedProductB = null;
        mBillboard.Reset();
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
        if (product.mSkill == ProductSkill.KeepCombo)
        {
            mBillboard.ItemKeepComboCount++;
            mBillboard.KeepCombo = Math.Max(mBillboard.KeepCombo, product.Combo);
        }

        mBillboard.MaxCombo = Math.Max(mBillboard.MaxCombo, product.Combo);
        mBillboard.CurrentScore += (UserSetting.scorePerProduct * product.Combo);
        EventOnChange?.Invoke(mBillboard, product);
        MenuInGame.Inst().AddScore(product);
    }
    private void SetSkipProduct(ProductColor color, int returnCount)
    {
        if (mSkipColor != ProductColor.None)
            return;

        mSkipColor = color;
        StartCoroutine(ReturnToStopSkipping(returnCount));
    }
    private IEnumerator ReturnToStopSkipping(int count)
    {
        int returnCount = mBillboard.RemainLimit - count;
        if (returnCount < 0) returnCount = 0;
        while (returnCount < mBillboard.RemainLimit)
            yield return null;

        mSkipColor = ProductColor.None;
    }
    private void RemoveLimit()
    {
        mBillboard.RemainLimit--;
        EventOnChange?.Invoke(mBillboard, null);
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
            + success + ","
            + mBillboard.CurrentScore + ","
            + mBillboard.MaxCombo + ","
            + (mStageInfo.MoveLimit - mBillboard.RemainLimit) + ","
            + "Item("
            + mBillboard.ItemOneMoreCount + "-"
            + mBillboard.ItemKeepComboCount + "-"
            + mBillboard.ItemSameColorCount + "-"
            + mBillboard.ItemReduceColorCount + ")";

        return ret;
    }


}
