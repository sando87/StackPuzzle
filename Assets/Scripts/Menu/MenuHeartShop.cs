using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuHeartShop : MonoBehaviour
{
    private const string UIObjName = "UISpace/CanvasPopup/LifeShop";
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
        //type1 : 영상 60초 : max life
        //type2 : 다이아 5 : max life
        //type3 : 다이아 20 : infinite life

        if (type == 0)
        {
            if (!NetClientApp.GetInstance().IsNetworkAlive)
            {
                MenuMessageBox.PopUp("Network NotReachable.", false, null);
                return;
            }

            if (GoogleADMob.Inst.RemainSec(AdsType.ChargeLifeA) > 0)
            {
                MenuMessageBox.PopUp("Ad Not Ready.", false, null);
                return;
            }

            if (!GoogleADMob.Inst.IsLoaded(AdsType.ChargeLifeA))
            {
                MenuMessageBox.PopUp("Ad was requested.\nPlease try again in a while.", false, null);
                return;
            }

            SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
            LOG.echo("[Request Charge Heart] " + AdsType.ChargeLifeA + "/" + Purchases.CountHeart());
            GoogleADMob.Inst.Show(AdsType.ChargeLifeA, (rewardSuccess) =>
            {
                if(rewardSuccess)
                    OnChargeHeartFromVideo(AdsType.ChargeLifeA);
            });
        }
        else if (type == 1)
        {
            if (!NetClientApp.GetInstance().IsNetworkAlive)
            {
                MenuMessageBox.PopUp("Network NotReachable.", false, null);
                return;
            }

            if (GoogleADMob.Inst.RemainSec(AdsType.ChargeLifeB) > 0)
            {
                MenuMessageBox.PopUp("Ad Not Ready.", false, null);
                return;
            }

            if (!GoogleADMob.Inst.IsLoaded(AdsType.ChargeLifeB))
            {
                MenuMessageBox.PopUp("Ad was requested.\nPlease try again in a while.", false, null);
                return;
            }

            SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
            LOG.echo("[Request Charge Heart] " + AdsType.ChargeLifeB + "/" + Purchases.CountHeart());
            GoogleADMob.Inst.Show(AdsType.ChargeLifeB, (rewardSuccess) =>
            {
                if (rewardSuccess)
                    OnChargeHeartFromVideo(AdsType.ChargeLifeB);
            });
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

    private void OnChargeHeartFromVideo(AdsType type)
    {
        switch (type)
        {
            case AdsType.ChargeLifeA: Purchases.ChargeHeart(5, 0); break;
            case AdsType.ChargeLifeB: Purchases.ChargeHeart(20, 0); break;
            default: break;
        }

        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectRewards);
        MenuStages.Inst.UpdateTopPanel();
        MenuInformBox.PopUp("finished playing video ");

        LOG.echo("[Response Charge Heart] " + type + "/" + Purchases.CountHeart());
    }

    private IEnumerator TimerCount()
    {
        double remainSec = 0;
        while (true)
        {
            remainSec = GoogleADMob.Inst.RemainSec(AdsType.ChargeLifeA);
            AdsRewardA.text = remainSec <= 0 ? "15m" : MenuBattle.TimeToString((int)remainSec);

            remainSec = GoogleADMob.Inst.RemainSec(AdsType.ChargeLifeB);
            AdsRewardB.text = remainSec <= 0 ? "60m" : MenuBattle.TimeToString((int)remainSec);

            yield return new WaitForSeconds(1);
        }
    }
}
