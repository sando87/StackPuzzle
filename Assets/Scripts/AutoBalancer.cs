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
    public bool DecideDirection(Product cenPro)
    {
        if (cenPro.IsChocoBlock())
            return false;

        if (cenPro.Left() != null)
        {
            Product pro = cenPro.Left();
            Product[] aroundPros = pro.GetAroundProducts();
            int sameColorCount = 0;
            foreach (Product item in aroundPros)
                if (cenPro.mColor == item.mColor)
                    sameColorCount++;

            if(sameColorCount > maxCount)
            {
                targetProduct = cenPro;
                direct = SwipeDirection.LEFT;
            }
        }

        if (cenPro.Right() != null)
        {
            Product pro = cenPro.Right();
            Product[] aroundPros = pro.GetAroundProducts();
            int sameColorCount = 0;
            foreach (Product item in aroundPros)
                if (cenPro.mColor == item.mColor)
                    sameColorCount++;

            if (sameColorCount > maxCount)
            {
                targetProduct = cenPro;
                direct = SwipeDirection.RIGHT;
            }
        }

        if (cenPro.Up() != null)
        {
            Product pro = cenPro.Up();
            Product[] aroundPros = pro.GetAroundProducts();
            int sameColorCount = 0;
            foreach (Product item in aroundPros)
                if (cenPro.mColor == item.mColor)
                    sameColorCount++;

            if (sameColorCount > maxCount)
            {
                targetProduct = cenPro;
                direct = SwipeDirection.UP;
            }
        }

        if (cenPro.Down() != null)
        {
            Product pro = cenPro.Down();
            Product[] aroundPros = pro.GetAroundProducts();
            int sameColorCount = 0;
            foreach (Product item in aroundPros)
                if (cenPro.mColor == item.mColor)
                    sameColorCount++;

            if (sameColorCount > maxCount)
            {
                targetProduct = cenPro;
                direct = SwipeDirection.DOWN;
            }
        }
        return true;
    }
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
        while (true)
        {
            yield return new WaitForSeconds(2);
            
            if (InGameManager.InstStage.gameObject.activeInHierarchy)
                mgr = InGameManager.InstStage;
            else if (InGameManager.InstPVP_Player.gameObject.activeInHierarchy)
                mgr = InGameManager.InstPVP_Player;
            else
                continue;

            if (mgr.IsIdle)
            {
                if (UnityEngine.Random.Range(0, 3) == 0)
                    AutoSwipeNextProduct(mgr);
                else
                    AutoClickNextProduct(mgr);
            }
        }
    }

    void AutoSwipeNextProduct(InGameManager mgr)
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

                AutoBalancerInfo info = new AutoBalancerInfo();
                info.DecideDirection(frame.ChildProduct);
                if(info.targetProduct != null)
                    candidates.Add(info);
            }
        }

        
        if (candidates.Count > 0)
        {
            candidates.Sort((lsh, rsh) => { return rsh.maxCount - lsh.maxCount; });
            mgr.OnSwipe(candidates[0].targetProduct.gameObject, candidates[0].direct);
        }
        else
        {
            int ranX = UnityEngine.Random.Range(0, mCntX);
            int ranY = UnityEngine.Random.Range(0, mCntY);
            Frame randomFrame = mgr.GetFrame(ranX, ranY);
            if (randomFrame != null && randomFrame.ChildProduct != null && !randomFrame.ChildProduct.IsChocoBlock())
            {
                Product randomPro = randomFrame.ChildProduct;
                if (ranY % 2 == 0)
                {
                    if (randomPro.Left() != null)
                        mgr.OnSwipe(randomPro.gameObject, SwipeDirection.LEFT);
                    else if (randomPro.Right() != null)
                        mgr.OnSwipe(randomPro.gameObject, SwipeDirection.RIGHT);
                }
                else
                {
                    if (randomPro.Up() != null)
                        mgr.OnSwipe(randomPro.gameObject, SwipeDirection.UP);
                    else if (randomPro.Down() != null)
                        mgr.OnSwipe(randomPro.gameObject, SwipeDirection.DOWN);
                }
            }
        }
    }
    void AutoClickNextProduct(InGameManager mgr)
    {
        int mCntX = mgr.CountX;
        int mCntY = mgr.CountY;
        List<KeyValuePair< Product, int>> candidates = new List<KeyValuePair<Product, int>>();
        Dictionary<Product, int> scannedProducts = new Dictionary<Product, int>();
        for (int y = 0; y < mCntY; ++y)
        {
            for (int x = 0; x < mCntX; ++x)
            {
                Frame frame = mgr.GetFrame(x, y);
                if (frame == null || frame.ChildProduct == null)
                    continue;

                Product pro = frame.ChildProduct;
                if (scannedProducts.ContainsKey(pro))
                    continue;

                List<Product> matchedList = new List<Product>();
                pro.SearchMatchedProducts(matchedList, pro.mColor);
                foreach (Product tmp in matchedList)
                    scannedProducts[tmp] = 1;

                if (matchedList.Count >= UserSetting.MatchCount)
                    candidates.Add(new KeyValuePair<Product, int>(pro, matchedList.Count));
            }
        }


        if (candidates.Count > 0)
        {
            candidates.Sort((lsh, rsh) => { return rsh.Value - lsh.Value; });
            mgr.OnClick(candidates[0].Key.gameObject);
        }
    }
}
