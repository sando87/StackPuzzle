using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MatchableInfo
{
    public List<Product> matches;
    public Product nearProduct;
    public SwipeDirection direct;
}

public class ComputerPlayer : MonoBehaviour
{
    int mCntX = 0;
    int mCntY = 0;
    
    private void OnEnable()
    {
        BattleFieldManager.Me.EventOnIdle = DoAction;
        BattleFieldManager.Me.GetComponent<SwipeDetector>().enabled = false;
        mCntX = BattleFieldManager.Me.CountX;
        mCntY = BattleFieldManager.Me.CountY;
    }
    private void OnDisable()
    {
        BattleFieldManager.Me.EventOnIdle = null;
        BattleFieldManager.Me.GetComponent<SwipeDetector>().enabled = true;
    }

    void DoAction()
    {
        List<MatchableInfo> candidates = new List<MatchableInfo>();
        Frame[,] frames = BattleFieldManager.Me.Frames;
        for (int y = 0; y < mCntY; ++y) 
        {
            for (int x = 0; x < mCntX; ++x)
            {
                if (frames[x, y].ChildProduct.IsChocoBlock())
                    continue;

                MatchableInfo info = frames[x, y].ChildProduct.ScanMatchable();
                if (info.matches.Count >= UserSetting.MatchCount)
                {
                    return;
                }
                //if (info.matches.Count >= UserSetting.MatchCount || info.nearProduct != null)
                //    candidates.Add(info);

            }
        }
    }
}
