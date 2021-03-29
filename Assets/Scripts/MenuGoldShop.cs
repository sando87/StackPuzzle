using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuGoldShop : MonoBehaviour
{
    private const string UIObjName = "CanvasPopUp/MenuGoldShop";

    public static void PopUp()
    {
        GameObject objMenu = GameObject.Find("UIGroup").transform.Find(UIObjName).gameObject;
        objMenu.SetActive(true);
    }

    public void OnClose()
    {
        gameObject.SetActive(false);
        MenuStages.PopUp();
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton2);
    }

    public void OnPurchaseGold(int gold, int costDiamond)
    {
        bool ret = Purchases.PurchaseGold(gold, costDiamond);
        if(!ret)
        {
            MenuInformBox.PopUp("Not enough Diamonds.");
            SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectAlarm);
            return;
        }

        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
    }
}
