using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemButton : Button
{
    private PurchaseItemType ItemType = PurchaseItemType.None;

    public void SetItem(PurchaseItemType item)
    {
        ItemType = item;
        transform.GetChild(0).GetComponent<Image>().sprite = ItemType.GetSprite();
        transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = ItemType.GetCount().ToString();
    }

    public PurchaseItemType GetItem()
    {
        return ItemType;
    }

    public void UpdateItem()
    {
        transform.GetChild(0).GetComponent<Image>().sprite = ItemType.GetSprite();
        transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = ItemType.GetCount().ToString();
    }
}
