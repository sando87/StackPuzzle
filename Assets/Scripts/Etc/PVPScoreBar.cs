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
    public Transform mRootScoreArea = null;
    public Image mCurrentScoreBar = null;

    public int CurrentScore { get; private set; } = 0;

    private Image mGreenSub = null;
    private Image mRedSub = null;

    void Awake()
    {
        mGreenSub = mCurrentScoreBar;
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
        Image newSubScoreBar = Instantiate(ScoreSubBar, mGreenSub.transform);
        newSubScoreBar.rectTransform.SetAnchoredPosX(mGreenSub.rectTransform.sizeDelta.x);
        newSubScoreBar.rectTransform.SetAnchoredWidth(width);
        mGreenSub = newSubScoreBar;

        newSubScoreBar.DOColor(Color.white, 5.5f).From(Color.green)
        .OnComplete(() =>
        {
            float newWidth = mCurrentScoreBar.rectTransform.sizeDelta.x + width;
            mCurrentScoreBar.rectTransform.SetAnchoredWidth(newWidth);
            mGreenSub = mCurrentScoreBar;

            if (newSubScoreBar.transform.childCount > 0)
            {
                newSubScoreBar.transform.GetChild(0).SetParent(mCurrentScoreBar.transform);
            }
            Destroy(newSubScoreBar.gameObject);
        });
    }
    public void DoEffectSubScore(int score)
    {
        float width = score;
        float newWidth = mCurrentScoreBar.rectTransform.sizeDelta.x - width;

        Image newSubScoreBar = Instantiate(ScoreSubBar, mCurrentScoreBar.transform);
        newSubScoreBar.rectTransform.SetAnchoredPosX(newWidth);
        newSubScoreBar.rectTransform.SetAnchoredWidth(width);

        mCurrentScoreBar.rectTransform.SetAnchoredWidth(newWidth);

        if (mRedSub == null)
        {
            mRedSub = newSubScoreBar;
        }
        else
        {
            mRedSub.transform.SetParent(newSubScoreBar.transform);
            mRedSub = newSubScoreBar;
        }

        newSubScoreBar.DOColor(Color.white, 5.5f).From(Color.red)
        .OnComplete(() =>
        {
            if (mRedSub == newSubScoreBar)
            {
                mRedSub = null;
            }
            
            Destroy(newSubScoreBar.gameObject);
        });
    }


    
    
}
