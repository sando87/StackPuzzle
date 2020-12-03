using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuBattle : MonoBehaviour
{
    private static MenuBattle mInst = null;
    private const string UIObjName = "MenuBattle";
    private const int mScorePerBar = 300;

    public Image ScoreBar1;
    public Image ScoreBar2;
    public Text CurrentScore;
    public Text KeepCombo;
    public Image MatchLock;
    public Image MatchUnLock;
    public GameObject EffectParent;
    public GameObject ItemPrefab;
    public GameObject ScoreStarPrefab;
    public NumbersUI ComboNumber;

    private MenuMessageBox mMenu;
    private int mAddedScore;
    private int mCurrentScore;
    private List<GameObject> mScoreStars = new List<GameObject>();


    private void Update()
    {
#if PLATFORM_ANDROID
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnClose();
        }
#endif
        UpdateScore();

    }

    public static MenuBattle Inst()
    {
        if (mInst == null)
            mInst = GameObject.Find("UIGroup").transform.Find(UIObjName).gameObject.GetComponent<MenuBattle>();
        return mInst;
    }

    public static void PopUp()
    {
        Inst().gameObject.SetActive(true);
        Inst().Init();
    }
    public static void Hide()
    {
        Inst().gameObject.SetActive(false);
    }

#if (UNITY_ANDROID || UNITY_IPHONE) && !UNITY_EDITOR
    private void OnApplicationPause(bool pause)
    {
        //FinishGame(false);
    }

    private void OnApplicationFocus(bool focus)
    {
        //FinishGame(false);
    }

    private void OnApplicationQuit()
    {
        //FinishGame(false);
    }
