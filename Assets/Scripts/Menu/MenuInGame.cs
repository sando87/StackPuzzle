using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class MenuInGame : MonoBehaviour
{
    private static MenuInGame mInst = null;
    private const string UIObjName = "UISpace/CanvasPopup/InGame";
    private StageInfo mStageInfo;
    private MenuMessageBox mMenu = null;

    public TextMeshProUGUI Limit;
    public TextMeshProUGUI TargetValue;
    public TextMeshProUGUI TargetScoreText;
    public Image TargetType;
    public NumbersUI ComboNumber;
    public GameObject GoldBundleImage;
    public TextMeshProUGUI GoldBundleText;
    public TextMeshProUGUI LevelCompleted;
    public TextMeshProUGUI LevelFailed;
    public Sprite ItemEmptyImage;
    public GameObject[] ItemSlots;
    public Button PauseButton;
    public Button SkipButton;
    public Image LimitFG;
    public Image GoalFG;
    public Image ScoreFG;
    public GameObject StarA = null;
    public GameObject StarB = null;
    public GameObject StarC = null;

    public GameObject EffectParent;

    public int CurrentScore { get; private set; }

    private void Update()
    {
#if PLATFORM_ANDROID
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnPause();
        }
#endif
    }

    public static MenuInGame Inst()
    {
        if (mInst == null)
            mInst = GameObject.Find(UIObjName).GetComponent<MenuInGame>();
        return mInst;
    }
    public static void PopUp(StageInfo info)
    {
        Inst().gameObject.SetActive(true);
        Inst().InitUIState(info);
    }
    public static void Hide()
    {
        Inst().gameObject.SetActive(false);
    }
    private void InitUIState(StageInfo info)
    {
        for (int i = 0; i < EffectParent.transform.childCount; ++i)
            Destroy(EffectParent.transform.GetChild(i).gameObject);

        mStageInfo = info;

        PauseButton.gameObject.SetActive(true);
        SkipButton.gameObject.SetActive(false);
        LevelCompleted.gameObject.SetActive(false);
        LevelFailed.gameObject.SetActive(false);

        if (info.MoveLimit != 0)
            UpdateMoveLimit(info.MoveLimit);
        else if (info.TimeLimit != 0)
            UpdateTimeLimit(info.TimeLimit);

        if (info.GoalTypeEnum == StageGoalType.Score)
        {
            TargetType.gameObject.SetActive(false);
            TargetScoreText.gameObject.SetActive(true);
            UpdateGoalValue();
        }
        else
        {
            TargetScoreText.gameObject.SetActive(false);
            TargetType.gameObject.SetActive(true);
            TargetType.sprite = info.GoalTypeImage;
            UpdateGoalValue();
        }
        ComboNumber.Clear();

        // ScoreBarObj.Init(info.StarPoint);
        Vector2 scoreSize = ScoreFG.rectTransform.sizeDelta;
        scoreSize.x = 0;
        ScoreFG.rectTransform.sizeDelta = scoreSize;
        CurrentScore = 0;
        GoldBundleText.text = "x0";

        PurchaseItemType[] items = MenuPlay.Inst().GetSelectedItems();
        for (int i = 0; i < ItemSlots.Length; ++i)
        {
            if (i < items.Length)
            {
                ItemSlots[i].name = items[i].ToInt().ToString();
                ItemSlots[i].GetComponentInChildren<Button>().enabled = true;
                ItemSlots[i].GetComponentInChildren<Image>().sprite = items[i].GetSprite();
                ItemSlots[i].GetComponentInChildren<Image>().color = Color.white;
                //ItemSlots[i].GetComponentInChildren<TextMeshProUGUI>().text = items[i].GetName();
            }
            else
            {
                ItemSlots[i].GetComponentInChildren<Button>().enabled = false;
                ItemSlots[i].GetComponentInChildren<Image>().color = Color.gray;
                ItemSlots[i].GetComponentInChildren<Image>().sprite = ItemEmptyImage;
                //ItemSlots[i].GetComponentInChildren<TextMeshProUGUI>().text = "Empty";
            }
        }

        InGameManager.InstStage.EventBreakTarget = (pos, type) =>
        {
            UpdateGoalValue();
        };
        InGameManager.InstStage.EventScore = (score) =>
        {
            AddScore(score);
        };
        InGameManager.InstStage.EventFinishPre = (success) =>
        {
            ShowFinishMessage(success);
        };
        InGameManager.InstStage.EventReward = (rewardCount, interval) =>
        {
            StartCoroutine(AnimateRewardCounting(rewardCount, interval));
        };
        InGameManager.InstStage.EventFinish = (success) =>
        {
            FinishGame(success);
        };
        InGameManager.InstStage.EventFinishFirst = (success) =>
        {
            if (success)
            {
                PauseButton.gameObject.SetActive(false);
                SkipButton.gameObject.SetActive(true);
            }
        };
        InGameManager.InstStage.EventReduceLimit = () =>
        {
            int remain = mStageInfo.MoveLimit - InGameManager.InstStage.GetBillboard().MoveCount;
            UpdateMoveLimit(remain);
        };
        InGameManager.InstStage.EventCombo = (combo) =>
        {
            CurrentCombo = combo;
        };
        InGameManager.InstStage.EventRemainTime = (remainSec) =>
        {
            if (remainSec == 10 && Limit.text.Equals("00:11"))
            {
                SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectCooltime);
                StartCoroutine(UnityUtils.AnimateStandOut(Limit.gameObject));
            }

            UpdateTimeLimit(remainSec);
        };
    }

    private int CurrentCombo
    {
        get
        {
            return ComboNumber.GetNumber();
        }
        set
        {
            if (value <= NumbersUI.ZeroNumber)
                ComboNumber.BreakCombo();
            else
                ComboNumber.SetNumber(value);
        }
    }

    public void OnClickItem()
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton2);
        Button btn = EventSystem.current.currentSelectedGameObject.GetComponent<Button>();
        PurchaseItemType itemType = int.Parse(btn.transform.parent.name).ToItemType();
        switch (itemType)
        {
            case PurchaseItemType.ExtendLimit:
                InGameManager.InstStage.UseItemExtendsLimits(btn.transform.position, Limit.transform.position);
                break;
            case PurchaseItemType.RemoveIce:
                {
                    bool ret = InGameManager.InstStage.UseItemBreakce(btn.transform.position, 10);
                    if (ret)
                        break;
                    else
                        return;
                }
            case PurchaseItemType.MakeSkill1:
                InGameManager.InstStage.UseItemMakeSkill1(btn.transform.position, 10);
                break;
            case PurchaseItemType.KeepCombo:
                {
                    bool ret = InGameManager.InstStage.UseItemMatch(btn.transform.position);
                    if (ret)
                        break;
                    else
                        return;
                }
            case PurchaseItemType.MakeSkill2:
                InGameManager.InstStage.UseItemMakeSkill2(btn.transform.position, 10);
                break;
            case PurchaseItemType.Meteor:
                InGameManager.InstStage.UseItemMeteor(5);
                break;
            default: break;
        }

        btn.GetComponent<Image>().color = Color.gray;
        btn.enabled = false;
        Purchases.UseItem(itemType);

        string log = "[UseItem] " + "Stage:" + mStageInfo.Num + ", Item:" + itemType + ", Count:" + itemType.GetCount();
        LOG.echo(log);
    }

    public string TimeToString(int second)
    {
        if (second < 0)
            return "00:00";

        int min = second / 60;
        int sec = second % 60;
        return string.Format("{0:00}:{1:00}", min, sec);
    }

    int GetStarCount()
    {
        float remainRate = 0;
        if (mStageInfo.TimeLimit > 0)
            remainRate = (float)MenuBattle.StringToSec(Limit.text) / mStageInfo.TimeLimit;
        else
            remainRate = (float)(mStageInfo.MoveLimit - InGameManager.InstStage.GetBillboard().MoveCount) / mStageInfo.MoveLimit;

        return remainRate >= 0.5f ? 3 : (remainRate >= 0.25f ? 2 : 1);
    }
    void UpdateStarCountUI()
    {
        int starCount = GetStarCount();

        StarA.SetActive(starCount >= 1);
        StarB.SetActive(starCount >= 2);
        StarC.SetActive(starCount >= 3);
    }

    public void FinishGame(bool success)
    {
        if (success)
        {
            //int starCount = InGameManager.InstStage.GetBillboard().GetGrade(mStageInfo);
            float limitRate = InGameManager.InstStage.LimitRate;
            int starCount = GetStarCount();
            bool isFirstThreeStar = starCount == 3 && UserSetting.GetStageStarCount(mStageInfo.Num) < 3;
            MapStage currentStage = MenuStages.Inst.FindStage(mStageInfo.Num);
            currentStage.UpdateStarCount(starCount);

            bool isFirstClear = false;
            MapStage nextStage = MenuStages.Inst.FindStage(mStageInfo.Num + 1);
            if (nextStage != null)
            {
                isFirstClear = nextStage.Locked;
                nextStage.UnLock();
            }

            string log = "[STAGE] " + "success," + mStageInfo.Num + "," + starCount + "," + CurrentScore;
            LOG.echo(log);

            SoundPlayer.Inst.StopBackMusic();
            SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectSuccess);
            MenuComplete.PopUp(mStageInfo.Num, starCount, CurrentScore, isFirstClear, isFirstThreeStar);
            InGameManager.InstStage.CleanUpGame();
            Hide();
        }
        else
        {
            string log = "[STAGE] " + "failed," + mStageInfo.Num;
            LOG.echo(log);

            SoundPlayer.Inst.StopBackMusic();
            SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectGameOver);

            if (GoogleADMob.Inst.RemainSec(AdsType.MissionFailed) <= 0
                && GoogleADMob.Inst.IsLoaded(AdsType.MissionFailed)
                && !Purchases.IsAdsSkip())
            {
                GoogleADMob.Inst.Show(AdsType.MissionFailed, (reward) =>
                {
                    MenuFailed.PopUp();
                    InGameManager.InstStage.CleanUpGame();
                    Hide();
                });
            }
            else
            {
                MenuFailed.PopUp();
                InGameManager.InstStage.CleanUpGame();
                Hide();
            }
        }
    }
    public void UpdateGoalValue()
    {
        if (mStageInfo.GoalTypeEnum == StageGoalType.Score)
        {
            int currentScore = InGameManager.InstStage.Billboard.GetGoalValue(mStageInfo.GoalTypeEnum);
            TargetScoreText.text = currentScore + " / " + mStageInfo.GoalValue;
            StartCoroutine(UnityUtils.AnimateStandOut(TargetScoreText.gameObject));

            Vector2 size = GoalFG.rectTransform.sizeDelta;
            float goalRate = (float)currentScore / mStageInfo.GoalValue;
            goalRate = Mathf.Clamp(goalRate, 0, 1);
            size.x = GoalFG.transform.parent.GetComponent<RectTransform>().sizeDelta.x * goalRate;
            GoalFG.rectTransform.sizeDelta = size;
        }
        else
        {
            int currentValue = InGameManager.InstStage.Billboard.GetGoalValue(mStageInfo.GoalTypeEnum);
            currentValue = Mathf.Max(0, currentValue);
            TargetValue.text = currentValue + " / " + mStageInfo.GoalValue;
            StartCoroutine(UnityUtils.AnimateStandOut(TargetValue.transform.parent.gameObject));

            Vector2 size = GoalFG.rectTransform.sizeDelta;
            float goalRate = (float)currentValue / mStageInfo.GoalValue;
            goalRate = Mathf.Clamp(goalRate, 0, 1);
            size.x = GoalFG.transform.parent.GetComponent<RectTransform>().sizeDelta.x * goalRate;
            GoalFG.rectTransform.sizeDelta = size;
        }
    }
    IEnumerator AnimateItem(GameObject obj, Vector3 worldDest, Action action)
    {
        float duration = 1.0f;
        float time = 0;
        Vector3 startPos = obj.transform.position;
        Vector3 destPos = worldDest;
        Vector3 dir = destPos - startPos;
        Vector3 offset = Vector3.zero;
        Vector3 axisZ = new Vector3(0, 0, 1);
        Vector3 deltaSize = new Vector3(0.01f, 0.01f, 0);
        float slope = -dir.y / (duration * duration);
        while (time < duration)
        {
            offset.y = slope * (time - duration) * (time - duration) + dir.y;
            offset.x = dir.x * time;
            obj.transform.position = startPos + offset;
            //obj.transform.localScale += time < duration * 0.5f ? deltaSize : -deltaSize;
            obj.transform.Rotate(axisZ, offset.x - dir.x);
            time += Time.deltaTime;
            yield return null;
        }

        action.Invoke();
        Destroy(obj);
    }

    public void OnPause()
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
        if (mMenu != null)
        {
            Destroy(mMenu);
            mMenu = null;
        }
        else
        {
            mMenu = MenuMessageBox.PopUp("Quit Stage?", true, (bool isOK) =>
            {
                if (isOK)
                {
                    FinishGame(false);
                }
            });
        }
    }

    public void OnSkip()
    {
        FinishGame(true);
    }

    private void ShowFinishMessage(bool isComplete)
    {
        if (isComplete)
        {
            SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectLevelComplete);
            LevelCompleted.gameObject.SetActive(true);
            LevelFailed.gameObject.SetActive(false);
            LevelCompleted.GetComponent<Animation>().Play();
        }
        else
        {
            LevelCompleted.gameObject.SetActive(false);
            LevelFailed.gameObject.SetActive(true);
            LevelFailed.GetComponent<Animation>().Play();
        }
    }

    private IEnumerator AnimateRewardCounting(int count, float interval)
    {
        int loopCnt = 0;
        float curLimit = 0;
        if (mStageInfo.TimeLimit > 0)
            curLimit = MenuBattle.StringToSec(Limit.text);
        else
            curLimit = mStageInfo.MoveLimit - InGameManager.InstStage.GetBillboard().MoveCount;

        float step = curLimit / count;
        while (loopCnt < count)
        {
            curLimit -= step;

            if (mStageInfo.TimeLimit > 0)
                UpdateTimeLimit((int)curLimit);
            else
                UpdateMoveLimit((int)curLimit);

            loopCnt++;
            yield return new WaitForSeconds(interval);
        }
    }

    void UpdateMoveLimit(int remainMove)
    {
        float remainRate = remainMove / (float)mStageInfo.MoveLimit;
        remainRate = Mathf.Clamp(remainRate, 0, 1);
        Limit.text = remainMove + " / " + mStageInfo.MoveLimit;
        Vector2 size = LimitFG.rectTransform.sizeDelta;
        size.x = LimitFG.transform.parent.GetComponent<RectTransform>().sizeDelta.x * remainRate;
        LimitFG.rectTransform.sizeDelta = size;

        UpdateStarCountUI();
    }
    void UpdateTimeLimit(int remainTimeSec)
    {
        Limit.text = TimeToString(remainTimeSec);

        float remainRate = (float)remainTimeSec / mStageInfo.TimeLimit;
        remainRate = Mathf.Clamp(remainRate, 0, 1);
        Vector2 size = LimitFG.rectTransform.sizeDelta;
        size.x = LimitFG.transform.parent.GetComponent<RectTransform>().sizeDelta.x * remainRate;
        LimitFG.rectTransform.sizeDelta = size;

        UpdateStarCountUI();
    }

    void AddScore(int score)
    {
        int goldBundleUnit = 100;
        int prevGoldBundleCount = CurrentScore / goldBundleUnit;
        CurrentScore += score;
        int newGoldBundleCount = CurrentScore / goldBundleUnit;

        if (newGoldBundleCount > prevGoldBundleCount)
        {
            GoldBundleImage.transform.DOScale(1.2f, 0.1f).From(1).SetLoops(2, LoopType.Yoyo);
        }

        int goldRemain = CurrentScore % goldBundleUnit;
        float scoreBarRate = goldRemain / (float)goldBundleUnit;
        float newWidth = ScoreFG.transform.parent.GetComponent<RectTransform>().sizeDelta.x * scoreBarRate;
        ScoreFG.rectTransform.sizeDelta = new Vector2(newWidth, ScoreFG.rectTransform.sizeDelta.y);
        GoldBundleText.text = "x" + newGoldBundleCount;
    }

}
