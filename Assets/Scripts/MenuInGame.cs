using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class MenuInGame : MonoBehaviour
{
    private static MenuInGame mInst = null;
    private const string UIObjName = "UISpace/CanvasPanel/InGame";
    private StageInfo mStageInfo;
    private int mAddedScore;
    private int mCurrentScore;
    private MenuMessageBox mMenu = null;

    public Text CurrentScore;
    public Text Limit;
    public Text StageLevel;
    public Text ScoreStarCount;
    public Text TargetValue;
    public Image TargetType;
    public Image ScoreBar1;
    public Image ScoreBar2;
    public NumbersUI ComboNumber;

    public GameObject EffectParent;
    public GameObject ItemPrefab;

    public int Score { get { return mCurrentScore + mAddedScore; } }
    public int RemainLimit { get { return int.Parse(Limit.text); } }

    private void Update()
    {
#if PLATFORM_ANDROID
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnPause();
        }
#endif
        UpdateScore();

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
        ScoreBar1.fillAmount = 0;
        ScoreBar2.gameObject.SetActive(false);

        mAddedScore = 0;
        mCurrentScore = 0;
        CurrentScore.text = "0";
        Limit.text = info.MoveLimit.ToString();
        TargetType.sprite = info.GoalTypeImage;
        TargetValue.text = info.GoalValue.ToString();
        StageLevel.text = info.Num.ToString();
        ComboNumber.Clear();

        InGameManager.InstStage.EventBreakTarget = (pos, type) => {
            ReduceGoalValue(pos, type);
        };
        InGameManager.InstStage.EventMatched = (products) => {
            mAddedScore += products[0].Combo * products.Length;
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
        Limit.GetComponent<Animation>().Play("touch");
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
            MenuComplete.PopUp(mStageInfo.Num, starCount, mCurrentScore, isFirstClear);
        }
        else
        {
            SoundPlayer.Inst.Player.Stop();
            SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectGameOver);
            MenuFailed.PopUp();
        }

        string log = mStageInfo.ToCSVString() + "," + InGameManager.InstStage.GetBillboard().ToCSVString();
        LOG.echo(log);
        InGameManager.InstStage.FinishGame();
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
            TargetValue.GetComponent<Animation>().Play("touch");
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

    private void UpdateScore()
    {
        int scorePerBar = UserSetting.ScorePerBar;
        if (mAddedScore <= 0)
            return;

        if (mAddedScore < 30)
        {
            mCurrentScore += mAddedScore;
            mAddedScore = 0;
            int n = mCurrentScore % scorePerBar;
            ScoreBar1.fillAmount = n / (float)scorePerBar;
            ScoreBar2.gameObject.SetActive(false);
            CurrentScore.text = mCurrentScore.ToString();
            CurrentScore.GetComponent<Animation>().Play("touch");
            ScoreStarCount.text = (mCurrentScore / scorePerBar).ToString();
            ScoreStarCount.GetComponent<Animation>().Play("touch");
        }
        else if ((mCurrentScore + mAddedScore) / scorePerBar > mCurrentScore / scorePerBar)
        {
            mCurrentScore += mAddedScore;
            mAddedScore = 0;

            int preScore = (mCurrentScore / scorePerBar) * scorePerBar;
            int addScore = mCurrentScore % scorePerBar;
            StartCoroutine(ScoreBarEffect(preScore, addScore));

            CurrentScore.text = mCurrentScore.ToString();
            CurrentScore.GetComponent<Animation>().Play("touch");
            ScoreStarCount.text = (mCurrentScore / scorePerBar).ToString();
            ScoreStarCount.GetComponent<Animation>().Play("touch");
        }
        else
        {
            StartCoroutine(ScoreBarEffect(mCurrentScore, mAddedScore));

            mCurrentScore += mAddedScore;
            mAddedScore = 0;

            CurrentScore.text = mCurrentScore.ToString();
            CurrentScore.GetComponent<Animation>().Play("touch");
            ScoreStarCount.text = (mCurrentScore / scorePerBar).ToString();
            ScoreStarCount.GetComponent<Animation>().Play("touch");
        }
    }
    private IEnumerator ScoreBarEffect(int prevScore, int addedScore)
    {
        int scorePerBar = UserSetting.ScorePerBar;
        int nextScore = prevScore + addedScore;
        float totalWidth = ScoreBar1.GetComponent<RectTransform>().rect.width;
        float fromRate = (prevScore % scorePerBar) / (float)scorePerBar;
        float toRate = (nextScore % scorePerBar) / (float)scorePerBar;
        float bar2Width = totalWidth * (toRate - fromRate) + 1;
        ScoreBar1.fillAmount = fromRate;
        ScoreBar2.gameObject.SetActive(true);
        RectTransform rt = ScoreBar2.GetComponent<RectTransform>();
        Vector2 pos = rt.anchoredPosition;
        Vector2 size = rt.sizeDelta;
        pos.x = totalWidth * toRate;
        size.x = bar2Width;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        float time = 0;
        float duration = 0.5f;
        float slope1 = (toRate - fromRate) / (duration * duration);
        float slope2 = -bar2Width / (duration * duration);
        while (time < duration)
        {
            size.x = slope2 * time * time + bar2Width;
            ScoreBar1.fillAmount = slope1 * time * time + fromRate;
            rt.sizeDelta = size;
            time += Time.deltaTime;
            yield return null;
        }

        ScoreBar1.fillAmount = toRate;
        ScoreBar2.gameObject.SetActive(false);

    }
}
