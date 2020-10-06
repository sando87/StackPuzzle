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

    private Product mSwipedProductA = null;
    private Product mSwipedProductB = null;
    private int mMaxCombo = 0;
    private int mItemOneMoreCount = 0;
    private int mItemKeepComboCount = 0;
    private int mItemSameColorCount = 0;
    private int mItemReduceColorCount = 0;
    private int mCoverCount = 0;
    private int mChocoCount = 0;
    private int mCurrentScore = 0;
    private int mRemainLimit = 0;
    private int mKeepCombo = 0;
    private ProductColor mSkipColor = ProductColor.None;
    private Queue<ProductSkill> mNextSkills = new Queue<ProductSkill>();
    private Dictionary<int,Frame> mDestroyes = new Dictionary<int, Frame>();

    public int CountX { get { return mStageInfo.XCount; } }
    public int CountY { get { return mStageInfo.YCount; } }
    public Frame[,] Frames { get { return mFrames; } }
    public bool MatchLock { get; set; }
    public bool Pause { get; set; }
    public Action<int, int, Product> EventOnChange;
    public Action<int> EventOnKeepCombo;

    public void StartGame(StageInfo info)
    {
        ResetGame();

        transform.parent.gameObject.SetActive(true);
        Pause = false;
        mStageInfo = info;
        mCurrentScore = 0;
        mRemainLimit = info.MoveLimit;
        SoundPlayer.Inst.PlayBackMusic(SoundPlayer.Inst.BackMusicInGame);

        GameObject mask = Instantiate(MaskPrefab, transform);
        mask.transform.localScale = new Vector3(info.XCount * 0.97f, info.YCount * 0.97f, 1);

        GetComponent<SwipeDetector>().EventSwipe = OnSwipe;

        StartCoroutine(CheckFinish());
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
                mFrames[x, y].EventBreakCover = () => { mCoverCount++; };
                mFrames[x, y].Empty = !info.GetCell(x, y).ProductMovable;
                Product pro = CreateNewProduct(mFrames[x, y]);
                pro.EventUnWrapChoco = () => { mChocoCount++; };
                //pro.WrapChocoBlock(!info.GetCell(x, y).ProductMovable);
            }
        }
    }
    public void FinishGame(bool success)
    {
        ResetGame();
        transform.parent.gameObject.SetActive(false);
    }
    public bool IsIdle()
    {
        int count = 0;
        foreach (Frame frame in mFrames)
            if (frame.ChildProduct != null && !frame.ChildProduct.IsLocked())
                count++;
        return mFrames.Length == count;
    }

    public void OnSwipe(GameObject obj, SwipeDirection dir)
    {
        if (!IsSwapable())
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

        if (targetProduct != null && !product.IsLocked() && !targetProduct.IsLocked() && !product.IsChocoBlock() && !targetProduct.IsChocoBlock())
        {
            RemoveLimit();
            product.StartSwipe(targetProduct.GetComponentInParent<Frame>(), mKeepCombo);
            targetProduct.StartSwipe(product.GetComponentInParent<Frame>(), mKeepCombo);
            mSwipedProductA = product;
            mSwipedProductB = targetProduct;
        }
    }
    private void OnMatch(List<Product> matches)
    {
        if (MatchLock)
            return;

        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectMatched);

        Product mainProduct = matches[0];
        if (mainProduct == mSwipedProductA || mainProduct == mSwipedProductB)
        {
            mKeepCombo = 0;
            EventOnKeepCombo?.Invoke(0);
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
                mItemOneMoreCount++;
            }
            else if (pro.mSkill == ProductSkill.BreakSameColor)
            {
                isSameColorEnable = true;
                mItemSameColorCount++;
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
        int idxX = baseFrame.IndexY;
        int curIdxY = baseFrame.IndexY;
        bool pushed = false;
        while(curIdxY < CountY)
        {
            Frame frame = mFrames[idxX, curIdxY];
            if (frame.Empty)
            {
                pushed = false;
                continue;
            }

            if (!pushed && frame.ChildProduct == null)
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
        int emptyCount = 0;
        while (curFrame != null)
        {
            Product pro = NextUpProductFrom(validFrame);
            if (pro == null)
            {
                validFrame = null;
                if (emptyCount == 0)
                    emptyCount = mStageInfo.YCount - curFrame.IndexY;
                pro = CreateNewProduct(curFrame, mNextSkills.Count > 0 ? mNextSkills.Dequeue() : ProductSkill.Nothing);
                pro.StartDropAnimate(curFrame, emptyCount, curFrame == baseFrame);
            }
            else
            {
                validFrame = pro.ParentFrame;
                pro.StartDropAnimate(curFrame, pro.ParentFrame.IndexY - curFrame.IndexY, curFrame == baseFrame);
                //if(curFrame.SkillBackupSpace != ProductSkill.Nothing)
                //{
                //    nextSkills.Enqueue(curFrame.SkillBackupSpace);
                //    curFrame.SkillBackupSpace = ProductSkill.Nothing;
                //}
            }

            curFrame = curFrame.Up();
        }
    }

    private IEnumerator CreateNextProducts()
    {
        while(true)
        {
            yield return new WaitForSeconds(0.1f);

            foreach (var vert in mDestroyes)
            {
                Frame[] baseFrames = NextBaseFrames(vert.Value);
                foreach (Frame baseFrame in baseFrames)
                    StartNextProducts(baseFrame);
            }

            mDestroyes.Clear();
        }
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
    private IEnumerator CheckFinish()
    {
        float time = 0;
        while (time < 3)
        {
            if (IsSwapable())
                yield return new WaitForSeconds(1);
            else
            {
                time = (mDestroyes.Count > 0) ? 0 : (time + Time.deltaTime);
                yield return null;
            }
        }
        FinishGame();
    }
    private void FinishGame()
    {
        string goal = mStageInfo.Goals[0].Split('/')[1];
        if (mCurrentScore >= int.Parse(goal))
        {
            int starCount = GetStarCount(mCurrentScore, int.Parse(goal));
            Stage currentStage = StageManager.Inst.GetStage(mStageInfo.Num);
            currentStage.UpdateStarCount(starCount);

            Stage nextStage = StageManager.Inst.GetStage(mStageInfo.Num + 1);
            if (nextStage != null)
                nextStage.UnLock();

            SoundPlayer.Inst.Player.Stop();
            MenuComplete.PopUp(mStageInfo.Num, starCount, mCurrentScore);

            FinishGame(true);
        }
        else if (mRemainLimit <= 0)
        {
            SoundPlayer.Inst.Player.Stop();
            MenuFailed.PopUp(mStageInfo.Num, int.Parse(goal), mCurrentScore);

            FinishGame(false);
        }
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
        mMaxCombo = 0;
        mItemOneMoreCount = 0;
        mItemKeepComboCount = 0;
        mItemSameColorCount = 0;
        mItemReduceColorCount = 0;
        mCoverCount = 0;
        mChocoCount = 0;
        mCurrentScore = 0;
        mRemainLimit = 0;
        mKeepCombo = 0;
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
            mItemKeepComboCount++;
            mKeepCombo = Math.Max(mKeepCombo, product.Combo);
            EventOnKeepCombo?.Invoke(mKeepCombo);
        }

        mMaxCombo = Math.Max(mMaxCombo, product.Combo);
        mCurrentScore += (UserSetting.scorePerProduct * product.Combo);
        EventOnChange?.Invoke(0, mCurrentScore, product);
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
        int returnCount = mRemainLimit - count;
        if (returnCount < 0) returnCount = 0;
        while (returnCount < mRemainLimit)
            yield return null;

        mSkipColor = ProductColor.None;
    }
    private void RemoveLimit()
    {
        mRemainLimit--;
        EventOnChange?.Invoke(mRemainLimit, 0, null);
    }
    private bool IsSwapable()
    {
        if (Pause)
            return false;

        string goal = mStageInfo.Goals[0].Split('/')[1];
        if (mCurrentScore >= int.Parse(goal))
            return false;

        if (mRemainLimit <= 0)
            return false;

        return true;
    }

    public static int GetStarCount(int score, int target)
    {
        return score / target;
    }
    public InGameState CheckState()
    {
        if (mStageInfo == null)
            return InGameState.Noting;

        if (MenuPause.IsPopped())
            return InGameState.Paused;

        int achieveCount = 0;
        foreach (string goalStr in mStageInfo.Goals)
        {
            string type = goalStr.Split('/')[0];
            int value = int.Parse(goalStr.Split('/')[1]);
            switch(type)
            {
                case "Score":
                    if (mCurrentScore >= value)
                        achieveCount++;
                    break;
                case "Combo":
                    if (mMaxCombo >= value)
                        achieveCount++;
                    break;
                case "ItemOneMore":
                    if (mItemOneMoreCount >= value)
                        achieveCount++;
                    break;
                case "ItemKeepCombo":
                    if (mItemKeepComboCount >= value)
                        achieveCount++;
                    break;
                case "ItemSameColor":
                    if (mItemSameColorCount >= value)
                        achieveCount++;
                    break;
                case "Cover":
                    if (mCoverCount >= value)
                        achieveCount++;
                    break;
                case "Choco":
                    if (mChocoCount >= value)
                        achieveCount++;
                    break;
                default: break;
            }
        }

        if (achieveCount >= mStageInfo.Goals.Count)
            return InGameState.Win;

        if(mRemainLimit <= 0)
            return InGameState.Lose;

        return InGameState.Running;
    }

}
