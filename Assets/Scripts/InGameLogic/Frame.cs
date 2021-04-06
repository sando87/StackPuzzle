using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Frame : MonoBehaviour
{
    private int mCoverCount;

    public Sprite[] Covers;
    public SpriteRenderer[] Borders;
    public SpriteRenderer CoverRenderer;

    public VerticalFrames VertFrames { get { return transform.parent.GetComponent<VerticalFrames>(); } }
    public InGameManager GameManager { get; private set; }
    public bool Empty { get; private set; }
    public int IndexX { get; private set; }
    public int IndexY { get; private set; }
    public bool IsBottom { get { return IndexY == 0; } }
    public bool IsTop { get { return IndexY == GameManager.CountY - 1; } }
    public Product ChildProduct { get; set; }
    public bool IsCovered { get { return mCoverCount > 0; } }

    public Action<Frame> EventBreakCover;

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
        GameManager = mgr;
        IndexX = idxX;
        IndexY = idxY;

        if (coverCount < 0)
        {
            Empty = true;
            mCoverCount = 0;
            CoverRenderer.sprite = Covers[0];
            gameObject.SetActive(false);
        }
        else
        {
            Empty = false;
            mCoverCount = coverCount;
            CoverRenderer.sprite = Covers[mCoverCount];
        }
    }

    public void ShowBorder(int pos)
    {
        //pos 0:Left, 1:Right, 2:Top, 3:Bottom
        Borders[pos].enabled = true;
    }


    public void BreakCover(int combo)
    {
        int backCount = mCoverCount;
        mCoverCount -= combo;
        mCoverCount = Mathf.Max(0, mCoverCount);
        CoverRenderer.sprite = Covers[mCoverCount];
        if (mCoverCount == 0 && backCount > 0)
            EventBreakCover?.Invoke(this);
    }

    public Frame Left()
    {
        return IndexX > 0 ? GameManager.Frame(IndexX - 1, IndexY) : null;
    }
    public Frame Right()
    {
        return IndexX < GameManager.CountX - 1 ? GameManager.Frame(IndexX + 1, IndexY) : null;
    }
    public Frame Down()
    {
        return IndexY > 0 ? GameManager.Frame(IndexX, IndexY - 1) : null;
    }
    public Frame Up()
    {
        return IndexY < GameManager.CountY - 1 ? GameManager.Frame(IndexX, IndexY + 1) : null;
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
