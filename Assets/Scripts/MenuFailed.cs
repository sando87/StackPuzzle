using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuComplete : MonoBehaviour
{
    public GameObject StageScreen;
    public static void PopUp(int level, int starCount, int score)
    {
        GameObject menuComp = GameObject.Find("PopUpMenu").transform.Find("MenuComplete").gameObject;

        MenuComplete menu = menuComp.GetComponent<MenuComplete>();
        menu.transform.Find("Image/Star1").gameObject.SetActive(starCount >= 1);
        menu.transform.Find("Image/Star2").gameObject.SetActive(starCount >= 2);
        menu.transform.Find("Image/Star3").gameObject.SetActive(starCount >= 3);
        menu.transform.Find("Image/Score").GetComponent<Text>().text = score.ToString();
        menu.transform.Find("Image/Level").GetComponent<Text>().text = level.ToString();

        menuComp.SetActive(true);
    }

    public void OnNext()
    {
        gameObject.SetActive(false);
        StageScreen.SetActive(true);
    }
}
