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
        Product prevSwippedProduct = null;
        InGameManager mgr = null;
        while (true)
        {
            yield return new WaitForSeconds(1.5f);
            
            if (InGameManager.InstStage.gameObject.activeInHierarchy)
                mgr = InGameManager.InstStage;
            else if (InGameManager.InstPVP_Player.gameObject.activeInHierarchy)
                mgr = InGameManager.InstPVP_Player;
            else
                continue;

            if (mgr.IsIdle)
            {
                prevSwippedProduct = AutoSwipeNextProduct(mgr, prevSwippedProduct);
                if(prevSwippedProduct == null)
                    AutoClickNextProduct(mgr);
            }
        }
    }

    Product AutoSwipeNextProduct(InGameManager mgr, Product prevSwippedProduct)
    {
        int mCntX = mgr.CountX;
        int mCntY = mgr.CountY;
        List<AutoBalancerInfo> candidates = new List<AutoBalancerInfo>();
        for (int y = 0; y < mCntY; ++y)
        {
            for (int x = 0; x < mCntX; ++x)
            {
                Frame frame = mgr.GetFrame(x, y);
                if (frame == null || frame.ChildProduct == null)
                    continue;

                Product cenPro = frame.ChildProduct;
                AutoBalancerInfo info = new AutoBalancerInfo();

                List<Product> matches = new List<Product>();
                cenPro.SearchMatchedProducts(matches, cenPro.mColor);
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

                if(info.maxCount > 0)
                    candidates.Add(info);
            }
        }

        if (candidates.Count > 0)
        {
            candidates.Sort((lsh, rsh) => { return rsh.maxCount - lsh.maxCount; });
            Product nextSwipeProduct = candidates[0].targetProduct;
            if(nextSwipeProduct != prevSwippedProduct)
            {
                mgr.OnSwipe(nextSwipeProduct.gameObject, candidates[0].direct);
                return nextSwipeProduct;
            }
        }

        return null;
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
                Product pro = mgr.GetFrame(x, fixedY).ChildProduct;
                if (pro == null || pro.IsLocked())
                    continue;

                List<Product> matchedList = new List<Product>();
                pro.SearchMatchedProducts(matchedList, pro.mColor);
                if (matchedList.Count >= UserSetting.MatchCount)
                {
                    mgr.OnClick(pro.gameObject);
                    return;
                }
            }
        }
    }
}
