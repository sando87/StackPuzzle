using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuMessageBox : MonoBehaviour
{
    public TextMeshProUGUI TitleText;
    public TextMeshProUGUI MessageText;
    public GameObject BtnYes;
    public GameObject BtnNo;
    public GameObject BtnSingleOk;

    public string Title {
        get { return TitleText.text; }
        set { TitleText.text = value; }
    }

    private Action<bool> mOnClick = null;

    public static MenuMessageBox PopUp(string message, bool twoButtonMode, Action<bool> onClick)
    {
        GameObject prefab = (GameObject)Resources.Load("Prefabs/MessageBoxNew", typeof(GameObject));
        GameObject objMenu = GameObject.Instantiate(prefab, GameObject.Find("UISpace/CanvasPopup").transform);
        MenuMessageBox box = objMenu.GetComponent<MenuMessageBox>();
        box.mOnClick = onClick;
        box.MessageText.text = message;
        if (twoButtonMode)
        {
            box.BtnYes.SetActive(true);
            box.BtnNo.SetActive(true);
            box.BtnSingleOk.SetActive(false);
        }
        else
        {
            box.BtnYes.SetActive(false);
            box.BtnNo.SetActive(false);
            box.BtnSingleOk.SetActive(true);
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
