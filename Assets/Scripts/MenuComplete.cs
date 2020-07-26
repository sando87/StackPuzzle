using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuFailed : MonoBehaviour
{
    public GameObject StageScreen;
    public static void PopUp(int level, int target, int score)
    {
        GameObject menuFailed = GameObject.Find("PopUpMenu").transform.Find("MenuFailed").gameObject;

        MenuFailed menu = menuFailed.GetComponent<MenuFailed>();
        menu.transform.Find("Image/Score").GetComponent<Text>().text = score.ToString();
        menu.transform.Find("Image/Level").GetComponent<Text>().text = level.ToString();
        menu.transform.Find("Image/Goal").GetComponent<Text>().text = target.ToString();

        menuFailed.SetActive(true);
    }

    public void OnAgain()
    {
        gameObject.SetActive(false);
        StageScreen.SetActive(true);
    }
}
