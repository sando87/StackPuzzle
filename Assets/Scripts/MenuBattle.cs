using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuBattle : MonoBehaviour
{
    private static MenuBattle mInst = null;
    private const string UIObjName = "UISpace/CanvasPanel/PVP";

    public GameObject EffectParent;
    public NumbersUI ComboPlayer;
    public NumbersUI ComboOpponent;
    public ScoreBar PVPScoreBar;

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
        //PVPScoreBar.Clear();

        InGameManager.InstPVP_Player.EventMatched = (products) => {
            //PVPScoreBar.AddScore(products[0].Combo * products.Length);
        };
        InGameManager.InstPVP_Player.EventFinish = (success) => {
            FinishGame(success);
        };
        InGameManager.InstPVP_Opponent.EventFinish = (success) => {
            FinishGame(!success);
        };
    }


    private void FinishGame(bool success)
    {
        int deltaExp = NextDeltaExp(success, UserSetting.UserScore, InGameManager.InstPVP_Player.ColorCount);
        UserSetting.UserScore += deltaExp;
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
            MenuFinishBattle.PopUp(success, UserSetting.UserInfo, deltaExp);
        }
        else
        {
            SoundPlayer.Inst.Player.Stop();
            SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectGameOver);
            MenuFinishBattle.PopUp(success, UserSetting.UserInfo, deltaExp);
        }

        string log = InGameManager.InstPVP_Player.GetBillboard().ToCSVString();
        LOG.echo(log);

        InGameManager.InstPVP_Player.FinishGame();
        InGameManager.InstPVP_Opponent.FinishGame();
        Hide();
    }
    private int NextDeltaExp(bool isWin, int curScore, float colorCount)
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
