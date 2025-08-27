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
        // UnityEngine.Purchasing.IAPButton[] btns = GetComponentsInChildren<UnityEngine.Purchasing.IAPButton>();
        // foreach (UnityEngine.Purchasing.IAPButton btn in btns)
        //     btn.IsOKPurchase = OnClickPurchase;
    }

    public void OnClose()
    {
        gameObject.SetActive(false);
        MenuStages.PopUp();
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton2);
    }

    public void OnButtonBuyProductA() { TryBuyProduct(IAPProductType.joypop_product_dia1); }
    public void OnButtonBuyProductB() { TryBuyProduct(IAPProductType.joypop_product_dia2); }
    public void OnButtonBuyProductC() { TryBuyProduct(IAPProductType.ProductID_C); }
    public void OnButtonBuyProductD() { TryBuyProduct(IAPProductType.ProductID_D); }

    void TryBuyProduct(IAPProductType productID)
    {
#if (UNITY_ANDROID || UNITY_IPHONE) && !UNITY_EDITOR
        if (!NetClientApp.GetInstance().IsNetworkAlive)
        {
            MenuMessageBox.PopUp("Network NotReachable.", false, null);
            return;
        }

        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
        IAPManager.Inst.BuyProduct(productID, (isSuccess) =>
        {
            OnResultPurchase(productID, isSuccess);
        });
#else
        MenuMessageBox.PopUp("구매진행(개발자용) : " + productID, true, (isOK) =>
        {
            if(isOK)
                OnResultPurchase(productID, true);
        });
#endif
    }
    void OnResultPurchase(IAPProductType productID, bool isSuccess)
    {
        if (isSuccess)
        {
            DoSccuessPurchaseDiamond(productID);
        }
        else
        {
            MenuMessageBox.PopUp(productID.ToString(), false, null);
        }
    }

    void DoSccuessPurchaseDiamond(IAPProductType productID)
    {
        if (productID == IAPProductType.joypop_product_dia1)
            Purchases.PurchaseDiamond(40);
        else if (productID == IAPProductType.joypop_product_dia2)
            Purchases.PurchaseDiamond(100);
        else if (productID == IAPProductType.ProductID_C)
            Purchases.PurchaseDiamond(250);
        else if (productID == IAPProductType.ProductID_D)
            Purchases.PurchaseDiamond(500);
        else
            LOG.warn();

        MenuStages.Inst.UpdateTopPanel();
        MenuInformBox.PopUp("Success Purchase!!");

        string log = "PurchaseDia," + productID + "," + Purchases.CountDiamond();
        LOG.trace(log);
    }

}
