using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScoreBar : MonoBehaviour
{
    [SerializeField] private Text StarCount = null;
    [SerializeField] private Slider MainBar = null;
    [SerializeField] private Slider EffectBar = null;
    [SerializeField] private GameObject GroupLine = null;
    [SerializeField] private GameObject SplitBarPrefab = null;

    private int ScorePerBar = UserSetting.ScorePerBar;
    public int CurrentScore { get; private set; }

    public void Init()
    {
        Clear();
        InitSplitBar();
    }
    public void Clear()
    {
        CurrentScore = 0;
        MainBar.normalizedValue = 0;
        EffectBar.normalizedValue = 0;
        EffectBar.gameObject.SetActive(false);
        if(StarCount != null)
        {
            StarCount.text = "0";
            StarCount.transform.parent.gameObject.SetActive(false);
        }
    }

    public void AddScore(int score)
    {
        float prvRate = GetRate(CurrentScore);
        CurrentScore += score;
        float nextRate = GetRate(CurrentScore);
        if (prvRate > nextRate)
            prvRate = 0;

        if(score > 100)
        {
            StopCoroutine("AnimateRate");
            StartCoroutine("AnimateRate", prvRate);
        }
        else
        {
            StopCoroutine("AnimateRate");
            EffectBar.gameObject.SetActive(false);
            MainBar.normalizedValue = nextRate;
        }

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
        float duration = 1.0f;
        float term = targetRate - fromRate;
        while (time < duration)
        {
            float curRate = fromRate + term * (time / duration);
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

        int count = UserSetting.ScorePerBar / UserSetting.ScorePerSplitBar;
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

        int starCount = CurrentScore / UserSetting.ScorePerBar;
        if (starCount <= 0)
            StarCount.transform.parent.gameObject.SetActive(false);
        else
        {
            StarCount.transform.parent.gameObject.SetActive(true);
            StarCount.text = starCount.ToString();
        }
    }

}
