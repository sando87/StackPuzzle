using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuPause : MonoBehaviour
{
    private const string UIObjName = "UISpace/CanvasPopup/PlayPause";

    public static void PopUp()
    {
        GameObject menuPlay = GameObject.Find(UIObjName);
        menuPlay.SetActive(true);
    }
    public static bool IsPopped()
    {
        GameObject menuPlay = GameObject.Find(UIObjName);
        return menuPlay.activeSelf;
    }

    public void OnQuit()
    {
        gameObject.SetActive(false);
        MenuInGame.Inst().FinishGame(false);
    }
    public void OnResume()
    {
        gameObject.SetActive(false);
    }
    public void OnSetting()
    {
        MenuSettings.PopUp();
    }
}
