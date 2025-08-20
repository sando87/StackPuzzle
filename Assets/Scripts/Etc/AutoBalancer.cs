using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class AutoBalancerInfo
{
    public Product targetProduct = null;
    public int maxCount = 0;
    public SwipeDirection direct = SwipeDirection.LEFT;

    public void Reset()
    {
        targetProduct = null;
        maxCount = 0;
        direct = SwipeDirection.LEFT;
    }
}

public class AutoBalancer : MonoBehaviour
{
    InGameManager mCurrentManager = null;

    void Awake()
    {
        StartCoroutine(CoInvokerBot());
    }

    IEnumerator CoInvokerBot()
    {
        while (true)
        {
            yield return new WaitUntil(() => UserSetting.UserInfo.IsBot);
            StartCoroutine(nameof(DoAutoBalancerNew));
            yield return new WaitUntil(() => !UserSetting.UserInfo.IsBot);
            StopCoroutine(nameof(DoAutoBalancerNew));
        }
    }

    IEnumerator DoAutoBalancerNew()
    {
        yield return new WaitForSeconds(5);
        const int MODE_SWIPE = 1;
        const int MODE_COMBOUP = 2;
        const int MODE_ATTACK = 3;
        int mode = MODE_SWIPE;
        int swipCount = 0;
        int maxSwipCount = 0;
        List<Product> swipedProducts = new List<Product>();
        while (true)
        {
            while (mCurrentManager == null)
            {
                if (InGameManager.InstStage.gameObject.activeInHierarchy)
                    mCurrentManager = InGameManager.InstStage;
                else if (InGameManager.InstPVP_Player.gameObject.activeInHierarchy)
                    mCurrentManager = InGameManager.InstPVP_Player;

                yield return null;
            }

            yield return new WaitUntil(() => mCurrentManager.IsIdle && mCurrentManager.IsAllProductIdle());
            yield return new WaitForSeconds(NextDelaySec());
            yield return new WaitUntil(() => mCurrentManager.IsIdle && mCurrentManager.IsAllProductIdle());
            bool isItemUse = IsUseItem();

            if (isItemUse && IsItemPossible(PurchaseItemType.ExtendLimit) && IsFlushedable())
            {
                UseItem(PurchaseItemType.ExtendLimit);
                continue;
            }

            if (isItemUse && IsItemPossible(PurchaseItemType.RemoveIce) && CountIceBlocks() > 10)
            {
                UseItem(PurchaseItemType.RemoveIce);
                continue;
            }

            int skipCount = 0;
            if (mode == MODE_SWIPE)
            {
                if (swipCount > maxSwipCount)
                {
                    swipedProducts.Clear();
                    swipCount = 0;
                    maxSwipCount = NextSwipeCount();
                    mode = MODE_COMBOUP;
                }
                else if (AutoSwipeNextProduct(mCurrentManager, swipedProducts))
                {
                    swipCount++;
                    continue;
                }
                else
                {
                    swipedProducts.Clear();
                    swipCount = 0;
                    maxSwipCount = NextSwipeCount();
                    mode = MODE_COMBOUP;
                    skipCount++;
                }
            }

            if (mode == MODE_COMBOUP)
            {
                if (AutoClickKeepCombo())
                {
                    mode = MODE_ATTACK;
                    continue;
                }
                else if (AutoClickNextProduct(mCurrentManager))
                {
                    mode = MODE_ATTACK;
                    continue;
                }
                else
                {
                    mode = MODE_ATTACK;
                    skipCount++;
                }
            }

            if (mode == MODE_ATTACK)
            {
                if (isItemUse)
                {
                    if (IsItemPossible(PurchaseItemType.KeepCombo))
                    {
                        UseItem(PurchaseItemType.KeepCombo);
                        mode = MODE_SWIPE;
                        continue;
                    }
                    else if (IsItemPossible(PurchaseItemType.MakeSkill1))
                    {
                        UseItem(PurchaseItemType.MakeSkill1);
                        mode = MODE_ATTACK;
                        continue;
                    }
                    else if (IsItemPossible(PurchaseItemType.MakeSkill2))
                    {
                        UseItem(PurchaseItemType.MakeSkill2);
                        mode = MODE_ATTACK;
                        continue;
                    }
                    else if (IsItemPossible(PurchaseItemType.Meteor))
                    {
                        UseItem(PurchaseItemType.Meteor);
                        mode = MODE_SWIPE;
                        continue;
                    }
                }
                
                if (AutoAttackSkill(mCurrentManager))
                {
                    mode = MODE_SWIPE;
                    continue;
                }
                else
                {
                    mode = MODE_SWIPE;
                    skipCount++;
                }
            }

            if (skipCount >= 3)
            {
                // gave up game...
            }
        }
    }

