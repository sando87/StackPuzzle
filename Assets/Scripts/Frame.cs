using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Frame : MonoBehaviour
{
    private int mIndexX;
    private int mIndexY;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Initialize(int idxX, int idxY, ProductManager pm)
    {
        mIndexX = idxX;
        mIndexY = idxY;
    }

    public Frame Left()
    {
        return mIndexX <= 0 ? null : ProductManager.Inst.GetFrame(mIndexX - 1, mIndexY);
    }
    public Frame Right()
    {
        return ProductManager.XCount - 1 <= mIndexX ? null : ProductManager.Inst.GetFrame(mIndexX + 1, mIndexY);
    }
    public Frame Down()
    {
        return mIndexY <= 0 ? null : ProductManager.Inst.GetFrame(mIndexX, mIndexY - 1);
    }
    public Frame Up()
    {
        return ProductManager.YCount - 1 <= mIndexY ? null : ProductManager.Inst.GetFrame(mIndexX, mIndexY + 1);
    }

}
