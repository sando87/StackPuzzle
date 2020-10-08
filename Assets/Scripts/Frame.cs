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

    public Sprite[] Covers;

    public bool Empty {
        get { return mIsEmpty; }
        set { mIsEmpty = value; gameObject.SetActive(!value); }
    }
    public ProductSkill SkillBackupSpace { get; set; }
    public int ComboBackupSpace { get; set; }
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

    public void Initialize(int idxX, int idxY, int coverCount)
    {
        mIndexX = idxX;
        mIndexY = idxY;

        if (coverCount < Covers.Length)
        {
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
        return GetFrame(mIndexX - 1, mIndexY);
    }
    public Frame Right()
    {
        return GetFrame(mIndexX + 1, mIndexY);
    }
    public Frame Down()
    {
        return GetFrame(mIndexX, mIndexY - 1);
    }
    public Frame Up()
    {
        return GetFrame(mIndexX, mIndexY + 1);
    }

}
