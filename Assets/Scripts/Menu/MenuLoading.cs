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

    private Func<bool> mCheckLoading;
    private Action mOnClose;
    private string[] mDots = { ".", "..", "..." };
    private float mAccTime = 0;
    private float mTimeout = 0;

    public static MenuLoading PopUp(string title, float timeout, Func<bool> checkLoading, Action onClose)
    {
        GameObject prefab = (GameObject)Resources.Load("Prefabs/MenuLoading", typeof(GameObject));
        GameObject objMenu = GameObject.Instantiate(prefab, GameObject.Find("UISpace/CanvasPopup").transform);
        MenuLoading box = objMenu.GetComponent<MenuLoading>();
        box.TitleText.text = title;
        box.MessageText.text = "";
        box.mCheckLoading = checkLoading;
        box.mOnClose = onClose;
        box.mTimeout = timeout;
        return box;
    }
    void Update()
    {
        mAccTime += Time.deltaTime;
        if (mCheckLoading() || mAccTime > mTimeout)
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
