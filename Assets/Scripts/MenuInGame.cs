using System;
using System.Collections;
using System.Collections.Generic;
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

    public GameObject EffectParent;
    public GameObject ItemPrefab;
                                                
    public int Score { get { return ScoreBarObj.CurrentScore; } }
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

        LevelCompleted.gameObject.SetActive(false);
        LevelFailed.gameObject.SetActive(false);
        Limit.text = info.MoveLimit.ToString();
        if (info.GoalTypeEnum == StageGoalType.Score)
        {
            TargetType.gameObject.SetActive(false);
            TargetScoreText.gameObject.SetActive(true);
            TargetScoreText.text = info.GoalValue.ToString();
            ScoreBarObj.ScorePerBar = Mathf.Min(info.GoalValue, UserSetting.ScorePerBar);
        }
        else
        {
            TargetScoreText.gameObject.SetActive(false);
            TargetType.gameObject.SetActive(true);
            TargetType.sprite = info.GoalTypeImage;
            TargetValue.text = info.GoalValue.ToString();
            ScoreBarObj.ScorePerBar = UserSetting.ScorePerBar;
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
        InGameManager.InstStage.EventMatched = (products) => {
            ScoreBarObj.SetScore(ScoreBarObj.CurrentScore + products[0].Combo * products.Length);
        };
        InGameManager.InstStage.EventFinishPre = (success) => {
            ShowFinishMessage(success);
        };
        InGameManager.InstStage.EventFinish = (success) => {
            FinishGame(success);
        };
        InGameManager.InstStage.EventReduceLimit = () => {
            ReduceLimit();
        };
        InGameManager.InstStage.EventCombo = (combo) => {
            CurrentCombo = combo;
        };
        InGameManager.InstStage.EventRemainTime = (remainSec) => {
            Limit.text = remainSec.ToString();
        };
        InGameManager.InstStage.EventRemainTime = (remainSec) => {
            Limit.text = remainSec.ToString();
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
            if (value <= 0)
                ComboNumber.BreakCombo();
            else
                ComboNumber.SetNumber(value);
        }
    }

    public void OnClickItem()
    {
        Button btn = EventSystem.current.currentSelectedGameObject.GetComponent<Button>();
        PurchaseItemType itemType = int.Parse(btn.transform.parent.name).ToItemType();
        switch(itemType)
        {
            case PurchaseItemType.ExtendLimit: InGameManager.InstStage.UseItemExtendsLimits(); break;
            case PurchaseItemType.RemoveIce: InGameManager.InstStage.UseItemBreakce(5); break;
            case PurchaseItemType.MakeSkill1: InGameManager.InstStage.UseItemMakeSkill1(5); break;
            case PurchaseItemType.MakeSkill2: InGameManager.InstStage.UseItemMakeSkill2(5); break;
            default: break;
        }

        Purchases.UseItem(itemType);
        btn.GetComponent<Image>().color = Color.gray;
        btn.enabled = false;
    }

    public void ReduceLimit()
    {
        int remain = mStageInfo.MoveLimit - InGameManager.InstStage.GetBillboard().MoveCount;
        remain = Mathf.Max(0, remain);
        Limit.text = remain.ToString();
        //Limit.GetComponent<Animation>().Play("touch");
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

            SoundPlayer.Inst.PlayerBack.Stop();
            SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectSuccess);
            MenuComplete.PopUp(mStageInfo.Num, starCount, ScoreBarObj.CurrentScore, isFirstClear, isFirstThreeStar);
        }
        else
        {
            SoundPlayer.Inst.PlayerBack.Stop();
            SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectGameOver);
            MenuFailed.PopUp();
        }

        string log = mStageInfo.ToCSVString() + "," + InGameManager.InstStage.GetBillboard().ToCSVString();
        LOG.echo(log);
        InGameManager.InstStage.CleanUpGame();
        Hide();
    }
    public void ReduceGoalValue(Vector3 worldPos, StageGoalType type)
    {
        if (type != mStageInfo.GoalTypeEnum)
            return;

        GameObject GoalTypeObj = GameObject.Instantiate(ItemPrefab, worldPos, Quaternion.identity, EffectParent.transform);
        Image img = GoalTypeObj.GetComponent<Image>();
        img.sprite = mStageInfo.GoalTypeImage;
        StartCoroutine(AnimateItem(GoalTypeObj, TargetValue.transform.position, () =>
        {
            int value = int.Parse(TargetValue.text) - 1;
            value = Mathf.Max(0, value);
            TargetValue.text = value.ToString();
            //TargetValue.GetComponent<Animation>().Play("touch");
        }));
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
}
