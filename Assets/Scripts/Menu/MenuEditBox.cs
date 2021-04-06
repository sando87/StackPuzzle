using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuEditBox : MonoBehaviour
{
    public TextMeshProUGUI Message;
    public TMP_InputField InputField;

    private Action<bool, string> EventClick = null;

    public static MenuEditBox PopUp(string message, string defaultText, Action<bool, string> onClick)
    {
        GameObject prefab = (GameObject)Resources.Load("Prefabs/EditName", typeof(GameObject));
        GameObject objMenu = GameObject.Instantiate(prefab, GameObject.Find("UISpace/CanvasPopup").transform);
        MenuEditBox box = objMenu.GetComponent<MenuEditBox>();
        box.EventClick = onClick;
        box.Message.text = message;
        box.InputField.text = defaultText;
        return box;
    }

    public void OnOK()
    {
        if(InputField.text.Length < UserSetting.NameLengthMin)
        {
            MenuInformBox.PopUp("Write at least 3 characters.");
            return;
        }
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
        EventClick?.Invoke(true, InputField.text);
        Destroy(gameObject);
    }
    public void OnCancle()
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton2);
        EventClick?.Invoke(false, InputField.text);
        Destroy(gameObject);
    }
}
