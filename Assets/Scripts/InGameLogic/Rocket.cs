using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class Rocket : MonoBehaviour
{
    private Frame mCurrentFrame = null;
    private InGameManager mIngameMgr = null;

    void Start() 
    {
        mIngameMgr = InGameManager.InstCurrent;
    }

    void Update()
    {
        Frame nowFrame = FindOnFrame();
        if(nowFrame == null) return;

        if(mCurrentFrame != nowFrame)
        {
            DoSomtiong(nowFrame);
            mCurrentFrame = nowFrame;
        }
    }

    private void DoSomtiong(Frame frame)
    {
        Product pro = frame.ChildProduct;
        if(pro == null || pro.IsLocked) return;

        mIngameMgr.DestroyProducts(new Product[] { pro });
    }

    private Frame FindOnFrame()
    {
        return mIngameMgr.FrameOfWorldPos(transform.position.x, transform.position.y);
    }
}

