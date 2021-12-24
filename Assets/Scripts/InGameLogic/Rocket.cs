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

    public Action<Frame> EventExplosion { get; set; } = null;

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
            EventExplosion?.Invoke(nowFrame);
            mCurrentFrame = nowFrame;
        }
    }

    private Frame FindOnFrame()
    {
        return mIngameMgr.FrameOfWorldPos(transform.position.x, transform.position.y);
    }
}

