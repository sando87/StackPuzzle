using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuLoading : MonoBehaviour
{
    public TextMeshProUGUI TitleText;
    public TextMeshProUGUI MessageText;

    private Func<bool> mIsLoaded;
    private Action mOnClose;
    private string[] mDots = { ".", "..", "..." };
    private float mAccTime = 0;
    private float mTimeout = 0;

    public static MenuLoading PopUp(string title, float timeout, Func<bool> isLoaded, Action onClose)
    {
        GameObject prefab = (GameObject)Resources.Load("Prefabs/MenuLoading", typeof(GameObject));
        GameObject objMenu = GameObject.Instantiate(prefab, GameObject.Find("UISpace/CanvasPopup").transform);
        MenuLoading box = objMenu.GetComponent<MenuLoading>();
        box.TitleText.text = LocaleManager.Inst.DoLocaleText(title, UserSetting.CurrentLang);
        box.MessageText.text = "";
        box.mIsLoaded = isLoaded;
        box.mOnClose = onClose;
        box.mTimeout = timeout;
        return box;
    }
    void Update()
    {
        mAccTime += Time.deltaTime;
        if (mIsLoaded() || mAccTime > mTimeout)
        {
            OnCloseLoading();
        }
        else
        {
            int dotCount = (int)mAccTime % mDots.Length;
            MessageText.text = mDots[dotCount];
        }
    }
    public void OnCloseLoading()
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton2);
        mOnClose?.Invoke();
        Destroy(gameObject);
    }

}
