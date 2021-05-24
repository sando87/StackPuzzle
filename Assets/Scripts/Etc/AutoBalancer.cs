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
    private int BotLevel = 0;

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
        ParseBotLevel();
        InGameManager mgr = null;
        int counter = 0;
        int counterLimit = NextSwipeCount();
        while (true)
        {
            yield return new WaitForSeconds(NextDelaySec());

            if (InGameManager.InstStage.gameObject.activeInHierarchy)
                mgr = InGameManager.InstStage;
            else if (InGameManager.InstPVP_Player.gameObject.activeInHierarchy)
                mgr = InGameManager.InstPVP_Player;
            else
                continue;

            if (mgr.IsIdle && mgr.IsAllProductIdle())
            {
                SwipeDirection dir = SwipeDirection.LEFT;
                Product nextTarget = FindSkill2(mgr, ref dir);
                if (nextTarget != null)
                {
                    counter = 0;
                    mgr.OnSwipe(nextTarget.gameObject, dir);
                    continue;
                }

                nextTarget = FindSkill1(mgr);
                if(nextTarget != null)
                {
                    counter = 0;
                    mgr.OnClick(nextTarget.gameObject);
                    continue;
                }

                if (counter >= counterLimit)
                {
                    counter = 0;
                    counterLimit = NextSwipeCount();
                    AutoClickNextProduct(mgr);
                    continue;
                }

                counter++;
                AutoSwipeNextProduct(mgr);
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
                if (cenPro == null || cenPro.IsLocked || cenPro.IsChocoBlock || cenPro.Skill != ProductSkill.Nothing)
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
                if (pro == null || pro.IsLocked || pro.IsChocoBlock)
                    continue;

                if(pro.Skill != ProductSkill.Nothing)
                {
                    if (pro.Left() != null && pro.Left().Skill != ProductSkill.Nothing)
                        mgr.OnSwipe(pro.gameObject, SwipeDirection.LEFT);
                    else if (pro.Right() != null && pro.Right().Skill != ProductSkill.Nothing)
                        mgr.OnSwipe(pro.gameObject, SwipeDirection.RIGHT);
                    else if (pro.Up() != null && pro.Up().Skill != ProductSkill.Nothing)
                        mgr.OnSwipe(pro.gameObject, SwipeDirection.UP);
                    else if (pro.Down() != null && pro.Down().Skill != ProductSkill.Nothing)
                        mgr.OnSwipe(pro.gameObject, SwipeDirection.DOWN);
                    else
                        mgr.OnClick(pro.gameObject);

                    return;
                }
                else
                {
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

    private Product FindSkill2(InGameManager mgr, ref SwipeDirection dir)
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
                if (pro == null || pro.IsLocked || pro.IsChocoBlock || pro.Skill == ProductSkill.Nothing)
                    continue;

                if (pro.Left() != null && pro.Left().Skill != ProductSkill.Nothing)
                {
                    dir = SwipeDirection.LEFT;
                    return pro;
                }
                else if (pro.Right() != null && pro.Right().Skill != ProductSkill.Nothing)
                {
                    dir = SwipeDirection.RIGHT;
                    return pro;
                }
                else if (pro.Up() != null && pro.Up().Skill != ProductSkill.Nothing)
                {
                    dir = SwipeDirection.UP;
                    return pro;
                }
                else if (pro.Down() != null && pro.Down().Skill != ProductSkill.Nothing)
                {
                    dir = SwipeDirection.DOWN;
                    return pro;
                }
            }
        }

        dir = SwipeDirection.LEFT;
        return null;
    }
    private Product FindSkill1(InGameManager mgr)
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
                if (pro == null || pro.IsLocked || pro.IsChocoBlock || pro.Skill == ProductSkill.Nothing)
                    continue;

                return pro;
            }
        }

        return null;
    }
    private void ParseBotLevel()
    {
        string[] strs = UserSetting.UserInfo.deviceName.Split('_');
        if (strs.Length <= 1)
            BotLevel = 0;
        else
            BotLevel = int.Parse(strs[0]);
    }
    private float NextDelaySec()
    {
        if(BotLevel < 0)
            return UnityEngine.Random.Range(1, 3);

        switch (BotLevel)
        {
            case 0: return UnityEngine.Random.Range(1, 3);
            case 1: return UnityEngine.Random.Range(0.7f, 2.5f);
            case 2: return UnityEngine.Random.Range(0.5f, 2);
            case 3: return UnityEngine.Random.Range(0.5f, 1.5f);
            case 4: return UnityEngine.Random.Range(0.5f, 1);
            case 5: return UnityEngine.Random.Range(0.3f, 0.8f);
            default: break; 
        }
        return UnityEngine.Random.Range(0.3f, 0.8f);
    }
    private int NextSwipeCount()
    {
        if (BotLevel < 0)
            return UnityEngine.Random.Range(0, 1);

        switch (BotLevel)
        {
            case 0: return UnityEngine.Random.Range(0, 1);
            case 1: return UnityEngine.Random.Range(1, 2);
            case 2: return UnityEngine.Random.Range(1, 3);
            case 3: return UnityEngine.Random.Range(2, 4);
            case 4: return UnityEngine.Random.Range(2, 5);
            case 5: return UnityEngine.Random.Range(3, 6);
            default: break;
        }
        return UnityEngine.Random.Range(3, 6);
    }
}
