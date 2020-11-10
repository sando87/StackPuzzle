using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuStages : MonoBehaviour
{
    private const string UIObjName = "MenuStages";
    private static MenuStages mInst = null;

    public Text HeartTimer;
    public Text HeartCount;
    public Text DiamondCount;
    private int mAutoNextStageNum = 1;

    private void Update()
    {
#if PLATFORM_ANDROID
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnClose();
        }
#endif
    }
    public static MenuStages Inst
    {
        get
        {
            if (mInst == null)
                mInst = GameObject.Find("UIGroup").transform.Find(UIObjName).GetComponent<MenuStages>();
            return mInst;
        }
    }
    public static void PopUp()
    {
        GameObject.Find("WorldSpace").transform.Find("StageScreen").gameObject.SetActive(true);

        Inst.gameObject.SetActive(true);
        Inst.StopCoroutine("UpdateHeartTimer");
        Inst.StartCoroutine("UpdateHeartTimer");

        if(UserSetting.IsBotPlayer)
            Inst.StartCoroutine(Inst.AutoStartNextStage());
    }
    public static void Hide()
    {
        Inst.gameObject.SetActive(false);
        GameObject.Find("WorldSpace").transform.Find("StageScreen").gameObject.SetActive(false);
    }

    IEnumerator AutoStartNextStage()
    {
        yield return new WaitForSeconds(1);
        StageInfo stageInfo = StageInfo.Load(mAutoNextStageNum);
        if (stageInfo == null)
        {
            mAutoNextStageNum = 1;
            stageInfo = StageInfo.Load(mAutoNextStageNum);
        }

        MenuPlay.PopUp(stageInfo);
        mAutoNextStageNum++;
    }

    public void OnClose()
    {
        SoundPlayer.Inst.Player.Stop();
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton2);
        Inst.gameObject.SetActive(false);
        GameObject.Find("WorldSpace").transform.Find("StageScreen").gameObject.SetActive(false);
        MenuMain.PopUp();
    }
    public void OnShopHeart()
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
        MenuHeartShop.PopUp();
        Hide();
    }
    public void OnShopDiamond()
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
        MenuDiamondShop.PopUp();
        Hide();
    }
    public void OnSettings()
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
        MenuSettings.PopUp();
        Hide();
    }
    public void OnShopItem()
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
        MenuItemShop.PopUp();
        Hide();
    }
    IEnumerator UpdateHeartTimer()
    {
        while(true)
        {
            Purchases.UpdateHeartTimer();
            int remainSec = Purchases.RemainSeconds();
            int remainLife = Purchases.CountHeart();
            HeartCount.text = remainLife.ToString();
            if (Purchases.MaxHeart())
            {
                HeartTimer.text = "Full";
            }
            else
            {
                int min = remainSec / 60;
                int sec = remainSec % 60;
                string secStr = string.Format("{0:D2}", sec);
                HeartTimer.text = min + ":" + secStr;
            }
            DiamondCount.text = Purchases.CountDiamond().ToString();
            yield return new WaitForSeconds(1);
        }
    }
}
