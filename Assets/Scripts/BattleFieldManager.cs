using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattleFieldManager : MonoBehaviour
{
    private static BattleFieldManager mMe = null;
    private static BattleFieldManager mOpp = null;
    public static BattleFieldManager Me
    {
        get
        {
            if(mMe ==  null)
                mMe = GameObject.Find("WorldSpace").transform.Find("BattleScreen/GameFieldMe").GetComponent<BattleFieldManager>();
            return mMe;
        }
    }
    public static BattleFieldManager Opp
    {
        get
        {
            if (mOpp == null)
                mOpp = GameObject.Find("WorldSpace").transform.Find("BattleScreen/GameFieldOpp").GetComponent<BattleFieldManager>();
            return mOpp;
        }
    }

    public const int MatchCount = 3;
    public const int attackScore = 10;
    public const float GridSize = 0.8f;
    public const int NextRequestCount = 500;

    public GameObject[] ProductPrefabs;
    public GameObject FramePrefab1;
    public GameObject FramePrefab2;
    public GameObject MaskPrefab;
    public GameObject AttackPointPrefab;
    public GameObject ComboNumPrefab;
    public BattleFieldManager Opponent;

    private Frame[,] mFrames = null;
    private Dictionary<int, Frame> mDestroyes = new Dictionary<int, Frame>();
    private List<ProductColor> mNextColors = new List<ProductColor>();
    private Queue<ProductSkill> mNextSkills = new Queue<ProductSkill>();
    private LinkedList<Header> mNetMessages = new LinkedList<Header>();
    private int mNextPositionIndex = 0;
    private int mThisUserPK = 0;
    private int mCountX = 0;
    private int mCountY = 0;
    private int mIdleCounter = -1;
    private bool mAtleastOneMatched = false;
    private int mCurrentCombo = 0;
    private int mKeepCombo = 0;
    private float mColorCount = 0;

    public bool IsIdle { get { return mIdleCounter < 0; } }
    public int CountX { get { return mCountX; } }
    public int CountY { get { return mCountY; } }
    public Frame[,] Frames { get { return mFrames; } }
    public AttackPoints AttackPoints { get; set; }
    public bool MatchLock { get; set; }
    public int UserPK { get { return mThisUserPK; } }
    public Action<int> EventOnKeepCombo;

    public void StartGame(int userPK, int XCount, int YCount, ProductColor[,] initColors, float colorCount)
    {
        ResetGame();
    
        mThisUserPK = userPK;
        mCountX = XCount;
        mCountY = YCount;
        mColorCount = colorCount;

        transform.parent.gameObject.SetActive(true);
    
        //GameObject mask = Instantiate(MaskPrefab, transform);
        //mask.transform.localScale = new Vector3(XCount * 0.97f, YCount * 0.97f, 1);

        if(IsPlayerField())
        {
            GetComponent<SwipeDetector>().EventSwipe = OnSwipe;
            GetComponent<SwipeDetector>().EventClick = OnClick;
            StartCoroutine(FlushChocos());
        }
        else
        {
            transform.localScale = new Vector3(UserSetting.BattleOppResize, UserSetting.BattleOppResize, 1);
            NetClientApp.GetInstance().EventResponse = ResponseFromOpponent;
            StartCoroutine(CheckNetMessage());
        }

        RequestNextColors(NextRequestCount);

        StartCoroutine(CheckNextProducts());

        StartCoroutine(CheckIdle());

        Vector3 localBasePos = new Vector3(-GridSize * XCount * 0.5f, -GridSize * YCount * 0.5f, 0);
        localBasePos.x += GridSize * 0.5f;
        localBasePos.y += GridSize * 0.5f;
        Vector3 localFramePos = new Vector3(0, 0, 0);
        mFrames = new Frame[XCount, YCount];
        for (int y = 0; y < YCount; y++)
        {
            for (int x = 0; x < XCount; x++)
            {
                GameObject frameObj = GameObject.Instantiate((x + y) % 2 == 0 ? FramePrefab1 : FramePrefab2, transform, false);
                localFramePos.x = GridSize * x;
                localFramePos.y = GridSize * y;
                frameObj.transform.localPosition = localBasePos + localFramePos;
                mFrames[x, y] = frameObj.GetComponent<Frame>();
                mFrames[x, y].Initialize(x, y, 0);
                mFrames[x, y].GetFrame = GetFrame;
                CreateNewProduct(mFrames[x, y], initColors[x,y]);
            }
        }

        SecondaryInitFrames();

        GameObject ap = Instantiate(AttackPointPrefab, transform);
        ap.transform.localPosition = localBasePos + new Vector3(-GridSize + 0.2f, GridSize * YCount - 0.1f, 0);
        AttackPoints = ap.GetComponent<AttackPoints>();

        MenuBattle.PopUp();
    }
    public static void FinishGame(bool success)
    {
        if (Me.mThisUserPK <= 0)
            return;

        SoundPlayer.Inst.Player.Stop();
        if (success)
            SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectSuccess);
        else
            SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectGameOver);

        NetClientApp.GetInstance().EventResponse = null;

        int deltaScore = NextDeltaScore(success, UserSetting.UserScore, Me.mColorCount);
        UserSetting.UserScore += deltaScore;
        UserSetting.Win = success;

        LOG.echo(Me.SummaryToCSVString(success, deltaScore));

        EndGame info = new EndGame();
        info.fromUserPk = BattleFieldManager.Me.UserPK;
        info.toUserPk = BattleFieldManager.Opp.UserPK;
        info.win = success;
        info.userInfo = UserSetting.UserInfo;
        NetClientApp.GetInstance().Request(NetCMD.EndGame, info, null);

        BattleFieldManager.Me.ResetGame();
        BattleFieldManager.Opp.ResetGame();

        BattleFieldManager.Me.transform.parent.gameObject.SetActive(false);

        MenuFinishBattle.PopUp(success, UserSetting.UserInfo, deltaScore);
    }

    public static int NextDeltaScore(bool isWin, int curScore, float colorCount)
    {
        float difficulty = (colorCount - 4.0f) * 5.0f;
        float curX = curScore * 0.01f;
        float degree = 90 - (Mathf.Atan(curX - difficulty) * Mathf.Rad2Deg);
        float nextX = 0;
        if (isWin)
            nextX = curX + (degree / 1000.0f);
        else
            nextX = curX - ((180 - degree) / 1000.0f);

        return (int)((nextX - curX) * 100.0f);
    }

    public void OnClick(GameObject obj)
    {
        if (!IsIdle)
            return;

        Product product = obj.GetComponent<Product>();
        mIdleCounter = 1;
        if (product.TryMatch())
        {
            SendControlInfo(product.ParentFrame.IndexX, product.ParentFrame.IndexY, SwipeDirection.LEFT, true);
        }
        else
        {
            SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectWrongMatched);
            product.mAnimation.Play("swap");
        }
    }
    public void OnSwipe(GameObject obj, SwipeDirection dir)
    {
        if (!IsIdle)
            return;

        Product product = obj.GetComponent<Product>();
        Product targetProduct = null;
        switch (dir)
        {
            case SwipeDirection.UP: targetProduct = product.Up(); break;
            case SwipeDirection.DOWN: targetProduct = product.Down(); break;
            case SwipeDirection.LEFT: targetProduct = product.Left(); break;
            case SwipeDirection.RIGHT: targetProduct = product.Right(); break;
        }

        if (targetProduct != null && !product.IsLocked() && !targetProduct.IsLocked())
        {
            SendControlInfo(product.ParentFrame.IndexX, product.ParentFrame.IndexY, dir, false);
            product.StartSwipe(targetProduct.GetComponentInParent<Frame>());
            targetProduct.StartSwipe(product.GetComponentInParent<Frame>());
        }
    }
    private void OnMatch(List<Product> matches)
    {
        mIdleCounter--;
        if (MatchLock || matches.Count < UserSetting.MatchCount)
            return;

        mAtleastOneMatched = true;
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectMatched);

        MakeSkillProduct(matches.Count);

        bool isSameColorEnable = false;
        foreach (Product pro in matches)
        {
            if (pro.mSkill == ProductSkill.SameColor)
            {
                isSameColorEnable = true;
                break;
            }
        }

        Product mainProduct = matches[0];
        List<Product> destroies = isSameColorEnable ? GetSameColorProducts(mainProduct.mColor) : matches;

        int currentCombo = mCurrentCombo;
        if (mainProduct.IsFirst && mKeepCombo > 0)
        {
            currentCombo = mKeepCombo;
            mKeepCombo = 0;
            if (IsPlayerField())
                MenuBattle.Inst().NextCombo = 0;
        }

        mCurrentCombo = currentCombo + 1;
        if (IsPlayerField())
            MenuBattle.Inst().CurrentCombo = mCurrentCombo;

        foreach (Product pro in destroies)
        {
            pro.Combo = currentCombo + 1;
            pro.StartDestroy();
            BreakItemSkill(pro);
            if (IsPlayerField())
            {
                MenuBattle.Inst().AddScore(pro);

                Vector3 pos = pro.transform.position;
                pos.z -= 1;
                GameObject obj = GameObject.Instantiate(ComboNumPrefab, pos, Quaternion.identity, pro.ParentFrame.transform);
                obj.GetComponent<Numbers>().Number = pro.Combo;
                pos.y += UserSetting.GridSize * 0.4f;
                StartCoroutine(Utils.AnimateConvex(obj, pos, 0.7f, () => {
                    Destroy(obj);
                }));
            }
        }

        Attack(destroies.Count * (currentCombo + 1), mainProduct.transform.position);
        mainProduct.StartFlash(matches);
    }
    private List<Product> GetSameColorProducts(ProductColor color)
    {
        List<Product> list = new List<Product>();
        foreach (Frame frame in mFrames)
        {
            if (frame.Empty || frame.ChildProduct == null || frame.ChildProduct.IsLocked() || frame.ChildProduct.IsChocoBlock())
                continue;
            if (frame.ChildProduct.mColor != color)
                continue;
            list.Add(frame.ChildProduct);
        }
        return list;
    }
    private void BreakItemSkill(Product product)
    {
        if (product.mSkill == ProductSkill.OneMore)
        {
            mCurrentCombo++;
            if (IsPlayerField())
                MenuBattle.Inst().OneMoreCombo(product);
        }
        else if (product.mSkill == ProductSkill.KeepCombo)
        {
            mKeepCombo = Mathf.Max(mKeepCombo, product.Combo);
            if (IsPlayerField())
                MenuBattle.Inst().KeepNextCombo(product);
        }
        else if (product.mSkill == ProductSkill.SameColor)
        {
        }
    }
    private void MakeSkillProduct(int matchedCount)
    {
        if (matchedCount <= UserSetting.MatchCount)
            return;

        switch (matchedCount)
        {
            case 5: mNextSkills.Enqueue(ProductSkill.OneMore); break;
            case 6: mNextSkills.Enqueue(ProductSkill.KeepCombo); break;
            case 7: mNextSkills.Enqueue(ProductSkill.SameColor); break;
            //default: mNextSkills.Enqueue(ProductSkill.OneMore); break;
        }
    }
    private void OnDestroyProduct(Product pro)
    {
        int idxX = pro.ParentFrame.IndexX;
        if (!mDestroyes.ContainsKey(idxX))
            mDestroyes[idxX] = pro.ParentFrame;
        else
        {
            if (pro.ParentFrame.IndexY < mDestroyes[idxX].IndexY)
                mDestroyes[idxX] = pro.ParentFrame;
        }
    }

    private void StartNextProducts(Frame baseFrame)
    {
        Frame curFrame = baseFrame;
        Frame validFrame = curFrame;
        while (curFrame != null)
        {
            Product pro = NextUpProductFrom(validFrame);
            if (pro == null)
                break;

            validFrame = pro.ParentFrame;
            pro.StartDropAnimate(curFrame, pro.ParentFrame.IndexY - curFrame.IndexY, curFrame == baseFrame);
            pro.Renderer.sortingLayerName = IsPlayerField() ? "BattleMaskMe" : "BattleMaskOpp";

            curFrame = curFrame.Up();
        }

        Frame topFrame = SubTopFrame(curFrame);
        int emptyCount = topFrame.IndexY - curFrame.IndexY + 1;
        while (curFrame != null)
        {
            Product pro = CreateNewProduct(curFrame, GetNextColor(), mNextSkills.Count > 0 ? mNextSkills.Dequeue() : ProductSkill.Nothing);
            pro.StartDropAnimate(curFrame, emptyCount, curFrame == baseFrame);
            pro.Renderer.sortingLayerName = IsPlayerField() ? "BattleMaskMe" : "BattleMaskOpp";

            curFrame = curFrame.Up();
        }
    }
    private IEnumerator CheckNextProducts()
    {
        while (true)
        {
            yield return null;

            int comborableCounter = 0;
            for (int x = 0; x < CountX; ++x)
            {
                if (!mDestroyes.ContainsKey(x))
                    continue;

                Frame[] baseFrames = NextBaseFrames(mDestroyes[x]);
                if (baseFrames.Length <= 0)
                    continue;

                comborableCounter += baseFrames.Length;
                foreach (Frame baseFrame in baseFrames)
                    StartNextProducts(baseFrame);

                mDestroyes.Remove(x);
            }

            if (comborableCounter > 0)
            {
                mIdleCounter = comborableCounter;
            }
        }
    }
    private Frame[] NextBaseFrames(Frame baseFrame)
    {
        List<Frame> bases = new List<Frame>();
        int idxX = baseFrame.IndexX;
        int curIdxY = baseFrame.IndexY;
        bool pushed = false;
        while (curIdxY < CountY)
        {
            Frame frame = mFrames[idxX, curIdxY];
            if (frame.Empty)
                pushed = false;
            else if (!pushed && frame.ChildProduct == null)
            {
                bases.Add(frame);
                pushed = true;
            }
            curIdxY++;
        }
        return bases.ToArray();
    }
    private void SecondaryInitFrames()
    {
        for (int x = 0; x < CountX; ++x)
        {
            SpriteMask mask = null;
            int maskOrder = 0;
            for (int y = 0; y < CountY; ++y)
            {
                Frame curFrame = mFrames[x, y];
                if (curFrame.Empty)
                    continue;

                Frame subTopFrame = SubTopFrame(curFrame);
                if (curFrame.Down() == null)
                {
                    int height = subTopFrame.IndexY - curFrame.IndexY + 1;
                    Vector3 centerPos = (curFrame.transform.position + subTopFrame.transform.position) * 0.5f;
                    mask = CreateMask(centerPos, height, maskOrder);
                    maskOrder++;
                }
                curFrame.SetSubTopFrame(subTopFrame);
                curFrame.SetSpriteMask(mask);

                if (curFrame.Left() == null) curFrame.ShowBorder(0);
                if (curFrame.Right() == null) curFrame.ShowBorder(1);
                if (curFrame.Up() == null) curFrame.ShowBorder(2);
                if (curFrame.Down() == null) curFrame.ShowBorder(3);
            }
        }
    }
    private Frame SubTopFrame(Frame baseFrame)
    {
        Frame curFrame = baseFrame;
        while (true)
        {
            if (curFrame.Up() == null)
                break;
            else
                curFrame = curFrame.Up();
        }
        return curFrame;
    }
    private SpriteMask CreateMask(Vector3 pos, float height, int layerOrder)
    {
        GameObject maskObj = Instantiate(MaskPrefab, transform);
        maskObj.transform.position = pos;
        maskObj.transform.localScale = new Vector3(1, height, 1);

        SpriteMask mask = maskObj.GetComponent<SpriteMask>();
        mask.isCustomRangeActive = true;
        mask.frontSortingOrder = layerOrder + 1;
        mask.backSortingOrder = layerOrder;
        mask.frontSortingLayerID = IsPlayerField() ? SortingLayer.NameToID("BattleMaskMe") : SortingLayer.NameToID("BattleMaskOpp");
        mask.backSortingLayerID = IsPlayerField() ? SortingLayer.NameToID("BattleMaskMe") : SortingLayer.NameToID("BattleMaskOpp");
        return mask;
    }
    private Product CreateNewProduct(Frame parent, ProductColor color,ProductSkill skill = ProductSkill.Nothing)
    {
        int typeIdx = (int)color - 1;
        GameObject obj = GameObject.Instantiate(ProductPrefabs[typeIdx], parent.transform, false);
        Product product = obj.GetComponent<Product>();
        product.transform.localPosition = new Vector3(0, 0, -1);
        product.ParentFrame = parent;
        product.EventMatched = OnMatch;
        product.EventDestroyed = OnDestroyProduct;
        product.ChangeSkilledProduct(skill);
        if (!IsPlayerField())
            obj.GetComponent<BoxCollider2D>().enabled = false;
        return product;
    }
    public bool IsAllIdle()
    {
        foreach (Frame frame in mFrames)
        {
            if (frame.Empty)
                continue;
            if (frame.ChildProduct == null || frame.ChildProduct.IsLocked())
                return false;
        }
        return true;
    }
    private IEnumerator CheckIdle()
    {
        while (true)
        {
            if (mIdleCounter == 0)
            {
                if (mAtleastOneMatched)
                {
                    mAtleastOneMatched = false;
                    mIdleCounter = 999;
                }
                else
                {
                    mIdleCounter = -1; //set Idle enable
                    mCurrentCombo = 0;
                    if (IsPlayerField())
                        MenuBattle.Inst().CurrentCombo = 0;
                }
            }
            yield return null;
        }
    }
    private IEnumerator FlushChocos()
    {
        while (true)
        {
            yield return null;
            if (AttackPoints.Count <= 0 || !AttackPoints.IsReady || !IsIdle)
                continue;

            int cnt = AttackPoints.Pop(20);
            List<Product> products = GetNextTargetProducts(cnt);
            RequestSendChoco(products);
            foreach (Product pro in products)
                pro.SetChocoBlock(1, true);
    
            if (CheckState() == InGameState.Lose)
                FinishGame(false);
        }
    }
    private List<Product> GetNextTargetProducts(int cnt)
    {
        float xCenter = (mCountX - 1.0f) * 0.5f;
        float yCenter = (mCountY - 1.0f) * 0.5f;
        List<Product> products = new List<Product>();
        for (int y = 0; y < mCountY; ++y)
        {
            for (int x = 0; x < mCountX; ++x)
            {
                if (mFrames[x, y].ChildProduct.IsChocoBlock())
                    continue;

                Product pro = mFrames[x, y].ChildProduct;
                float distX = Math.Abs(xCenter - x);
                float distY = Math.Abs(yCenter - y);
                float max = Math.Max(distX, distY);
                float weight = max + UnityEngine.Random.Range(-0.4f, 0.4f);
                pro.Weight = weight;
                products.Add(pro);
            }
        }

        products.Sort((lsb, msb) =>
        {
            return lsb.Weight - msb.Weight > 0 ? -1 : 1;
        });
        return (products.Count < cnt) ? products : products.GetRange(0, cnt);
    }
    public InGameState CheckState()
    {
        Dictionary<ProductColor, int> colorCount = new Dictionary<ProductColor, int>();
        foreach (Frame frame in mFrames)
        {
            Product pro = frame.ChildProduct;
            if (pro == null || pro.IsChocoBlock())
                continue;

            if (colorCount.ContainsKey(pro.mColor))
                colorCount[pro.mColor] += 1;
            else
                colorCount[pro.mColor] = 1;

            if (colorCount[pro.mColor] >= MatchCount)
                return InGameState.Running;
        }

        return InGameState.Lose;
    }


    private void SendControlInfo(int idxX, int idxY, SwipeDirection dir, bool isClick)
    {
        if (!IsPlayerField())
            return;

        SwipeInfo info = new SwipeInfo();
        info.idxX = idxX;
        info.idxY = idxY;
        info.isClick = isClick;
        info.fromUserPk = mThisUserPK;
        info.toUserPk = Opponent.UserPK;
        info.dir = dir;
        NetClientApp.GetInstance().Request(NetCMD.SendSwipe, info, null);
    }
    private void ResponseFromOpponent(Header responseMsg)
    {
        if (!gameObject.activeInHierarchy)
            return;
        if (responseMsg.Ack == 1)
            return;


        if (responseMsg.Cmd == NetCMD.EndGame)
        {
            mNetMessages.AddFirst(responseMsg);
        }
        else
        {
            mNetMessages.AddLast(responseMsg);
        }
    }
    private IEnumerator CheckNetMessage()
    {
        while (true)
        {
            yield return null;

            if (mNetMessages.Count > 0 && IsIdle)
            {
                Header msg = mNetMessages.First.Value;
                if (msg.Cmd == NetCMD.EndGame)
                {
                    EndGame res = msg.body as EndGame;
                    FinishGame(!res.win);
                }
                else if (msg.Cmd == NetCMD.SendSwipe)
                {
                    SwipeInfo res = msg.body as SwipeInfo;
                    Product pro = mFrames[res.idxX, res.idxY].ChildProduct;
                    if (res.isClick)
                        OnClick(pro.gameObject);
                    else
                        OnSwipe(pro.gameObject, res.dir);

                    mNetMessages.RemoveFirst();
                }
                else if (msg.Cmd == NetCMD.SendChoco)
                {
                    ChocoInfo res = msg.body as ChocoInfo;
                    AttackPoints.Pop(res.xIndicies.Length);
                    for (int i = 0; i < res.xIndicies.Length; ++i)
                    {
                        int idxX = res.xIndicies[i];
                        int idxY = res.yIndicies[i];
                        Product pro = GetFrame(idxX, idxY).ChildProduct;
                        pro.SetChocoBlock(1, true);
                    }

                    mNetMessages.RemoveFirst();
                }
            }
        }
    }
    public bool IsPlayerField()
    {
        return UserSetting.UserPK == mThisUserPK;
    }
    private void ResetGame()
    {
        int cnt = transform.childCount;
        for (int i = 0; i < cnt; ++i)
            Destroy(transform.GetChild(i).gameObject);

        mDestroyes.Clear();
        mNextColors.Clear();
        mNextSkills.Clear();
        mNetMessages.Clear();

        mFrames = null;
        mNextPositionIndex = 0;
        MatchLock = false;
        mThisUserPK = 0;
        mCountX = 0;
        mCountY = 0;
        mIdleCounter = -1;
        mAtleastOneMatched = false;
        mCurrentCombo = 0;
        mKeepCombo = 0;
    }

    private Product NextUpProductFrom(Frame frame)
    {
        Frame curFrame = frame;
        while (curFrame != null)
        {
            if (curFrame.ChildProduct != null)
                return curFrame.ChildProduct;

            curFrame = curFrame.Up();
        }
        return null;
    }
    public Frame GetFrame(int x, int y)
    {
        if (x < 0 || x >= mCountX || y < 0 || y >= mCountY)
            return null;
        return mFrames[x, y];
    }
    private void Attack(int score, Vector3 fromPos)
    {
        int point = score / attackScore;
        if (point <= 0)
            return;

        int remainPt = AttackPoints.Count;
        if (remainPt <= 0)
        {
            Opponent.AttackPoints.Add(point, fromPos);
        }
        else
        {
            AttackPoints.Add(-point, fromPos);
        }
            
    }
    private ProductColor GetNextColor()
    {
        int remainCount = mNextColors.Count - mNextPositionIndex;
        if (remainCount < NextRequestCount / 3)
            RequestNextColors(NextRequestCount);

        ProductColor next = mNextColors[mNextPositionIndex];
        mNextPositionIndex++;
        return next;
    }
    private void RequestNextColors(int count)
    {
        NextProducts info = new NextProducts();
        info.userPk = mThisUserPK;
        info.offset = mNextColors.Count;
        info.requestCount = count;
        NetClientApp.GetInstance().Request(NetCMD.NextProducts, info, (_res)=>
        {
            NextProducts res = _res as NextProducts;
            mNextColors.AddRange(res.nextProducts);
        });
    }
    private void RequestSendChoco(List<Product> chocos)
    {
        List<int> xIndicies = new List<int>();
        List<int> yIndicies = new List<int>();
        foreach (Product pro in chocos)
        {
            xIndicies.Add(pro.ParentFrame.IndexX);
            yIndicies.Add(pro.ParentFrame.IndexY);
        }

        ChocoInfo info = new ChocoInfo();
        info.toUserPk = Opponent.UserPK;
        info.fromUserPk = mThisUserPK;
        info.xIndicies = xIndicies.ToArray();
        info.yIndicies = yIndicies.ToArray();
        NetClientApp.GetInstance().Request(NetCMD.SendChoco, info, null);
    }
    public string SummaryToCSVString(bool success, int deltaScore)
    {
        //pvp, XCount, YCount, UserPk, score, colorCount, OppUserPk, Success, deltaScore, heartCount
        string ret = 
            "PVP Mode,"
            + CountX + ","
            + CountY + ","
            + Me.UserPK + ","
            + UserSetting.UserScore + ","
            + mColorCount + ","
            + Opponent.UserPK + ","
            + success + ","
            + deltaScore + ","
            + Purchases.CountHeart();

        return ret;
    }
}
