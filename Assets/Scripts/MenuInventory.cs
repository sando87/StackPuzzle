using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuInventory : MonoBehaviour
{
    private const string UIObjName = "CanvasPopUp/MenuInventory";

    public GameObject ItemSlots;
    public TextMeshProUGUI ItemDescription;

    public static void PopUp()
    {
        GameObject objMenu = GameObject.Find("UISpace").transform.Find(UIObjName).gameObject;
        objMenu.SetActive(true);
        objMenu.GetComponent<MenuInventory>().Init();
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
    private void UpdateState()
    {
        int count = System.Enum.GetValues(typeof(PurchaseItemType)).Length;
        Button[] itemSlots = ItemSlots.GetComponentsInChildren<Button>();
        for(int i = 0; i < count; ++i)
        {
            PurchaseItemType type = i.ToItemType();
            int itemCount = type.GetCount();
            Button slot = itemSlots[i];
            Image img = slot.GetComponent<Image>();
            slot.onClick.RemoveAllListeners();

            if (itemCount > 0)
            {
                img.sprite = type.GetSprite();
                img.raycastTarget = true;
                slot.onClick.AddListener(() => ItemDescription.text = type.GetDescription());
                slot.GetComponentInChildren<Text>().text = type.GetCount().ToString();
            }
            else
            {
                img.sprite = type.GetSprite();
                img.raycastTarget = false;
                img.color = Color.gray;
            }
        }
    }
}
