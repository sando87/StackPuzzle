using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;

[Serializable]
public class LocaleLang
{
    public string EnglishID;
    public string Korea;
    public string Japan;
    public string China;
}

public enum LocaleSupportLangType
{
    None, English, Korea, Japan, China
}

public class LocaleManager : MonoBehaviour
{
    private static LocaleManager mInst = null;
    public static LocaleManager Inst { get { if (mInst == null) mInst = FindObjectOfType<LocaleManager>(); return mInst; } }

    Dictionary<string, LocaleLang> mDicLocaleLangs = new Dictionary<string, LocaleLang>();

    void Awake()
    {
        TextAsset ta = Resources.Load<TextAsset>("locale");
        string csvFormatRawData = ta.text;
        string[] lines = csvFormatRawData.Split(Environment.NewLine);
        for (int i = 1; i < lines.Length; i++)
        {
            string[] parts = lines[i].Split(',');
            LocaleLang lang = new LocaleLang();
            lang.EnglishID = parts[0];
            lang.Korea = parts[1];
            lang.Japan = parts[2];
            lang.China = parts[3];

            mDicLocaleLangs[lang.EnglishID] = lang;
        }
    }

    public string DoLocaleText(string englishID, LocaleSupportLangType type)
    {
        if (!mDicLocaleLangs.ContainsKey(englishID))
        {
            return "(noData)" + englishID;
        }

        switch (type)
        {
            case LocaleSupportLangType.English: return mDicLocaleLangs[englishID].EnglishID;
            case LocaleSupportLangType.Korea: return mDicLocaleLangs[englishID].Korea;
            case LocaleSupportLangType.Japan: return mDicLocaleLangs[englishID].Japan;
            case LocaleSupportLangType.China: return mDicLocaleLangs[englishID].China;
        }
        return "(noData)" + englishID;
    }

}
