using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuGoldShop : MonoBehaviour
{
    private const string UIObjName = "UISpace/CanvasPopup/GoldShop";

    public static void PopUp()
    {
        GameObject objMenu = GameObject.Find(UIObjName);
        objMenu.SetActive(true);
    }
    public static void Hide()
    {
        GameObject objMenu = GameObject.Find(UIObjName);
        objMenu.SetActive(false);
    }

    public void OnClose()
    {
        gameObject.SetActive(false);
        MenuStages.PopUp();
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton2);
    }

    public void OnPurchaseGold()
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
        GameObject btnObj = EventSystem.current.currentSelectedGameObject;
        string goldText = btnObj.transform.Find("Text_Gold").GetComponent<TextMeshProUGUI>().text;
        int gold = int.Parse(goldText.Replace(",", ""));
        int costDiamond = int.Parse(btnObj.transform.Find("Group_Cost/Text_Cost").GetComponent<TextMeshProUGUI>().text);

        MenuMessageBox.PopUp(costDiamond + " Diamonds are used.", true, (isOK) =>
        {
            if(isOK)
            {
                if(Purchases.PurchaseGold(gold, costDiamond))
                {
                    MenuInformBox.PopUp("Success.");

                    string log = "[Purchase Gold] "
                    + "Gold:" + gold + "/" + Purchases.CountGold()
                    + ", Dia:" + costDiamond + "/" + Purchases.CountDiamond();
                    LOG.echo(log);
                }
                else
                    MenuInformBox.PopUp("Not enough Diamonds.");

                MenuStages.Inst.UpdateTopPanel();
            }
        });
    }
}
