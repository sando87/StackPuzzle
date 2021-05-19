﻿using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuHeartShop : MonoBehaviour
{
    private const string UIObjName = "UISpace/CanvasPopup/LifeShop";

    public static void PopUp()
    {
        MenuHeartShop objMenu = GameObject.Find(UIObjName).GetComponent<MenuHeartShop>();
        objMenu.gameObject.SetActive(true);
        NetClientApp.GetInstance().IsKeepConnection = true;
    }
    public static void Hide()
    {
        MenuHeartShop objMenu = GameObject.Find(UIObjName).GetComponent<MenuHeartShop>();
        objMenu.gameObject.SetActive(false);
    }

    public void OnClose()
    {
        gameObject.SetActive(false);
        MenuStages.PopUp();
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton2);
        NetClientApp.GetInstance().IsKeepConnection = false;
    }
    
    public void OnChargeHeart()
    {
        GameObject btnObj = EventSystem.current.currentSelectedGameObject;
        int type = int.Parse(btnObj.name.Replace("ItemType", ""));

        //type0 : 영상 15초 : +5 life
        //type1 : 영상 60초 : max life
        //type2 : 다이아 5 : max life
        //type3 : 다이아 20 : infinite life

        if (type == 0)
        {
            if (NetClientApp.GetInstance().IsDisconnected())
            {
                MenuNetConnector.PopUp(() => OnChargeHeart());
                return;
            }

            //Call AD API
            //Invoke("OnChargeHeartFromVideo", 2);
            SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
            GoogleADMob.Inst.ShowRewardAd();
        }
        else if (type == 1)
        {
            if (NetClientApp.GetInstance().IsDisconnected())
            {
                MenuNetConnector.PopUp(() => OnChargeHeart());
                return;
            }

            //Call AD API
            Invoke("OnChargeHeartFromVideo", 4);
            SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
        }
        else if (type == 2)
        {
            int diamond = 5;
            SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
            MenuMessageBox.PopUp(diamond + " Diamonds are used.", true, (isOK) =>
            {
                if (isOK)
                {
                    if(Purchases.IsHeartMax())
                        MenuInformBox.PopUp("LIFE Max.");
                    else
                    {
                        if (Purchases.ChargeHeart(20, diamond))
                        {
                            MenuInformBox.PopUp("Success!!");

                            string log = "[Charge Heart] "
                            + "Dia:" + diamond + "/" + Purchases.CountDiamond()
                            + ", Heart:" + Purchases.CountHeart();
                            LOG.echo(log);
                        }
                        else
                            MenuInformBox.PopUp("Not enough Diamonds.");
                    }

                    MenuStages.Inst.UpdateTopPanel();
                }
            });
        }
        else if (type == 3)
        {
            int diamond = 20;
            SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
            MenuMessageBox.PopUp(diamond + " Diamonds are used.", true, (isOK) =>
            {
                if (isOK)
                {
                    if (Purchases.ChargeHeartInfinite(diamond))
                    {
                        MenuInformBox.PopUp("Success!!");

                        string log = "[Charge Heart] "
                        + "Dia:" + diamond + "/" + Purchases.CountDiamond()
                        + ", Heart:" + (Purchases.IsInfinite() ? "Infinite" : Purchases.CountHeart().ToString());
                        LOG.echo(log);
                    }
                    else
                    {
                        if (Purchases.IsInfinite())
                            MenuInformBox.PopUp("Already Infinite Mode.");
                        else
                            MenuInformBox.PopUp("Not enough Diamonds.");
                    }

                    MenuStages.Inst.UpdateTopPanel();
                }
            });
        }
    }

    private void OnChargeHeartFromVideo()
    {
        int sec = 15;
        switch (sec)
        {
            case 15: Purchases.ChargeHeart(5, 0); break;
            case 60: Purchases.ChargeHeart(20, 0); break;
            default: break;
        }

        MenuStages.Inst.UpdateTopPanel();
        MenuInformBox.PopUp("finished playing video ");

        string log = "[Charge Heart] "
        + "Heart:" + sec + "/" + Purchases.CountHeart();
        LOG.echo(log);
    }
}
