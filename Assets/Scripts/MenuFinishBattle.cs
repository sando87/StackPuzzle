using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuFinishBattle : MonoBehaviour
{
    private const string UIObjName = "UISpace/CanvasPopup/PlayFinishPVP";

    public GameObject WinEffect;
    public GameObject LoseEffect;

    public static void PopUp(bool win, UserInfo info, int deltaExp)
    {
        GameObject objMenu = GameObject.Find(UIObjName).gameObject;
        objMenu.SetActive(true);

        MenuFinishBattle menu = objMenu.GetComponent<MenuFinishBattle>();
        menu.WinEffect.SetActive(win);
        menu.LoseEffect.SetActive(!win);

        if (UserSetting.IsBotPlayer)
            menu.StartCoroutine(menu.AutoNext());
    }

    public void OnOK()
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
        gameObject.SetActive(false);
        MenuWaitMatch.PopUp();
    }

    public void OnClose()
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
        gameObject.SetActive(false);
        MenuStages.PopUp();
    }

    private IEnumerator AutoNext()
    {
        yield return new WaitForSeconds(1);
        OnOK();
    }
}
