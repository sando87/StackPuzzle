using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuStages : MonoBehaviour
{
    private const string UIObjName = "UISpace/CanvasPopup/Stages";
    private static MenuStages mInst = null;

    public Image HeartTimer;
    public TextMeshProUGUI HeartCount;
    public TextMeshProUGUI DiamondCount;
    public TextMeshProUGUI GoldCount;

    public GameObject StatusGroup;
    public GameObject BackButton;
    public GameObject BottomGroup;

    public GameObject BackgroundStageField;

    private int mAutoNextStageNum = 1;


    private MenuMessageBox mMenu = null;

    private void Start()
    {
        int highestStageNum = UserSetting.GetHighestStageNumber();
        SetViewToStage(highestStageNum);
    }
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
        Inst.gameObject.SetActive(true);
        Inst.StopCoroutine("UpdateHeartTimer");
        Inst.StartCoroutine("UpdateHeartTimer");
        Inst.StatusGroup.SetActive(true);
        Inst.BackButton.SetActive(true);
        Inst.BottomGroup.SetActive(true);
        Inst.BackgroundStageField.SetActive(true);
    }
    public static void Hide()
    {
        Inst.gameObject.SetActive(false);
    }
    public static void HideHalf()
    {
        Inst.StatusGroup.SetActive(true);
        Inst.BackButton.SetActive(false);
        Inst.BottomGroup.SetActive(false);
        Inst.BackgroundStageField.SetActive(false);
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
        MenuGoldShop.Hide();
        MenuDiamondShop.Hide();
        MenuItemShop.Hide();
        HideHalf();
    }
    public void OnShopGold()
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
        MenuGoldShop.PopUp();
        MenuHeartShop.Hide();
        MenuDiamondShop.Hide();
        MenuItemShop.Hide();
        HideHalf();
    }
    public void OnShopDiamond()
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
        MenuDiamondShop.PopUp();
        MenuHeartShop.Hide();
        MenuGoldShop.Hide();
        MenuItemShop.Hide();
        HideHalf();
    }
    public void OnShopItem()
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
        MenuItemShop.PopUp();
        MenuHeartShop.Hide();
        MenuGoldShop.Hide();
        MenuDiamondShop.Hide();
        HideHalf();
    }
    public void OnInventory()
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
        MenuInventory.PopUp();
        Hide();
    }
    public void OnSettings()
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
        MenuSettings.PopUp();
        Hide();
    }
    public void OnBattle()
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
        MenuWaitMatch.PopUp();
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

    public void UpdateTopPanel()
    {
        HeartCount.text = Purchases.CountHeart().ToString();
        GoldCount.text = Purchases.CountGold().ToString();
        DiamondCount.text = Purchases.CountDiamond().ToString();
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

    public MapStage FindStage(int number)
    {
        ScrollRect sr = BackgroundStageField.GetComponentInChildren<ScrollRect>();
        MapStage[] stages = sr.content.GetComponentsInChildren<MapStage>();
        foreach (MapStage stage in stages)
            if (stage.Number == number)
                return stage;
        return null;
    }
    public void SetViewToStage(int number)
    {
        MapStage stage = FindStage(number);
        ScrollRect sr = BackgroundStageField.GetComponentInChildren<ScrollRect>();
        float lowY = sr.content.GetChild(0).transform.position.y;
        float highY = sr.content.GetChild(sr.content.transform.childCount - 1).transform.position.y;
        float value = (stage.transform.position.y - lowY) / (highY - lowY);
        sr.verticalNormalizedPosition = value;
    }
}
