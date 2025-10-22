using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuDiamondShop : MonoBehaviour
{
    private const string UIObjName = "UISpace/CanvasGameUI/DiamondShop";

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

    public void OnButtonBuyDiaSmall() { TryBuyProduct(IAPProductType.joypop_product_dia1); }
    public void OnButtonBuyDiaBig() { TryBuyProduct(IAPProductType.joypop_product_dia2); }
    public void OnButtonBuyDiaMass() { TryBuyProduct(IAPProductType.joypop_product_dia3); }
    public void OnButtonBuyRemoveAds() { TryBuyProduct(IAPProductType.joypop_product_ads); }

    void TryBuyProduct(IAPProductType productID)
    {
#if (UNITY_ANDROID || UNITY_IPHONE) && !UNITY_EDITOR
        if (!NetClientApp.GetInstance().IsNetworkAlive)
        {
            MenuMessageBox.PopUp("Network NotReachable", false, null);
            return;
        }

        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
        IAPManager.Inst.BuyProduct(productID, (isSuccess) =>
        {
            OnResultPurchase(productID, isSuccess);
        });
#else
        MenuMessageBox.PopUp("Purchase(Test) : " + productID, true, (isOK) =>
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
            MenuMessageBox.PopUp("Failed", false, null);
        }
    }

    void DoSccuessPurchaseDiamond(IAPProductType productID)
    {
        if (productID == IAPProductType.joypop_product_dia1)
            Purchases.PurchaseDiamond(40);
        else if (productID == IAPProductType.joypop_product_dia2)
            Purchases.PurchaseDiamond(200);
        else if (productID == IAPProductType.joypop_product_dia3)
            Purchases.PurchaseDiamond(1000);
        else if (productID == IAPProductType.joypop_product_ads)
            Purchases.PurchaseAdsSkip();

        MenuStages.Inst.UpdateTopPanel();
        MenuInformBox.PopUp("Success");

        string log = "PurchaseDia," + productID + "," + Purchases.CountDiamond();
        LOG.trace(log);
    }

}
