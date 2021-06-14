using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScoreBar : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI TotalScore = null;
    [SerializeField] private GameObject StarA = null;
    [SerializeField] private GameObject StarB = null;
    [SerializeField] private GameObject StarC = null;
    [SerializeField] private Slider MainBar = null;
    [SerializeField] private GameObject GroupLine = null;
    [SerializeField] private GameObject SplitBarPrefab = null;
    [SerializeField] private GameObject ValueEndPos = null;

    //private int ScorePerBar = UserSetting.ScorePerBar;
    public int ScorePerBar { get; private set; } = UserSetting.ScorePerBar;
    public int CurrentScore { get; private set; } = 0;
    public Vector3 EndPosition { get { return ValueEndPos.transform.position; } }
    public int CurrentStarCount { get { return StarC.activeSelf ? 3 : (StarB.activeSelf ? 2 : (StarA.activeSelf ? 1 : 0)); } }

    public void Init(int scorePerBar)
    {
        ScorePerBar = scorePerBar;
        CurrentScore = 0;
        MainBar.normalizedValue = 0;
        StarA.SetActive(false);
        StarB.SetActive(false);
        StarC.SetActive(false);

        if (TotalScore != null)
            TotalScore.text = CurrentScore.ToString();

        UpdateSplitBar();
    }


    public void SetScore(int score)
    {
        score = Mathf.Max(0, score);
        CurrentScore = score;
        UpdateScoreDisplay();
        UpdateSplitBar();
    }

    private void UpdateScoreDisplay()
    {
        if(TotalScore != null)
            TotalScore.text = CurrentScore.ToString();

        float rate = CurrentScore / (float)ScorePerBar;;
        MainBar.normalizedValue = Mathf.Min(1.0f, rate);

        StarA.SetActive(rate >= 0.5f);
        StarB.SetActive(rate >= 0.75f);
        StarC.SetActive(rate >= 1);
    }

    private void UpdateSplitBar()
    {
        int targetScore = Mathf.Max(CurrentScore, ScorePerBar);
        int preLineCount = GroupLine.transform.childCount - 1;
        int curLineCount = (targetScore / 100);
        if (preLineCount < curLineCount)
        {
            for (int i = preLineCount; i < curLineCount; ++i)
                Instantiate(SplitBarPrefab, GroupLine.transform);
        }
        else if (preLineCount > curLineCount)
        {
            for (int i = 1; i < GroupLine.transform.childCount; ++i)
                Destroy(GroupLine.transform.GetChild(i).gameObject);

            for (int i = 0; i < curLineCount; ++i)
                Instantiate(SplitBarPrefab, GroupLine.transform);
        }
    }

}
