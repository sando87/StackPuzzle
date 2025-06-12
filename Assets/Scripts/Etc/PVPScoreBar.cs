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
    [SerializeField] public Image HitPoint = null;

    public int CurrentScore { get; private set; } = 0;
    public bool IsFlushable 
    { 
        get 
        { 
            return Time.time > mTouchedTime + UserSetting.ChocoFlushInterval 
                    && Mathf.Abs(CurrentScore) >= UserSetting.ScorePerAttack 
                    && mIsTweening == 0; 
        }
    }

    private Image mPrevSub = null;
    private float mTouchedTime = 0;
    private int mIsTweening = 0;

    void Awake()
    {
        mPrevSub = CurrentScoreBar;
    }

    public void Init()
    {
        CurrentScore = 0;
        CurrentScoreBar.transform.SetParent(RootScoreArea);
        CurrentScoreBar.rectTransform.SetAnchoredPosX(0);
        CurrentScoreBar.rectTransform.SetAnchoredWidth(0);

        HitPoint.transform.SetParent(CurrentScoreBar.transform);
        HitPoint.rectTransform.anchorMin = new Vector2(1, 0.5f);
        HitPoint.rectTransform.anchorMax = new Vector2(1, 0.5f);
        HitPoint.rectTransform.SetAnchoredPosX(0);

        mPrevSub = CurrentScoreBar;
        mIsTweening = 0;
    }

    void UpdateCurrentScoreBar(int score)
    {
        float newWidth = Mathf.Abs(score);
        CurrentScoreBar.rectTransform.SetAnchoredWidth(newWidth);
        CurrentScoreBar.color = score > 0 ? new Color(0.2f, 1, 0.2f, 1) : new Color(1, 0.2f, 0.2f, 1);
    }

    public void AddScore(int score)
    {
        int newScore = CurrentScore + score;
        if(CurrentScore != 0 && newScore * CurrentScore <= 0)
        {
            // 점수가 반대방향으로 전환될 때 예외처리
            HitPoint.transform.SetParent(RootScoreArea);
            if(CurrentScoreBar.transform.childCount > 0)
            {
                Destroy(CurrentScoreBar.transform.GetChild(0).gameObject);
            }

            UpdateCurrentScoreBar(newScore);

            HitPoint.transform.SetParent(CurrentScoreBar.transform);
            HitPoint.rectTransform.anchorMin = new Vector2(1, 0.5f);
            HitPoint.rectTransform.anchorMax = new Vector2(1, 0.5f);
            HitPoint.rectTransform.SetAnchoredPosX(0);

            mPrevSub = CurrentScoreBar;
            mIsTweening = 0;
        }
        else
        {
            if (CurrentScore * score >= 0)
            {
                // 게이지가 증가해야 할때(양수에 더하거나 음수에서 뺄 때)
                DoEffectAddScore(score);
            }
            else
            {
                // 게이지가 감소해야 할때(양수에서 빼거나 음수에서 더할 때)
                DoEffectSubScore(score);
            }
        }

        CurrentScore += score;
        mTouchedTime = Time.time;
    }

    void DoEffectAddScore(float score)
    {
        float width = Mathf.Abs(score);
        Image newSubScoreBar = Instantiate(ScoreSubBar, mPrevSub.transform);
        newSubScoreBar.name = "Add";
        bool isAddedPrevious = mPrevSub == CurrentScoreBar || mPrevSub.name.Contains("Add");

        newSubScoreBar.rectTransform.pivot = new Vector2(0, 0.5f);
        newSubScoreBar.rectTransform.anchorMin = new Vector2(isAddedPrevious ? 1 : 0, 0.5f);
        newSubScoreBar.rectTransform.anchorMax = new Vector2(isAddedPrevious ? 1 : 0, 0.5f);
        newSubScoreBar.rectTransform.SetAnchoredPosX(0);
        newSubScoreBar.rectTransform.SetAnchoredWidth(width);

        HitPoint.transform.SetParent(newSubScoreBar.transform);
        HitPoint.rectTransform.anchorMin = new Vector2(1, 0.5f);
        HitPoint.rectTransform.anchorMax = new Vector2(1, 0.5f);
        HitPoint.rectTransform.SetAnchoredPosX(0);

        mPrevSub = newSubScoreBar;

        mIsTweening++;
        Color startColor = score > 0 ? new Color(0.7f, 1, 0.7f, 1) : new Color(1, 0.7f, 0.7f, 1);
        Color endColor = score > 0 ? new Color(0.2f, 1, 0.2f, 1) : new Color(1, 0.2f, 0.2f, 1);
        newSubScoreBar.DOColor(endColor, 2.0f).From(startColor)
        .OnComplete(() =>
        {
            mIsTweening--;
            mTouchedTime = Time.time;
            float newWidth = CurrentScoreBar.rectTransform.sizeDelta.x + width;
            CurrentScoreBar.rectTransform.SetAnchoredWidth(newWidth);
            CurrentScoreBar.color = CurrentScore > 0 ? new Color(0.2f, 1, 0.2f, 1) : new Color(1, 0.2f, 0.2f, 1);

            if (mPrevSub == newSubScoreBar)
            {
                HitPoint.transform.SetParent(CurrentScoreBar.transform);
                HitPoint.rectTransform.anchorMin = new Vector2(1, 0.5f);
                HitPoint.rectTransform.anchorMax = new Vector2(1, 0.5f);
                HitPoint.rectTransform.SetAnchoredPosX(0);

                mPrevSub = CurrentScoreBar;
                mIsTweening = 0;
                UpdateCurrentScoreBar(CurrentScore);
            }
            else
            {
                Transform childSubBar = newSubScoreBar.transform.GetChild(0);
                childSubBar.SetParent(CurrentScoreBar.transform);
                RectTransform childRect = childSubBar.GetComponent<RectTransform>();
                childRect.SetAnchoredPosX(0);
                childRect.anchorMin = new Vector2(1, 0.5f);
                childRect.anchorMax = new Vector2(1, 0.5f);
            }

            Destroy(newSubScoreBar.gameObject);
        });
    }
    void DoEffectSubScore(int score)
    {
        float width = Mathf.Abs(score);
        Image newSubScoreBar = Instantiate(ScoreSubBar, mPrevSub.transform);
        newSubScoreBar.name = "Sub";
        bool isAddedPrevious = mPrevSub == CurrentScoreBar || mPrevSub.name.Contains("Add");

        newSubScoreBar.rectTransform.pivot = new Vector2(1, 0.5f);
        newSubScoreBar.rectTransform.anchorMin = new Vector2(isAddedPrevious ? 1 : 0, 0.5f);
        newSubScoreBar.rectTransform.anchorMax = new Vector2(isAddedPrevious ? 1 : 0, 0.5f);
        newSubScoreBar.rectTransform.SetAnchoredPosX(0);
        newSubScoreBar.rectTransform.SetAnchoredWidth(width);

        HitPoint.transform.SetParent(newSubScoreBar.transform);
        HitPoint.rectTransform.anchorMin = new Vector2(0, 0.5f);
        HitPoint.rectTransform.anchorMax = new Vector2(0, 0.5f);
        HitPoint.rectTransform.SetAnchoredPosX(0);

        mPrevSub = newSubScoreBar;

        mIsTweening++;
        Color startColor = score > 0 ? new Color(0, 1, 0, 1) : new Color(1, 0, 0, 1);
        Color endColor = score > 0 ? new Color(1, 1, 1, 1) : new Color(1, 1, 1, 1);
        newSubScoreBar.DOColor(endColor, 2.0f).From(startColor)
        .OnComplete(() =>
        {
            mIsTweening--;
            mTouchedTime = Time.time;
            float newWidth = CurrentScoreBar.rectTransform.sizeDelta.x - width;
            CurrentScoreBar.rectTransform.SetAnchoredWidth(newWidth);
            CurrentScoreBar.color = CurrentScore > 0 ? new Color(0.2f, 1, 0.2f, 1) : new Color(1, 0.2f, 0.2f, 1);

            if (mPrevSub == newSubScoreBar)
            {
                HitPoint.transform.SetParent(CurrentScoreBar.transform);
                HitPoint.rectTransform.anchorMin = new Vector2(1, 0.5f);
                HitPoint.rectTransform.anchorMax = new Vector2(1, 0.5f);
                HitPoint.rectTransform.SetAnchoredPosX(0);

                mPrevSub = CurrentScoreBar;
                mIsTweening = 0;
                UpdateCurrentScoreBar(CurrentScore);
            }
            else
            {
                Transform childSubBar = newSubScoreBar.transform.GetChild(0);
                childSubBar.SetParent(CurrentScoreBar.transform);
                RectTransform childRect = childSubBar.GetComponent<RectTransform>();
                childRect.SetAnchoredPosX(0);
                childRect.anchorMin = new Vector2(1, 0.5f);
                childRect.anchorMax = new Vector2(1, 0.5f);
            }

            Destroy(newSubScoreBar.gameObject);
        });
    }
    public int DoFlush(int score)
    {
        if(Mathf.Abs(score) > Mathf.Abs(CurrentScore))
            return 0;
        
        float width = Mathf.Abs(score);
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

        CurrentScore = CurrentScore > 0 ? CurrentScore - score : CurrentScore + score;

        mIsTweening = 1;
        newSubScoreBar.rectTransform.DOAnchorPosX(-width, 2.0f).SetEase(Ease.InQuad)
        .OnComplete(() =>
        {
            mIsTweening = 0;
            mTouchedTime = Time.time;
            CurrentScoreBar.transform.SetParent(RootScoreArea);
            CurrentScoreBar.rectTransform.SetAnchoredPosX(0);
            Destroy(newSubScoreBar.gameObject);
        });

        return score;
    }
}
