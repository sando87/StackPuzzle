using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuPause : MonoBehaviour
{
    private const string UIObjName = "MenuPause";

    public static void PopUp()
    {
        GameObject menuPlay = GameObject.Find("UIGroup").transform.Find(UIObjName).gameObject;
        menuPlay.SetActive(true);
    }

    public void OnQuit()
    {
        gameObject.SetActive(false);
        InGameManager.Inst.FinishGame(false);
    }
    public void OnResume()
    {
        gameObject.SetActive(false);
        InGameManager.Inst.ResumeGame();
    }
}
