using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Frame : MonoBehaviour
{
    private int mIndexX;
    private int mIndexY;
    public SpriteMask SpriteMask;
    public int IndexX { get { return mIndexX; } }
    public int IndexY { get { return mIndexY; } }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Initialize(int idxX, int idxY, InGameManager pm)
    {
        mIndexX = idxX;
        mIndexY = idxY;
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

    public void EnableMask(bool enable)
    {
        SpriteMask.enabled = enable;
    }

}
