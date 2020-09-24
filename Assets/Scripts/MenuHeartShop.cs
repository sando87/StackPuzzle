using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuHeartShop : MonoBehaviour
{
    private const string UIObjName = "CanvasPopUp/MenuHeartShop";

    public Text CurrentHeart;
    public Text CurrentDiamond;

    public static void PopUp()
    {
        GameObject objMenu = GameObject.Find("UIGroup").transform.Find(UIObjName).gameObject;
        objMenu.SetActive(true);
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
    }

    public void OnClose()
    {
        gameObject.SetActive(false);
        MenuStages.PopUp();
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
        CurrentHeart.text = Purchases.CountHeart().ToString();
        CurrentDiamond.text = Purchases.CountDiamond().ToString();
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
    }
    public void OnChargeHeartFromDiamond(int diamond)
    {
        switch (diamond)
        {
            case 10: Purchases.ChargeHeart(20, diamond); break;
            case 20: Purchases.ChargeHeart(50, diamond); break;
            case 100: Purchases.ChargeHeartInfinite(); break;
            default: break;
        }
        CurrentHeart.text = Purchases.CountHeart().ToString();
        CurrentDiamond.text = Purchases.CountDiamond().ToString();
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
    }
}
