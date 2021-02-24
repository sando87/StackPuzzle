using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
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
    public Image TargetType;
    public NumbersUI ComboNumber;
    public ScoreBar ScoreBarObj;

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

        Limit.text = info.MoveLimit.ToString();
        TargetType.sprite = info.GoalTypeImage;
        TargetValue.text = info.GoalValue.ToString();
        ComboNumber.Clear();
        ScoreBarObj.Clear();

        InGameManager.InstStage.EventBreakTarget = (pos, type) => {
            ReduceGoalValue(pos, type);
        };
        InGameManager.InstStage.EventMatched = (products) => {
            ScoreBarObj.SetScore(ScoreBarObj.CurrentScore + products[0].Combo * products.Length);
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
    }

    public int CurrentCombo
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
            int starCount = InGameManager.InstStage.GetBillboard().GetGrade(mStageInfo);
            Stage currentStage = StageManager.Inst.GetStage(mStageInfo.Num);
            currentStage.UpdateStarCount(starCount);

            bool isFirstClear = false;
            Stage nextStage = StageManager.Inst.GetStage(mStageInfo.Num + 1);
            if (nextStage != null)
            {
                isFirstClear = nextStage.Locked;
                nextStage.UnLock();
            }

            SoundPlayer.Inst.Player.Stop();
            SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectSuccess);
            MenuComplete.PopUp(mStageInfo.Num, starCount, ScoreBarObj.CurrentScore, isFirstClear);
        }
        else
        {
            SoundPlayer.Inst.Player.Stop();
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
}
