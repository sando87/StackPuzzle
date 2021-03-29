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
    private Button[] SelectedItems = new Button[3];
    private int CurrentSelectItemIndex = 0;

    public TextMeshProUGUI StageLevel;
    public TextMeshProUGUI TargetValue;
    public TextMeshProUGUI TargetScore;
    public Image TargetType;
    public GameObject ItemSlots;
    public Button ItemSlotPrefab;

    public static void PopUp(StageInfo info)
    {
        GameObject menuPlay = GameObject.Find(UIObjName);
        MenuPlay menu = menuPlay.GetComponent<MenuPlay>();
        menu.UpdateUIState(info);
        menuPlay.SetActive(true);
        StageManager.Inst.gameObject.SetActive(false);

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
    }

    public void OnClose()
    {
        gameObject.SetActive(false);
        StageManager.Inst.gameObject.SetActive(true);
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

        SoundPlayer.Inst.Player.Stop();
        SoundPlayer.Inst.PlayBackMusic(SoundPlayer.Inst.BackMusicInGame);
        InGameManager.InstStage.StartGameInStageMode(mStageInfo, UserSetting.UserInfo);
        InGameManager.InstStage.InitProducts();
        MenuInGame.PopUp(mStageInfo);
        MenuStages.Hide();
        StageManager.Inst.Activate(false);
        gameObject.SetActive(false);

        if (!UserSetting.IsBotPlayer)
            Purchases.UseHeart();
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

    private void CreateItemButtons(PurchaseItemType[] items)
    {
        Button[] btns = ItemSlots.GetComponentsInChildren<Button>();
        foreach (Button btn in btns)
            Destroy(btn.gameObject);

        foreach (PurchaseItemType item in items)
        {
            Button slot = Instantiate(ItemSlotPrefab, ItemSlots.transform);
            slot.name = item.ToInt().ToString();
            slot.GetComponent<Image>().sprite = item.GetSprite();
            slot.onClick.AddListener(OnClickItem);
        }
    }

    private void OnClickItem()
    {
        Button selBtn = EventSystem.current.currentSelectedGameObject.GetComponent<Button>();
        SelectedItems[CurrentSelectItemIndex] = selBtn;
        CurrentSelectItemIndex = (CurrentSelectItemIndex + 1) % SelectedItems.Length;
        UpdateItemButton();
    }

    private void UpdateItemButton()
    {
        Button[] btns = ItemSlots.GetComponentsInChildren<Button>();
        foreach (Button btn in btns)
        {
            //Off
        }

        foreach (Button btn in SelectedItems)
        {
            //On
        }
    }

    private PurchaseItemType[] GetSelectedItems()
    {
        List<PurchaseItemType> rets = new List<PurchaseItemType>();
        foreach (Button btn in SelectedItems)
        {
            if (btn == null)
                continue;

            PurchaseItemType item = int.Parse(btn.name).ToItemType();
            rets.Add(item);
        }
        return rets.ToArray();
    }

}
