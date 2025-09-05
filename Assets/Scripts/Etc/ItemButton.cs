using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemButton : MonoBehaviour
{
    [SerializeField] private Sprite BtnBgImgA = null;
    [SerializeField] private Sprite BtnBgImgB = null;
    [SerializeField] private Sprite AdsImage = null;
    [SerializeField] private Image ItemImg = null;
    [SerializeField] private Image ItemEmptyImg = null;
    [SerializeField] private TextMeshProUGUI ItemCount = null;

    private PurchaseItemType ItemType = PurchaseItemType.None;

    public void SetItem(PurchaseItemType item)
    {
        ItemType = item;
        UpdateItem();
    }

    public PurchaseItemType GetItem()
    {
        return ItemType;
    }

    public void UpdateItem()
    {
        if (ItemType == PurchaseItemType.None)
        {
            SetEmptyImage();
        }
        else
        {
            SetItemImage();
        }
    }

    public void SetEmptyImage()
    {
        GetComponent<Image>().sprite = BtnBgImgB;
        ItemImg.gameObject.SetActive(false);
        ItemEmptyImg.gameObject.SetActive(true);

        ItemCount.gameObject.SetActive(false);
    }
    public void SetAdsImage()
    {
        GetComponent<Image>().sprite = BtnBgImgA;
        ItemImg.gameObject.SetActive(true);
        ItemEmptyImg.gameObject.SetActive(false);
        ItemImg.sprite = AdsImage;

        ItemCount.gameObject.SetActive(true);
        ItemCount.text = ItemType.GetCount().ToString();
    }
    public void SetItemImage()
    {
        GetComponent<Image>().sprite = BtnBgImgA;
        ItemImg.gameObject.SetActive(true);
        ItemEmptyImg.gameObject.SetActive(false);
        ItemImg.sprite = ItemType.GetSprite();

        ItemCount.gameObject.SetActive(true);
        ItemCount.text = ItemType.GetCount().ToString();
    }

    public void HideItemCount()
    {
        ItemCount.gameObject.SetActive(false);
    }

    public void SetEnable(bool enable)
    {
        ItemImg.color = enable ? Color.white : Color.gray;
        GetComponent<Button>().enabled = enable;
    }

    public bool IsEnabled()
    {
        return GetComponent<Button>().enabled;
    }

    public void AddEvent(Action<PurchaseItemType> eventClick)
    {
        GetComponent<Button>().onClick.AddListener(() => eventClick(ItemType));
    }
}
