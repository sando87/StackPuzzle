using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuItemShop : MonoBehaviour
{
    private const string UIObjName = "UISpace/CanvasPopup/ItemShop";

    public GameObject ItemStateParent;
    public GameObject ItemSlotPrefab;
    private List<TextMeshProUGUI> ItemCounts = new List<TextMeshProUGUI>();

    public static void PopUp()
    {
        GameObject objMenu = GameObject.Find(UIObjName);
        objMenu.SetActive(true);
        objMenu.GetComponent<MenuItemShop>().Init();
    }
    public static void Hide()
    {
        GameObject objMenu = GameObject.Find(UIObjName);
        objMenu.SetActive(false);
    }

    public void Init()
    {
        if(ItemCounts.Count <= 0)
        {
            int count = System.Enum.GetValues(typeof(PurchaseItemType)).Length;
            for (int i = 0; i < count; ++i)
            {
                PurchaseItemType type = i.ToItemType();
                GameObject obj = Instantiate(ItemSlotPrefab, ItemStateParent.transform);
                obj.transform.GetChild(0).GetComponent<Image>().sprite = type.GetSprite();
                ItemCounts.Add(obj.GetComponentInChildren<TextMeshProUGUI>());
            }
            ItemCounts[0].transform.parent.gameObject.SetActive(false);
        }
        UpdateState();
    }
    public void OnClose()
    {
        gameObject.SetActive(false);
        MenuStages.PopUp();
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton2);
    }
    public void OnChargeItemUseGold()
    {
        GameObject btnObj = EventSystem.current.currentSelectedGameObject;
        int type = int.Parse(btnObj.name.Replace("ItemType", ""));
        string itemCountText = btnObj.transform.Find("Text_Count").GetComponent<TextMeshProUGUI>().text;
        int cnt = int.Parse(itemCountText.Replace("x", ""));
        int cost = int.Parse(btnObj.transform.Find("Group_Cost/Text_Cost").GetComponent<TextMeshProUGUI>().text);

        if (Purchases.ChargeItemUseGold(type.ToItemType(), cnt, cost))
        {
            SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectCashGold);
            MenuInformBox.PopUp("Success.", 0.8f);
        }
        else
        {
            SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectWrongMatched);
            MenuInformBox.PopUp("Not enough Golds.");
        }

        UpdateState();
    }
    public void OnChargeItem()
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
        GameObject btnObj = EventSystem.current.currentSelectedGameObject;
        int type = int.Parse(btnObj.name.Replace("ItemType", ""));
        string itemCountText = btnObj.transform.Find("Text_Count").GetComponent<TextMeshProUGUI>().text;
        int cnt = int.Parse(itemCountText.Replace("x", ""));
        int cost = int.Parse(btnObj.transform.Find("Group_Cost/Text_Cost").GetComponent<TextMeshProUGUI>().text);

        MenuMessageBox.PopUp(cost + " Diamonds are used.", true, (isOK) =>
        {
            if(isOK)
            {
                if(Purchases.ChargeItemUseDia(type.ToItemType(), cnt, cost))
                {
                    MenuInformBox.PopUp("Success.", 0.8f);

                    string log = "[Purchase Item] "
                    + "ItemType:" + type
                    + ", Count:" + cnt
                    + ", Cost:" + cost
                    + ", Current:" + type.ToItemType().GetCount();
                    LOG.echo(log);
                }
                else
                    MenuInformBox.PopUp("Not enough Diamonds.");

                UpdateState();
            }
        });
    }
    private void UpdateState()
    {
        MenuStages.Inst.UpdateTopPanel();
        UpdateItemState();
    }

    private void UpdateItemState()
    {
        int count = System.Enum.GetValues(typeof(PurchaseItemType)).Length;
        for (int i = 0; i < count; ++i)
        {
            PurchaseItemType type = i.ToItemType();
            ItemCounts[i].text = type.GetCount().ToString();
        }
    }
}
