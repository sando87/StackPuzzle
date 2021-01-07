using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AutoBalancerInfo
{
    public Product targetProduct = null;
    public int maxCount = 0;
    public SwipeDirection direct = SwipeDirection.LEFT;
}

public class AutoBalancer : MonoBehaviour
{
    public static bool AutoBalance
    {
        set
        {
            if (value)
                GameObject.Find("AutoBalancer").GetComponent<AutoBalancer>().StartCoroutine("DoAutoBalancer");
            else
                GameObject.Find("AutoBalancer").GetComponent<AutoBalancer>().StopCoroutine("DoAutoBalancer");
        }
    }

    IEnumerator DoAutoBalancer()
    {
        InGameManager mgr = null;
        int counter = 0;
        int counterLimit = 5;
        while (true)
        {
            yield return new WaitForSeconds(1);
            
            if (InGameManager.InstStage.gameObject.activeInHierarchy)
                mgr = InGameManager.InstStage;
            else if (InGameManager.InstPVP_Player.gameObject.activeInHierarchy)
                mgr = InGameManager.InstPVP_Player;
            else
                continue;

            if (mgr.IsIdle)
            {
                counter++;
                if (counter > counterLimit)
                {
                    counter = 0;
                    counterLimit = UnityEngine.Random.Range(3, 7);
                    AutoClickNextProduct(mgr);
                }
                else
                {
                    AutoSwipeNextProduct(mgr);
                }
            }
        }
    }

    bool AutoSwipeNextProduct(InGameManager mgr)
    {
        int mCntX = mgr.CountX;
        int mCntY = mgr.CountY;
        int yOff = UnityEngine.Random.Range(0, mCntY);
        for (int y = 0; y < mCntY; ++y)
        {
            int fixedY = (y + yOff) % mCntY;
            for (int x = 0; x < mCntX; ++x)
            {
                Product cenPro = mgr.Frame(x, fixedY).ChildProduct;
                if (cenPro == null || cenPro.IsLocked || cenPro.IsChocoBlock)
                    continue;

                AutoBalancerInfo info = new AutoBalancerInfo();
                List<Product> matches = new List<Product>();
                cenPro.SearchMatchedProducts(matches, cenPro.Color);
                if (matches.Count >= UserSetting.MatchCount)
                    continue;


                int leftMatchCount = mgr.NextMatchCount(cenPro, SwipeDirection.LEFT);
                if(leftMatchCount > info.maxCount)
                {
                    info.maxCount = leftMatchCount;
                    info.direct = SwipeDirection.LEFT;
                    info.targetProduct = cenPro.Left();
                }
                int rightMatchCount = mgr.NextMatchCount(cenPro, SwipeDirection.RIGHT);
                if (rightMatchCount > info.maxCount)
                {
                    info.maxCount = rightMatchCount;
                    info.direct = SwipeDirection.RIGHT;
                    info.targetProduct = cenPro.Right();
                }
                int upMatchCount = mgr.NextMatchCount(cenPro, SwipeDirection.UP);
                if (upMatchCount > info.maxCount)
                {
                    info.maxCount = upMatchCount;
                    info.direct = SwipeDirection.UP;
                    info.targetProduct = cenPro.Up();
                }
                int downMatchCount = mgr.NextMatchCount(cenPro, SwipeDirection.DOWN);
                if (downMatchCount > info.maxCount)
                {
                    info.maxCount = downMatchCount;
                    info.direct = SwipeDirection.DOWN;
                    info.targetProduct = cenPro.Down();
                }

                if(info.maxCount > 1)
                {
                    mgr.OnSwipe(cenPro.gameObject, info.direct);
                    return true;
                }
            }
        }

        return false;
    }
    void AutoClickNextProduct(InGameManager mgr)
    {
        int mCntX = mgr.CountX;
        int mCntY = mgr.CountY;
        int yOff = UnityEngine.Random.Range(0, mCntY);
        for (int y = 0; y < mCntY; ++y)
        {
            int fixedY = (y + yOff) % mCntY;
            for (int x = 0; x < mCntX; ++x)
            {
                Product pro = mgr.Frame(x, fixedY).ChildProduct;
                if (pro == null || pro.IsLocked)
                    continue;

                List<Product> matchedList = new List<Product>();
                pro.SearchMatchedProducts(matchedList, pro.Color);
                if (matchedList.Count >= UserSetting.MatchCount)
                {
                    mgr.OnClick(pro.gameObject);
                    return;
                }
            }
        }
    }
}
