using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuDiamondShop : MonoBehaviour
{
    private const string UIObjName = "UISpace/CanvasPopup/DiamondShop";

    
    public static void PopUp()
    {
        MenuDiamondShop objMenu = GameObject.Find(UIObjName).GetComponent<MenuDiamondShop>();
        objMenu.gameObject.SetActive(true);
        NetClientApp.GetInstance().IsKeepConnection = true;
    }
    public static void Hide()
    {
        MenuDiamondShop objMenu = GameObject.Find(UIObjName).GetComponent<MenuDiamondShop>();
        objMenu.gameObject.SetActive(false);
        NetClientApp.GetInstance().IsKeepConnection = false;
    }

    public void OnClose()
    {
        gameObject.SetActive(false);
        MenuStages.PopUp();
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton2);
        NetClientApp.GetInstance().IsKeepConnection = false;
    }

    public void OnRequestPurchaseDiamond(int realmoney)
    {
        if (NetClientApp.GetInstance().IsDisconnected())
        {
            MenuNetConnector.PopUp(() => OnRequestPurchaseDiamond(realmoney));
            return;
        }

        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);

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

    public void HandleOnPurchaseOK(UnityEngine.Purchasing.Product pro)
    {
        LOG.echo(pro.receipt);
    }
    public void HandleOnPurchaseError(UnityEngine.Purchasing.Product pro, UnityEngine.Purchasing.PurchaseFailureReason reason)
    {
        LOG.echo(reason.ToString());
    }

}
