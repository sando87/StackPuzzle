using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuPause : MonoBehaviour
{
    private const string UIObjName = "CanvasPopUp/MenuPause";

    public static void PopUp()
    {
        GameObject menuPlay = GameObject.Find("UIGroup").transform.Find(UIObjName).gameObject;
        menuPlay.SetActive(true);
    }

    public void OnQuit()
    {
        gameObject.SetActive(false);
        InGameManager.Inst.FinishGame(false);
        MenuInGame.Hide();
        MenuStages.PopUp();
        StageManager.Inst.Activate(true);
    }
    public void OnResume()
    {
        gameObject.SetActive(false);
        InGameManager.Inst.ResumeGame();
    }
}
