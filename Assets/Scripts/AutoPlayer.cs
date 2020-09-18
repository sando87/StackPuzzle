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

public class AutoPlayer : MonoBehaviour
{
    List<KeyValuePair<Product, SwipeDirection>> nexts = new List<KeyValuePair<Product, SwipeDirection>>();

    private void Start()
    {
        StartCoroutine(DoAutoPlay());
    }

    IEnumerator DoAutoPlay()
    {
        while(true)
        {
            yield return new WaitForSeconds(2);
            if (!InGameManager.Inst.gameObject.activeInHierarchy)
                continue;
            if (InGameManager.Inst.IsIdle())
                ScanNextProduct();
        }
    }

    void ScanNextProduct()
    {
        if (nexts.Count > 1)
        {
            InGameManager.Inst.MatchLock = true;
            InGameManager.Inst.OnSwipe(nexts[0].Key.gameObject, nexts[0].Value);
            nexts.Remove(nexts[0]);
            return;
        }
        else if (nexts.Count == 1)
        {
            InGameManager.Inst.MatchLock = false;
            InGameManager.Inst.OnSwipe(nexts[0].Key.gameObject, nexts[0].Value);
            nexts.Clear();
            return;
        }

        int mCntX = InGameManager.Inst.CountX;
        int mCntY = InGameManager.Inst.CountY;
        int baseX = UnityEngine.Random.Range(0, mCntX);
        int baseY = UnityEngine.Random.Range(0, mCntY);
        Frame[,] frames = InGameManager.Inst.Frames;
        for (int y = 0; y < mCntY; ++y)
        {
            int fixedY = (baseY + y) % mCntY;
            for (int x = 0; x < mCntX; ++x)
            {
                int fixedX = (baseX + x) % mCntX;
                if (frames[fixedX, fixedY].ChildProduct.IsChocoBlock())
                    continue;

                MatchableInfo info = ScanMatchable(frames[fixedX, fixedY].ChildProduct);
                if (info.matches.Count >= UserSetting.MatchCount)
                {
                    if (info.matches[0].Right() == info.matches[1])
                        nexts.Add(new KeyValuePair<Product, SwipeDirection>(info.matches[0], SwipeDirection.RIGHT));
                    else if (info.matches[0].Left() == info.matches[1])
                        nexts.Add(new KeyValuePair<Product, SwipeDirection>(info.matches[0], SwipeDirection.LEFT));
                    else if (info.matches[0].Up() == info.matches[1])
                        nexts.Add(new KeyValuePair<Product, SwipeDirection>(info.matches[0], SwipeDirection.UP));
                    else if (info.matches[0].Down() == info.matches[1])
                        nexts.Add(new KeyValuePair<Product, SwipeDirection>(info.matches[0], SwipeDirection.DOWN));
                    return;
                }
                else if (info.nearProduct != null)
                {
                    Frame to = info.matches[0].ParentFrame;
                    Frame from = info.nearProduct.ParentFrame;
                    if (to.IndexX == from.IndexX)
                    {
                        int cntY = Math.Abs(to.IndexY - from.IndexY) - 1;
                        for (int i = 0; i < cntY; ++i)
                            nexts.Add(new KeyValuePair<Product, SwipeDirection>(from.ChildProduct, to.IndexY < from.IndexY ? SwipeDirection.DOWN : SwipeDirection.UP));
                    }
                    else if (to.IndexY == from.IndexY)
                    {
                        int cntX = Math.Abs(to.IndexX - from.IndexX) - 1;
                        for (int i = 0; i < cntX; ++i)
                            nexts.Add(new KeyValuePair<Product, SwipeDirection>(from.ChildProduct, to.IndexX < from.IndexX ? SwipeDirection.LEFT : SwipeDirection.RIGHT));
                    }
                    else
                    {
                        bool xAlign = UnityEngine.Random.Range(0, 1) == 0;
                        int cntX = Math.Abs(to.IndexX - from.IndexX) - (xAlign ? 0 : 1);
                        int cntY = Math.Abs(to.IndexY - from.IndexY) - (xAlign ? 1 : 0);
                        for (int i = 0; i < cntX; ++i)
                            nexts.Add(new KeyValuePair<Product, SwipeDirection>(from.ChildProduct, to.IndexX < from.IndexX ? SwipeDirection.LEFT : SwipeDirection.RIGHT));
                        for (int i = 0; i < cntY; ++i)
                            nexts.Add(new KeyValuePair<Product, SwipeDirection>(from.ChildProduct, to.IndexY < from.IndexY ? SwipeDirection.DOWN : SwipeDirection.UP));
                        nexts.Sort((lsh, rsh) => { return UnityEngine.Random.Range(-1, 1); });
                    }

                    return;
                }

            }
        }

        nexts.Add(new KeyValuePair<Product, SwipeDirection>(frames[baseX, baseY].ChildProduct, (SwipeDirection)UnityEngine.Random.Range(0, 4)));
    }
    MatchableInfo ScanMatchable(Product product)
    {
        MatchableInfo info = new MatchableInfo();
        info.matches = new List<Product>();
        info.nearProduct = null;
        product.SearchMatchedProducts(info.matches, product.mColor);
        if (info.matches.Count == UserSetting.MatchCount - 1)
        {
            info.nearProduct = SearchNearProducts(info.matches);
        }

        return info;
    }
    Product SearchNearProducts(List<Product> matches)
    {
        int idx = 0;
        int idxX = matches[0].ParentFrame.IndexX;
        int idxY = matches[0].ParentFrame.IndexY;
        while (idx <= 10)
        {
            int num = (idx / 2) + 1;
            int sign = num % 2 == 0 ? -1 : 1;
            while (--num >= 0)
            {
                idxX += idx % 2 == 0 ? sign : 0;
                idxY += idx % 2 == 0 ? 0 : sign;
                Frame targetFrame = InGameManager.Inst.GetFrame(idxX, idxY);
                if (targetFrame == null)
                    continue;

                Product target = targetFrame.ChildProduct;
                if (!matches.Contains(target) && target.mColor == matches[0].mColor && !target.IsChocoBlock())
                    return target;
            }
            idx++;
        }
        return null;
    }
}
