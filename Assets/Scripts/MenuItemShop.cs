using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuItemShop : MonoBehaviour
{
    private const string UIObjName = "CanvasPopUp/MenuItemShop";

    public Text CurrentDiamond;
    public Text CurrentItemA;
    public Text CurrentItemB;
    public Text CurrentItemC;
    public Text CurrentItemD;

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
    public void OnChargeItem(int type, int cnt, int diamond)
    {
        Purchases.ChargeItem(type, cnt, diamond);
        int currentItemCount = Purchases.CountItem(type);
        switch(type)
        {
            case 0: CurrentItemA.text = currentItemCount.ToString(); break;
            case 1: CurrentItemB.text = currentItemCount.ToString(); break;
            case 2: CurrentItemC.text = currentItemCount.ToString(); break;
            case 3: CurrentItemD.text = currentItemCount.ToString(); break;
            default: break;
        }
        CurrentDiamond.text = Purchases.CountDiamond().ToString();
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
    }
}
