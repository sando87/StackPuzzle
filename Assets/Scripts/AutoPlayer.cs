using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AutoPlayer : MonoBehaviour
{
    public static bool AutoPlay
    {
        set
        {
            if (value)
                GameObject.Find("AutoPlayer").GetComponent<AutoPlayer>().StartCoroutine("DoAutoPlay");
            else
                GameObject.Find("AutoPlayer").GetComponent<AutoPlayer>().StopCoroutine("DoAutoPlay");
        }
    }

    IEnumerator DoAutoPlay()
    {
        while(true)
        {
            yield return new WaitForSeconds(2);
            if (!BattleFieldManager.Me.gameObject.activeInHierarchy)
                continue;

            if (BattleFieldManager.Me.IsIdle)
                ScanNextProduct();
        }
    }

    void ScanNextProduct()
    {
        int mCntX = BattleFieldManager.Me.CountX;
        int mCntY = BattleFieldManager.Me.CountY;
        List<AutoBalancerInfo> candidates = new List<AutoBalancerInfo>();
        for (int y = 0; y < mCntY; ++y)
        {
            for (int x = 0; x < mCntX; ++x)
            {
                Frame frame = BattleFieldManager.Me.GetFrame(x, y);
                if (frame == null || frame.ChildProduct == null)
                    continue;

                AutoBalancerInfo info = new AutoBalancerInfo();
                info.DecideDirection(frame.ChildProduct);
                if (info.targetProduct != null)
                    candidates.Add(info);
            }
        }


        if (candidates.Count > 0)
        {
            candidates.Sort((lsh, rsh) => { return rsh.matchedList.Count - lsh.matchedList.Count; });
            BattleFieldManager.Me.OnSwipe(candidates[0].targetProduct.gameObject, candidates[0].direct);
        }
        else
        {
            int ranX = UnityEngine.Random.Range(0, mCntX);
            int ranY = UnityEngine.Random.Range(0, mCntY);
            Frame randomFrame = BattleFieldManager.Me.GetFrame(ranX, ranY);
            if (randomFrame != null && randomFrame.ChildProduct != null && !randomFrame.ChildProduct.IsChocoBlock())
            {
                Product randomPro = randomFrame.ChildProduct;
                if (ranY % 2 == 0)
                {
                    if (randomPro.Left() != null)
                        InGameManager.Inst.OnSwipe(randomPro.gameObject, SwipeDirection.LEFT);
                    else if (randomPro.Right() != null)
                        InGameManager.Inst.OnSwipe(randomPro.gameObject, SwipeDirection.RIGHT);
                }
                else
                {
                    if (randomPro.Up() != null)
                        InGameManager.Inst.OnSwipe(randomPro.gameObject, SwipeDirection.UP);
                    else if (randomPro.Down() != null)
                        InGameManager.Inst.OnSwipe(randomPro.gameObject, SwipeDirection.DOWN);
                }
            }
        }
    }
}
