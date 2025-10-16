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
    private Vector2 mIconImgSizeOri = Vector2.zero;
    private bool mIsUseable = false;

    void Awake()
    {
        mIconImgSizeOri = ItemImg.rectTransform.sizeDelta;
    }

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
        SetIconImage(ItemImg, AdsImage);

        ItemCount.gameObject.SetActive(true);
        ItemCount.text = ItemType.GetCount().ToString();
    }
    public void SetItemImage()
    {
        GetComponent<Image>().sprite = BtnBgImgA;
        ItemImg.gameObject.SetActive(true);
        ItemEmptyImg.gameObject.SetActive(false);
        SetIconImage(ItemImg, ItemType.GetSprite());

        ItemCount.gameObject.SetActive(true);
        ItemCount.text = ItemType.GetCount().ToString();
    }

    void SetIconImage(Image img, Sprite sprite)
    {
        img.sprite = sprite;
        Vector2 resized = sprite.ExSetSizeFitBig(mIconImgSizeOri);
        img.rectTransform.sizeDelta = resized;
    }

    public void HideItemCount()
    {
        ItemCount.gameObject.SetActive(false);
    }

    public void SetEnable(bool enable)
    {
        ItemImg.color = enable ? Color.white : Color.gray;
        GetComponent<Image>().color = enable ? Color.white : Color.gray;
        ItemEmptyImg.color =  enable ? Color.white : Color.gray;
        GetComponent<Button>().enabled = enable;
        mIsUseable = enable;
    }

    public bool IsUseable()
    {
        return mIsUseable;
    }

    public void AddEvent(Action<PurchaseItemType> eventClick)
    {
        GetComponent<Button>().onClick.AddListener(() => eventClick(ItemType));
    }
}
