using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattleFieldManager : MonoBehaviour
{
    public const int MatchCount = 3;
    public const int attackScore = 5;
    public const float GridSize = 0.8f;

    public GameObject[] ProductPrefabs;
    public GameObject FramePrefab1;
    public GameObject FramePrefab2;
    public GameObject MaskPrefab;
    public BattleFieldManager Opponent;

    private Frame[,] mFrames = null;

    private int mKeepCombo = 0;

    public bool MatchLock { get; set; }

    public Action<int, int, Product> EventOnChange;
    public Action<bool> EventOnFinish;

    private void Update()
    {
        int idleCount = 0;
        foreach (Frame frame in mFrames)
        {
            if (frame.ChildProduct == null)
                continue;

            Product pro = frame.ChildProduct;
            if (!pro.IsLocked())
            {
                idleCount++;
                if (frame.Down() != null && frame.Down().ChildProduct == null)
                    pro.StartToDrop();
            }
        }

        if (!IsSwapable() && idleCount == mFrames.Length)
            FinishGame();
    }

    public void OnSwipe(GameObject obj, SwipeDirection dir)
    {
        if (!IsSwapable())
            return;

        Product product = obj.GetComponent<Product>();
        Product targetProduct = null;
        switch(dir)
        {
            case SwipeDirection.UP: targetProduct = product.Up(); break;
            case SwipeDirection.DOWN: targetProduct = product.Down(); break;
            case SwipeDirection.LEFT: targetProduct = product.Left(); break;
            case SwipeDirection.RIGHT: targetProduct = product.Right(); break;
        }

        if (targetProduct != null && !product.IsLocked() && !targetProduct.IsLocked() && !product.IsChocoBlock() && !targetProduct.IsChocoBlock())
        {
            //AttckSwipe(idxX, idxY, matchable);
            RemoveLimit();
            targetProduct.StartSwipe(targetProduct.GetComponentInParent<Frame>(), mKeepCombo);
            targetProduct.StartSwipe(targetProduct.GetComponentInParent<Frame>(), mKeepCombo);
            mKeepCombo = 0;
        }
    }

    public void StartGame(StageInfo info)
    {
        ResetGame();

        InitFieldInfo fieldInfo = new InitFieldInfo();

        NetClientApp.GetInstance().Request(NetCMD.InitField, fieldInfo, (object response) =>
        {
            InitFieldInfo _info = response as InitFieldInfo;
        });

        gameObject.SetActive(true);
        SoundPlayer.Inst.PlayBackMusic(SoundPlayer.Inst.BackMusicInGame);

        GameObject mask = Instantiate(MaskPrefab, transform);
        mask.transform.localScale = new Vector3(info.XCount * 0.97f, info.YCount * 0.97f, 1);

        GetComponent<SwipeDetector>().EventSwipe = OnSwipe;

        Vector3 localBasePos = new Vector3(-GridSize * info.XCount * 0.5f, -GridSize * info.YCount * 0.5f, 0);
        localBasePos.x += GridSize * 0.5f;
        localBasePos.y += GridSize * 0.5f;
        Vector3 localFramePos = new Vector3(0, 0, 0);
        mFrames = new Frame[info.XCount, info.YCount + 1];
        for (int y = 0; y < info.YCount + 1; y++)
        {
            for (int x = 0; x < info.XCount; x++)
            {
                GameObject frameObj = GameObject.Instantiate((x+y)%2 == 0 ? FramePrefab1 : FramePrefab2, transform, false);
                localFramePos.x = GridSize * x;
                localFramePos.y = GridSize * y;
                frameObj.transform.localPosition = localBasePos + localFramePos;
                mFrames[x, y] = frameObj.GetComponent<Frame>();
                mFrames[x, y].Initialize(x, y, info.GetCell(x, y).FrameCoverCount, this);
                Product pro = CreateNewProduct(mFrames[x, y]);
                pro.WrapChocoBlock(!info.GetCell(x, y).ProductMovable);
                if (y == info.YCount)
                    frameObj.GetComponent<SpriteRenderer>().enabled = false;
            }
        }

    }
    public void PauseGame()
    {
        mIsPaused = true;
    }
    public void ResumeGame()
    {
        mIsPaused = false;
    }
    public void FinishGame(bool success)
    {
        ResetGame();
        EventOnFinish?.Invoke(success);
        gameObject.SetActive(false);
    }
    public void ResetGame()
    {
        int cnt = transform.childCount;
        for (int i = 0; i < cnt; ++i)
            Destroy(transform.GetChild(i).gameObject);

        mFrames = null;
        mKeepCombo = 0;
        MatchLock = false;
    }
    public int XCount { get { return mStageInfo.XCount; } }
    public int YCount { get { return mStageInfo.YCount; } }

    public Frame GetFrame(int x, int y)
    {
        return mFrames[x, y];
    }
    public void AddScore(Product product)
    {
        mCurrentScore += (scorePerProduct * product.Combo);
        EventOnChange?.Invoke(0, mCurrentScore, product);
    }
    public void KeepCombo(int combo)
    {
        if (combo > mKeepCombo)
            mKeepCombo = combo;
    }
    public bool IsSkippingColor()
    {
        return mSkipColor != ProductColor.None;
    }
    public void SetSkipProduct(ProductColor color, int returnCount)
    {
        if (mSkipColor != ProductColor.None)
            return;

        mSkipColor = color;
        StartCoroutine(ReturnToStopSkipping(returnCount));
    }
    IEnumerator ReturnToStopSkipping(int count)
    {
        int returnCount = mRemainLimit - count;
        if (returnCount < 0) returnCount = 0;
        while (returnCount < mRemainLimit)
            yield return null;

        mSkipColor = ProductColor.None;
    }
    void RemoveLimit()
    {
        mRemainLimit--;
        EventOnChange?.Invoke(mRemainLimit, 0, null);
    }
    public Frame GetFrame(float worldPosX, float worldPosY)
    {
        float offX = worldPosX - transform.position.x;
        float offY = worldPosY - transform.position.y;
        int idxX = (int)(offX / (float)GridSize);
        int idxY = (int)(offY / (float)GridSize);
        return mFrames[idxX, idxY];
    }
    public Product CreateNewProduct(Frame parent)
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
        return product;
    }

    IEnumerator CheckDropableProduct()
    {
        while(true)
        {
            yield return new WaitForSeconds(0.25f);

            int idleCount = 0;
            foreach (Frame frame in mFrames)
            {
                if (frame.ChildProduct == null)
                    continue;
            
                Product pro = frame.ChildProduct;
                if (!pro.IsLocked())
                {
                    idleCount++;
                    if (frame.Down() != null && frame.Down().ChildProduct == null)
                        pro.StartToDrop();
                }
            }

            if (!IsSwapable() && idleCount == mFrames.Length)
                break;
        }

        FinishGame();

    }
    static public int GetStarCount(int score, int target)
    {
        return score / target;
    }

    bool IsSwapable()
    {
        if (mIsPaused)
            return false;

        if (mCurrentScore >= mStageInfo.GoalScore)
            return false;

        if (mRemainLimit <= 0)
            return false;

        return true;
    }

    void FinishGame()
    {
        if (mCurrentScore >= mStageInfo.GoalScore)
        {
            int starCount = GetStarCount(mCurrentScore, mStageInfo.GoalScore);
            Stage currentStage = StageManager.Inst.GetStage(mStageInfo.Num);
            currentStage.UpdateStarCount(starCount);

            Stage nextStage = StageManager.Inst.GetStage(mStageInfo.Num + 1);
            if(nextStage != null)
                nextStage.UnLock();

            SoundPlayer.Inst.Player.Stop();
            MenuComplete.PopUp(mStageInfo.Num, starCount, mCurrentScore);
            
            FinishGame(true);
        }
        else if(mRemainLimit <= 0)
        {
            SoundPlayer.Inst.Player.Stop();
            MenuFailed.PopUp(mStageInfo.Num, mStageInfo.GoalScore, mCurrentScore);

            FinishGame(false);
        }
    }

    public Product[] GetSameProducts(ProductColor color)
    {
        List<Product> pros = new List<Product>();
        foreach(Frame frame in mFrames)
        {
            Product pro = frame.ChildProduct;
            if (pro != null && pro.mColor == color)
                pros.Add(pro);
        }
        return pros.ToArray();
    }

}
