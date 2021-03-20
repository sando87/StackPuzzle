using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScoreBar : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI TotalScore = null;
    [SerializeField] private TextMeshProUGUI StarCount = null;
    [SerializeField] private Slider MainBar = null;
    [SerializeField] private Slider EffectBar = null;
    [SerializeField] private GameObject GroupLine = null;
    [SerializeField] private GameObject SplitBarPrefab = null;

    //private int ScorePerBar = UserSetting.ScorePerBar;
    public int ScorePerBar { get; set; } = UserSetting.ScorePerBar;
    public int CurrentScore { get; private set; } = 0;

    public void Clear()
    {
        CurrentScore = 0;
        MainBar.normalizedValue = 0;

        if (TotalScore != null)
            TotalScore.text = CurrentScore.ToString();

        if (StarCount != null)
            StarCount.text = "0";

        InitSplitBar();
    }


    public void SetScore(int score)
    {
        score = Mathf.Max(0, score);
        if (CurrentScore / ScorePerBar != score / ScorePerBar)
            GetComponentInChildren<Animation>().Play();

        CurrentScore = score;
        UpdateScoreDisplay();
    }

    private void UpdateScoreDisplay()
    {
        if(TotalScore != null)
            TotalScore.text = CurrentScore.ToString();

        float rate = GetRate(CurrentScore);
        MainBar.normalizedValue = rate;

        UpdateStarCount();
    }

    private float GetRate(int score)
    {
        return (score % ScorePerBar) / (float)ScorePerBar;
    }
    private IEnumerator AnimateRate(float fromRate)
    {
        float targetRate = GetRate(CurrentScore);
        EffectBar.gameObject.SetActive(true);
        EffectBar.normalizedValue = targetRate;
        float time = 0;
        float duration = 0.2f;
        float term = targetRate - fromRate;
        while (time < duration)
        {
            float rate = UnityUtils.AccelPlus(time, duration);
            float curRate = fromRate + term * rate;
            MainBar.normalizedValue = curRate;
            time += Time.deltaTime;
            yield return null;
        }
        EffectBar.gameObject.SetActive(false);
    }

    private void InitSplitBar()
    {
        foreach (Transform chid in GroupLine.transform)
            Destroy(chid.gameObject);

        int count = ScorePerBar / UserSetting.ScorePerSplitBar;
        float gap = (1.0f / count) * GroupLine.GetComponent<RectTransform>().rect.width;
        for (int i = 0; i < count; ++i)
        {
            GameObject subBar = Instantiate(SplitBarPrefab, GroupLine.transform);
            RectTransform tr = subBar.GetComponent<RectTransform>();
            tr.anchoredPosition = new Vector2(gap * (i + 1), 0);
        }
    }

    private void UpdateStarCount()
    {
        if (StarCount == null)
            return;

        int starCount = CurrentScore / ScorePerBar;
        StarCount.text = starCount.ToString();
    }

}
