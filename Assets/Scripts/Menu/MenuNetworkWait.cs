using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuNetworkWait : MonoBehaviour
{
    public TextMeshProUGUI MessageText;

    public static MenuNetworkWait PopUp(string message)
    {
        GameObject prefab = (GameObject)Resources.Load("Prefabs/MenuInformBox", typeof(GameObject));
        GameObject objMenu = GameObject.Instantiate(prefab, GameObject.Find("UISpace/CanvasPopup").transform);
        MenuNetworkWait box = objMenu.GetComponent<MenuNetworkWait>();
        box.MessageText.text = message;
        NetClientApp.GetInstance().ConnectASync(() =>
        {
            MenuInformBox.PopUp("Connection success.");
            Destroy(box.gameObject);
        });
        return box;
    }

    IEnumerator WaitConnection(float timeout)
    {
        int time = 0;
        string msg = MessageText.text;
        while (time < timeout)
        {
            switch(time % 3)
            {
                case 0: MessageText.text = msg + "."; break;
                case 1: MessageText.text = msg + ".."; break;
                case 2: MessageText.text = msg + "..."; break;
            }
            yield return new WaitForSeconds(1);
            time += 1;
        }
        MenuInformBox.PopUp("Connection failure.");
        NetClientApp.GetInstance().DisConnect();
        Destroy(gameObject);
    }


}
