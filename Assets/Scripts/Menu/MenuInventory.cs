using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuInventory : MonoBehaviour
{
    private const string UIObjName = "UISpace/CanvasPopup/Inventory";

    public GameObject ItemSlots;
    public GameObject ItemSlotPrefab;
    public Image ItemImage;
    public TextMeshProUGUI ItemName;
    public TextMeshProUGUI ItemDescription;

    private List<TextMeshProUGUI> ItemCounts = new List<TextMeshProUGUI>();

    public static void PopUp()
    {
        GameObject objMenu = GameObject.Find(UIObjName);
        objMenu.SetActive(true);
        objMenu.GetComponent<MenuInventory>().Init();
    }

    public void Init()
    {
        if (ItemCounts.Count <= 0)
        {
            int count = System.Enum.GetValues(typeof(PurchaseItemType)).Length;
            for (int i = 0; i < count; ++i)
            {
                PurchaseItemType type = i.ToItemType();
                GameObject obj = Instantiate(ItemSlotPrefab, ItemSlots.transform);
                obj.name = i.ToString();
                obj.transform.GetChild(0).GetComponentInChildren<Image>().sprite = type.GetSprite();
                obj.GetComponentInChildren<Button>().onClick.AddListener(UpdateItemDetail);
                ItemCounts.Add(obj.GetComponentInChildren<TextMeshProUGUI>());
            }
        }
        UpdateItemCount();

        ItemImage.sprite = PurchaseItemType.ExtendLimit.GetSprite();
        ItemName.text = PurchaseItemType.ExtendLimit.GetName();
        ItemDescription.text = PurchaseItemType.ExtendLimit.GetDescription();
    }
    public void OnClose()
    {
        gameObject.SetActive(false);
        MenuStages.PopUp();
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton2);
    }

    private void UpdateItemCount()
    {
        int count = System.Enum.GetValues(typeof(PurchaseItemType)).Length;
        for (int i = 0; i < count; ++i)
        {
            PurchaseItemType type = i.ToItemType();
            ItemCounts[i].text = type.GetCount().ToString();
        }
    }

    private void UpdateItemDetail()
    {
        GameObject curBtn = EventSystem.current.currentSelectedGameObject;
        PurchaseItemType type = int.Parse(curBtn.name).ToItemType();
        ItemImage.sprite = type.GetSprite();
        ItemName.text = type.GetName();
        ItemDescription.text = type.GetDescription();
    }
}
