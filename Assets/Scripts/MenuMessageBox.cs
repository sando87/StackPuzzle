using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuMessageBox : MonoBehaviour
{
    public Text mMessage;
    public GameObject OkButton;
    public GameObject CancleButton;

    private Action<bool> mOnClick = null;

    public static void PopUp(string message, bool twoButtonMode, Action<bool> onClick)
    {
        GameObject prefab = (GameObject)Resources.Load("Prefabs/MessageBox", typeof(GameObject));
        GameObject objMenu = GameObject.Instantiate(prefab, GameObject.Find("UIGroup/CanvasPopUp").transform);
        MenuMessageBox box = objMenu.GetComponent<MenuMessageBox>();
        box.mOnClick = onClick;
        box.mMessage.text = message;
        if (twoButtonMode)
        {
            box.OkButton.SetActive(true);
            box.CancleButton.SetActive(true);
        }
        else
        {
            box.OkButton.SetActive(true);
            box.CancleButton.SetActive(false);
            Vector3 pos = box.OkButton.transform.localPosition;
            pos.x = 0;
            box.OkButton.transform.localPosition = pos;
        }
    }

    public void OnOK()
    {
        Destroy(gameObject);
        mOnClick?.Invoke(true);
    }
    public void OnCancle()
    {
        Destroy(gameObject);
        mOnClick?.Invoke(false);
    }
}
