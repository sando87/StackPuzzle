using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AutoBalancerInfo
{
    public Product targetProduct = null;
    public List<Product> matchedList = new List<Product>();
    public SwipeDirection direct = SwipeDirection.LEFT;
    public bool DecideDirection(Product cenPro)
    {
        if (cenPro.IsChocoBlock())
            return false;

        if (cenPro.Left() != null)
        {
            Product pro = cenPro.Left();
            List<Product> matches = new List<Product>();
            pro.SearchMatchedProductsAround(matches, cenPro.mColor, SwipeDirection.RIGHT);
            if(matches.Count > matchedList.Count)
            {
                targetProduct = cenPro;
                matchedList = matches;
                direct = SwipeDirection.LEFT;
            }
        }

        if (cenPro.Right() != null)
        {
            Product pro = cenPro.Right();
            List<Product> matches = new List<Product>();
            pro.SearchMatchedProductsAround(matches, cenPro.mColor, SwipeDirection.LEFT);
            if (matches.Count > matchedList.Count)
            {
                targetProduct = cenPro;
                matchedList = matches;
                direct = SwipeDirection.RIGHT;
            }
        }

        if (cenPro.Up() != null)
        {
            Product pro = cenPro.Up();
            List<Product> matches = new List<Product>();
            pro.SearchMatchedProductsAround(matches, cenPro.mColor, SwipeDirection.DOWN);
            if (matches.Count > matchedList.Count)
            {
                targetProduct = cenPro;
                matchedList = matches;
                direct = SwipeDirection.UP;
            }
        }

        if (cenPro.Down() != null)
        {
            Product pro = cenPro.Down();
            List<Product> matches = new List<Product>();
            pro.SearchMatchedProductsAround(matches, cenPro.mColor, SwipeDirection.UP);
            if (matches.Count > matchedList.Count)
            {
                targetProduct = cenPro;
                matchedList = matches;
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
        while(true)
        {
            yield return new WaitForSeconds(2);
            if (!InGameManager.Inst.gameObject.activeInHierarchy)
                continue;

            if (InGameManager.Inst.IsIdle)
            {
                if (UnityEngine.Random.Range(0, 3) == 0)
                    AutoSwipeNextProduct();
                else
                    AutoClickNextProduct();
            }
        }
    }

    void AutoSwipeNextProduct()
    {
        int mCntX = InGameManager.Inst.CountX;
        int mCntY = InGameManager.Inst.CountY;
        List<AutoBalancerInfo> candidates = new List<AutoBalancerInfo>();
        for (int y = 0; y < mCntY; ++y)
        {
            for (int x = 0; x < mCntX; ++x)
            {
                Frame frame = InGameManager.Inst.GetFrame(x, y);
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
            candidates.Sort((lsh, rsh) => { return rsh.matchedList.Count - lsh.matchedList.Count; });
            InGameManager.Inst.OnSwipe(candidates[0].targetProduct.gameObject, candidates[0].direct);
        }
        else
        {
            int ranX = UnityEngine.Random.Range(0, mCntX);
            int ranY = UnityEngine.Random.Range(0, mCntY);
            Frame randomFrame = InGameManager.Inst.GetFrame(ranX, ranY);
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
    void AutoClickNextProduct()
    {
        int mCntX = InGameManager.Inst.CountX;
        int mCntY = InGameManager.Inst.CountY;
        List<KeyValuePair< Product, int>> candidates = new List<KeyValuePair<Product, int>>();
        Dictionary<Product, int> scannedProducts = new Dictionary<Product, int>();
        for (int y = 0; y < mCntY; ++y)
        {
            for (int x = 0; x < mCntX; ++x)
            {
                Frame frame = InGameManager.Inst.GetFrame(x, y);
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
            InGameManager.Inst.OnClick(candidates[0].Key.gameObject);
        }
    }
}
