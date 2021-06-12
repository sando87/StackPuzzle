using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuTermsAndConditions : MonoBehaviour
{
    private static MenuTermsAndConditions mInst = null;
    private const string UIObjName = "UISpace/CanvasPopup/TermsAndConditions";

    [SerializeField] private Button BtnAgree = null;
    [SerializeField] private Button BtnDeveloper = null;
    [SerializeField] private Button BtnClose = null;
    [SerializeField] private GameObject DeveloperView = null;

    private Action EventAgree = null; 

    public static MenuTermsAndConditions Inst()
    {
        if (mInst == null)
            mInst = GameObject.Find(UIObjName).GetComponent<MenuTermsAndConditions>();
        return mInst;
    }

    public static void PopUp(Action eventAgree = null)
    {
        Inst().gameObject.SetActive(true);
        Inst().EventAgree = eventAgree;
        Inst().UpdateUI();
    }

    public void UpdateUI()
    {
        if (UserSetting.IsTermsAgreement)
        {
            BtnAgree.gameObject.SetActive(false);
            BtnDeveloper.gameObject.SetActive(true);
            BtnClose.gameObject.SetActive(true);
            DeveloperView.SetActive(false);
        }
        else
        {
            BtnAgree.gameObject.SetActive(true);
            BtnDeveloper.gameObject.SetActive(false);
            BtnClose.gameObject.SetActive(false);
            DeveloperView.SetActive(false);
        }
    }

    public void OnAgree()
    {
        UserSetting.IsTermsAgreement = true;
        EventAgree?.Invoke();
        gameObject.SetActive(false);
    }

    public void OnDeveloper()
    {
        DeveloperView.SetActive(true);
    }

    public void OnClose()
    {
        gameObject.SetActive(false);
    }
    public void OnCloseDeveloper()
    {
        DeveloperView.SetActive(false);
    }

}
