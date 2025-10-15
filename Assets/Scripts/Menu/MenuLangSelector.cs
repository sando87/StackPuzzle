using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuLangSelector : MonoBehaviour
{
    [SerializeField] Transform RootButtons;
    [SerializeField] Button LangButtonPrefab;

    private Action<LocaleSupportLangType> EventSelectLang = null;

    public static MenuLangSelector PopUp(Action<LocaleSupportLangType> onSelect)
    {
        GameObject prefab = (GameObject)Resources.Load("Prefabs/LangSelector", typeof(GameObject));
        GameObject objMenu = GameObject.Instantiate(prefab, GameObject.Find("UISpace/CanvasGameUI").transform);
        MenuLangSelector box = objMenu.GetComponent<MenuLangSelector>();
        box.EventSelectLang = onSelect;
        box.UpdateLangSelector();
        return box;
    }

    public void UpdateLangSelector()
    {
        int count = RootButtons.childCount;
        for (int i = count - 1; i >= 0; i--)
        {
            Destroy(RootButtons.GetChild(i).gameObject);
        }

        int langCount = Enum.GetValues(typeof(LocaleSupportLangType)).Length;
        for (int i = 1; i < langCount; i++)
        {
            LocaleSupportLangType langType = (LocaleSupportLangType)i;
            if (UserSetting.CurrentLang == langType)
            {
                Button btn = Instantiate(LangButtonPrefab, RootButtons);
                btn.GetComponentInChildren<TextMeshProUGUI>().text = LocaleManager.Inst.DoLocaleText(langType.ToString(), UserSetting.CurrentLang);
                btn.GetComponent<Image>().color = Color.gray;
                btn.enabled = false;
            }
            else
            {
                Button btn = Instantiate(LangButtonPrefab, RootButtons);
                btn.GetComponentInChildren<TextMeshProUGUI>().text = LocaleManager.Inst.DoLocaleText(langType.ToString(), UserSetting.CurrentLang);
                btn.onClick.AddListener(() => OnClickLangButton(langType));
            }
        }
    }

    void OnClickLangButton(LocaleSupportLangType langType)
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
        EventSelectLang?.Invoke(langType);
        Destroy(gameObject);
    }

    public void OnCancle()
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton2);
        Destroy(gameObject);
    }
}
