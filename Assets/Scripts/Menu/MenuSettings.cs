﻿using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuSettings : MonoBehaviour
{
    private static MenuSettings mInst = null;
    private const string UIObjName = "UISpace/CanvasPopup/Settings";
    private int mTouchCount = 0;

    public Slider SoundSFX;
    public Slider SoundBack;
    public TextMeshProUGUI UserName;
    public TextMeshProUGUI ExpLevel;
    public TextMeshProUGUI Exp;
    public Slider ExpBar;

    public static void PopUp()
    {
        MenuSettings menu = GameObject.Find(UIObjName).GetComponent<MenuSettings>();
        menu.gameObject.SetActive(true);
        menu.SoundSFX.value = UserSetting.VolumeSFX;
        menu.SoundBack.value = UserSetting.VolumeBackground;
        menu.UserName.text = UserSetting.UserName;
        menu.UpdateExpBar();
    }


    public static MenuSettings Inst()
    {
        if (mInst == null)
            mInst = GameObject.Find(UIObjName).GetComponent<MenuSettings>();
        return mInst;
    }

    public void OnClose()
    {
        gameObject.SetActive(false);
        MenuStages.PopUp();
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton2);
    }

    public void OnToggleSoundMute()
    {
        bool isMute = SoundPlayer.Inst.OnOff();
        //SoundOFF.gameObject.SetActive(isMute);
        UserSetting.Mute = isMute;
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
    }

    public void OnAutoPlay()
    {
        StopCoroutine("DetectFiveTouch");
        StartCoroutine("DetectFiveTouch");
    }

    IEnumerator DetectFiveTouch()
    {
        mTouchCount++;
        if(mTouchCount >= 5)
        {
            string currentDeviceName = UserSetting.UserInfo.deviceName;
            SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
            MenuEditBox.PopUp("write test device name, or 'off' for disable", currentDeviceName,(isOK, inputText) =>
            {
                if(isOK)
                {
                    if (inputText == "off")
                        UserSetting.SwitchBotPlayer(false, "");
                    else
                        UserSetting.SwitchBotPlayer(true, inputText);
                }
            });
        }
        yield return new WaitForSeconds(1);
        mTouchCount = 0;
    }

    public void OnClickEditUSerName()
    {
        EditUserName();
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
    }

    public static void EditUserName()
    {
        string currentUserName = UserSetting.UserInfo.userName;
        MenuEditBox.PopUp("Write your name.", currentUserName, (isOK, inputText) =>
        {
            if (isOK)
            {
                UserInfo info = new UserInfo();
                info.userPk = UserSetting.UserPK;
                info.userName = inputText;
                NetClientApp.GetInstance().Request(NetCMD.EditUserName, info, (_body) =>
                {
                    UserInfo res = Utils.Deserialize<UserInfo>(ref _body);
                    UserSetting.UserName = res.userName;
                    MenuSettings.Inst().UserName.text = UserSetting.UserName;
                });
            }
        });
    }

    public void OnTouchVolumSFX()
    {
        UserSetting.VolumeSFX = SoundSFX.value;
        SoundPlayer.Inst.AdjustVolumeSFX(SoundSFX.value);
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
    }
    public void OnTouchVolumBack()
    {
        UserSetting.VolumeBackground = SoundBack.value;
        SoundPlayer.Inst.AdjustVolumeBack(SoundBack.value);
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
    }
    private void UpdateExpBar()
    {
        int score = UserSetting.UserScore;
        int level = UserSetting.ToLevel(score);
        ExpLevel.text = level.ToString();
        Exp.text = score.ToString();
        int dd = score % UserSetting.ScorePerLevel;
        float rate = (float)dd / UserSetting.ScorePerLevel;
        ExpBar.normalizedValue = rate;
    }
}