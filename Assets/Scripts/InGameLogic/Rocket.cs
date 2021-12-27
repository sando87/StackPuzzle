using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class Rocket : MonoBehaviour
{
    private Dictionary<Frame, int> mFrames = new Dictionary<Frame, int>();
    private InGameManager mIngameMgr = null;

    public bool IsBig { get; set; } = false;
    public Action<Frame> EventExplosion { get; set; } = null;

    void Start() 
    {
        mIngameMgr = InGameManager.InstCurrent;
    }

    void Update()
    {
        Frame[] nowFrames = FindOnFrames();
        if(nowFrames.Length == 0) return;

        foreach(Frame nextFrame in nowFrames)
        {
            if (!mFrames.ContainsKey(nextFrame))
            {
                EventExplosion?.Invoke(nextFrame);
                mFrames[nextFrame] = 1;
            }
        }

    }

    private Frame[] FindOnFrames()
    {
        List<Frame> frames = new List<Frame>();
        Frame frame = mIngameMgr.FrameOfWorldPos(transform.position.x, transform.position.y);
        if(frame != null)
        {
            frames.Add(frame);
        }

        if(IsBig)
        {
            Vector3 upPosition = transform.position + transform.up * mIngameMgr.GridSize;
            Frame upframe = mIngameMgr.FrameOfWorldPos(upPosition.x, upPosition.y);
            if (upframe != null)
            {
                frames.Add(upframe);
            }

            Vector3 downPosition = transform.position - transform.up * mIngameMgr.GridSize;
            Frame downframe = mIngameMgr.FrameOfWorldPos(downPosition.x, downPosition.y);
            if (downframe != null)
            {
                frames.Add(downframe);
            }
        }

        return frames.ToArray();
    }
}

