using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuWaitMatch : MonoBehaviour
{
    private const string UIObjName = "CanvasPopUp/MenuWaitMatch";
    private bool mIsSearching = false;

    public Text State;
    public GameObject BtnMatch;
    public GameObject BtnCancle;

    public static void PopUp()
    {
        GameObject menuMatch = GameObject.Find("UIGroup").transform.Find(UIObjName).gameObject;
        menuMatch.GetComponent<MenuWaitMatch>().ResetMatchUI();
        menuMatch.SetActive(true);
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton2);
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
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
    }

    public void OnCancle()
    {
        mIsSearching = false;
        StopCoroutine("WaitOpponent");
        State.text = "Match Ready";
        BtnCancle.SetActive(false);
        BtnMatch.SetActive(true);
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);

        SearchOpponentInfo info = new SearchOpponentInfo();
        info.userPk = UserSetting.UserPK;
        NetClientApp.GetInstance().Request(NetCMD.StopMatching, info, null);
    }

    public void OnMatch()
    {
        if(NetClientApp.GetInstance().IsDisconnected())
        {
            MenuMessageBox.PopUp(gameObject, "Network Disconnected", false, null);
            return;
        }

        UserInfo userInfo = UserSetting.LoadUserInfo();
        if (userInfo.userPk <= 0)
            StartCoroutine(WaitForAddingUserInfo());
        else
            RequestMatch();

        mIsSearching = true;
        BtnCancle.SetActive(true);
        BtnMatch.SetActive(false);
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);

        StartCoroutine("WaitOpponent");
    }

    IEnumerator WaitOpponent()
    {
        int n = 0;
        while(true)
        {
            switch(n%3)
            {
                case 0: State.text = "Matching.."; break;
                case 1: State.text = "Matching..."; break;
                case 2: State.text = "Matching...."; break;
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
        info.userScore = UserSetting.UserScore;
        info.opponentUserPk = -1;
        info.opponentUserScore = -1;
        info.isDone = false;
        NetClientApp.GetInstance().Request(NetCMD.SearchOpponent, info, (_res) =>
        {
            SearchOpponentInfo res = _res as SearchOpponentInfo;
            if (res.isDone && mIsSearching)
            {
                if (res.opponentUserPk == -1)
                    FailMatch();
                else
                    SuccessMatch(res.opponentUserPk);
            }
            return;
        });
    }

    private void SuccessMatch(int oppPk)
    {
        mIsSearching = false;
        StopCoroutine("WaitOpponent");
        State.text = "Matched Player : " + oppPk;

        InitFieldInfo info = new InitFieldInfo();
        info.XCount = 7;
        info.YCount = 7;

        info.userPk = UserSetting.UserPK;
        NetClientApp.GetInstance().Request(NetCMD.GetInitField, info, (_res) =>
        {
            InitFieldInfo res = _res as InitFieldInfo;
            BattleFieldManager.Me.StartGame(res.userPk, res.XCount, res.YCount, res.products);
        });

        info.userPk = oppPk;
        NetClientApp.GetInstance().Request(NetCMD.GetInitField, info, (_res) =>
        {
            InitFieldInfo res = _res as InitFieldInfo;
            BattleFieldManager.Opp.StartGame(res.userPk, res.XCount, res.YCount, res.products);
        });

        MenuStages.Hide();
        StageManager.Inst.Activate(false);
        gameObject.SetActive(false);
    }
    private void FailMatch()
    {
        mIsSearching = false;
        StopCoroutine("WaitOpponent");
        State.text = "Match Failed";
        BtnCancle.SetActive(false);
        BtnMatch.SetActive(true);
    }
    private void ResetMatchUI()
    {
        mIsSearching = false;
        BtnCancle.SetActive(false);
        BtnMatch.SetActive(true);
        State.text = "Match Ready";
        StopCoroutine("WaitOpponent");

    }
}
