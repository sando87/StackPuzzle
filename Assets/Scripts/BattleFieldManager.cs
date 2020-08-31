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
    private int mThisUserPK = 1;
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

        if (idleCount == mFrames.Length)
            FinishGame();
    }

    public void OnSwipe(GameObject obj, SwipeDirection dir)
    {
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
            AttackSwipe(product.ParentFrame.IndexX, product.ParentFrame.IndexY);
            targetProduct.StartSwipe(targetProduct.GetComponentInParent<Frame>(), mKeepCombo);
            targetProduct.StartSwipe(targetProduct.GetComponentInParent<Frame>(), mKeepCombo);
            mKeepCombo = 0;
        }
    }

    private void AttackSwipe(int idxX, int idxY)
    {
        SwipeInfo info = new SwipeInfo();
        info.idxX = idxX;
        info.idxY = idxY;
        info.matchable = !MatchLock;
        info.userPk = mThisUserPK;
        NetClientApp.GetInstance().Request(NetCMD.AttackSwipe, info, null);
    }

    public void StartGame(int userPK)
    {
        ResetGame();

        mThisUserPK = userPK;
        InitFieldInfo info = new InitFieldInfo();
        info.XCount = 8;
        info.YCount = 8;
        info.userPk = userPK;
        NetClientApp.GetInstance().Request(NetCMD.InitField, info, (object response) =>
        {
            InitFieldInfo res = response as InitFieldInfo;

            gameObject.SetActive(true);
            SoundPlayer.Inst.PlayBackMusic(SoundPlayer.Inst.BackMusicInGame);

            GameObject mask = Instantiate(MaskPrefab, transform);
            mask.transform.localScale = new Vector3(info.XCount * 0.97f, info.YCount * 0.97f, 1);

            RegisterSwipeEvent();

            Vector3 localBasePos = new Vector3(-GridSize * info.XCount * 0.5f, -GridSize * info.YCount * 0.5f, 0);
            localBasePos.x += GridSize * 0.5f;
            localBasePos.y += GridSize * 0.5f;
            Vector3 localFramePos = new Vector3(0, 0, 0);
            mFrames = new Frame[info.XCount, info.YCount + 1];
            for (int y = 0; y < info.YCount + 1; y++)
            {
                for (int x = 0; x < info.XCount; x++)
                {
                    GameObject frameObj = GameObject.Instantiate((x + y) % 2 == 0 ? FramePrefab1 : FramePrefab2, transform, false);
                    localFramePos.x = GridSize * x;
                    localFramePos.y = GridSize * y;
                    frameObj.transform.localPosition = localBasePos + localFramePos;
                    mFrames[x, y] = frameObj.GetComponent<Frame>();
                    mFrames[x, y].Initialize(x, y, 0, null);
                    CreateNewProduct(mFrames[x, y], res.products[x, y]);
                    if (y == info.YCount)
                        frameObj.GetComponent<SpriteRenderer>().enabled = false;
                }
            }

        });
    }

    private void RegisterSwipeEvent()
    {
        if (IsPlayerField())
        {
            GetComponent<SwipeDetector>().EventSwipe = OnSwipe;
        }
        else
        {
            GetComponent<SwipeDetector>().enabled = false;
            NetClientApp.GetInstance().WaitResponse(NetCMD.AttackSwipe, (object response) =>
            {
                SwipeInfo res = response as SwipeInfo;
                if (res.userPk == mThisUserPK)
                    return;

                MatchLock = res.matchable;
                Product pro = mFrames[res.idxX, res.idxY].ChildProduct;
                OnSwipe(pro.gameObject, res.dir);
            });
        }
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

    public Frame GetFrame(int x, int y)
    {
        return mFrames[x, y];
    }
    public void KeepCombo(int combo)
    {
        if (combo > mKeepCombo)
            mKeepCombo = combo;
    }
    public Frame GetFrame(float worldPosX, float worldPosY)
    {
        float offX = worldPosX - transform.position.x;
        float offY = worldPosY - transform.position.y;
        int idxX = (int)(offX / (float)GridSize);
        int idxY = (int)(offY / (float)GridSize);
        return mFrames[idxX, idxY];
    }
    public Product CreateNewProduct(Frame parent, ProductColor color)
    {
        int typeIdx = (int)color;
        GameObject obj = GameObject.Instantiate(ProductPrefabs[typeIdx], parent.transform, false);
        Product product = obj.GetComponent<Product>();
        product.transform.localPosition = new Vector3(0, 0, -1);
        product.ParentFrame = parent;
        if(!IsPlayerField())
            obj.GetComponent<BoxCollider2D>().enabled = false;
        return product;
    }
    public bool IsPlayerField()
    {
        //return SettingUserPK != mThisUserPK;
        return true;
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

            if (idleCount == mFrames.Length)
                break;
        }

        FinishGame();

    }
    static public int GetStarCount(int score, int target)
    {
        return score / target;
    }


    void FinishGame()
    {
        //if (mCurrentScore >= mStageInfo.GoalScore)
        //{
        //    int starCount = GetStarCount(mCurrentScore, mStageInfo.GoalScore);
        //    Stage currentStage = StageManager.Inst.GetStage(mStageInfo.Num);
        //    currentStage.UpdateStarCount(starCount);
        //
        //    Stage nextStage = StageManager.Inst.GetStage(mStageInfo.Num + 1);
        //    if(nextStage != null)
        //        nextStage.UnLock();
        //
        //    SoundPlayer.Inst.Player.Stop();
        //    MenuComplete.PopUp(mStageInfo.Num, starCount, mCurrentScore);
        //    
        //    FinishGame(true);
        //}
        //else if(mRemainLimit <= 0)
        //{
        //    SoundPlayer.Inst.Player.Stop();
        //    MenuFailed.PopUp(mStageInfo.Num, mStageInfo.GoalScore, mCurrentScore);
        //
        //    FinishGame(false);
        //}
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