    IEnumerator DoAutoBalancer()
    {
        yield return null;
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

                //nextTarget = FindSkill1(mgr);
                //if(nextTarget != null)
                //{
                //    counter = 0;
                //    mgr.OnClick(nextTarget.gameObject);
                //    continue;
                //}

                if (counter >= counterLimit)
                {
                    counter = 0;
                    counterLimit = NextSwipeCount();
                    // if(!AutoClickNextProduct(mgr))
                    //     AutoSwipeNextProduct(mgr);

                    continue;
                }

                counter++;
                // AutoSwipeNextProduct(mgr);
            }
        }
    }

    bool AutoSwipeNextProduct(InGameManager mgr, List<Product> swipedProducts)
    {
        AutoBalancerInfo info = new AutoBalancerInfo();
        List<Product> matches = new List<Product>();
        List<SwipeDirection> dirs = new List<SwipeDirection>();
        int mCntX = mgr.CountX;
        int mCntY = mgr.CountY;
        int yOff = UnityEngine.Random.Range(0, mCntY);
        for (int y = 0; y < mCntY; ++y)
        {
            int fixedY = (y + yOff) % mCntY;
            for (int x = 0; x < mCntX; ++x)
            {
                Frame frame = mgr.Frame(x, fixedY);
                Product cenPro = frame.ChildProduct;
                if (!IsValid(frame) || cenPro.Skill != ProductSkill.Nothing)
                    continue;

                if (swipedProducts.Contains(cenPro))
                    continue;

                info.Reset();
                matches.Clear();
                cenPro.SearchMatchedProducts(matches, cenPro.Color);
                if (matches.Count >= UserSetting.MatchCount)
                    continue;

                dirs.Clear();
                int leftMatchCount = mgr.NextMatchCount(cenPro, SwipeDirection.LEFT);
                if (UserSetting.MatchCount <= leftMatchCount && leftMatchCount <= UserSetting.MatchCount + 1)
                {
                    dirs.Add(SwipeDirection.LEFT);
                    info.maxCount = leftMatchCount;
                    info.direct = SwipeDirection.LEFT;
                    info.targetProduct = cenPro.Left();
                }
                int rightMatchCount = mgr.NextMatchCount(cenPro, SwipeDirection.RIGHT);
                if (UserSetting.MatchCount <= rightMatchCount && rightMatchCount <= UserSetting.MatchCount + 1)
                {
                    dirs.Add(SwipeDirection.RIGHT);
                    info.maxCount = rightMatchCount;
                    info.direct = SwipeDirection.RIGHT;
                    info.targetProduct = cenPro.Right();
                }
                int upMatchCount = mgr.NextMatchCount(cenPro, SwipeDirection.UP);
                if (UserSetting.MatchCount <= upMatchCount && upMatchCount <= UserSetting.MatchCount + 1)
                {
                    dirs.Add(SwipeDirection.UP);
                    info.maxCount = upMatchCount;
                    info.direct = SwipeDirection.UP;
                    info.targetProduct = cenPro.Up();
                }
                int downMatchCount = mgr.NextMatchCount(cenPro, SwipeDirection.DOWN);
                if (UserSetting.MatchCount <= downMatchCount && downMatchCount <= UserSetting.MatchCount + 1)
                {
                    dirs.Add(SwipeDirection.DOWN);
                    info.maxCount = downMatchCount;
                    info.direct = SwipeDirection.DOWN;
                    info.targetProduct = cenPro.Down();
                }

                if (dirs.Count > 0)
                {
                    SwipeDirection selectedDir = dirs[UnityEngine.Random.Range(0, dirs.Count)];
                    swipedProducts.Add(cenPro);
                    swipedProducts.Add(cenPro.Dir(selectedDir));
                    mgr.OnSwipe(cenPro.gameObject, selectedDir);
                    return true;
                }
            }
        }

        return false;
    }
    bool AutoClickNextProduct(InGameManager mgr)
    {
        Dictionary<Product, int> donePros = new Dictionary<Product, int>();
        List<Product> firstMatches = new List<Product>();
        int mCntX = mgr.CountX;
        int mCntY = mgr.CountY;
        int yOff = UnityEngine.Random.Range(0, mCntY);
        List<Product[]> productGroups = null;
        int maxCombo = 0;
        for (int y = 0; y < mCntY; ++y)
        {
            int fixedY = (y + yOff) % mCntY;
            for (int x = 0; x < mCntX; ++x)
            {
                Frame frame = mgr.Frame(x, fixedY);
                Product pro = frame.ChildProduct;
                if (!IsValid(frame))
                    continue;

                if (pro.Skill == ProductSkill.Nothing)
                {
                    donePros.Clear();
                    firstMatches.Clear();
                    pro.SearchMatchedProducts(firstMatches, pro.Color);
                    if (firstMatches.Count >= UserSetting.MatchCount)
                    {
                        List<Product[]> groups = mgr.FindAllLinkedProductGroups(firstMatches, donePros);
                        int curCombo = groups.Count;
                        if (curCombo > maxCombo)
                        {
                            maxCombo = curCombo;
                            productGroups = groups;
                        }
                    }
                }
            }
        }

        if (productGroups != null)
        {
            Product[] lastOne = productGroups.Last();
            mgr.OnClick(lastOne[0].gameObject);
            return true;
        }

        return false;
    }

    private bool AutoAttackSkill(InGameManager mgr)
    {
        SwipeDirection dir = SwipeDirection.LEFT;
        Product nextTarget = FindSkill2(mgr, ref dir, ProductSkill.SameColor);
        if (nextTarget != null)
        {
            mgr.OnSwipe(nextTarget.gameObject, dir);
            return true;
        }

        nextTarget = FindSkill2(mgr, ref dir);
        if (nextTarget != null)
        {
            mgr.OnSwipe(nextTarget.gameObject, dir);
            return true;
        }

        nextTarget = FindSkill1(mgr, ProductSkill.SameColor);
        if (nextTarget != null)
        {
            mgr.OnClick(nextTarget.gameObject);
            return true;
        }

        nextTarget = FindSkill1(mgr);
        if (nextTarget != null)
        {
            mgr.OnClick(nextTarget.gameObject);
            return true;
        }

        return false;
    }

    private Product FindSkill2(InGameManager mgr, ref SwipeDirection dir, ProductSkill firstSkill = ProductSkill.Nothing)
    {
        int mCntX = mgr.CountX;
        int mCntY = mgr.CountY;
        int yOff = UnityEngine.Random.Range(0, mCntY);
        for (int y = 0; y < mCntY; ++y)
        {
            int fixedY = (y + yOff) % mCntY;
            for (int x = 0; x < mCntX; ++x)
            {
                Frame frame = mgr.Frame(x, fixedY);
                Product pro = frame.ChildProduct;
                if (!IsValid(frame) || pro.Skill == ProductSkill.Nothing)
                    continue;

                if (firstSkill != ProductSkill.Nothing && pro.Skill != firstSkill)
                    continue;

                Frame nextFrame = frame.Left();
                if (IsValid(nextFrame) && nextFrame.ChildProduct.Skill != ProductSkill.Nothing)
                {
                    dir = SwipeDirection.LEFT;
                    return pro;
                }

                nextFrame = frame.Right();
                if (IsValid(nextFrame) && nextFrame.ChildProduct.Skill != ProductSkill.Nothing)
                {
                    dir = SwipeDirection.RIGHT;
                    return pro;
                }

                nextFrame = frame.Up();
                if (IsValid(nextFrame) && nextFrame.ChildProduct.Skill != ProductSkill.Nothing)
                {
                    dir = SwipeDirection.UP;
                    return pro;
                }

                nextFrame = frame.Down();
                if (IsValid(nextFrame) && nextFrame.ChildProduct.Skill != ProductSkill.Nothing)
                {
                    dir = SwipeDirection.DOWN;
                    return pro;
                }
            }
        }

        dir = SwipeDirection.LEFT;
        return null;
    }
    private Product FindSkill1(InGameManager mgr, ProductSkill firstSkill = ProductSkill.Nothing)
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
                if (pro == null || pro.IsLocked || pro.ParentFrame.IsObstacled() || pro.IsObstacled() || pro.Skill == ProductSkill.Nothing)
                    continue;

                if (firstSkill != ProductSkill.Nothing && pro.Skill != firstSkill)
                    continue;

                return pro;
            }
        }

        return null;
    }

    private int CountIceBlocks()
    {
        int ret = 0;
        int mCntX = mCurrentManager.CountX;
        int mCntY = mCurrentManager.CountY;
        for (int y = 0; y < mCntY; ++y)
        {
            for (int x = 0; x < mCntX; ++x)
            {
                Product pro = mCurrentManager.Frame(x, y).ChildProduct;
                if (pro == null || pro.IsLocked)
                    continue;

                if (pro.IsIceBlock)
                    ret++;
            }
        }

        return ret;
    }
    private Product FindKeepCombo()
    {
        int mCntX = mCurrentManager.CountX;
        int mCntY = mCurrentManager.CountY;
        for (int y = 0; y < mCntY; ++y)
        {
            for (int x = 0; x < mCntX; ++x)
            {
                Product pro = mCurrentManager.Frame(x, y).ChildProduct;
                if (pro == null || pro.IsLocked)
                    continue;

                if (pro.Skill == ProductSkill.KeepCombo)
                    return pro;
            }
        }

        return null;
    }


    private float NextDelaySec()
    {
        // 첫번째 자리수 숫자
        int level = UserSetting.UserInfo.botLevel / 100;
        switch (level)
        {
            case 1: return UnityEngine.Random.Range(1, 6);
            case 2: return UnityEngine.Random.Range(0.5f, 4);
            case 3: return UnityEngine.Random.Range(0.1f, 1.5f);
            default: break;
        }
        return UnityEngine.Random.Range(0.5f, 4);
    }
    private int NextSwipeCount()
    {
        // 두번째 자리수 숫자
        int level = (UserSetting.UserInfo.botLevel / 10) % 10;
        switch (level)
        {
            case 1: return UnityEngine.Random.Range(0, 1);
            case 2: return UnityEngine.Random.Range(5, 10);
            case 3: return UnityEngine.Random.Range(15, 20);
            default: break;
        }
        return 10;
    }
    public static bool IsUseItem()
    {
        // 세번째 자리수 숫자
        int level = UserSetting.UserInfo.botLevel % 10;
        int percent = UnityEngine.Random.Range(0, 1000) % 100;
        switch (level)
        {
            case 1: return percent < 0;
            case 2: return percent < 50;
            case 3: return percent < 100;
            default: break;
        }
        return false;
    }
    private bool IsValid(Frame frame)
    {
        return frame != null && !frame.IsObstacled() && frame.ChildProduct != null && !frame.ChildProduct.IsLocked && !frame.ChildProduct.IsObstacled();
    }

    bool IsItemPossible(PurchaseItemType itemType)
    {
        if (mCurrentManager == InGameManager.InstPVP_Player)
        {
            return MenuBattle.Inst().IsItemPossible(itemType);
        }
        else if (mCurrentManager == InGameManager.InstStage)
        {
            return MenuInGame.Inst().IsItemPossible(itemType);
        }
        return false;
    }
    void UseItem(PurchaseItemType itemType)
    {
        if (mCurrentManager == InGameManager.InstPVP_Player)
        {
            MenuBattle.Inst().UseItemByAutoBot(itemType);
        }
        else if (mCurrentManager == InGameManager.InstStage)
        {
            MenuInGame.Inst().UseItemByAutoBot(itemType);
        }
    }
    bool IsFlushedable()
    {
        return mCurrentManager.PVPScoreBar.CurrentScore < -UserSetting.ScorePerAttack;
    }

    bool AutoClickKeepCombo()
    {
        Product keepCombo = FindKeepCombo();
        if (keepCombo == null)
            return false;

        if (mCurrentManager.IsPossibleKeepCombo(keepCombo))
        {
            mCurrentManager.OnClick(keepCombo.gameObject);
            return true;
        }
        return false;
    }
}
