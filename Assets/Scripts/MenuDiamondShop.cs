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
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
    }

    public void OnClose()
    {
        gameObject.SetActive(false);
        MenuStages.PopUp();
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
    }

    public void OnPurchaseDiamond(int cost)
    {
        switch(cost)
        {
            case 500: Purchases.PurchaseDiamond(10); break;
            case 1000: Purchases.PurchaseDiamond(30); break;
            case 2500: Purchases.PurchaseDiamond(100); break;
            case 5000: Purchases.PurchaseDiamond(300); break;
            default: break;
        }
        CurrentDiamond.text = Purchases.CountDiamond().ToString();
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
    }
}
