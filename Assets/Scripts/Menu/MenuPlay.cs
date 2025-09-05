using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuPlay : MonoBehaviour
{
    private const string UIObjName = "UISpace/CanvasPopup/StageDetail";
    private StageInfo mStageInfo;

    public TextMeshProUGUI StageLevel;
    public TextMeshProUGUI TargetValue;
    public TextMeshProUGUI TargetScore;
    public Image TargetType;
    public RewardUISet _RewardUISet;

    public static MenuPlay Inst()
    {
        return GameObject.Find(UIObjName).GetComponent<MenuPlay>();
    }
    public static void PopUp(StageInfo info)
    {
        GameObject menuPlay = GameObject.Find(UIObjName);
        menuPlay.SetActive(true);
        MenuPlay menu = menuPlay.GetComponent<MenuPlay>();
        menu.UpdateUIState(info);

        if (UserSetting.IsBotPlayer)
            menu.StartCoroutine(menu.AutoStart());
    }
    IEnumerator AutoStart()
    {
        yield return new WaitForSeconds(1);
        // ItemButton[] btns = GetComponentsInChildren<ItemButton>();
        // btns[0].SetItem((PurchaseItemType)(UnityEngine.Random.Range(0, 2) + 1));
        // btns[1].SetItem((PurchaseItemType)(UnityEngine.Random.Range(0, 2) + 3));
        // btns[2].SetItem((PurchaseItemType)(UnityEngine.Random.Range(0, 2) + 5));
        OnPlay();
    }
    public void UpdateUIState(StageInfo info)
    {
        int starCount = UserSetting.GetStageStarCount(info.Num);
        mStageInfo = info;
        StageLevel.text = string.Format(LocaleManager.Inst.DoLocaleText("STAGE {0}", UserSetting.CurrentLang), info.Num);
        if (info.GoalTypeEnum == StageGoalType.Score)
        {
            TargetType.gameObject.SetActive(false);
            TargetScore.gameObject.SetActive(true);
            TargetScore.text = string.Format(LocaleManager.Inst.DoLocaleText("Score {0}", UserSetting.CurrentLang), info.GoalValue);
        }
        else
        {
            TargetScore.gameObject.SetActive(false);
            TargetType.gameObject.SetActive(true);
            TargetType.sprite = info.GoalTypeImage;
            TargetValue.text = info.GoalValue.ToString();
        }


        ItemButton[] btns = GetComponentsInChildren<ItemButton>();
        foreach (ItemButton btn in btns)
        {
            btn.UpdateItem();
        }

        _RewardUISet.UpdateToReady(mStageInfo);
    }

    public void OnClose()
    {
        gameObject.SetActive(false);
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton2);
    }

    public void OnPlay()
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
        if (Purchases.CountHeart() <= 0)
        {
            MenuMessageBox.PopUp("No Life", false, null);
            return;
        }

        SoundPlayer.Inst.StopBackMusic();
        SoundPlayer.Inst.PlayBackMusic(SoundPlayer.Inst.BackMusicInGameStage);
        InGameManager.InstStage.StartGameInStageMode(mStageInfo, UserSetting.UserInfo);
        InGameManager.InstStage.InitProducts();
        MenuInGame.PopUp(mStageInfo);
        MenuStages.Hide();
        gameObject.SetActive(false);

        if (!UserSetting.IsBotPlayer)
        {
            Purchases.UseHeart();

            string log = "StageStart," + UserSetting.SessionID + "," + mStageInfo.Num + "," + Purchases.CountHeart();
            LOG.trace(log);

            LogToGoogleForms.Instance.LogStageStart(UserSetting.SessionID, mStageInfo.Num);
        }
    }

    private PurchaseItemType[] ScanOwnedItems()
    {
        List<PurchaseItemType> rets = new List<PurchaseItemType>();
        var items = System.Enum.GetValues(typeof(PurchaseItemType));
        foreach (PurchaseItemType item in items)
        {
            if (item.GetCount() > 0)
                rets.Add(item);
        }
        return rets.ToArray();
    }

    private bool IsChecked(Button btn)
    {
        return btn.transform.GetChild(0).GetChild(0).gameObject.activeSelf;
    }
    private void SetCheck(Button btn, bool isCheck)
    {
        btn.transform.GetChild(0).GetChild(0).gameObject.SetActive(isCheck);
    }

    public PurchaseItemType[] GetSelectedItems()
    {
        List<PurchaseItemType> rets = new List<PurchaseItemType>();
        ItemButton[] btns = GetComponentsInChildren<ItemButton>();
        foreach (ItemButton btn in btns)
        {
            if (btn.GetItem().GetCount() > 0)
                rets.Add(btn.GetItem());
        }
        return rets.ToArray();
    }

    public void OnChangeItem()
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
        ItemButton curBtn = EventSystem.current.currentSelectedGameObject.GetComponent<ItemButton>();
        MenuItemSelector.PopUp((item) =>
        {
            UnSelectSameItem(item);
            curBtn.SetItem(item);
        });
    }

    void UnSelectSameItem(PurchaseItemType itemType)
    {
        ItemButton[] btns = GetComponentsInChildren<ItemButton>();
        foreach (ItemButton btn in btns)
        {
            if (btn.GetItem() == itemType)
            {
                btn.SetItem(PurchaseItemType.None);
            }
        }
    }

}
