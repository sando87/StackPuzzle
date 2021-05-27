using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuDiamondShop : MonoBehaviour
{
    private const string UIObjName = "UISpace/CanvasPopup/DiamondShop";

    public static void PopUp()
    {
        MenuDiamondShop objMenu = GameObject.Find(UIObjName).GetComponent<MenuDiamondShop>();
        objMenu.gameObject.SetActive(true);
    }
    public static void Hide()
    {
        MenuDiamondShop objMenu = GameObject.Find(UIObjName).GetComponent<MenuDiamondShop>();
        objMenu.gameObject.SetActive(false);
    }

    private void Start()
    {
        UnityEngine.Purchasing.IAPButton[] btns = GetComponentsInChildren<UnityEngine.Purchasing.IAPButton>();
        foreach (UnityEngine.Purchasing.IAPButton btn in btns)
            btn.IsOKPurchase = OnClickPurchase;
    }

    public void OnClose()
    {
        gameObject.SetActive(false);
        MenuStages.PopUp();
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton2);
    }

    public bool OnClickPurchase(string productID)
    {
#if (UNITY_ANDROID || UNITY_IPHONE) && !UNITY_EDITOR
        if (!NetClientApp.GetInstance().IsNetworkAlive)
        {
            MenuMessageBox.PopUp("Network NotReachable.", false, null);
            return false;
        }

        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
        LOG.echo("[Request Purchase] " + "ProductId:" + productID);
        return true; //true 반환시 구매 진행
#else
        return false;
#endif
    }
    public void HandleOnPurchaseOK(UnityEngine.Purchasing.Product pro)
    {
        LOG.echo(pro.definition.id);
        OnSccuessPurchaseDiamond(pro.definition.id);
    }
    public void HandleOnPurchaseError(UnityEngine.Purchasing.Product pro, UnityEngine.Purchasing.PurchaseFailureReason reason)
    {
        LOG.echo(reason.ToString());
        MenuMessageBox.PopUp(reason.ToString(), false, null);
    }

    public void OnSccuessPurchaseDiamond(string productID)
    {
        switch (productID)
        {
            case "test0001": Purchases.PurchaseDiamond(10); break;
            case "test0002": Purchases.PurchaseDiamond(50); break;
            case "test0003": Purchases.PurchaseDiamond(100); break;
            case "test0004": Purchases.PurchaseDiamond(150); break;
            default: LOG.warn(); break;
        }

        MenuStages.Inst.UpdateTopPanel();
        MenuInformBox.PopUp("Success Purchase!!");

        string log = "[Response Purchase] " + "ProductID:" + productID + ", CurrentTotalDia:" + Purchases.CountDiamond();
        LOG.echo(log);
    }

}
