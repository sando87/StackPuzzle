using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuItemShop : MonoBehaviour
{
    private const string UIObjName = "CanvasPopUp/MenuItemShop";

    public Text CurrentDiamond;
    public Text CurrentItemA;
    public Text CurrentItemB;
    public Text CurrentItemC;
    public Text CurrentItemD;

    public static void PopUp()
    {
        GameObject objMenu = GameObject.Find("UIGroup").transform.Find(UIObjName).gameObject;
        objMenu.SetActive(true);
        objMenu.GetComponent<MenuItemShop>().Init();
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
    }

    public void Init()
    {
        CurrentDiamond.text = Purchases.CountDiamond().ToString();
        CurrentItemA.text = Purchases.CountItem(0).ToString();
        CurrentItemB.text = Purchases.CountItem(1).ToString();
        CurrentItemC.text = Purchases.CountItem(2).ToString();
        CurrentItemD.text = Purchases.CountItem(3).ToString();

    }
    public void OnClose()
    {
        gameObject.SetActive(false);
        MenuStages.PopUp();
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
    }
    public void OnChargeItem(GameObject item)
    {
        int type = int.Parse(item.name.Replace("ItemType", ""));
        int cnt = int.Parse(item.transform.Find("ItemCount/Text").GetComponent<Text>().text);
        int cost = int.Parse(item.transform.Find("BtnPurchase/Text").GetComponent<Text>().text);

        Purchases.ChargeItem(type, cnt, cost);
        int currentItemCount = Purchases.CountItem(type);
        switch(type)
        {
            case 0: CurrentItemA.text = currentItemCount.ToString(); break;
            case 1: CurrentItemB.text = currentItemCount.ToString(); break;
            case 2: CurrentItemC.text = currentItemCount.ToString(); break;
            case 3: CurrentItemD.text = currentItemCount.ToString(); break;
            default: break;
        }
        CurrentDiamond.text = Purchases.CountDiamond().ToString();
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
    }
}
