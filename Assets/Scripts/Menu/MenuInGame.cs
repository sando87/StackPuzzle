﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
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
    public ScoreBar ScoreBarObj;
    public TextMeshProUGUI LevelCompleted;
    public TextMeshProUGUI LevelFailed;
    public Sprite ItemEmptyImage;
    public GameObject[] ItemSlots;
    public Button PauseButton;
    public Button SkipButton;

    public GameObject EffectParent;

    public int RemainLimit { get { return int.Parse(Limit.text); } }

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
        if(mInst == null)
            mInst = GameObject.Find(UIObjName).GetComponent<MenuInGame>();
        return mInst;
    }
    public static void PopUp(StageInfo info)
    {
        Inst().InitUIState(info);
        Inst().gameObject.SetActive(true);
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
        Limit.text = info.MoveLimit.ToString();
        ScoreBarObj.ScorePerBar = UserSetting.ScorePerBar;
        if (info.GoalTypeEnum == StageGoalType.Score)
        {
            TargetType.gameObject.SetActive(false);
            TargetScoreText.gameObject.SetActive(true);
            TargetScoreText.text = info.GoalValue.ToString();
        }
        else
        {
            TargetScoreText.gameObject.SetActive(false);
            TargetType.gameObject.SetActive(true);
            TargetType.sprite = info.GoalTypeImage;
            TargetValue.text = info.GoalValue.ToString();
        }
        ComboNumber.Clear();
        ScoreBarObj.Clear();

        PurchaseItemType[] items = MenuPlay.Inst().GetSelectedItems();
        for(int i = 0; i < ItemSlots.Length; ++i)
        {
            if (i < items.Length)
            {
                ItemSlots[i].name = items[i].ToInt().ToString();
                ItemSlots[i].GetComponentInChildren<Button>().enabled = true;
                ItemSlots[i].GetComponentInChildren<Image>().sprite = items[i].GetSprite();
                ItemSlots[i].GetComponentInChildren<Image>().color = Color.white;
                ItemSlots[i].GetComponentInChildren<TextMeshProUGUI>().text = items[i].GetName();
            }
            else
            {
                ItemSlots[i].GetComponentInChildren<Button>().enabled = false;
                ItemSlots[i].GetComponentInChildren<Image>().color = Color.gray;
                ItemSlots[i].GetComponentInChildren<Image>().sprite = ItemEmptyImage;
                ItemSlots[i].GetComponentInChildren<TextMeshProUGUI>().text = "Empty";
            }
        }

        InGameManager.InstStage.EventBreakTarget = (pos, type) => {
            ReduceGoalValue(pos, type);
        };
        InGameManager.InstStage.EventScore = (score) => {
            ScoreBarObj.SetScore(ScoreBarObj.CurrentScore + score);
        };
        InGameManager.InstStage.EventFinishPre = (success) => {
            ShowFinishMessage(success);
        };
        InGameManager.InstStage.EventReward = (rewardCount, interval) => {
            StartCoroutine(AnimateRewardCounting(rewardCount, interval));
        };
        InGameManager.InstStage.EventFinish = (success) => {
            FinishGame(success);
        };
        InGameManager.InstStage.EventFinishFirst = (success) => {
            if (success)
            {
                PauseButton.gameObject.SetActive(false);
                SkipButton.gameObject.SetActive(true);
            }
        };
        InGameManager.InstStage.EventReduceLimit = () => {
            ReduceMoveLimit();
        };
        InGameManager.InstStage.EventCombo = (combo) => {
            CurrentCombo = combo;
        };
        InGameManager.InstStage.EventRemainTime = (remainSec) => {
            if(remainSec == 10 && Limit.text.Equals("00:11"))
            {
                SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectCooltime);
                StartCoroutine(UnityUtils.AnimateStandOut(Limit.gameObject));
            }
            Limit.text = TimeToString(remainSec);
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
        switch(itemType)
        {
            case PurchaseItemType.ExtendLimit:
                InGameManager.InstStage.UseItemExtendsLimits(btn.transform.position, Limit.transform.position);
                break;
            case PurchaseItemType.RemoveIce:
                InGameManager.InstStage.UseItemBreakce(btn.transform.position, 10);
                break;
            case PurchaseItemType.MakeSkill1:
                InGameManager.InstStage.UseItemMakeSkill1(btn.transform.position, 10);
                break;
            case PurchaseItemType.MakeCombo:
                InGameManager.InstStage.UseItemMatch(btn.transform.position);
                break;
            case PurchaseItemType.MakeSkill2:
                InGameManager.InstStage.UseItemMakeSkill2(btn.transform.position, 10);
                break;
            case PurchaseItemType.PowerUp:
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

    public void ReduceMoveLimit()
    {
        int remain = mStageInfo.MoveLimit - InGameManager.InstStage.GetBillboard().MoveCount;
        remain = Mathf.Max(0, remain);
        Limit.text = remain.ToString();
    }

    public string TimeToString(int second)
    {
        if (second < 0)
            return "00:00";

        int min = second / 60;
        int sec = second % 60;
        return string.Format("{0:00}:{1:00}", min, sec);
    }

    public void FinishGame(bool success)
    {
        if (success)
        {
            //int starCount = InGameManager.InstStage.GetBillboard().GetGrade(mStageInfo);
            float limitRate = InGameManager.InstStage.LimitRate;
            int starCount = limitRate < 0.6f ? 3 : (limitRate < 0.8f ? 2 : 1);
            bool isFirstThreeStar = starCount == 3 && UserSetting.GetStageStarCount(mStageInfo.Num) < 3;
            Stage currentStage = StageManager.Inst.GetStage(mStageInfo.Num);
            currentStage.UpdateStarCount(starCount);

            bool isFirstClear = false;
            Stage nextStage = StageManager.Inst.GetStage(mStageInfo.Num + 1);
            if (nextStage != null)
            {
                isFirstClear = nextStage.Locked;
                nextStage.UnLock();
            }

            string log = "[STAGE] " + "success," + mStageInfo.Num + "," + starCount + "," + ScoreBarObj.CurrentScore;
            LOG.echo(log);

            SoundPlayer.Inst.PlayerBack.Stop();
            SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectSuccess);
            MenuComplete.PopUp(mStageInfo.Num, starCount, ScoreBarObj.CurrentScore, isFirstClear, isFirstThreeStar);
            InGameManager.InstStage.CleanUpGame();
            Hide();
        }
        else
        {
            string log = "[STAGE] " + "failed," + mStageInfo.Num;
            LOG.echo(log);

            SoundPlayer.Inst.PlayerBack.Stop();
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
    public void ReduceGoalValue(Vector3 worldPos, StageGoalType type)
    {
        if (type != mStageInfo.GoalTypeEnum)
            return;

        if (type == StageGoalType.Score)
        {
            int remainScore = mStageInfo.GoalValue - InGameManager.InstStage.Billboard.CurrentScore;
            remainScore = Mathf.Max(remainScore, 0);
            TargetScoreText.text = remainScore.ToString();
            StartCoroutine(UnityUtils.AnimateStandOut(TargetScoreText.gameObject));
            return;
        }
        else
        {
            int value = int.Parse(TargetValue.text) - 1;
            value = Mathf.Max(0, value);
            TargetValue.text = value.ToString();
            StartCoroutine(UnityUtils.AnimateStandOut(TargetValue.transform.parent.gameObject));
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
            curLimit = float.Parse(Limit.text);

        float step = curLimit / count;
        while (loopCnt < count)
        {
            curLimit -= step;

            if (mStageInfo.TimeLimit > 0)
                Limit.text = TimeToString((int)curLimit);
            else
                Limit.text = ((int)curLimit).ToString();

            loopCnt++;
            yield return new WaitForSeconds(interval);
        }
    }

}
