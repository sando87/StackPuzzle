using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuStages : MonoBehaviour
{
    private const string UIObjName = "MenuStages";

    public Text HeartTimer;
    public Text HeartCount;
    public Text DiamondCount;

    private void Update()
    {
#if PLATFORM_ANDROID
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnClose();
        }
#endif
    }
    public static void PopUp()
    {
        GameObject obj = GameObject.Find("UIGroup").transform.Find(UIObjName).gameObject;
        GameObject.Find("WorldSpace").transform.Find("StageScreen").gameObject.SetActive(true);
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
    IEnumerator UpdateHeartTimer()
    {
        while(true)
        {
            Purchases.UpdateHeartTimer();
            int remainSec = Purchases.RemainSeconds();
            int remainLife = Purchases.CountHeart();
            HeartCount.text = remainLife.ToString();
            if (Purchases.MaxHeart())
            {
                HeartTimer.text = "Full";
            }
            else
            {
                int min = remainSec / 60;
                int sec = remainSec % 60;
                string secStr = string.Format("{0:D2}", sec);
                HeartTimer.text = min + ":" + secStr;
            }
            DiamondCount.text = Purchases.CountDiamond().ToString();
            yield return new WaitForSeconds(1);
        }
    }
}
