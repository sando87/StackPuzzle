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

    [SerializeField] LocaleLang[] LocalLangs = null;

    Dictionary<string, LocaleLang> mDicLocaleLangs = new Dictionary<string, LocaleLang>();

    void Awake()
    {
        foreach (LocaleLang lang in LocalLangs)
            mDicLocaleLangs[lang.EnglishID] = lang;
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
