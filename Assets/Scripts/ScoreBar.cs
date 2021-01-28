using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScoreBar : MonoBehaviour
{
    private int mTotalScore = UserSetting.ScorePerBar;
    private int mCurrentScore = 0;
    private int mAddedScore = 0;

    public Image ScoreBarMain;
    public Image ScoreBarEffect;
    public GameObject GroupLine;
    public GameObject SplitBarPrefab;

    public int CurrentScore { get { return mCurrentScore + mAddedScore; } }
    
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        mCurrentScore += mAddedScore;
        mAddedScore = 0;
        float rate = (float)mCurrentScore / mTotalScore;
        if (rate < 1)
        {
            ScoreBarMain.fillAmount = rate;
            ScoreBarEffect.fillAmount = 0;
        }
        else
        {
            mTotalScore *= 3;
            RenewSplitBar();
            rate = (float)mCurrentScore / mTotalScore;
            ScoreBarMain.fillAmount = rate;
            ScoreBarEffect.fillAmount = 0;
        }
    }

    public void AddScore(int score)
    {
        mAddedScore += score;
    }
    public void Init(int curScore, int totalScore)
    {
        mTotalScore = totalScore;
        mCurrentScore = curScore;
        RenewSplitBar();
    }
    public void Clear()
    {
        ScoreBarMain.fillAmount = 0;
        ScoreBarEffect.gameObject.SetActive(false);

        mAddedScore = 0;
        mCurrentScore = 0;
        mTotalScore = UserSetting.ScorePerBar;

        RenewSplitBar();
    }

    private void RenewSplitBar()
    {
        foreach (Transform chid in GroupLine.transform)
            Destroy(chid.gameObject);

        int count = mTotalScore / UserSetting.ScorePerSplitBar;
        float gap = ((float)UserSetting.ScorePerSplitBar / mTotalScore) * GroupLine.GetComponent<RectTransform>().sizeDelta.x;
        for (int i = 0; i < count; ++i)
        {
            GameObject subBar = Instantiate(SplitBarPrefab, GroupLine.transform);
            subBar.transform.localPosition = new Vector3(gap * (i + 1), 0, 0);
        }
    }


    private void UpdateScoreBar()
    {
        if (mAddedScore <= 0)
            return;

        int scorePerBar = UserSetting.ScorePerBar;
        if (mAddedScore < 30)
        {
            mCurrentScore += mAddedScore;
            mAddedScore = 0;
            int n = mCurrentScore % scorePerBar;
            ScoreBarMain.fillAmount = n / (float)scorePerBar;
            ScoreBarEffect.gameObject.SetActive(false);
        }
        else if ((mCurrentScore + mAddedScore) / scorePerBar > mCurrentScore / scorePerBar)
        {
            mCurrentScore += mAddedScore;
            mAddedScore = 0;

            int preScore = (mCurrentScore / scorePerBar) * scorePerBar;
            int addScore = mCurrentScore % scorePerBar;
            StartCoroutine(AnimScoreBarEffect(preScore, addScore));
        }
        else
        {
            StartCoroutine(AnimScoreBarEffect(mCurrentScore, mAddedScore));

            mCurrentScore += mAddedScore;
            mAddedScore = 0;
        }
    }
    private IEnumerator AnimScoreBarEffect(int prevScore, int addedScore)
    {
        int scorePerBar = UserSetting.ScorePerBar;
        int nextScore = prevScore + addedScore;
        float totalWidth = ScoreBarMain.GetComponent<RectTransform>().rect.width;
        float fromRate = (prevScore % scorePerBar) / (float)scorePerBar;
        float toRate = (nextScore % scorePerBar) / (float)scorePerBar;
        float bar2Width = totalWidth * (toRate - fromRate) + 1;
        ScoreBarMain.fillAmount = fromRate;
        ScoreBarEffect.gameObject.SetActive(true);
        RectTransform rt = ScoreBarEffect.GetComponent<RectTransform>();
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
            ScoreBarMain.fillAmount = slope1 * time * time + fromRate;
            rt.sizeDelta = size;
            time += Time.deltaTime;
            yield return null;
        }

        ScoreBarMain.fillAmount = toRate;
        ScoreBarEffect.gameObject.SetActive(false);

    }
    
}
