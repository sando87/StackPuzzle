using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuWaitMatch : MonoBehaviour
{
    private const string UIObjName = "CanvasPopUp/MenuWaitMatch";
    private bool mIsSearching = false;

    public Text HeartTimer;
    public Text HeartCount;
    public Text MyUserInfo;
    public Text OppUserInfo;
    public Text CountDown;
    public GameObject BtnClose;
    public GameObject BtnMatch;
    public GameObject BtnCancle;

    public static void PopUp(bool autoPlay = false)
    {
        GameObject menuMatch = GameObject.Find("UIGroup").transform.Find(UIObjName).gameObject;
        MenuWaitMatch menu = menuMatch.GetComponent<MenuWaitMatch>();
        menuMatch.SetActive(true);
        menu.ResetMatchUI();

        if (autoPlay)
            menu.StartCoroutine(menu.AutoMatch());
    }

    public void OnClose()
    {
        if(mIsSearching)
        {
            SearchOpponentInfo info = new SearchOpponentInfo();
            info.userPk = UserSetting.UserPK;
            NetClientApp.GetInstance().Request(NetCMD.StopMatching, info, null);
        }

        mIsSearching = false;
        gameObject.SetActive(false);
        MenuMain.PopUp();
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton2);
    }

    public void OnCancle()
    {
        ResetMatchUI();
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton2);

        SearchOpponentInfo info = new SearchOpponentInfo();
        info.userPk = UserSetting.UserPK;
        NetClientApp.GetInstance().Request(NetCMD.StopMatching, info, null);
    }

    public void OnMatch()
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);

        if (NetClientApp.GetInstance().IsDisconnected())
        {
            MenuMessageBox.PopUp("Network Disconnected", false, null);
            return;
        }

#if (UNITY_ANDROID || UNITY_IPHONE) && !UNITY_EDITOR
        //if (UserSetting.StageIsLocked(16))
        //{
        //    MenuMessageBox.PopUp("Required\n15 Stages", false, null);
        //    return;
        //}
        //
        //if (Purchases.CountHeart() <= 0)
        //{
        //    MenuMessageBox.PopUp("No Life", false, null);
        //    return;
        //}
#endif

        UserInfo userInfo = UserSetting.LoadUserInfo();
        if (userInfo.userPk <= 0)
            StartCoroutine(WaitForAddingUserInfo());
        else
            RequestMatch();

        mIsSearching = true;
        BtnCancle.SetActive(true);
        BtnMatch.SetActive(false);

        StartCoroutine("WaitOpponent");
    }

    IEnumerator WaitOpponent()
    {
        int n = 0;
        while(true)
        {
            switch(n%3)
            {
                case 0: OppUserInfo.text = "Matching.."; break;
                case 1: OppUserInfo.text = "Matching..."; break;
                case 2: OppUserInfo.text = "Matching...."; break;
            }
            n++;
            yield return new WaitForSeconds(1);
        }
    }

    IEnumerator WaitForAddingUserInfo()
    {
        int limit = 60;
        while(0 < limit--)
        {
            yield return new WaitForSeconds(1);
            if (UserSetting.UserPK > 0)
            {
                RequestMatch();
                break;
            }
        }
    }

    private void RequestMatch()
    {
        SearchOpponentInfo info = new SearchOpponentInfo();
        info.userPk = UserSetting.UserPK;
        info.colorCount = 4.2f; // 4~6.0f
        info.oppUser = null;
        info.isBotPlayer = UserSetting.UserInfo.deviceName.Contains("home") ? true : false;
        info.isDone = false;
        NetClientApp.GetInstance().Request(NetCMD.SearchOpponent, info, (_body) =>
        {
            SearchOpponentInfo res = Utils.Deserialize<SearchOpponentInfo>(ref _body);
            if (res.isDone && mIsSearching)
            {
                if (res.oppUser.userPk == -1)
                {
                    FailMatch();

                    if (UserSetting.IsBotPlayer)
                        StartCoroutine(AutoMatch());
                }
                else
                    StartCoroutine(StartCountDown(res));
            }
            return;
        });
    }

    IEnumerator StartCountDown(SearchOpponentInfo matchInfo)
    {
        if (!UserSetting.IsBotPlayer)
            Purchases.UseHeart();

        StageInfo info = StageInfo.Load(0);

        InGameManager.InstPVP_Opponent.StartGame(info, matchInfo.oppUser);
        InGameManager.InstPVP_Player.StartGame(info, UserSetting.UserInfo);

        mIsSearching = false;
        StopCoroutine("WaitOpponent");
        UpdateUserInfo(matchInfo.oppUser);
        BtnClose.SetActive(false);
        BtnMatch.SetActive(false);
        BtnCancle.SetActive(false);

        CountDown.gameObject.SetActive(true);
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectCountDown);
        CountDown.text = "3";
        yield return new WaitForSeconds(1);
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectCountDown);
        CountDown.text = "2";
        yield return new WaitForSeconds(1);
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectCountDown);
        CountDown.text = "1";
        yield return new WaitForSeconds(1);

        InGameManager.InstPVP_Player.InitProducts();

        gameObject.SetActive(false);
        MenuBattle.PopUp();
    }
    private void FailMatch()
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectAlarm);
        ResetMatchUI();
    }
    private IEnumerator AutoMatch()
    {
        yield return new WaitForSeconds(5);
        OnMatch();
    }
    private void ResetMatchUI()
    {
        mIsSearching = false;
        BtnCancle.SetActive(false);
        BtnMatch.SetActive(true);
        BtnClose.SetActive(true);
        UpdateUserInfo(UserSetting.UserInfo);
        OppUserInfo.text = "No Matched User";
        CountDown.text = "0";
        CountDown.gameObject.SetActive(false);
        StopCoroutine("WaitOpponent");
        StopCoroutine("UpdateHeartTimer");
        StartCoroutine("UpdateHeartTimer");
    }
    private void UpdateUserInfo(UserInfo info)
    {
        string text =
            "ID : #" + info.userPk + "\n" +
            "Name : " + info.userName + "\n" +
            "Score : " + info.score + "\n" +
            "Win/Lose : " + info.win + "/" + info.lose;

        if (UserSetting.UserPK == info.userPk)
            MyUserInfo.text = text;
        else
            OppUserInfo.text = text;
    }
    IEnumerator UpdateHeartTimer()
    {
        while (true)
        {
            Purchases.UpdateHeartTimer();
            int remainSec = Purchases.RemainSeconds();
            int remainLife = Purchases.CountHeart();
            HeartCount.text = remainLife.ToString();
            if (Purchases.MaxHeart())
            {
                HeartTimer.text = "Full";
            }
            else
            {
                int min = remainSec / 60;
                int sec = remainSec % 60;
                string secStr = string.Format("{0:D2}", sec);
                HeartTimer.text = min + ":" + secStr;
            }
            yield return new WaitForSeconds(1);
        }
    }
}
