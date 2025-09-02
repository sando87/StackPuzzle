using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;

[Serializable]
public class LocaleLang
{
    public string English;
    public string Korean;
    public string Japanese;
    public string Chinese;
    public string Spanish;
    public string German;
    public string French;
    public string Portuguese;
    public string Russian;
}

public enum LocaleSupportLangType
{
    None, English, Korean, Japanese, Chinese, Spanish, German, French, Portuguese, Russian
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
            lang.English = parts[0];
            lang.Korean = parts[1];
            lang.Japanese = parts[2];
            lang.Chinese = parts[3];
            lang.Spanish = parts[4];
            lang.German = parts[5];
            lang.French = parts[6];
            lang.Portuguese = parts[7];
            lang.Russian = parts[8];

            mDicLocaleLangs[lang.English] = lang;
        }
    }

    public string DoLocaleText(string englishID, LocaleSupportLangType type)
    {
        if (mDicLocaleLangs.ContainsKey(englishID))
        {
            switch (type)
            {
                case LocaleSupportLangType.English: return mDicLocaleLangs[englishID].English;
                case LocaleSupportLangType.Korean: return mDicLocaleLangs[englishID].Korean;
                case LocaleSupportLangType.Japanese: return mDicLocaleLangs[englishID].Japanese;
                case LocaleSupportLangType.Chinese: return mDicLocaleLangs[englishID].Chinese;
                case LocaleSupportLangType.Spanish: return mDicLocaleLangs[englishID].Spanish;
                case LocaleSupportLangType.German: return mDicLocaleLangs[englishID].German;
                case LocaleSupportLangType.French: return mDicLocaleLangs[englishID].French;
                case LocaleSupportLangType.Portuguese: return mDicLocaleLangs[englishID].Portuguese;
                case LocaleSupportLangType.Russian: return mDicLocaleLangs[englishID].Russian;
            }
        }

#if UNITY_EDITOR
        return "(noData)" + englishID;
#else
        return englishID;
#endif
    }

}
