﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InGameManager : MonoBehaviour
{
    private static InGameManager mInst = null;
    public static InGameManager Inst
    {
        get
        {
            if (mInst == null)
                mInst = GameObject.Find("WorldSpace").transform.Find("GameScreen").GetComponent<InGameManager>();
            return mInst;
        }
    }

    public const int MatchCount = 3;
    public const float SwipeDetectRange = 0.1f;
    public const float GridSize = 0.8f;
    public const float ComboDuration = 2.0f;
    public GameObject[] ProductPrefabs;
    public GameObject FramePrefab1;
    public GameObject FramePrefab2;

    private Frame[,] mFrames = null;
    private Product mDownProduct = null;
    private Vector3 mDownPosition;
    private StageInfo mStageInfo;
    private bool mIsRunning;
    private int mNewProductCycle = 0;
    private int mCurrentScore = 0;
    private int mRemainLimit = 0;
    private int mCurrentCombo = 0;

    public GameObject GameField;
    public GameObject FieldMask;

    public Action<int, int, int, ProductColor> EventOnChange;
    public Action<bool> EventOnFinish;

    void Update()
    {
        if (!mIsRunning)
            return;

        if(IsSwapable())
            CheckSwipe();

        CheckDropableProduct();

        CheckFinishGame();
    }

    public void StartGame(StageInfo info)
    {
        ResetGame();

        gameObject.SetActive(true);
        mIsRunning = true;
        mStageInfo = info;
        mCurrentScore = 0;
        mRemainLimit = info.MoveLimit;
        SoundPlayer.Inst.PlayBackMusic(SoundPlayer.Inst.BackMusicInGame);
        FieldMask.transform.localScale = new Vector3(info.XCount * 0.97f, info.YCount * 0.97f, 1);

        Vector3 localBasePos = new Vector3(-GridSize * info.XCount * 0.5f, -GridSize * info.YCount * 0.5f, 0);
        localBasePos.x += GridSize * 0.5f;
        localBasePos.y += GridSize * 0.5f;
        Vector3 localFramePos = new Vector3(0, 0, 0);
        mFrames = new Frame[info.XCount, info.YCount + 1];
        for (int y = 0; y < info.YCount + 1; y++)
        {
            for (int x = 0; x < info.XCount; x++)
            {
                GameObject frameObj = GameObject.Instantiate((x+y)%2 == 0 ? FramePrefab1 : FramePrefab2, GameField.transform, false);
                localFramePos.x = GridSize * x;
                localFramePos.y = GridSize * y;
                frameObj.transform.localPosition = localBasePos + localFramePos;
                mFrames[x, y] = frameObj.GetComponent<Frame>();
                mFrames[x, y].Initialize(x, y);
                CreateNewProduct(mFrames[x, y]);
                if (y == info.YCount)
                    frameObj.GetComponent<SpriteRenderer>().enabled = false;
            }
        }

    }
    public void ClearCombo(int scorePerCombo)
    {
        mCurrentScore += (mCurrentCombo * scorePerCombo);
        mCurrentCombo = 0;
    }
    public void PauseGame()
    {
        mIsRunning = false;
    }
    public void ResumeGame()
    {
        mIsRunning = true;
    }
    public void FinishGame(bool success)
    {
        ResetGame();
        EventOnFinish?.Invoke(success);
        gameObject.SetActive(false);
    }
    public void ResetGame()
    {
        int cnt = GameField.transform.childCount;
        for (int i = 0; i < cnt; ++i)
            Destroy(GameField.transform.GetChild(i).gameObject);

        mFrames = null;
        mDownProduct = null;
        mDownPosition = Vector3.zero;
        mStageInfo = null;
        //EventOnChange = null;
        mIsRunning = false;
        mCurrentScore = 0;
        mRemainLimit = 0;
        mCurrentCombo = 0;
    }
    public int XCount { get { return mStageInfo.XCount; } }
    public int YCount { get { return mStageInfo.YCount; } }

    public Frame GetFrame(int x, int y)
    {
        return mFrames[x, y];
    }
    public void AddScore(int score, ProductColor color)
    {
        mCurrentCombo++;
        mCurrentScore += (score * mCurrentCombo);
        EventOnChange?.Invoke(0, mCurrentScore, mCurrentCombo, color);
    }
    void RemoveLimit()
    {
        mRemainLimit--;
        EventOnChange?.Invoke(mRemainLimit, 0, 0, ProductColor.Blue);
    }
    public Frame GetFrame(float worldPosX, float worldPosY)
    {
        float offX = worldPosX - GameField.transform.position.x;
        float offY = worldPosY - GameField.transform.position.y;
        int idxX = (int)(offX / (float)GridSize);
        int idxY = (int)(offY / (float)GridSize);
        return mFrames[idxX, idxY];
    }
    public Product CreateNewProduct(Frame parent)
    {
        int colorCount = Math.Min(mStageInfo.ColorCount, ProductPrefabs.Length);
        int typeIdx = UnityEngine.Random.Range(0, colorCount);
        GameObject obj = GameObject.Instantiate(ProductPrefabs[typeIdx], parent.transform, false);
        Product product = obj.GetComponent<Product>();
        product.transform.localPosition = new Vector3(0, 0, -1);
        product.ParentFrame = parent;
        return product;
    }
    void CheckSwipe()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 worldPt = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Collider2D hit = Physics2D.OverlapPoint(worldPt);
            if (hit != null)
            {
                mDownProduct = hit.gameObject.GetComponent<Product>();
                mDownPosition = worldPt;
            }
        }
        else if (Input.GetMouseButton(0))
        {
            if(mDownProduct != null)
            {
                Vector3 curWorldPt = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                if ((curWorldPt - mDownPosition).magnitude >= SwipeDetectRange)
                {
                    Vector2 _currentSwipe = new Vector2(curWorldPt.x - mDownPosition.x, curWorldPt.y - mDownPosition.y);
                    _currentSwipe.Normalize();

                    Product target = null;
                    if (_currentSwipe.y > 0 && _currentSwipe.x > -0.5f && _currentSwipe.x < 0.5f)
                        target = mDownProduct.Up();
                    if (_currentSwipe.y < 0 && _currentSwipe.x > -0.5f && _currentSwipe.x < 0.5f)
                        target = mDownProduct.Down();
                    if (_currentSwipe.x < 0 && _currentSwipe.y > -0.5f && _currentSwipe.y < 0.5f)
                        target = mDownProduct.Left();
                    if (_currentSwipe.x > 0 && _currentSwipe.y > -0.5f && _currentSwipe.y < 0.5f)
                        target = mDownProduct.Right();

                    if (target != null && !mDownProduct.IsLocked() && !target.IsLocked())
                    {
                        RemoveLimit();
                        mDownProduct.StartSwipe(target.GetComponentInParent<Frame>());
                        target.StartSwipe(mDownProduct.GetComponentInParent<Frame>());
                    }

                    mDownProduct = null;
                }
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            mDownProduct = null;
        }
    }
    void CheckDropableProduct()
    {
        mNewProductCycle++;
        if(mNewProductCycle > 60)
        {
            mNewProductCycle = 0;
            foreach (Frame frame in mFrames)
            {
                if (frame.ChildProduct == null)
                    continue;
                if (frame.Down() == null)
                    continue;
                if (frame.Down().ChildProduct != null)
                    continue;
            
                Product pro = frame.ChildProduct;
                if (!pro.IsLocked())
                    pro.StartToDrop();
                    
            }
        }
    }
    public int GetStarCount()
    {
        float rate = (float)mCurrentScore / (float)mStageInfo.GoalScore;
        if (rate < 0.3f)
            return 0;
        else if (rate < 0.6f)
            return 1;
        else if (rate < 0.9f)
            return 2;
        return 3;
    }

    bool IsSwapable()
    {
        if (mCurrentScore >= mStageInfo.GoalScore)
            return false;

        if (mRemainLimit <= 0)
            return false;

        return true;
    }

    void CheckFinishGame()
    {
        if (mCurrentCombo > 0) //wait for combo
            return;

        if (mCurrentScore >= mStageInfo.GoalScore)
        {
            Stage currentStage = StageManager.Inst.GetStage(mStageInfo.Num);
            currentStage.UpdateStarCount(GetStarCount());

            Stage nextStage = StageManager.Inst.GetStage(mStageInfo.Num + 1);
            if(nextStage != null)
                nextStage.UnLock();

            SoundPlayer.Inst.Player.Stop();
            MenuComplete.PopUp(mStageInfo.Num, GetStarCount(), mCurrentScore);
            
            FinishGame(true);
        }
        else if(mRemainLimit <= 0)
        {
            mRemainLimit--;
            if(mRemainLimit < -200)
            {
                SoundPlayer.Inst.Player.Stop();
                MenuFailed.PopUp(mStageInfo.Num, mStageInfo.GoalScore, mCurrentScore);

                FinishGame(false);
            }
        }
    }

}