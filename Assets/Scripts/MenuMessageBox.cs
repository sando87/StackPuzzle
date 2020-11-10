using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuMessageBox : MonoBehaviour
{
    public Text mTitle;
    public Text mMessage;
    public GameObject OkButton;
    public GameObject CancleButton;

    public string Title {
        get { return mTitle.text; }
        set { mTitle.text = value; }
    }

    private Action<bool> mOnClick = null;

    public static MenuMessageBox PopUp(string message, bool twoButtonMode, Action<bool> onClick)
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
        return box;
    }

    public void OnOK()
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
        Destroy(gameObject);
        mOnClick?.Invoke(true);
    }
    public void OnCancle()
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton2);
        Destroy(gameObject);
        mOnClick?.Invoke(false);
    }
}
