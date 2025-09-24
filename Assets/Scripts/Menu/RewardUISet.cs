using System.Collections;
using System.Collections.Generic;
using System.Threading;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RewardUISet : MonoBehaviour
{
    public GameObject RewardPrefab;
    public GameObject RewardPackPrefab;
    public GameObject RewardParent;

    private StageInfo mStageInfo = null;

    public GameObject GoldReward { get; private set; } = null;
    public GameObject EventItemReward { get; private set; } = null;
    public GameObject PackBoxReward { get; private set; } = null;

    private Image mEventItemBarFrom = null;
    private Image mEventItemBarTo = null;

    Image BG(GameObject obj) { return obj.GetComponent<Image>(); }
    Image Icon(GameObject obj) { return obj.transform.Find("Icon").GetComponent<Image>(); }
    TextMeshProUGUI Text(GameObject obj) { return obj.transform.Find("Text_Value").GetComponent<TextMeshProUGUI>(); }
    Transform FX(GameObject obj) { return obj.transform.Find("Fx_Star"); }
    Transform Glow(GameObject obj) { return obj.transform.Find("Glow"); }
    Transform Adv(GameObject obj) { return obj.transform.Find("Adv"); }
    Transform Gauge(GameObject obj) { return obj.transform.Find("Gauge_Outline"); }
    Image GaugeFillTo(GameObject obj) { return obj.transform.Find("Gauge_Outline/Gauge_MaskTo").GetComponent<Image>(); }
    Image GaugeFillFrom(GameObject obj) { return obj.transform.Find("Gauge_Outline/Gauge_MaskFrom").GetComponent<Image>(); }
    

    private void ClearRewards()
    {
        for (int i = 0; i < RewardParent.transform.childCount; ++i)
        {
            GameObject obj = RewardParent.transform.GetChild(i).gameObject;
            Destroy(obj);
        }
    }

    public void UpdateToReady(StageInfo stageInfo)
    {
        mStageInfo = stageInfo;
        bool isStageCleared = UserSetting.UserSettingInfo.GetStageStarCount(stageInfo.Num) > 0;
        bool is3StarCleared = UserSetting.UserSettingInfo.GetStageStarCount(stageInfo.Num) >= 3;
        bool isPackageRewarded = UserSetting.UserSettingInfo.IsRewardedBox(stageInfo.Num);

        ClearRewards();

        // 기본 골드 보상
        // GoldReward = Instantiate(RewardPrefab, RewardParent.transform);
        // Icon(GoldReward).sprite = PurchaseItemTypeExtensions.GetGoldSprite();
        // Text(GoldReward).gameObject.SetActive(false);
        // SetReward_Ready(GoldReward);

        var rewardInfos = stageInfo.GetRewardInfos();
        foreach (var rewardInfo in rewardInfos)
        {
            string rewardString = rewardInfo.Item1;
            Sprite rewardImage = rewardInfo.Item2;
            int rewardCount = rewardInfo.Item3;

            if (rewardImage == PurchaseItemTypeExtensions.GetChestSprite())
            {
                PackBoxReward = Instantiate(RewardPackPrefab, RewardParent.transform);
                PackBoxReward.name = rewardString;
                Text(PackBoxReward).text = rewardCount.ToString();
                Glow(PackBoxReward).gameObject.SetActive(false);
                PackBoxReward.GetComponent<Button>().enabled = false;
                if (isPackageRewarded)
                {
                    SetReward_Finished(PackBoxReward);
                }
                else
                {
                    SetReward_Ready(PackBoxReward);
                }
            }
            else
            {
                GameObject obj = Instantiate(RewardPrefab, RewardParent.transform);
                obj.name = rewardString;
                SetIconImage(Icon(obj), rewardImage);
                Text(obj).text = rewardCount.ToString();
                if (isStageCleared)
                {
                    SetReward_Finished(obj);
                }
                else
                {
                    SetReward_Ready(obj);
                }
            }
        }

        EventItemReward = Instantiate(RewardPrefab, RewardParent.transform);
        SetIconImage(Icon(EventItemReward), UserSetting.UserSettingInfo.CurrentEventItem.GetSprite());
        Text(EventItemReward).gameObject.SetActive(false);
        Gauge(EventItemReward).gameObject.SetActive(true);
        mEventItemBarFrom = GaugeFillFrom(EventItemReward);
        mEventItemBarTo = GaugeFillTo(EventItemReward);
        if (is3StarCleared)
        {
            SetReward_Finished(EventItemReward);
        }
        else
        {
            SetReward_Ready(EventItemReward);
        }

        UserSetting.UserSettingInfo.GetRateRangeOfEventItem(0, out float rateFrom, out float rateTo);
        SetEventItemRate(rateFrom, rateTo);
    }

    public void UpdateForRewarding(StageInfo stageInfo, bool isFirstClear, bool isFirstThreeStarClear)
    {
        mStageInfo = stageInfo;
        bool isPackageRewarded = UserSetting.UserSettingInfo.IsRewardedBox(stageInfo.Num);

        ClearRewards();

        // 기본 골드 보상
        GoldReward = Instantiate(RewardPrefab, RewardParent.transform);
        SetIconImage(Icon(GoldReward), PurchaseItemTypeExtensions.GetGoldSprite());
        Text(GoldReward).gameObject.SetActive(true);
        Text(GoldReward).text = "0";
        SetReward_Rewardable(GoldReward);

        var rewardInfos = stageInfo.GetRewardInfos();
        foreach (var rewardInfo in rewardInfos)
        {
            string rewardString = rewardInfo.Item1;
            Sprite rewardImage = rewardInfo.Item2;
            int rewardCount = rewardInfo.Item3;

            if (rewardImage == PurchaseItemTypeExtensions.GetChestSprite())
            {
                PackBoxReward = Instantiate(RewardPackPrefab, RewardParent.transform);
                PackBoxReward.name = rewardString;
                Text(PackBoxReward).text = rewardCount.ToString();
                if (isPackageRewarded)
                {
                    SetReward_Finished(PackBoxReward);
                    SetPackageBox_Finished();
                }
                else
                {
                    SetReward_Rewardable(PackBoxReward);
                    SetPackageBox_Rewardable();
                }
            }
            else
            {
                GameObject obj = Instantiate(RewardPrefab, RewardParent.transform);
                obj.name = rewardString;
                SetIconImage(Icon(obj), rewardImage);
                Text(obj).text = rewardCount.ToString();
                if (!isFirstClear)
                {
                    SetReward_Finished(obj);
                }
                else
                {
                    SetReward_Rewardable(obj);
                }
            }
        }

        EventItemReward = Instantiate(RewardPrefab, RewardParent.transform);
        SetIconImage(Icon(EventItemReward), UserSetting.UserSettingInfo.CurrentEventItem.GetSprite());
        Text(EventItemReward).gameObject.SetActive(false);
        Gauge(EventItemReward).gameObject.SetActive(true);
        mEventItemBarFrom = GaugeFillFrom(EventItemReward);
        mEventItemBarTo = GaugeFillTo(EventItemReward);
        if (!isFirstThreeStarClear)
        {
            SetReward_Finished(EventItemReward);
        }
        else
        {
            SetReward_Rewardable(EventItemReward);
        }

        UserSetting.UserSettingInfo.GetRateRangeOfEventItem(0, out float rateFrom, out float rateTo);
        SetEventItemRate(rateFrom, rateTo);
    }


    void SetReward_Finished(GameObject rewardObj)
    {
        BG(rewardObj).color = Color.gray;
        Icon(rewardObj).GetComponent<Image>().color = Color.gray;
        FX(rewardObj).gameObject.SetActive(false);
    }
    void SetReward_Ready(GameObject rewardObj)
    {
        BG(rewardObj).color = Color.white;
        Icon(rewardObj).GetComponent<Image>().color = Color.white;
        FX(rewardObj).gameObject.SetActive(false);
    }
    void SetReward_Rewardable(GameObject rewardObj)
    {
        BG(rewardObj).color = Color.white;
        Icon(rewardObj).GetComponent<Image>().color = Color.white;
        FX(rewardObj).gameObject.SetActive(true);
    }

    public void SetPackageBox_Rewardable()
    {
        if (PackBoxReward != null)
        {
            PackBoxReward.GetComponent<Button>().enabled = true;

            FX(PackBoxReward).gameObject.SetActive(true);
            Glow(PackBoxReward).gameObject.SetActive(true);
            Adv(PackBoxReward).gameObject.SetActive(true);

            PackBoxReward.GetComponent<Button>().onClick.AddListener(OnClickReward);
        }
    }
    public void SetPackageBox_Finished()
    {
        if (PackBoxReward != null)
        {
            PackBoxReward.GetComponent<Button>().enabled = false;

            FX(PackBoxReward).gameObject.SetActive(false);
            Glow(PackBoxReward).gameObject.SetActive(false);
            Adv(PackBoxReward).gameObject.SetActive(false);
            SetReward_Finished(PackBoxReward);

            PackBoxReward.GetComponent<Button>().onClick.RemoveListener(OnClickReward);
        }
    }

    public void SetEventItemRate(float rateFrom, float rateTo, float duration = 0)
    {
        if (mEventItemBarFrom != null && mEventItemBarTo != null)
        {
            if (duration <= 0)
            {
                mEventItemBarFrom.DOKill();
                mEventItemBarFrom.fillAmount = rateFrom;
                mEventItemBarTo.DOKill();
                mEventItemBarTo.fillAmount = rateTo;
            }
            else
            {
                mEventItemBarFrom.DOKill();
                mEventItemBarFrom.fillAmount = rateFrom;
                mEventItemBarTo.DOKill();
                mEventItemBarTo.fillAmount = rateTo;

                mEventItemBarFrom.DOFillAmount(rateTo, duration);
            }
        }
    }
    public void ChangeEventItemTween(Sprite nextItemImage)
    {
        if (EventItemReward != null)
        {
            mEventItemBarFrom.DOFillAmount(0, 1);
            mEventItemBarTo.DOFillAmount(0, 1);
            Image img = Icon(EventItemReward);
            Vector3 curPos = img.transform.position;
            img.transform.DOMoveY(curPos.y + 1, 0.5f);
            img.DOFade(0, 0.5f);
            this.ExDelayedCoroutine(0.6f, () =>
            {
                SetIconImage(img, nextItemImage);
                img.transform.DOMoveY(curPos.y, 0.5f).From(curPos.y - 1);
                img.DOFade(1, 0.5f);
            });
        }
    }

    public void OpenGoldBoxTween()
    {
        if (PackBoxReward == null)
            return;
            
        StartCoroutine(CoOpenGoldBox());
    }
    IEnumerator CoOpenGoldBox()
    {
        Button packBtn = PackBoxReward.GetComponent<Button>();
        string[] subRewards = packBtn.name.Split(' ');
        
        UserSetting.UserSettingInfo.DoRewardBox(mStageInfo.Num);
        foreach (string subReward in subRewards)
            StageInfo.DoReward(subReward);

        packBtn.enabled = false;

        Transform fxBefore = PackBoxReward.transform.Find("Fx_Star");
        Transform glow = PackBoxReward.transform.Find("Glow");
        Transform adv = PackBoxReward.transform.Find("Adv");
        Transform dimObj = PackBoxReward.transform.Find("Dimed");
        Transform fxAfter = PackBoxReward.transform.Find("Fx_StarAfter");
        Transform items = PackBoxReward.transform.Find("Items");
        
        fxBefore.gameObject.SetActive(false);
        glow.gameObject.SetActive(false);
        adv.gameObject.SetActive(false);

        dimObj.gameObject.SetActive(true);
        dimObj.GetComponent<Image>().DOFade(1, 0.5f);
        yield return new WaitForSeconds(0.5f);
        fxAfter.gameObject.SetActive(true);

        items.gameObject.SetActive(true);
        for (int i = 0; i < items.childCount; ++i)
        {
            if (i < subRewards.Length)
            {
                var rewardInfo = StageInfo.StringToRewardInfo(subRewards[i]);
                items.GetChild(i).gameObject.SetActive(true);
                items.GetChild(i).GetComponent<Image>().sprite = rewardInfo.Item2;
                items.GetChild(i).GetComponentInChildren<TextMeshProUGUI>().text = rewardInfo.Item3.ToString();
            }
            else
            {
                items.GetChild(i).gameObject.SetActive(false);
            }
        }

        items.GetComponent<RectTransform>().DOLocalMoveY(200, 1).From(0).SetEase(Ease.OutQuad);

        float time = 0;
        while (time < 1)
        {
            items.GetComponent<HorizontalLayoutGroup>().spacing = (1 - time) * -500.0f;
            yield return null;
            time += Time.deltaTime;
        }
        items.GetComponent<HorizontalLayoutGroup>().spacing = 0;
        
        yield return new WaitForSeconds(0.5f);
        dimObj.GetComponent<Image>().DOFade(0, 0.5f);
        yield return new WaitForSeconds(0.5f);
        dimObj.gameObject.SetActive(false);
    }

    public void DoReword()
    {
        var rewardInfos = mStageInfo.GetRewardInfos();
        foreach (var rewardInfo in rewardInfos)
        {
            string rewardString = rewardInfo.Item1;
            Sprite rewardImage = rewardInfo.Item2;
            if (rewardImage != PurchaseItemTypeExtensions.GetChestSprite())
            {
                StageInfo.DoReward(rewardString);
            }
        }
    }
    private void OnClickReward()
    {
        if (Purchases.IsAdsSkip())
        {
            OpenGoldBoxTween();
            return;
        }

        if (!NetClientApp.GetInstance().IsNetworkAlive)
        {
            MenuMessageBox.PopUp("Network NotReachable", false, null);
            return;
        }

        if (!GoogleADMob.Inst.IsLoaded(AdsType.RewardItem))
        {
            MenuMessageBox.PopUp("Ad Not Ready", false, null);
            return;
        }

        GoogleADMob.Inst.Show(AdsType.RewardItem, (rewarded) =>
        {
            if (rewarded)
            {
                OpenGoldBoxTween();
            }
        });
    }

    void SetIconImage(Image img, Sprite sprite)
    {
        img.sprite = sprite;
        Vector2 resized = sprite.ExSetSizeFitSmall(new Vector2(90, 90));
        img.rectTransform.sizeDelta = resized;
    }
}
