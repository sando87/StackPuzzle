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
    public const int attackScore = 1;
    public const float GridSize = 0.8f;
    public const int NextRequestCount = 500;

    public GameObject[] ProductPrefabs;
    public GameObject FramePrefab1;
    public GameObject FramePrefab2;
    public GameObject MaskPrefab;
    public GameObject AttackPointPrefab;
    public BattleFieldManager Opponent;

    private Frame[,] mFrames = null;
    private Dictionary<int, List<Frame>> mDestroyes = new Dictionary<int, List<Frame>>();
    private List<ProductColor> mNextColors = new List<ProductColor>();
    private int mNextPositionIndex = 0;
    private int mThisUserPK = 0;
    private int mKeepCombo = 0;
    private int mCountX = 0;
    private int mCountY = 0;

    public int CountX { get { return mCountX; } }
    public int CountY { get { return mCountY; } }
    public Frame[,] Frames { get { return mFrames; } }
    public AttackPoints AttackPoints { get; set; }
    public bool MatchLock { get; set; }
    public int UserPK { get { return mThisUserPK; } }
    public Action<Product> EventOnChange;

    public void StartGame(int userPK, int XCount, int YCount, ProductColor[,] initColors)
    {
        ResetGame();
    
        mThisUserPK = userPK;
        mCountX = XCount;
        mCountY = YCount;

        transform.parent.gameObject.SetActive(true);
        SoundPlayer.Inst.PlayBackMusic(SoundPlayer.Inst.BackMusicInGame);
    
        GameObject mask = Instantiate(MaskPrefab, transform);
        mask.transform.localScale = new Vector3(XCount * 0.97f, YCount * 0.97f, 1);

        if(IsPlayerField())
            GetComponent<SwipeDetector>().EventSwipe = OnSwipe;
        else
            NetClientApp.GetInstance().EventResponse = ResponseFromOpponent;

        RequestNextColors(NextRequestCount);

        StartCoroutine(CheckNextProducts());

        if (IsPlayerField())
        {
            StartCoroutine("CheckIdle");
            StartCoroutine(CheckFinish());
        }

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

        GameObject ap = Instantiate(AttackPointPrefab, transform);
        ap.transform.localPosition = localBasePos + new Vector3(0, GridSize * YCount, 0);
        AttackPoints = ap.GetComponent<AttackPoints>();

        MenuBattle.PopUp();
    }
    public static void FinishGame(bool success)
    {
        NetClientApp.GetInstance().EventResponse = null;

        int deltaScore = success ? 1 : -1;
        UserSetting.UserScore += deltaScore;

        EndGame info = new EndGame();
        info.fromUserPk = BattleFieldManager.Me.UserPK;
        info.toUserPk = BattleFieldManager.Opp.UserPK;
        info.win = success;
        info.score = UserSetting.UserScore;
        NetClientApp.GetInstance().Request(NetCMD.EndGame, info, null);

        BattleFieldManager.Me.ResetGame();
        BattleFieldManager.Opp.ResetGame();

        BattleFieldManager.Me.transform.parent.gameObject.SetActive(false);

        MenuFinishBattle.PopUp(success, UserSetting.UserScore, deltaScore);
    }

    public void OnSwipe(GameObject obj, SwipeDirection dir)
    {
        Product product = obj.GetComponent<Product>();
        Product targetProduct = null;
        switch (dir)
        {
            case SwipeDirection.UP: targetProduct = product.Up(); break;
            case SwipeDirection.DOWN: targetProduct = product.Down(); break;
            case SwipeDirection.LEFT: targetProduct = product.Left(); break;
            case SwipeDirection.RIGHT: targetProduct = product.Right(); break;
        }

        if (targetProduct != null && !product.IsLocked() && !targetProduct.IsLocked() && !product.IsChocoBlock() && !targetProduct.IsChocoBlock())
        {
            SendSwipeInfo(product.ParentFrame.IndexX, product.ParentFrame.IndexY, dir);
            product.StartSwipe(targetProduct.GetComponentInParent<Frame>(), mKeepCombo);
            targetProduct.StartSwipe(product.GetComponentInParent<Frame>(), mKeepCombo);
            mKeepCombo = 0;
        }
    }
    private void OnMatch(List<Product> matches)
    {
        if (MatchLock)
            return;

        StopCoroutine("CheckIdle");
        StartCoroutine("CheckIdle");

        Product mainProduct = matches[0];
        mainProduct.BackupSkillToFrame(matches.Count, true);

        List<Product> allSameColors = ApplySkillEffects(matches);

        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectMatched);

        List<Product> destroies = allSameColors.Count > 0 ? allSameColors : matches;
        int nextCombo = mainProduct.Combo + 1;
        foreach (Product pro in destroies)
        {
            pro.Combo = nextCombo;
            pro.StartDestroy();
            EventOnChange?.Invoke(pro);
        }

        Attack(destroies.Count * nextCombo, mainProduct.transform.position);
        mainProduct.StartFlash(matches);
    }
    private void OnDestroyProduct(Product pro)
    {
        int idxX = pro.ParentFrame.IndexX;
        if (!mDestroyes.ContainsKey(idxX))
            mDestroyes[idxX] = new List<Frame>();
        mDestroyes[idxX].Add(pro.ParentFrame);
    }

    private IEnumerator CheckNextProducts()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.1f);

            foreach (var vert in mDestroyes)
            {
                int idxX = vert.Key;
                List<Frame> vertFrames = vert.Value;
                vertFrames.Sort((a, b) => { return a.IndexY - b.IndexY; });

                Queue<ProductSkill> nextSkills = new Queue<ProductSkill>();

                Frame curFrame = vertFrames[0];
                Frame validFrame = curFrame;
                int emptyCount = 0;
                while (curFrame != null)
                {
                    Product pro = NextUpProductFrom(validFrame);
                    if (pro == null)
                    {
                        validFrame = null;
                        if (emptyCount == 0)
                            emptyCount = mCountY - curFrame.IndexY;
                        pro = CreateNewProduct(curFrame, GetNextColor(), nextSkills.Count > 0 ? nextSkills.Dequeue() : ProductSkill.Nothing);
                        pro.StartDropAnimate(curFrame, emptyCount, curFrame == vertFrames[0]);
                    }
                    else
                    {
                        validFrame = pro.ParentFrame;
                        pro.StartDropAnimate(curFrame, pro.ParentFrame.IndexY - curFrame.IndexY, curFrame == vertFrames[0]);
                        if (curFrame.SkillBackupSpace != ProductSkill.Nothing)
                        {
                            nextSkills.Enqueue(curFrame.SkillBackupSpace);
                            curFrame.SkillBackupSpace = ProductSkill.Nothing;
                        }
                    }

                    curFrame = curFrame.Up();
                }
            }

            mDestroyes.Clear();
        }
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
    private List<Product> ApplySkillEffects(List<Product> matches)
    {
        int skillComboCount = 0;
        bool keepCombo = false;
        List<Product> allSameColors = new List<Product>();
        foreach (Product pro in matches)
        {
            if (pro.mSkill == ProductSkill.MatchOneMore)
                skillComboCount++;
            else if (pro.mSkill == ProductSkill.KeepCombo)
                keepCombo = true;
            else if (pro.mSkill == ProductSkill.BreakSameColor && allSameColors.Count == 0)
            {
                foreach (Frame frame in mFrames)
                    if (frame.ChildProduct != null && frame.ChildProduct.mColor == matches[0].mColor)
                        allSameColors.Add(frame.ChildProduct);
            }
        }

        if (skillComboCount > 0)
            foreach (Product pro in matches)
                pro.Combo += skillComboCount;

        if (keepCombo)
            mKeepCombo = Math.Max(mKeepCombo, matches[0].Combo);

        return allSameColors;
    }
    public bool IsIdle()
    {
        int count = 0;
        foreach (Frame frame in mFrames)
            if (frame.ChildProduct != null && !frame.ChildProduct.IsLocked())
                count++;
        return mFrames.Length == count;
    }
    private void FlushAttackPoints()
    {
        if (AttackPoints.Count == 0)
            return;

        if (!AttackPoints.IsReady)
            return;

        int cnt = AttackPoints.Pop(20);
        List<Product> products = GetNextTargetProducts(cnt);
        RequestSendChoco(products);
        foreach (Product pro in products)
            pro.WrapChocoBlock(true);
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
    private IEnumerator CheckIdle()
    {
        while (true)
        {
            yield return new WaitForSeconds(UserSetting.MatchInterval);
            int cnt = 0;
            while (true)
            {
                if (IsIdle())
                    cnt++;
                else
                    cnt = 0;

                if(cnt >= 3)
                {
                    FlushAttackPoints();
                    break;
                }
                yield return null;
            }
        }


    }
    private IEnumerator CheckFinish()
    {
        while (true)
        {
            yield return new WaitForSeconds(1);

            bool isFinished = true;
            Dictionary<ProductColor, int> colorCount = new Dictionary<ProductColor, int>();
            foreach(Frame frame in mFrames)
            {
                Product pro = frame.ChildProduct;
                if (pro == null || pro.IsChocoBlock())
                    continue;

                if (colorCount.ContainsKey(pro.mColor))
                    colorCount[pro.mColor] += 1;
                else
                    colorCount[pro.mColor] = 1;

                if(colorCount[pro.mColor] >= MatchCount)
                {
                    isFinished = false;
                    break;
                }
            }

            if (isFinished)
                break;
        }

        FinishGame(false);
    }


    private void SendSwipeInfo(int idxX, int idxY, SwipeDirection dir)
    {
        if (!IsPlayerField())
            return;

        SwipeInfo info = new SwipeInfo();
        info.idxX = idxX;
        info.idxY = idxY;
        info.matchLock = MatchLock;
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

        if (responseMsg.Cmd == NetCMD.SendSwipe)
        {
            SwipeInfo res = responseMsg.body as SwipeInfo;
            //if (res.fromUserPk == mThisUserPK)
            {
                MatchLock = res.matchLock;
                Product pro = mFrames[res.idxX, res.idxY].ChildProduct;
                OnSwipe(pro.gameObject, res.dir);
            }
        }
        else if(responseMsg.Cmd == NetCMD.EndGame)
        {
            EndGame res = responseMsg.body as EndGame;
            //if (res.fromUserPk == mThisUserPK)
            {
                FinishGame(true);
            }

        }
        else if (responseMsg.Cmd == NetCMD.SendChoco)
        {
            ChocoInfo res = responseMsg.body as ChocoInfo;
            //if (res.fromUserPk == mThisUserPK)
            {
                AttackPoints.Pop(res.xIndicies.Length);
                for(int i = 0; i < res.xIndicies.Length; ++i)
                {
                    int idxX = res.xIndicies[i];
                    int idxY = res.yIndicies[i];
                    Product pro = GetFrame(idxX, idxY).ChildProduct;
                    pro.WrapChocoBlock(true);
                }
            }
        }

    }
    private bool IsPlayerField()
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

        mFrames = null;
        mNextPositionIndex = 0;
        MatchLock = false;
        mThisUserPK = 0;
        mKeepCombo = 0;
        mCountX = 0;
        mCountY = 0;
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
        if (remainPt == 0)
            Opponent.AttackPoints.Add(point, fromPos);
        else
        {
            if (remainPt >= point)
            {
                AttackPoints.Add(-point, fromPos);
            }
            else
            {
                AttackPoints.Add(-remainPt, fromPos);
                Opponent.AttackPoints.Add(point - remainPt, fromPos);
            }
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
}
