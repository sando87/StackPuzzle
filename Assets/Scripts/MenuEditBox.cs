using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuEditBox : MonoBehaviour
{
    public Text mMessage;
    public InputField mInputField;
    public GameObject OkButton;
    public GameObject CancleButton;

    private Action<bool, string> mOnClick = null;

    public static MenuEditBox PopUp(string message, bool twoButtonMode, Action<bool, string> onClick)
    {
        GameObject prefab = (GameObject)Resources.Load("Prefabs/EditBox", typeof(GameObject));
        GameObject objMenu = GameObject.Instantiate(prefab, GameObject.Find("UIGroup/CanvasPopUp").transform);
        MenuEditBox box = objMenu.GetComponent<MenuEditBox>();
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
        return box;
    }

    public void OnOK()
    {
        mOnClick?.Invoke(true, mInputField.text);
        Destroy(gameObject);
    }
    public void OnCancle()
    {
        mOnClick?.Invoke(false, mInputField.text);
        Destroy(gameObject);
    }
}
