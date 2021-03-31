using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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

    public void OnPurchaseGold()
    {
        GameObject btnObj = EventSystem.current.currentSelectedGameObject;
        string goldText = btnObj.transform.Find("Text_Gold").GetComponent<TextMeshProUGUI>().text;
        int gold = int.Parse(goldText.Replace(",", ""));
        int costDiamond = int.Parse(btnObj.transform.Find("Group_Cost/Text_Cost").GetComponent<TextMeshProUGUI>().text);

        bool ret = Purchases.PurchaseGold(gold, costDiamond);
        if(!ret)
        {
            MenuInformBox.PopUp("Not enough Diamonds.");
            SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectAlarm);
            return;
        }

        MenuStages.Inst.UpdateTopPanel();
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
    }
}
