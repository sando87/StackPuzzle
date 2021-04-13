using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuNetConnector : MonoBehaviour
{
    private const string UIObjName = "UISpace/CanvasPopup/MenuNetConnector";
    public TextMeshProUGUI MessageText;

    public static MenuNetConnector PopUp()
    {
        MenuNetConnector menu = GameObject.Find(UIObjName).GetComponent<MenuNetConnector>();
        menu.gameObject.SetActive(true);
        menu.Init();
        return menu;
    }

    public void Init()
    {
        MessageText.text = "Connecting";
        StartCoroutine("UpdateConnectionMessage");
        NetClientApp.GetInstance().ConnectASync((isConnected) =>
        {
            if (!gameObject.activeInHierarchy)
                return;

            if (isConnected)
            {
                MenuInformBox.PopUp("Connection success.");
                StopCoroutine("UpdateConnectionMessage");
                gameObject.SetActive(false);
            }
            else
            {
                MenuInformBox.PopUp("Connection failure.");
                StopCoroutine("UpdateConnectionMessage");
                gameObject.SetActive(false);
            }
            
        });
    }

    IEnumerator UpdateConnectionMessage()
    {
        int time = 0;
        string msg = MessageText.text;
        while (true)
        {
            switch(time % 3)
            {
                case 0: MessageText.text = msg + ".."; break;
                case 1: MessageText.text = msg + "..."; break;
                case 2: MessageText.text = msg + "...."; break;
            }
            yield return new WaitForSeconds(1);
            time += 1;
        }
    }

    public void OnCancle()
    {
        StopCoroutine("UpdateConnectionMessage");
        gameObject.SetActive(false);
    }


}
