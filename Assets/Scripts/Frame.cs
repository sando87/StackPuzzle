﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Frame : MonoBehaviour
{
    private int mIndexX;
    private int mIndexY;
    private int mCoverCount;

    public Sprite[] Covers;

    public int IndexX { get { return mIndexX; } }
    public int IndexY { get { return mIndexY; } }
    public bool IsDummy { get { return mIndexY == InGameManager.Inst.YCount; } }
    public Product ChildProduct { get { return GetComponentInChildren<Product>(); } }
    public SpriteRenderer CoverRenderer;

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

    public void BreakCover(int combo)
    {
        mCoverCount -= combo;
        mCoverCount = Mathf.Max(0, mCoverCount);
        CoverRenderer.sprite = Covers[mCoverCount];
    }
    public bool IsCovered()
    {
        return mCoverCount > 0;
    }

    public Frame Left()
    {
        return mIndexX <= 0 ? null : InGameManager.Inst.GetFrame(mIndexX - 1, mIndexY);
    }
    public Frame Right()
    {
        return InGameManager.Inst.XCount - 1 <= mIndexX ? null : InGameManager.Inst.GetFrame(mIndexX + 1, mIndexY);
    }
    public Frame Down()
    {
        return mIndexY <= 0 ? null : InGameManager.Inst.GetFrame(mIndexX, mIndexY - 1);
    }
    public Frame Up()
    {
        return InGameManager.Inst.YCount - 1 <= mIndexY ? null : InGameManager.Inst.GetFrame(mIndexX, mIndexY + 1);
    }
    public Frame UpDummy()
    {
        return InGameManager.Inst.GetFrame(mIndexX, InGameManager.Inst.YCount);
    }

}
