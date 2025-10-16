using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuFailed : MonoBehaviour
{
    private const string UIObjName = "UISpace/CanvasGameUI/PlayFailed";

    public static void PopUp()
    {
        GameObject menuFailed = GameObject.Find(UIObjName).gameObject;

        MenuFailed menu = menuFailed.GetComponent<MenuFailed>();

        menuFailed.SetActive(true);
    }

    public void OnAgain()
    {
        gameObject.SetActive(false);
        MenuInGame.Hide();
        MenuStages.PopUp();
        SoundPlayer.Inst.PlayBackMusic(SoundPlayer.Inst.BackMusicMap);
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
    }
}
