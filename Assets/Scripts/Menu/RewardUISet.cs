using System.Collections;
using System.Collections.Generic;
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

    public static void PopUp(int level, int starCount, int score, bool isFirstClear, bool isFirstThreeStar)
    {
        // GameObject menuComp = GameObject.Find(UIObjName);

        // RewardUISet menu = menuComp.GetComponent<RewardUISet>();
        // menu.UpdateUIState(level, starCount, score, isFirstClear, isFirstThreeStar);

        // if (UserSetting.IsBotPlayer)
        //     menu.StartCoroutine(menu.AutoEnd());
    }

    public void UpdateCurrentState()
    {

    }

    private void UpdateUIState(int level, int starCount, int score, bool isFirstClear, bool isFirstThreeStar)
    {
        StageInfo stageInfo = null;
        if (isFirstThreeStar)
        {
            ClearRewards();
            CreateRewordSlot(stageInfo, true);
            UserSetting.UserSettingInfo.AddExpOfEventItem(30, out float rateFrom, out float rateTo);

            // 이벤트아이템 게이지 올라가는 연출..

            // 이벤트 게이지 완료시 아이템 획득 데이터 처리
            if (UserSetting.UserSettingInfo.IsDoneEventItem())
            {
                Purchases.AddItem(UserSetting.UserSettingInfo.CurrentEventItem);
                UserSetting.UserSettingInfo.ResetNextNewEventItem();
            }
        }
        else
        {
            if (UserSetting.GetStageStarCount(level) < 3)
            {
                ClearRewards();
                CreateRewordSlot(stageInfo, false);
            }
            else
            {
                ClearRewards();
            }
        }
    }


    private void ClearRewards()
    {
        for (int i = 1; i < RewardParent.transform.childCount; ++i)
        {
            GameObject obj = RewardParent.transform.GetChild(i).gameObject;
            Destroy(obj);
        }
    }
    private void CreateRewordSlot(StageInfo stageInfo, bool enabled)
    {
        mStageInfo = stageInfo;
        var rewardInfos = stageInfo.GetRewardInfos();
        foreach (var rewardInfo in rewardInfos)
        {
            string rewardString = rewardInfo.Item1;
            Sprite rewardImage = rewardInfo.Item2;
            int rewardCount = rewardInfo.Item3;

            if (rewardImage == PurchaseItemTypeExtensions.GetChestSprite())
            {
                GameObject obj = Instantiate(RewardPackPrefab, RewardParent.transform);
                obj.name = rewardString;
                obj.GetComponentInChildren<TextMeshProUGUI>().text = rewardCount.ToString();
                if (enabled)
                {
                    obj.GetComponent<Button>().onClick.AddListener(OnClickReward);
                }
                else
                {
                    obj.GetComponent<Button>().enabled = false;
                    obj.GetComponent<Image>().color = Color.gray;
                    obj.transform.GetChild(0).gameObject.SetActive(false);
                    obj.transform.GetChild(1).gameObject.SetActive(false);
                    obj.transform.GetChild(2).GetComponent<Image>().color = Color.gray;
                }
            }
            else
            {
                GameObject obj = Instantiate(RewardPrefab, RewardParent.transform);
                obj.name = rewardString;
                obj.transform.GetChild(0).GetComponent<Image>().sprite = rewardImage;
                obj.GetComponentInChildren<TextMeshProUGUI>().text = rewardCount.ToString();
                if (enabled)
                {
                    StageInfo.DoReward(rewardString);
                }
                else
                {
                    obj.GetComponentInChildren<ParticleSystem>().gameObject.SetActive(false);
                    obj.transform.GetChild(0).GetComponent<Image>().color = Color.gray;
                }
            }
        }
    }
    
    public void UpdateRewordSlot(StageInfo stageInfo)
    {
        ClearRewards();
        
        var rewardInfos = stageInfo.GetRewardInfos();
        foreach (var rewardInfo in rewardInfos)
        {
            string rewardString = rewardInfo.Item1;
            Sprite rewardImage = rewardInfo.Item2;
            int rewardCount = rewardInfo.Item3;

            if (rewardImage == PurchaseItemTypeExtensions.GetChestSprite())
            {
                GameObject obj = Instantiate(RewardPackPrefab, RewardParent.transform);
                obj.name = rewardString;
                obj.GetComponentInChildren<TextMeshProUGUI>().text = rewardCount.ToString();
                if (UserSetting.UserSettingInfo.IsRewardedBox(stageInfo.Num))
                {
                    obj.GetComponent<Button>().enabled = false;
                    obj.GetComponent<Image>().color = Color.gray;
                    obj.transform.GetChild(0).gameObject.SetActive(false);
                    obj.transform.GetChild(1).gameObject.SetActive(false);
                    obj.transform.GetChild(2).GetComponent<Image>().color = Color.gray;
                }
                else
                {
                    obj.GetComponent<Button>().onClick.AddListener(OnClickReward);
                }
            }
            else
            {
                GameObject obj = Instantiate(RewardPrefab, RewardParent.transform);
                obj.name = rewardString;
                obj.transform.GetChild(0).GetComponent<Image>().sprite = rewardImage;
                obj.GetComponentInChildren<TextMeshProUGUI>().text = rewardCount.ToString();
                bool isStageCleared = UserSetting.UserSettingInfo.GetStageStarCount(stageInfo.Num) > 0;
                if (rewardImage == PurchaseItemTypeExtensions.GetGoldSprite() || !isStageCleared)
                {
                    obj.GetComponentInChildren<ParticleSystem>().gameObject.SetActive(true);
                    obj.transform.GetChild(0).GetComponent<Image>().color = Color.white;
                }
                else
                {
                    obj.GetComponentInChildren<ParticleSystem>().gameObject.SetActive(false);
                    obj.transform.GetChild(0).GetComponent<Image>().color = Color.gray;
                }
            }
        }
    }
    
    private void DoReword(StageInfo stageInfo)
    {
        var rewardInfos = stageInfo.GetRewardInfos();
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
