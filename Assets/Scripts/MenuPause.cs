using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuPause : MonoBehaviour
{
    private const string UIObjName = "CanvasPopUp/MenuPause";

    public GameObject GameField;

    public static void PopUp()
    {
        GameObject menuPlay = GameObject.Find("UIGroup").transform.Find(UIObjName).gameObject;
        menuPlay.SetActive(true);
    }
    public static bool IsPopped()
    {
        GameObject menuPlay = GameObject.Find("UIGroup").transform.Find(UIObjName).gameObject;
        return menuPlay.activeSelf;
    }

    public void OnQuit()
    {
        gameObject.SetActive(false);
        GameField.GetComponent<InGameManager>().FinishGame(false);
        MenuInGame.Hide();
        MenuStages.PopUp();
        StageManager.Inst.Activate(true);
    }
    public void OnResume()
    {
        gameObject.SetActive(false);
        GameField.GetComponent<InGameManager>().Pause = false;
    }
}
