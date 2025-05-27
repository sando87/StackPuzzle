using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class PVPScoreBar : MonoBehaviour
{
    [SerializeField] private Image ScoreSubBar = null;
    [SerializeField] private Transform RootScoreArea = null;
    [SerializeField] private Image CurrentScoreBar = null;

    public int CurrentScore { get; private set; } = 0;

    private Image mPrevSub = null;

    void Awake()
    {
        mPrevSub = CurrentScoreBar;
    }

    public void AddScore(int score)
    {
        if (CurrentScore * score > 0)
        {
            DoEffectAddScore(Mathf.Abs(score));
        }
        else
        {
            DoEffectSubScore(Mathf.Abs(score));
        }

        CurrentScore += score;
    }

    public void DoEffectAddScore(float score)
    {
        float width = score;
        Image newSubScoreBar = Instantiate(ScoreSubBar, mPrevSub.transform);
        newSubScoreBar.name = "Add";
        bool isAddedPrevious = mPrevSub == CurrentScoreBar || mPrevSub.name.Contains("Add");

        newSubScoreBar.rectTransform.pivot = new Vector2(0, 0.5f);
        newSubScoreBar.rectTransform.anchorMin = new Vector2(isAddedPrevious ? 1 : 0, 0.5f);
        newSubScoreBar.rectTransform.anchorMax = new Vector2(isAddedPrevious ? 1 : 0, 0.5f);
        newSubScoreBar.rectTransform.SetAnchoredPosX(0);
        newSubScoreBar.rectTransform.SetAnchoredWidth(width);

        mPrevSub = newSubScoreBar;
        
        newSubScoreBar.DOColor(Color.white, 5.5f).From(Color.green)
        .OnComplete(() =>
        {
            float newWidth = CurrentScoreBar.rectTransform.sizeDelta.x + width;
            CurrentScoreBar.rectTransform.SetAnchoredWidth(newWidth);

            if (newSubScoreBar.transform.childCount > 0)
            {
                Transform childSubBar = newSubScoreBar.transform.GetChild(0);
                childSubBar.SetParent(CurrentScoreBar.transform);
                RectTransform childRect = childSubBar.GetComponent<RectTransform>();
                childRect.SetAnchoredPosX(0);
                childRect.anchorMin = new Vector2(1, 0.5f);
                childRect.anchorMax = new Vector2(1, 0.5f);
            }
            else
            {
                mPrevSub = CurrentScoreBar;
            }

            Destroy(newSubScoreBar.gameObject);
        });
    }
    public void DoEffectSubScore(int score)
    {
        float width = score;
        Image newSubScoreBar = Instantiate(ScoreSubBar, mPrevSub.transform);
        newSubScoreBar.name = "Sub";
        bool isAddedPrevious = mPrevSub == CurrentScoreBar || mPrevSub.name.Contains("Add");

        newSubScoreBar.rectTransform.pivot = new Vector2(1, 0.5f);
        newSubScoreBar.rectTransform.anchorMin = new Vector2(isAddedPrevious ? 1 : 0, 0.5f);
        newSubScoreBar.rectTransform.anchorMax = new Vector2(isAddedPrevious ? 1 : 0, 0.5f);
        newSubScoreBar.rectTransform.SetAnchoredPosX(0);
        newSubScoreBar.rectTransform.SetAnchoredWidth(width);

        mPrevSub = newSubScoreBar;

        newSubScoreBar.DOColor(Color.white, 5.5f).From(Color.red)
        .OnComplete(() =>
        {
            float newWidth = CurrentScoreBar.rectTransform.sizeDelta.x - width;
            CurrentScoreBar.rectTransform.SetAnchoredWidth(newWidth);

            if (newSubScoreBar.transform.childCount > 0)
            {
                Transform childSubBar = newSubScoreBar.transform.GetChild(0);
                childSubBar.SetParent(CurrentScoreBar.transform);
                RectTransform childRect = childSubBar.GetComponent<RectTransform>();
                childRect.SetAnchoredPosX(0);
                childRect.anchorMin = new Vector2(1, 0.5f);
                childRect.anchorMax = new Vector2(1, 0.5f);
            }
            else
            {
                mPrevSub = CurrentScoreBar;
            }

            Destroy(newSubScoreBar.gameObject);
        });
    }
    public void DoFlush(int score)
    {
        float width = score;
        Image newSubScoreBar = Instantiate(ScoreSubBar, RootScoreArea);
        newSubScoreBar.color = Color.blue;
        newSubScoreBar.rectTransform.pivot = new Vector2(0, 0.5f);
        newSubScoreBar.rectTransform.anchorMin = new Vector2(0, 0.5f);
        newSubScoreBar.rectTransform.anchorMax = new Vector2(0, 0.5f);
        newSubScoreBar.rectTransform.SetAnchoredPosX(0);
        newSubScoreBar.rectTransform.SetAnchoredWidth(width);

        float newWidth = CurrentScoreBar.rectTransform.sizeDelta.x - width;
        CurrentScoreBar.transform.SetParent(newSubScoreBar.transform);
        CurrentScoreBar.rectTransform.SetAnchoredPosX(width);
        CurrentScoreBar.rectTransform.SetAnchoredWidth(newWidth);

        newSubScoreBar.rectTransform.DOAnchorPosX(-width, 5.5f)
        .OnComplete(() =>
        {
            CurrentScoreBar.transform.SetParent(RootScoreArea);
            CurrentScoreBar.rectTransform.SetAnchoredPosX(0);
            Destroy(newSubScoreBar.gameObject);
        });
    }




}
