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
        OnPlay();
    }
    public void UpdateUIState(StageInfo info)
    {
        int starCount = UserSetting.GetStageStarCount(info.Num);
        mStageInfo = info;
        StageLevel.text = "STAGE " + info.Num.ToString();
        if(info.GoalTypeEnum == StageGoalType.Score)
        {
            TargetType.gameObject.SetActive(false);
            TargetScore.gameObject.SetActive(true);
            TargetScore.text = "Score " + info.GoalValue.ToString();
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

        SoundPlayer.Inst.PlayerBack.Stop();
        SoundPlayer.Inst.PlayBackMusic(SoundPlayer.Inst.BackMusicInGame);
        InGameManager.InstStage.StartGameInStageMode(mStageInfo, UserSetting.UserInfo);
        InGameManager.InstStage.InitProducts();
        MenuInGame.PopUp(mStageInfo);
        MenuStages.Hide();
        gameObject.SetActive(false);

        if (!UserSetting.IsBotPlayer)
        {
            Purchases.UseHeart();

            string log = "[STAGE Start] " + "Stage:" + mStageInfo.Num + ", HeartCount:" + Purchases.CountHeart();
            LOG.echo(log);
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
            if(btn.GetItem().GetCount() > 0)
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
            curBtn.SetItem(item);
        });
    }

}
