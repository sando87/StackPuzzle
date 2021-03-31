using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuHeartShop : MonoBehaviour
{
    private const string UIObjName = "CanvasPopUp/LifeShop";

    public static void PopUp()
    {
        GameObject objMenu = GameObject.Find("UIGroup").transform.Find(UIObjName).gameObject;
        objMenu.SetActive(true);
    }

    public void OnClose()
    {
        gameObject.SetActive(false);
        MenuStages.PopUp();
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton2);
    }
    
    public void OnChargeHeart()
    {
        GameObject btnObj = EventSystem.current.currentSelectedGameObject;
        int type = int.Parse(btnObj.name.Replace("ItemType", ""));

        //type0 : 영상 15초 : +5 life
        //type1 : 영상 60초 : max life
        //type2 : 다이아 5 : +20 life
        //type3 : 다이아 20 : infinite life

        bool ret = false;
        if (type == 0)
        {
            ret = Purchases.ChargeHeartLimit(5);
        }
        else if (type == 1)
        {
            ret = Purchases.ChargeHeartLimit(20);
        }
        else if (type == 2)
        {
            ret = Purchases.ChargeHeart(20, 5);
        }
        else if (type == 3)
        {
            ret = Purchases.ChargeHeartInfinite();
        }

        if (!ret)
            MenuInformBox.PopUp("Not enough Diamonds.");

        MenuStages.Inst.UpdateTopPanel();
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
    }

    public void OnChargeHeartFromVideo(int sec)
    {
        switch (sec)
        {
            case 15: Purchases.ChargeHeartLimit(5); break;
            case 60: Purchases.ChargeHeartLimit(20); break;
            default: break;
        }

        MenuStages.Inst.UpdateTopPanel();
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
    }
}
