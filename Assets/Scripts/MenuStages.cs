using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuStages : MonoBehaviour
{
    private const string UIObjName = "MenuStages";

    public static void PopUp()
    {
        GameObject.Find("UIGroup").transform.Find(UIObjName).gameObject.SetActive(true);
        GameObject.Find("WorldSpace").transform.Find("StageScreen").gameObject.SetActive(true);
    }
    public static void Hide()
    {
        GameObject.Find("UIGroup").transform.Find(UIObjName).gameObject.SetActive(false);
        GameObject.Find("WorldSpace").transform.Find("StageScreen").gameObject.SetActive(false);
    }

    public void OnClose()
    {
        GameObject.Find("UIGroup").transform.Find(UIObjName).gameObject.SetActive(false);
        GameObject.Find("WorldSpace").transform.Find("StageScreen").gameObject.SetActive(false);
        MenuMain.PopUp();
    }
    public void OnShopHeart()
    {
        MenuHeartShop.PopUp();
        Hide();
    }
    public void OnShopDiamond()
    {
        MenuDiamondShop.PopUp();
        Hide();
    }
    public void OnSettings()
    {
        MenuSettings.PopUp();
        Hide();
    }
    public void OnShopItem()
    {
        MenuItemShop.PopUp();
        Hide();
    }
}
