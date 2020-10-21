using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuInGame : MonoBehaviour
{
    private static MenuInGame mInst = null;
    private const string UIObjName = "MenuInGame";
    private const int mScorePerBar = 300;
    private StageInfo mStageInfo;
    private int mAddedScore;
    private int mCurrentScore;

    public Text CurrentScore;
    public Text KeepCombo;
    public Text Limit;
    public Text StageLevel;
    public Text TargetValue;
    public Image TargetType;
    public Image Lock;
    public Image UnLock;
    public Image ScoreBar1;
    public Image ScoreBar2;
    public Image BarStar1;
    public Image BarStar2;
    public Image BarStar3;
    public Image BarStar4;
    public Image BarStar5;
    public GameObject ParentPanel;
    public GameObject ComboText;
    public GameObject GameField;
    public GameObject GoalTypePrefab;

    private void Update()
    {
#if PLATFORM_ANDROID
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnPause();
        }
#endif
        UpdateScore();

        CheckFinish();
    }

    private void UpdateScore()
    {
        if (mAddedScore <= 0)
            return;

        if (mAddedScore < 30)
        {
            mCurrentScore += mAddedScore;
            mAddedScore = 0;
            int n = mCurrentScore % mScorePerBar;
            ScoreBar1.fillAmount = n / (float)mScorePerBar;
            ScoreBar2.gameObject.SetActive(false);
            CurrentScore.text = mCurrentScore.ToString();
            CurrentScore.GetComponent<Animation>().Play("touch");
        }
        else if ((mCurrentScore+mAddedScore)/mScorePerBar > mCurrentScore/mScorePerBar)
        {
            mCurrentScore += mAddedScore;
            mAddedScore = 0;
            int n = mCurrentScore % mScorePerBar;
            ScoreBar1.fillAmount = n / (float)mScorePerBar;
            ScoreBar2.gameObject.SetActive(false);
            CurrentScore.text = mCurrentScore.ToString();
            CurrentScore.GetComponent<Animation>().Play("touch");
        }
        else
        {
            StartCoroutine(ScoreBarEffect(mCurrentScore, mAddedScore));
            mCurrentScore += mAddedScore;
            mAddedScore = 0;
            int n = mCurrentScore % mScorePerBar;
            CurrentScore.text = mCurrentScore.ToString();
            CurrentScore.GetComponent<Animation>().Play("touch");

        }

        BarStar1.gameObject.SetActive(mCurrentScore / mScorePerBar > 0);
        BarStar2.gameObject.SetActive(mCurrentScore / mScorePerBar > 1);
        BarStar3.gameObject.SetActive(mCurrentScore / mScorePerBar > 2);
        BarStar4.gameObject.SetActive(mCurrentScore / mScorePerBar > 3);
        BarStar5.gameObject.SetActive(mCurrentScore / mScorePerBar > 4);
    }

    private void CheckFinish()
    {
        if (!InGameManager.Inst.IsIdle)
            return;

        if (mStageInfo.GoalType == "Score")
        {
            if (mCurrentScore > mStageInfo.GoalValue)
                InGameManager.Inst.FinishGame(true);
        }
        else
        {
            if (TargetValue.text == "0")
                InGameManager.Inst.FinishGame(true);
        }

        if(Limit.text == "0")
        {
            InGameManager.Inst.FinishGame(false);
        }
    }
    private IEnumerator ScoreBarEffect(int prevScore, int addedScore)
    {
        int nextScore = prevScore + addedScore;
        float totalWidth = ScoreBar1.sprite.rect.width;
        float fromRate = (prevScore % mScorePerBar) / (float)mScorePerBar;
        float toRate = (nextScore % mScorePerBar) / (float)mScorePerBar;
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

    public static MenuInGame Inst()
    {
        if(mInst == null)
            mInst = GameObject.Find("UIGroup").transform.Find(UIObjName).gameObject.GetComponent<MenuInGame>();
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
        mStageInfo = info;
        ScoreBar1.fillAmount = 0;
        ScoreBar2.gameObject.SetActive(false);
        BarStar1.gameObject.SetActive(false);
        BarStar2.gameObject.SetActive(false);
        BarStar3.gameObject.SetActive(false);
        BarStar4.gameObject.SetActive(false);
        BarStar5.gameObject.SetActive(false);
        Lock.gameObject.SetActive(false);
        UnLock.gameObject.SetActive(true);


        mAddedScore = 0;
        mCurrentScore = 0;
        CurrentScore.text = "0";
        Limit.text = info.MoveLimit.ToString();
        TargetType.sprite = info.GoalTypeImage;
        TargetValue.text = info.GoalValue.ToString();
        KeepCombo.text = "0";
        StageLevel.text = info.Num.ToString();

        //GameField.GetComponent<InGameManager>().EventOnChange = UpdatePanel;
    }

    public void AddScore(Product product)
    {
        mAddedScore += product.Combo;
        GameObject comboTextObj = GameObject.Instantiate(ComboText, product.transform.position, Quaternion.identity, ParentPanel.transform);
        Text combo = comboTextObj.GetComponent<Text>();
        combo.text = product.Combo.ToString();
        StartCoroutine(ComboEffect(comboTextObj));
    }
    IEnumerator ComboEffect(GameObject obj)
    {
        float time = 0;
        while(time < 0.7)
        {
            float x = (time * 10) + 1;
            float y = (1 / x) * Time.deltaTime;
            Vector3 pos = obj.transform.position;
            pos.y += y;
            obj.transform.position = pos;
            time += Time.deltaTime;
            yield return null;
        }
        Destroy(obj);
    }

    public void SetNextCombo(int combo)
    {
        KeepCombo.text = combo.ToString();
        KeepCombo.GetComponent<Animation>().Play("touch");
    }

    public void ReduceLimit()
    {
        int value = int.Parse(Limit.text) - 1;
        value = Mathf.Max(0, value);
        Limit.text = value.ToString();
        Limit.GetComponent<Animation>().Play("touch");
    }

    public void ReduceGoalValue(Vector3 worldPos, StageGoalType type)
    {
        if (type != mStageInfo.GoalTypeEnum)
            return;

        GameObject GoalTypeObj = GameObject.Instantiate(GoalTypePrefab, worldPos, Quaternion.identity, ParentPanel.transform);
        Image img = GoalTypeObj.GetComponent<Image>();
        img.sprite = mStageInfo.GoalTypeImage;
        StartCoroutine(SkillMatchedEffect(GoalTypeObj));
    }
    IEnumerator SkillMatchedEffect(GameObject obj)
    {
        float duration = Random.Range(0.4f, 0.5f);
        float height = 1.2f;
        float a = -1 * height / (duration * duration);
        Vector3 startPos = obj.transform.position;
        Vector3 offset = Vector3.zero;
        float dx = 1;
        float time = 0;
        while (time < duration)
        {
            offset.y = a * (time - duration) * (time - duration) + height;
            offset.x = dx * time;
            obj.transform.position = startPos + offset;
            time += Time.deltaTime;
            yield return null;
        }

        duration = Random.Range(0.2f, 0.3f);
        time = 0;
        startPos = obj.transform.position;
        Vector3 destPos = TargetValue.transform.position;
        Vector3 dir = destPos - startPos;
        float slope = dir.magnitude / (duration * duration);
        dir.Normalize();
        while (time < duration)
        {
            float dist = slope * time * time;
            obj.transform.position = startPos + (dir * dist);
            time += Time.deltaTime;
            yield return null;
        }

        int value = int.Parse(TargetValue.text) - 1;
        value = Mathf.Max(0, value);
        TargetValue.text = value.ToString();
        TargetValue.GetComponent<Animation>().Play("touch");
        Destroy(obj);
    }

    public void OnPause()
    {
        MenuPause.PopUp();
    }
    public void OnLockMatch(bool enableLock)
    {
        GameField.GetComponent<InGameManager>().MatchLock = enableLock;
        Lock.gameObject.SetActive(enableLock);
        UnLock.gameObject.SetActive(!enableLock);
    }
}
