using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuDiamondShop : MonoBehaviour
{
    private const string UIObjName = "UISpace/CanvasPopup/DiamondShop";

    
    public static void PopUp()
    {
        NetClientApp.GetInstance().HeartCheck();

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

    public void OnRequestPurchaseDiamond(int realmoney)
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);

        if (NetClientApp.GetInstance().IsDisconnected())
        {
            MenuNetConnector.PopUp();
            return;
        }

        MenuMessageBox.PopUp("Do you really want to buy diamonds?", true, (isOK) =>
        {
            if(isOK)
            {
                string log = "[Purchase Request] " + "Realmoney:" + realmoney;
                LOG.echo(log);

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
            default: LOG.warn(); break;
        }

        MenuStages.Inst.UpdateTopPanel();
        MenuInformBox.PopUp("Success Purchase : " + realmoney);

        string log = "[Purchase Response] " + "Realmoney:" + realmoney + ", Dia:" + Purchases.CountDiamond();
        LOG.echo(log);
    }
}
