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
        Button[] slots = ItemSlotRoot.GetComponentsInChildren<Button>();
        int itemTypeCount = System.Enum.GetValues(typeof(PurchaseItemType)).Length;
        for(int i = 0; i < itemTypeCount; ++i)
        {
            PurchaseItemType type = (PurchaseItemType)i;
            int itemCount = type.GetCount();
            slots[i].name = i.ToString();
            slots[i].GetComponent<Image>().sprite = type.GetSprite();
            slots[i].GetComponentInChildren<TextMeshProUGUI>().text = itemCount.ToString();
            if(itemCount > 0)
                slots[i].onClick.AddListener(OnSelectItem);
        }
    }

    public void OnSelectItem()
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);

        Button curBtn = EventSystem.current.currentSelectedGameObject.GetComponent<Button>();
        PurchaseItemType type = (PurchaseItemType)int.Parse(curBtn.name);
        EventSelectItem?.Invoke(type);
        Destroy(gameObject);
    }
    public void OnCancle()
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton2);
        Destroy(gameObject);
    }
}
