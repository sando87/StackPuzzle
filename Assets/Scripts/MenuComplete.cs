using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuComplete : MonoBehaviour
{
    private const string UIObjName = "CanvasPopUp/MenuComplete";

    public Image Star1;
    public Image Star2;
    public Image Star3;
    public Text Score;
    public Text StageLevel;

    public static void PopUp(int level, int starCount, int score)
    {
        GameObject menuComp = GameObject.Find("UIGroup").transform.Find(UIObjName).gameObject;

        MenuComplete menu = menuComp.GetComponent<MenuComplete>();
        menu.Star1.gameObject.SetActive(starCount >= 1);
        menu.Star2.gameObject.SetActive(starCount >= 2);
        menu.Star3.gameObject.SetActive(starCount >= 3);
        menu.Score.text = score.ToString();
        menu.StageLevel.text = level.ToString();

        menuComp.SetActive(true);
    }

    public void OnNext()
    {
        gameObject.SetActive(false);
        MenuStages.PopUp();
        StageManager.Inst.Activate(true);
    }
}
