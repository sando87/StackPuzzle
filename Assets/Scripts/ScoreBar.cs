using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScoreBar : MonoBehaviour
{
    private int mCurrentScore = 0;
    private int mAddedScore = 0;

    public Image ScoreBarMain;
    public Image ScoreBarEffect;
    public GameObject GroupLine;

    public int CurrentScore { get { return mCurrentScore + mAddedScore; } }
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        UpdateScoreBar();
    }

    public void AddScore(int score)
    {
        mAddedScore += score;
    }
    public void Clear()
    {
        ScoreBarMain.fillAmount = 0;
        ScoreBarEffect.gameObject.SetActive(false);

        mAddedScore = 0;
        mCurrentScore = 0;

        int count = GroupLine.transform.childCount;
        for (int i = 5; i < count; ++i)
            Destroy(GroupLine.transform.GetChild(i).gameObject);
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
    private void UpdateGroupLine()
    {

    }
}
