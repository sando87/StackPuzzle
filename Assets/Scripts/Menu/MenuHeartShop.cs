using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuHeartShop : MonoBehaviour
{
    private const string UIObjName = "UISpace/CanvasGameUI/LifeShop";
    public TextMeshProUGUI AdsRewardA;
    public TextMeshProUGUI AdsRewardB;

    public static void PopUp()
    {
        MenuHeartShop objMenu = GameObject.Find(UIObjName).GetComponent<MenuHeartShop>();
        objMenu.gameObject.SetActive(true);
        objMenu.StartCoroutine(objMenu.TimerCount());
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
    }
    
    public void OnChargeHeart()
    {
        GameObject btnObj = EventSystem.current.currentSelectedGameObject;
        int type = int.Parse(btnObj.name.Replace("ItemType", ""));

        //type0 : 영상 15초 : +5 life
        //type1 : 골드 850 : +5 life
        //type2 : 다이아 3 : +20 life
        //type3 : 다이아 120 : infinite life

        if (type == 0)
        {
            if (!NetClientApp.GetInstance().IsNetworkAlive)
            {
                MenuMessageBox.PopUp("Network NotReachable", false, null);
                return;
            }

            if (GoogleADMob.Inst.RemainSec(AdsType.ChargeLifeA) > 0)
            {
                MenuMessageBox.PopUp("Ad Not Ready", false, null);
                return;
            }

            if (!GoogleADMob.Inst.IsLoaded(AdsType.ChargeLifeA))
            {
                MenuMessageBox.PopUp("Ad Not Ready", false, null);
                return;
            }

            SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
            GoogleADMob.Inst.Show(AdsType.ChargeLifeA, (rewardSuccess) =>
            {
                if(rewardSuccess)
                    OnChargeHeartFromVideo(AdsType.ChargeLifeA);
            });
        }
        else if (type == 1)
        {
            int gold = 850;
            int lifeCount = 5;
            SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
            
            if (Purchases.IsHeartMax())
                MenuInformBox.PopUp("LIFE Max");
            else
            {
                if (Purchases.ChargeHeartWithGold(lifeCount, gold))
                {
                    MenuInformBox.PopUp("Success");
                }
                else
                {
                    MenuInformBox.PopUp("Not enough golds");
                }
            }

            MenuStages.Inst.UpdateTopPanel();
        }
        else if (type == 2)
        {
            int diamond = 3;
            int lifeCount = 20;
            SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
            string msg = string.Format(LocaleManager.Inst.DoLocaleText("{0} diamonds will be consumed", UserSetting.CurrentLang), diamond);
            MenuMessageBox.PopUp(msg, true, (isOK) =>
            {
                if (isOK)
                {
                    if(Purchases.IsHeartMax())
                        MenuInformBox.PopUp("LIFE Max");
                    else
                    {
                        if (Purchases.ChargeHeartWithDia(lifeCount, diamond))
                        {
                            MenuInformBox.PopUp("Success");
                        }
                        else
                        {
                            MenuInformBox.PopUp("Not enough diamonds");
                        }
                    }

                    MenuStages.Inst.UpdateTopPanel();
                }
            });
        }
        else if (type == 3)
        {
            int diamond = 120;
            SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
            string msg = string.Format(LocaleManager.Inst.DoLocaleText("{0} diamonds will be consumed", UserSetting.CurrentLang), diamond);
            MenuMessageBox.PopUp(msg, true, (isOK) =>
            {
                if (isOK)
                {
                    if (Purchases.ChargeHeartInfinite(diamond))
                    {
                        MenuInformBox.PopUp("Success");
                    }
                    else
                    {
                        if (Purchases.IsInfinite())
                            MenuInformBox.PopUp("Already Infinite Mode");
                        else
                            MenuInformBox.PopUp("Not enough diamonds");
                    }

                    MenuStages.Inst.UpdateTopPanel();
                }
            });
        }
    }

    private void OnChargeHeartFromVideo(AdsType type)
    {
        switch (type)
        {
            case AdsType.ChargeLifeA: Purchases.ChargeHeartWithDia(5, 0); break;
            // case AdsType.ChargeLifeB: Purchases.ChargeHeart(20, 0); break;
            default: break;
        }

        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectRewards);
        MenuStages.Inst.UpdateTopPanel();
        MenuInformBox.PopUp("Lifes are rewarded");
    }

    private IEnumerator TimerCount()
    {
        double remainSec = 0;
        while (true)
        {
            remainSec = GoogleADMob.Inst.RemainSec(AdsType.ChargeLifeA);
            AdsRewardA.text = remainSec <= 0 ? "60:00" : MenuBattle.TimeToString((int)remainSec);

            // remainSec = GoogleADMob.Inst.RemainSec(AdsType.ChargeLifeB);
            // AdsRewardB.text = remainSec <= 0 ? "60:00" : MenuBattle.TimeToString((int)remainSec);

            yield return new WaitForSeconds(1);
        }
    }
}
