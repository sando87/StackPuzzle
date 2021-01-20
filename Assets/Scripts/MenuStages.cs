using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuStages : MonoBehaviour
{
    private const string UIObjName = "UISpace/CanvasPanel/Stages";
    private static MenuStages mInst = null;

    public Image HeartTimer;
    public TextMeshProUGUI HeartCount;
    public TextMeshProUGUI DiamondCount;
    public TextMeshProUGUI GoldCount;
    private int mAutoNextStageNum = 1;


    private MenuMessageBox mMenu = null;

    private void Update()
    {
        QuitProgram();
    }
    public static MenuStages Inst
    {
        get
        {
            if (mInst == null)
                mInst = GameObject.Find(UIObjName).GetComponent<MenuStages>();
            return mInst;
        }
    }
    public static void PopUp()
    {
        GameObject.Find("WorldSpace").transform.Find("StageScreen").gameObject.SetActive(true);

        Inst.gameObject.SetActive(true);
        //Inst.StopCoroutine("UpdateHeartTimer");
        //Inst.StartCoroutine("UpdateHeartTimer");
    }
    public static void Hide()
    {
        Inst.gameObject.SetActive(false);
        GameObject.Find("WorldSpace").transform.Find("StageScreen").gameObject.SetActive(false);
    }


    public void AutoStartAfterSec(float second)
    {
        Invoke("AutoStartNextStage", second);
    }
    private void AutoStartNextStage()
    {
        StageInfo stageInfo = StageInfo.Load(mAutoNextStageNum);
        if (stageInfo == null)
        {
            mAutoNextStageNum = 1;
            stageInfo = StageInfo.Load(mAutoNextStageNum);
        }

        MenuPlay.PopUp(stageInfo);
        mAutoNextStageNum++;
    }

    public void OnExit()
    {
        MenuMessageBox.PopUp("Quit??", true, (bool isOK) =>
        {
            if (isOK)
                Application.Quit();
            else
                mMenu = null;
        });
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
                HeartTimer.fillAmount = 1;
            }
            else
            {
                //int min = remainSec / 60;
                //int sec = remainSec % 60;
                //string secStr = string.Format("{0:D2}", sec);
                //HeartTimer.text = min + ":" + secStr;
                float rate = remainSec / (UserSetting.HeartChargingIntervalMin * 60.0f);
                HeartTimer.fillAmount = rate;
            }
            GoldCount.text = Purchases.CountGold().ToString();
            DiamondCount.text = Purchases.CountDiamond().ToString();
            yield return new WaitForSeconds(1);
        }
    }

    private void QuitProgram()
    {
#if PLATFORM_ANDROID
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (mMenu == null)
            {
                mMenu = MenuMessageBox.PopUp("Quit??", true, (bool isOK) =>
                {
                    if (isOK)
                        Application.Quit();
                    else
                        mMenu = null;
                });
            }
            else
            {
                Application.Quit();
            }
        }
#endif
    }
}
