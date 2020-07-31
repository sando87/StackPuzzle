using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuStages : MonoBehaviour
{
    private const string UIObjName = "MenuStages";

    public static void PopUp()
    {
        GameObject menu = GameObject.Find("UIGroup").transform.Find(UIObjName).gameObject;
        //menu.GetComponent<MenuStages>().DoSomething();
        menu.SetActive(true);
    }
    public static void Hide()
    {
        GameObject menu = GameObject.Find("UIGroup").transform.Find(UIObjName).gameObject;
        menu.SetActive(false);
    }

    public void OnClose()
    {
        gameObject.SetActive(false);
    }
}
