using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Frame : MonoBehaviour
{
    private int mIndexX;
    private int mIndexY;
    private int mCoverCount;
    private bool mIsEmpty;
    private Frame mSubTopFrame;
    private SpriteMask mMask;
    private InGameManager mGameManager;

    public Sprite[] Covers;
    public SpriteRenderer[] Borders;

    public bool Empty { get { return mIsEmpty; } }
    public ProductSkill SkillBackupSpace { get; set; }
    public int IndexX { get { return mIndexX; } }
    public int IndexY { get { return mIndexY; } }
    public Product ChildProduct { get { return GetComponentInChildren<Product>(); } }
    public SpriteRenderer CoverRenderer;
    public Func<int, int, Frame> GetFrame;
    public Action EventBreakCover;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Initialize(InGameManager mgr, int idxX, int idxY, int coverCount)
    {
        mGameManager = mgr;
        mIndexX = idxX;
        mIndexY = idxY;

        if (coverCount < 0)
        {
            mIsEmpty = true;
            mCoverCount = 0;
            CoverRenderer.sprite = Covers[0];
            gameObject.SetActive(false);
        }
        else
        {
            mIsEmpty = false;
            mCoverCount = coverCount;
            CoverRenderer.sprite = Covers[mCoverCount];
        }
    }

    public void SetSubTopFrame(Frame subTopFrame)
    {
        mSubTopFrame = subTopFrame;
    }
    public void SetSpriteMask(SpriteMask mask)
    {
        mMask = mask;
    }
    public void ShowBorder(int pos)
    {
        //pos 0:Left, 1:Right, 2:Top, 3:Bottom
        Borders[pos].enabled = true;
    }

    public int MaskLayerOrder { get { return mMask.backSortingOrder; } }

    public void BreakCover(int combo)
    {
        int backCount = mCoverCount;
        mCoverCount -= combo;
        mCoverCount = Mathf.Max(0, mCoverCount);
        CoverRenderer.sprite = Covers[mCoverCount];
        if (mCoverCount == 0 && backCount > 0)
            EventBreakCover?.Invoke();
    }
    public bool IsCovered()
    {
        return mCoverCount > 0;
    }

    public Frame Left()
    {
        return mGameManager.GetFrame(mIndexX - 1, mIndexY);
    }
    public Frame Right()
    {
        return mGameManager.GetFrame(mIndexX + 1, mIndexY);
    }
    public Frame Down()
    {
        return mGameManager.GetFrame(mIndexX, mIndexY - 1);
    }
    public Frame Up()
    {
        return mGameManager.GetFrame(mIndexX, mIndexY + 1);
    }
    public Frame[] GetAroundFrames()
    {
        Frame frame = null;
        List<Frame> frames = new List<Frame>();
        frame = Left(); if (frame != null && !frame.Empty) frames.Add(frame);
        frame = Right(); if (frame != null && !frame.Empty) frames.Add(frame);
        frame = Up(); if (frame != null && !frame.Empty) frames.Add(frame);
        frame = Down(); if (frame != null && !frame.Empty) frames.Add(frame);
        return frames.ToArray();
    }

}
