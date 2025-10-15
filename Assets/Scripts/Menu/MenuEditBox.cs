using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuEditBox : MonoBehaviour
{
    public TextMeshProUGUI Title;
    public TMP_InputField InputField;

    private Action<bool, string> EventClick = null;

    public static MenuEditBox PopUp(string title, string defaultText, Action<bool, string> onClick)
    {
        GameObject prefab = (GameObject)Resources.Load("Prefabs/EditName", typeof(GameObject));
        GameObject objMenu = GameObject.Instantiate(prefab, GameObject.Find("UISpace/CanvasGameUI").transform);
        MenuEditBox box = objMenu.GetComponent<MenuEditBox>();
        box.EventClick = onClick;
        box.Title.text = LocaleManager.Inst.DoLocaleText(title, UserSetting.CurrentLang);
        box.InputField.text = LocaleManager.Inst.DoLocaleText(defaultText, UserSetting.CurrentLang);
        return box;
    }

    public void OnOK()
    {
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
