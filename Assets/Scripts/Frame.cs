using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Frame : MonoBehaviour
{
    private int mIndexX;
    private int mIndexY;
    private int mCoverCount;
    private InGameManager mGameField;

    public Sprite[] Covers;

    public int IndexX { get { return mIndexX; } }
    public int IndexY { get { return mIndexY; } }
    public InGameManager GameField { get { return mGameField; } }
    public bool IsDummy { get { return mIndexY == GameField.YCount; } }
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

    public void Initialize(int idxX, int idxY, int coverCount, InGameManager field)
    {
        mGameField = field;
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
        return mIndexX <= 0 ? null : GameField.GetFrame(mIndexX - 1, mIndexY);
    }
    public Frame Right()
    {
        return GameField.XCount - 1 <= mIndexX ? null : GameField.GetFrame(mIndexX + 1, mIndexY);
    }
    public Frame Down()
    {
        return mIndexY <= 0 ? null : GameField.GetFrame(mIndexX, mIndexY - 1);
    }
    public Frame Up()
    {
        return GameField.YCount - 1 <= mIndexY ? null : GameField.GetFrame(mIndexX, mIndexY + 1);
    }
    public Frame UpDummy()
    {
        return GameField.GetFrame(mIndexX, GameField.YCount);
    }

}
