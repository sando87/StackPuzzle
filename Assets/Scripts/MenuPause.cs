using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuPause : MonoBehaviour
{
    private const string UIObjName = "UISpace/CanvasPopup/Play_Pause";

    public GameObject GameField;

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
        InGameManager.InstStage.FinishGame();
    }
    public void OnResume()
    {
        gameObject.SetActive(false);
        //InGameManager.InstStage.Pause = false;
    }
}