#endif

    private void Init()
    {
        for (int i = 0; i < EffectParent.transform.childCount; ++i)
            Destroy(EffectParent.transform.GetChild(i).gameObject);

        mScoreStars.Clear();

        mMenu = null;
        mAddedScore = 0;
        mCurrentScore = 0;
        CurrentScore.text = "0";
        KeepCombo.text = "0";
        //Lock(false);
        ScoreBar1.fillAmount = 0;
        ScoreBar2.gameObject.SetActive(false);
        ComboNumber.Clear();

        InGameManager.InstPVP_Player.EventDestroyed = (products) => {
            mAddedScore += products[0].Combo * products.Length;
        };
        InGameManager.InstPVP_Player.EventFinish = (success) => {
            FinishGame(success);
        };
        InGameManager.InstPVP_Opponent.EventFinish = (success) => {
            FinishGame(!success);
        };
    }


    public void AddScore(Product product)
    {
        mAddedScore += product.Combo;
    }
    public int CurrentCombo
    {
        get { return ComboNumber.GetNumber(); }
        set
        {
            if (value == 0)
                ComboNumber.BreakCombo();
            else
                ComboNumber.SetNumber(value);
        }
    }
    public int NextCombo
    {
        get { return int.Parse(KeepCombo.text); }
        set
        {
            int pre = int.Parse(KeepCombo.text);
            if (value > pre)
            {
                KeepCombo.text = value.ToString();
                KeepCombo.GetComponent<Animation>().Play("touch");
            }
            else if (value == 0)
            {
                KeepCombo.text = "0";
                KeepCombo.GetComponent<Animation>().Play("touch");
            }
        }
    }
    public void KeepNextCombo(Product product)
    {
        if (product.mSkill != ProductSkill.KeepCombo)
            return;

        int nextCombo = product.Combo;
        Vector3 pos = product.transform.position;
        pos.z = 0;
        GameObject obj = GameObject.Instantiate(ItemPrefab, pos, Quaternion.identity, EffectParent.transform);
        Image img = obj.GetComponent<Image>();
        img.sprite = product.Renderer.sprite;
        StartCoroutine(UnityUtils.AnimateConvex(obj, KeepCombo.transform.position, 1.0f, () =>
        {
            NextCombo = nextCombo;
            Destroy(obj);
        }));
    }
    public void OneMoreCombo(Product product)
    {
        if (product.mSkill != ProductSkill.OneMore)
            return;

        Vector3 pos = product.transform.position;
        pos.z = 0;
        GameObject obj = GameObject.Instantiate(ItemPrefab, pos, Quaternion.identity, EffectParent.transform);
        Image img = obj.GetComponent<Image>();
        img.sprite = product.Renderer.sprite;
        StartCoroutine(UnityUtils.AnimateConvex(obj, ComboNumber.transform.position, 1.0f, () =>
        {
            CurrentCombo++;
            Destroy(obj);
        }));
    }
    private void UpdateScore()
    {
        if (mAddedScore <= 0)
            return;

        if (mAddedScore < 30)
        {
            mCurrentScore += mAddedScore;
            mAddedScore = 0;
            int n = mCurrentScore % mScorePerBar;
            ScoreBar1.fillAmount = n / (float)mScorePerBar;
            ScoreBar2.gameObject.SetActive(false);
            CurrentScore.text = mCurrentScore.ToString();
            CurrentScore.GetComponent<Animation>().Play("touch");
        }
        else if ((mCurrentScore + mAddedScore) / mScorePerBar > mCurrentScore / mScorePerBar)
        {
            mCurrentScore += mAddedScore;
            mAddedScore = 0;

            int preScore = (mCurrentScore / mScorePerBar) * mScorePerBar;
            int addScore = mCurrentScore % mScorePerBar;
            StartCoroutine(ScoreBarEffect(preScore, addScore));

            CurrentScore.text = mCurrentScore.ToString();
            CurrentScore.GetComponent<Animation>().Play("touch");
        }
        else
        {
            StartCoroutine(ScoreBarEffect(mCurrentScore, mAddedScore));

            mCurrentScore += mAddedScore;
            mAddedScore = 0;

            CurrentScore.text = mCurrentScore.ToString();
            CurrentScore.GetComponent<Animation>().Play("touch");
        }

        FillScoreStar();
    }
    private void FillScoreStar()
    {
        int starCount = mCurrentScore / mScorePerBar;
        float pixelPerUnit = GetComponent<CanvasScaler>().referencePixelsPerUnit;
        float imgWidth = ScoreStarPrefab.GetComponent<Image>().sprite.rect.width / pixelPerUnit;
        float barWidth = ScoreBar1.GetComponent<RectTransform>().rect.width / pixelPerUnit;
        Vector3 basePos = ScoreBar1.transform.position + new Vector3((imgWidth - barWidth) * 0.5f, 0.3f, 0);
        while (mScoreStars.Count < starCount)
        {
            basePos = ScoreBar1.transform.position + new Vector3((imgWidth - barWidth) * 0.5f, 0.3f, 0);
            basePos.x += (imgWidth * mScoreStars.Count);
            GameObject obj = GameObject.Instantiate(ScoreStarPrefab, basePos, Quaternion.identity, EffectParent.transform);
            mScoreStars.Add(obj);
        }
    }
    private IEnumerator ScoreBarEffect(int prevScore, int addedScore)
    {
        int nextScore = prevScore + addedScore;
        float totalWidth = ScoreBar1.GetComponent<RectTransform>().rect.width;
        float fromRate = (prevScore % mScorePerBar) / (float)mScorePerBar;
        float toRate = (nextScore % mScorePerBar) / (float)mScorePerBar;
        float bar2Width = totalWidth * (toRate - fromRate) + 1;
        ScoreBar1.fillAmount = fromRate;
        ScoreBar2.gameObject.SetActive(true);
        RectTransform rt = ScoreBar2.GetComponent<RectTransform>();
        Vector2 pos = rt.anchoredPosition;
        Vector2 size = rt.sizeDelta;
        pos.x = totalWidth * toRate;
        size.x = bar2Width;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        float time = 0;
        float duration = 0.5f;
        float slope1 = (toRate - fromRate) / (duration * duration);
        float slope2 = -bar2Width / (duration * duration);
        while (time < duration)
        {
            size.x = slope2 * time * time + bar2Width;
            ScoreBar1.fillAmount = slope1 * time * time + fromRate;
            rt.sizeDelta = size;
            time += Time.deltaTime;
            yield return null;
        }

        ScoreBar1.fillAmount = toRate;
        ScoreBar2.gameObject.SetActive(false);

    }
    private void FinishGame(bool success)
    {
        int deltaScore = NextDeltaScore(success, UserSetting.UserScore, InGameManager.InstPVP_Player.ColorCount);
        UserSetting.UserScore += deltaScore;
        UserSetting.Win = success;

        PVPInfo req = new PVPInfo();
        req.cmd = PVPCommand.EndGame;
        req.oppUserPk = InGameManager.InstPVP_Opponent.UserPk;
        req.success = success;
        req.userInfo = UserSetting.UserInfo;
        NetClientApp.GetInstance().Request(NetCMD.PVP, req, null);

        if (success)
        {
            SoundPlayer.Inst.Player.Stop();
            SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectSuccess);
            MenuFinishBattle.PopUp(success, UserSetting.UserInfo, deltaScore);
        }
        else
        {
            SoundPlayer.Inst.Player.Stop();
            SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectGameOver);
            MenuFinishBattle.PopUp(success, UserSetting.UserInfo, deltaScore);
        }

        string log = InGameManager.InstPVP_Player.GetBillboard().ToCSVString();
        LOG.echo(log);

        InGameManager.InstPVP_Player.FinishGame();
        InGameManager.InstPVP_Opponent.FinishGame();
        Hide();
    }
    private int NextDeltaScore(bool isWin, int curScore, float colorCount)
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


    private void Lock(bool locked)
    {
        MatchLock.gameObject.SetActive(locked);
        MatchUnLock.gameObject.SetActive(!locked);
    }
    public void OnClose()
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton2);
        if (mMenu != null)
        {
            Destroy(mMenu);
            mMenu = null;
        }
        else
        {
            mMenu = MenuMessageBox.PopUp("Finish Game?", true, (bool isOK) =>
            {
                if (isOK)
                    FinishGame(false);
            });

        }
    }
    public void OnLockMatch(bool locked)
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
        Lock(locked);
    }
}
