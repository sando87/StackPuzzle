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

    public void OnSound()
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
            SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
            MenuEditBox.PopUp("write test device name, no name for disable", (isOK, inputText) =>
            {
                if(isOK)
                {
                    if (inputText.Length <= 0)
                        UserSetting.SwitchBotPlayer(false, "");
                    else
                        UserSetting.SwitchBotPlayer(true, inputText);
                }
            });
        }
        yield return new WaitForSeconds(1);
        mTouchCount = 0;
    }
}
