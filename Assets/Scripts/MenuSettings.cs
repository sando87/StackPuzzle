using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuSettings : MonoBehaviour
{
    private const string UIObjName = "CanvasPopUp/MenuSettings";
    private int mTouchCount = 0;

    public Image SoundOFF;

    public static void PopUp()
    {
        GameObject objMenu = GameObject.Find("UIGroup").transform.Find(UIObjName).gameObject;
        objMenu.SetActive(true);
        objMenu.GetComponent<MenuSettings>().SoundOFF.gameObject.SetActive(UserSetting.Mute);
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
    }

    public void OnClose()
    {
        gameObject.SetActive(false);
        MenuStages.PopUp();
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
    }

    public void OnSound()
    {
        bool isMute = SoundPlayer.Inst.OnOff();
        SoundOFF.gameObject.SetActive(isMute);
        UserSetting.Mute = isMute;
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
            MenuEditBox.PopUp("write test device name, no name for disable", true, (isOK, inputText) =>
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
