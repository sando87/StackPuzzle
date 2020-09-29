using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuStages : MonoBehaviour
{
    private const string UIObjName = "MenuStages";
    Text HeartTimer;
    Text HeartCount;

    public static void PopUp()
    {
        GameObject.Find("UIGroup").transform.Find(UIObjName).gameObject.SetActive(true);
        GameObject obj = GameObject.Find("WorldSpace").transform.Find("StageScreen").gameObject;
        MenuStages menu = obj.GetComponent<MenuStages>();

        obj.SetActive(true);
        menu.StopCoroutine("UpdateHeartTimer");
        menu.StartCoroutine("UpdateHeartTimer");
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
    IEnumerable UpdateHeartTimer()
    {
        while(true)
        {
            yield return new WaitForSeconds(1);
            Purchases.UpdateHeartTimer();
            int remainSec = Purchases.RemainSeconds();
            int remainLife = Purchases.CountHeart();
            HeartCount.text = remainLife.ToString();
            if (remainSec > 0)
            {
                int min = remainSec / 60;
                int sec = remainSec % 60;
                string secStr = string.Format("{0:D2}", sec);
                HeartTimer.text = min + ":" + secStr;
            }
            else
            {
                HeartTimer.text = "Full";
            }
        }
    }
}
