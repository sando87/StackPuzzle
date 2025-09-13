using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Purchasing;

public class LocaleTranslator : MonoBehaviour
{
    TextMeshProUGUI mUIText = null;
    string mEnglishID = "";

    void Awake()
    {
        if (mUIText == null)
        {
            mUIText = GetComponent<TextMeshProUGUI>();
            mEnglishID = mUIText != null ? mUIText.text : "";
        }
    }

    void OnEnable()
    {
        DoTranlateText();
    }

    public void DoTranlateText()
    {
        if (mUIText != null)
        {
            mUIText.font = LocaleManager.Inst.CurrentFontAsset;
            mUIText.text = LocaleManager.Inst.DoLocaleText(mEnglishID, UserSetting.CurrentLang);
        }
    }

}
