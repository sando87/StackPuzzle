using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuItemShop : MonoBehaviour
{
    private const string UIObjName = "CanvasPopUp/MenuItemShop";

    public GameObject ItemStateParent;
    public GameObject ItemSlotPrefab;

    public static void PopUp()
    {
        GameObject objMenu = GameObject.Find("UIGroup").transform.Find(UIObjName).gameObject;
        objMenu.SetActive(true);
        objMenu.GetComponent<MenuItemShop>().Init();
    }

    public void Init()
    {
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

        bool ret = Purchases.ChargeItemUseGold(type.ToItemType(), cnt, cost);
        if (!ret)
            MenuInformBox.PopUp("Not enough Golds.");

        UpdateState();
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
    }
    public void OnChargeItem()
    {
        GameObject btnObj = EventSystem.current.currentSelectedGameObject;
        int type = int.Parse(btnObj.name.Replace("ItemType", ""));
        int cnt = int.Parse(btnObj.transform.Find("Text_Count").GetComponent<TextMeshProUGUI>().text);
        int cost = int.Parse(btnObj.transform.Find("Group_Cost/Text_Cost").GetComponent<TextMeshProUGUI>().text);

        bool ret = Purchases.ChargeItemUseDia(type.ToItemType(), cnt, cost);
        if (!ret)
            MenuInformBox.PopUp("Not enough Diamonds.");

        UpdateState();
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
    }
    private void UpdateState()
    {
        MenuStages.Inst.UpdateTopPanel();
        UpdateItemState();
    }

    private void UpdateItemState()
    {
        int count = System.Enum.GetValues(typeof(PurchaseItemType)).Length;
        Image[] itemSlots = ItemStateParent.GetComponentsInChildren<Image>();
        for (int i = 0; i < count; ++i)
        {
            PurchaseItemType type = i.ToItemType();
            GameObject obj = Instantiate(ItemSlotPrefab, ItemStateParent.transform);
            Image img = obj.GetComponentInChildren<Image>();
            img.sprite = type.GetSprite();
            img.GetComponentInChildren<TextMeshProUGUI>().text = type.GetCount().ToString();
        }
    }
}
