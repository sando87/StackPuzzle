using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuDiamondShop : MonoBehaviour
{
    private const string UIObjName = "CanvasPopUp/MenuDiamondShop";

    public Text CurrentDiamond;
    
    public static void PopUp()
    {
        GameObject objMenu = GameObject.Find("UIGroup").transform.Find(UIObjName).gameObject;
        objMenu.SetActive(true);
        objMenu.GetComponent<MenuDiamondShop>().CurrentDiamond.text = Purchases.CountDiamond().ToString();
    }

    public void OnClose()
    {
        gameObject.SetActive(false);
        MenuStages.PopUp();
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton2);
    }

    public void OnPurchaseDiamond(int cost)
    {
        switch(cost)
        {
            case 1: Purchases.PurchaseDiamond(10); break;
            case 5: Purchases.PurchaseDiamond(50); break;
            case 10: Purchases.PurchaseDiamond(100); break;
            case 15: Purchases.PurchaseDiamond(150); break;
            default: break;
        }
        CurrentDiamond.text = Purchases.CountDiamond().ToString();
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
    }
}
