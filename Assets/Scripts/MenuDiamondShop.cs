﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuDiamondShop : MonoBehaviour
{
    private const string UIObjName = "CanvasPopUp/MenuDiamondShop";

    
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

    public void OnRequestPurchaseDiamond(int realmoney)
    {
        MenuMessageBox.PopUp("Do you really want to buy diamonds?", true, (isOK) =>
        {
            if(isOK)
            {
                //Call Purchase API
                OnSccuessPurchaseDiamond(realmoney);
            }
        });
    }

    public void OnSccuessPurchaseDiamond(int realmoney)
    {
        switch (realmoney)
        {
            case 1000: Purchases.PurchaseDiamond(10); break;
            case 5000: Purchases.PurchaseDiamond(50); break;
            case 10000: Purchases.PurchaseDiamond(100); break;
            case 15000: Purchases.PurchaseDiamond(150); break;
            default: LOG.warn(); return;
        }

        MenuInformBox.PopUp("Success Purchase : " + realmoney);

        MenuStages.Inst.UpdateTopPanel();
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
    }
}
