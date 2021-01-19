using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuFailed : MonoBehaviour
{
    private const string UIObjName = "UISpace/CanvasPopup/Play_Continue";

    public static void PopUp()
    {
        GameObject menuFailed = GameObject.Find(UIObjName).gameObject;

        MenuFailed menu = menuFailed.GetComponent<MenuFailed>();

        menuFailed.SetActive(true);

        if (UserSetting.IsBotPlayer)
            menu.StartCoroutine(menu.AutoEnd());
    }
    IEnumerator AutoEnd()
    {
        yield return new WaitForSeconds(1);
        OnAgain();
    }

    public void OnAgain()
    {
        gameObject.SetActive(false);
        MenuInGame.Hide();
        MenuStages.PopUp();
        StageManager.Inst.Activate(true);
        SoundPlayer.Inst.PlayBackMusic(SoundPlayer.Inst.BackMusicMap);
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
    }
}
