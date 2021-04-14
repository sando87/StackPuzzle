using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuBattle : MonoBehaviour
{
    private static MenuBattle mInst = null;
    private const string UIObjName = "UISpace/CanvasPopup/PVP";

    public GameObject EffectParent;
    public NumbersUI ComboPlayer;
    public NumbersUI ComboOpponent;
    public ScoreBar PVPScoreBarPlayer;
    public ScoreBar PVPScoreBarOpponent;
    public TextMeshProUGUI PlayerName;
    public TextMeshProUGUI PlayerScore;
    public TextMeshProUGUI OpponentName;
    public TextMeshProUGUI OpponentScore;
    public GameObject PlayerRect;
    public GameObject OpponentRect;

    private MenuMessageBox mMenu;

    private void Update()
    {
#if PLATFORM_ANDROID
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnClose();
        }
#endif
    }

    public static MenuBattle Inst()
    {
        if (mInst == null)
            mInst = GameObject.Find(UIObjName).GetComponent<MenuBattle>();
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


        mMenu = null;
        ComboPlayer.Clear();
        ComboOpponent.Clear();
        PVPScoreBarPlayer.Clear();
        PVPScoreBarOpponent.Clear();
        PlayerName.text = InGameManager.InstPVP_Player.UserInfo.userName;
        OpponentName.text = InGameManager.InstPVP_Opponent.UserInfo.userName;
        PlayerScore.text = InGameManager.InstPVP_Player.UserInfo.score.ToString();
        OpponentScore.text = InGameManager.InstPVP_Opponent.UserInfo.score.ToString();

        InGameManager.InstPVP_Player.EventMatched = (products) => {
            PVPScoreBarPlayer.SetScore(PVPScoreBarPlayer.CurrentScore + products[0].Combo * products.Length);
        };
        InGameManager.InstPVP_Player.EventFinish = (success) => {
            FinishGame(success);
        };
        InGameManager.InstPVP_Player.EventCombo = (combo) => {
            if (combo <= 0)
                ComboPlayer.BreakCombo();
            else
                ComboPlayer.SetNumber(combo);
        };
        InGameManager.InstPVP_Opponent.EventMatched = (products) => {
            PVPScoreBarOpponent.SetScore(PVPScoreBarOpponent.CurrentScore + products[0].Combo * products.Length);
        };
        InGameManager.InstPVP_Opponent.EventFinish = (success) => {
            FinishGame(!success);
        };
        InGameManager.InstPVP_Opponent.EventCombo = (combo) => {
            if (combo <= 0)
                ComboOpponent.BreakCombo();
            else
                ComboOpponent.SetNumber(combo);
        };
    }
    private void FinishGame(bool success)
    {
        int prevScore = UserSetting.UserScore;

        PVPInfo req = new PVPInfo();
        req.cmd = PVPCommand.EndGame;
        req.oppUserPk = InGameManager.InstPVP_Opponent.UserPk;
        req.success = success;
        req.userInfo = UserSetting.UserInfo;
        bool ret = NetClientApp.GetInstance().Request(NetCMD.PVP, req, (_body) =>
        {
            PVPInfo resBody = Utils.Deserialize<PVPInfo>(ref _body);
            UserSetting.UpdateUserInfo(resBody.userInfo);
        });

        if(!ret)
            MenuInformBox.PopUp("Network Disconnected");

        if (success)
        {
            SoundPlayer.Inst.PlayerBack.Stop();
            SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectSuccess);
            MenuFinishBattle.PopUp(success, prevScore);
        }
        else
        {
            SoundPlayer.Inst.PlayerBack.Stop();
            SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectGameOver);
            MenuFinishBattle.PopUp(success, prevScore);
        }

        string log = "[PVP] " + (success ? "win" : "lose") + ", oppPK:" + InGameManager.InstPVP_Opponent.UserPk;
        LOG.echo(log);

        InGameManager.InstPVP_Player.CleanUpGame();
        InGameManager.InstPVP_Opponent.CleanUpGame();
        Hide();
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

}
