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

    private Image mEventItemBar = null;

    Image BG(GameObject obj) { return obj.GetComponent<Image>(); }
    Image Icon(GameObject obj) { return obj.transform.Find("Icon").GetComponent<Image>(); }
    TextMeshProUGUI Text(GameObject obj) { return obj.transform.Find("Text_Value").GetComponent<TextMeshProUGUI>(); }
    Transform FX(GameObject obj) { return obj.transform.Find("Fx_Star"); }
    Transform Glow(GameObject obj) { return obj.transform.Find("Glow"); }
    Transform Adv(GameObject obj) { return obj.transform.Find("Adv"); }
    Transform Gauge(GameObject obj) { return obj.transform.Find("Gauge_Outline"); }
    Image GaugeFill(GameObject obj) { return obj.transform.Find("Gauge_Outline/Gauge_BG/Gauge_Fill").GetComponent<Image>(); }

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
                Icon(obj).sprite = rewardImage;
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
        Icon(EventItemReward).sprite = UserSetting.UserSettingInfo.CurrentEventItem.GetSprite();
        Text(EventItemReward).gameObject.SetActive(false);
        Gauge(EventItemReward).gameObject.SetActive(true);
        mEventItemBar = GaugeFill(EventItemReward);
        if (is3StarCleared)
        {
            SetReward_Finished(EventItemReward);
        }
        else
        {
            SetReward_Ready(EventItemReward);
        }

        UserSetting.UserSettingInfo.GetRateRangeOfEventItem(0, out float rateFrom, out float rateTo);
        LOG.trace(rateTo);
        SetEventItemRate(rateTo);
    }

    public void UpdateForRewarding(StageInfo stageInfo, bool isFirstClear, bool isFirstThreeStarClear)
    {
        mStageInfo = stageInfo;
        bool isPackageRewarded = UserSetting.UserSettingInfo.IsRewardedBox(stageInfo.Num);

        ClearRewards();

        // 기본 골드 보상
        GoldReward = Instantiate(RewardPrefab, RewardParent.transform);
        Icon(GoldReward).sprite = PurchaseItemTypeExtensions.GetGoldSprite();
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
                Icon(obj).sprite = rewardImage;
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
        Icon(EventItemReward).sprite = UserSetting.UserSettingInfo.CurrentEventItem.GetSprite();
        Text(EventItemReward).gameObject.SetActive(false);
        Gauge(EventItemReward).gameObject.SetActive(true);
        mEventItemBar = GaugeFill(EventItemReward);
        if (!isFirstThreeStarClear)
        {
            SetReward_Finished(EventItemReward);
        }
        else
        {
            SetReward_Rewardable(EventItemReward);
        }

        UserSetting.UserSettingInfo.GetRateRangeOfEventItem(0, out float rateFrom, out float rateTo);
        LOG.trace(rateTo);
        SetEventItemRate(rateTo);
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

    public void SetEventItemRate(float rate, float duration = 0)
    {
        if (mEventItemBar != null)
        {
            if (duration <= 0)
            {
                mEventItemBar.DOKill();
                mEventItemBar.transform.localScale = new Vector3(rate, 1, 1);
            }
            else
            {
                mEventItemBar.DOKill();
                mEventItemBar.transform.DOScaleX(rate, duration);
            }
        }
    }
    public void ChangeEventItemTween(Sprite nextItemImage)
    {
        if (EventItemReward != null)
        {
            mEventItemBar.transform.DOScaleX(0, 1);
            Image img = Icon(EventItemReward);
            Vector3 curPos = img.transform.position;
            img.transform.DOMoveY(curPos.y + 1, 0.5f);
            img.DOFade(0, 0.5f);
            this.ExDelayedCoroutine(0.6f, () =>
            {
                img.sprite = nextItemImage;
                img.transform.DOMoveY(curPos.y, 0.5f).From(curPos.y - 1);
                img.DOFade(1, 0.5f);
            });
        }
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
        Button btn = EventSystem.current.currentSelectedGameObject.GetComponent<Button>();
        string[] subRewards = btn.name.Split(' ');

        if (Purchases.IsAdsSkip())
        {
            UserSetting.UserSettingInfo.DoRewardBox(mStageInfo.Num);
            foreach (string subReward in subRewards)
                StageInfo.DoReward(subReward);

            btn.transform.GetChild(0).gameObject.SetActive(false);
            btn.transform.GetChild(1).gameObject.SetActive(false);
            btn.enabled = false;
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
                UserSetting.UserSettingInfo.DoRewardBox(mStageInfo.Num);
                foreach (string subReward in subRewards)
                    StageInfo.DoReward(subReward);

                btn.transform.GetChild(0).gameObject.SetActive(false);
                btn.transform.GetChild(1).gameObject.SetActive(false);
                btn.enabled = false;
            }
        });
    }
}
