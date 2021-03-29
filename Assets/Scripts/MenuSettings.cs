using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuSettings : MonoBehaviour
{
    private const string UIObjName = "UISpace/CanvasPopup/Settings";
    private int mTouchCount = 0;

    public static void PopUp()
    {
        GameObject objMenu = GameObject.Find(UIObjName);
        objMenu.SetActive(true);
        //objMenu.GetComponent<MenuSettings>().SoundOFF.gameObject.SetActive(UserSetting.Mute);
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
                });
            }
        });
    }

    public void OnTouchVolumSFX(float volume)
    {
        UserSetting.VolumeSFX = volume;
        SoundPlayer.Inst.AdjustVolumeSFX(volume);
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
    }
    public void OnTouchVolumBack(float volume)
    {
        UserSetting.VolumeBackground = volume;
        SoundPlayer.Inst.AdjustVolumeBack(volume);
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
    }
}
