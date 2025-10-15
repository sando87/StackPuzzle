using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuSettings : MonoBehaviour
{
    private static MenuSettings mInst = null;
    private const string UIObjName = "UISpace/CanvasGameUI/Settings";
    private int mTouchCount = 0;

    public Slider SoundSFX;
    public Slider SoundBack;
    public TextMeshProUGUI UserName;
    public TextMeshProUGUI ExpLevel;
    public TextMeshProUGUI Exp;
    public Slider ExpBar;
    public Image LeagueLevel;
    public TextMeshProUGUI LeagueText;
    public TextMeshProUGUI LangText;
    public GameObject AlarmOn;
    public GameObject AlarmOff;

    public static void PopUp()
    {
        MenuSettings menu = GameObject.Find(UIObjName).GetComponent<MenuSettings>();
        menu.gameObject.SetActive(true);
        menu.UpdateUserInfoUI();
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
#if UNITY_STANDALONE_WIN
        StopCoroutine("DetectFiveTouch");
        StartCoroutine("DetectFiveTouch");
#endif
    }

    IEnumerator DetectFiveTouch()
    {
        mTouchCount++;
        if (mTouchCount >= 5)
        {
            MenuMessageBox.PopUp("Do Unlock All Stages", false, (isOK) =>
            {
                if (isOK)
                {
                    for (int i = 0; i < UserSetting.StageTotalCount; ++i)
                    {
                        UserSetting.SetStageStarCount(i + 1, 0);
                    }
                }
            });
        }

        if (mTouchCount >= 5)
        {
            string currentBotLevel = UserSetting.UserInfo.botLevel.ToString();
            SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
            MenuEditBox.PopUp("SwitchingToBot(000~333)", currentBotLevel, (isOK, inputText) =>
            {
                if (isOK)
                {
                    if (int.TryParse(inputText, out int botLevel))
                    {
                        UserSetting.SwitchBotPlayer(botLevel);
                    }
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
        MenuEditBox.PopUp("Edit Username", currentUserName, (isOK, inputText) =>
        {
            if (isOK)
            {
                UserSetting.EditUserName(inputText);
                MenuSettings.Inst().UpdateUserInfoUI();
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
    private void UpdateUserInfoUI()
    {
        SoundSFX.value = UserSetting.VolumeSFX;
        SoundBack.value = UserSetting.VolumeBackground;
        UserName.text = UserSetting.UserName;
        LeagueLevel.sprite = UserSetting.UserInfo.maxLeague.GetSprite();
        LeagueText.text = UserSetting.UserInfo.maxLeague.GetText();
        LangText.text = UserSetting.CurrentLang.ToString();
        AlarmOn.gameObject.SetActive(UserSetting.IsAlarmOn);
        AlarmOff.gameObject.SetActive(!UserSetting.IsAlarmOn);

        int score = UserSetting.UserScore;
        int level = Utils.ToLevel(score);
        ExpLevel.text = level.ToString();
        Exp.text = score.ToString();
        int dd = score % Utils.ScorePerLevel;
        float rate = (float)dd / Utils.ScorePerLevel;
        ExpBar.normalizedValue = rate;
    }

    public void OnBtnTerms()
    {
        MenuTermsAndConditions.PopUp();
    }

    public void OnBtnLanguage()
    {
        MenuLangSelector.PopUp((langType) =>
        {
            UserSetting.CurrentLang = langType;

            UpdateUserInfoUI();

            LocaleTranslator[] textUIs = GetComponentsInChildren<LocaleTranslator>();
            foreach (LocaleTranslator textUI in textUIs)
            {
                textUI.DoTranlateText();
            }

        });
    }

    public void OnToggleAlarm(bool isON)
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);

        AlarmOn.gameObject.SetActive(isON);
        AlarmOff.gameObject.SetActive(!isON);

        UserSetting.IsAlarmOn = isON;
        MobileNotificationManager.Inst.SetUpAlarm(isON);
    }
}
