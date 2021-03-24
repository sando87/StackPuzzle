using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuItemShop : MonoBehaviour
{
    private const string UIObjName = "CanvasPopUp/MenuItemShop";

    public Text CurrentItemA;
    public Text CurrentItemB;
    public Text CurrentItemC;
    public Text CurrentItemD;

    public static void PopUp()
    {
        GameObject objMenu = GameObject.Find("UIGroup").transform.Find(UIObjName).gameObject;
        objMenu.SetActive(true);
        objMenu.GetComponent<MenuItemShop>().Init();
    }

    public void Init()
    {
        UpdateState();
    }
    public void OnClose()
    {
        gameObject.SetActive(false);
        MenuStages.PopUp();
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton2);
    }
    public void OnChargeItem(GameObject item)
    {
        int type = int.Parse(item.name.Replace("ItemType", ""));
        int cnt = int.Parse(item.transform.Find("ItemCount/Text").GetComponent<Text>().text);
        int cost = int.Parse(item.transform.Find("BtnPurchase/Text").GetComponent<Text>().text);

        bool ret = Purchases.ChargeItemUseDia(type.ToItemType(), cnt, cost);
        if (!ret)
            MenuInformBox.PopUp("Not enough Diamonds.");

        UpdateState();
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
    }
    public void OnChargeItemUseGold(GameObject item)
    {
        int type = int.Parse(item.name.Replace("ItemType", ""));
        int cnt = int.Parse(item.transform.Find("ItemCount/Text").GetComponent<Text>().text);
        int cost = int.Parse(item.transform.Find("BtnPurchase/Text").GetComponent<Text>().text);

        bool ret = Purchases.ChargeItemUseGold(type.ToItemType(), cnt, cost);
        if (!ret)
            MenuInformBox.PopUp("Not enough Golds.");

        UpdateState();
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
    }
    private void UpdateState()
    {
        MenuStages.Inst.UpdateTopPanel();
        CurrentItemA.text = Purchases.CountItem(0.ToItemType()).ToString();
        CurrentItemB.text = Purchases.CountItem(1.ToItemType()).ToString();
        CurrentItemC.text = Purchases.CountItem(2.ToItemType()).ToString();
        CurrentItemD.text = Purchases.CountItem(3.ToItemType()).ToString();
    }
}
