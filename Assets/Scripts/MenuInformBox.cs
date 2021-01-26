using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuInformBox : MonoBehaviour
{
    public TextMeshProUGUI MessageText;

    public static MenuInformBox PopUp(string message)
    {
        GameObject prefab = (GameObject)Resources.Load("Prefabs/MenuInformBox", typeof(GameObject));
        GameObject objMenu = GameObject.Instantiate(prefab, GameObject.Find("UISpace/CanvasPopup").transform);
        MenuInformBox box = objMenu.GetComponent<MenuInformBox>();
        box.MessageText.text = message;
        Destroy(box.gameObject, UserSetting.InfoBoxDisplayTime);
        return box;
    }

}
