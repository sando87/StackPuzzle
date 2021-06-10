using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuItemSelector : MonoBehaviour
{
    public GameObject ItemSlotRoot;
    private Action<PurchaseItemType> EventSelectItem = null;

    public static MenuItemSelector PopUp(Action<PurchaseItemType> onSelect)
    {
        GameObject prefab = (GameObject)Resources.Load("Prefabs/ItemSelector", typeof(GameObject));
        GameObject objMenu = GameObject.Instantiate(prefab, GameObject.Find("UISpace/CanvasPopup").transform);
        MenuItemSelector box = objMenu.GetComponent<MenuItemSelector>();
        box.EventSelectItem = onSelect;
        box.UpdateItemSelector();
        return box;
    }

    public void UpdateItemSelector()
    {
        ItemButton[] slots = ItemSlotRoot.GetComponentsInChildren<ItemButton>();
        int itemTypeCount = System.Enum.GetValues(typeof(PurchaseItemType)).Length;
        for(int i = 0; i < itemTypeCount; ++i)
        {
            PurchaseItemType type = (PurchaseItemType)i;
            slots[i].SetItem(type);
            slots[i].AddEvent(OnSelectItem);
        }
    }

    public void OnSelectItem(PurchaseItemType item)
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
        EventSelectItem?.Invoke(item);
        Destroy(gameObject);
    }
    public void OnCancle()
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton2);
        Destroy(gameObject);
    }
}
