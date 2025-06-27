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
    [SerializeField] private Image flushImageRoot = null;
    [SerializeField] private Transform[] SubGroup = null;
    [SerializeField] private Sprite[] FlushImages = null;
    [SerializeField] private Image FlushImagePrafab = null;
    [SerializeField] private Image LockImage = null;

    private Color SocreColorPlayer = new Color(30f / 255f, 199f / 255f, 26f / 255f, 1);
    private Color SocreColorOpponent = new Color(199f / 255f, 47f / 255f, 26f / 255f, 1);

    public int CurrentScore { get; private set; } = 0;
    public bool IsIdle 
    { 
        get 
        { 
            return Time.time > mTouchedTime + UserSetting.ChocoFlushInterval && mIsTweening == 0;
        }
    }

    private Image mPrevSub = null;
    private float mTouchedTime = 0;
    private int mIsTweening = 0;
    private int mZoomIndex = 0;
    private float mWidthPerScore = 4.0f; // 스코어 1점을 UI상 표현하는 너비
    private int mMaxAttackCount = 256; // UI창에서 표현할 수 있는 최대 얼음 조각 개수
    private int MaxZoomCount { get { return SubGroup.Length - 1; } }

    void Awake()
    {
        mPrevSub = CurrentScoreBar;
        StartCoroutine(CoZoomInOut());
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
        mZoomIndex = 0;
        RootScoreArea.DOKill();
        RootScoreArea.localScale = Vector3.one;

        InitZoomUISet();
        InitFlushImageObjects();
    }

    void InitFlushImageObjects()
    {
        if(SubGroup[0].childCount > 0)
            return;

        float stepWidth = UserSetting.ScorePerAttack * mWidthPerScore;
        float totalWidth = UserSetting.ScorePerAttack * mMaxAttackCount * mWidthPerScore;
        flushImageRoot.rectTransform.SetAnchoredWidth(totalWidth);
        int step = 1;
        float height = 0.25f;

        for (int i = step; i <= mMaxAttackCount; i += step)
        {
            if(i % 4 == 0) continue;

            float offsetPosX = i * stepWidth;
            Image image = Instantiate(FlushImagePrafab, SubGroup[0]);
            image.rectTransform.anchorMin = new Vector2(offsetPosX / totalWidth, height);
            image.rectTransform.anchorMax = new Vector2(offsetPosX / totalWidth, height);
            image.rectTransform.SetAnchoredPosX(0);
            image.sprite = FlushImages[0];
        }

        step = 4;
        for (int i = step; i <= mMaxAttackCount; i += step)
        {
            if (i % 16 == 0) continue;

            float offsetPosX = i * stepWidth;
            Image image = Instantiate(FlushImagePrafab, SubGroup[1]);
            image.rectTransform.anchorMin = new Vector2(offsetPosX / totalWidth, height);
            image.rectTransform.anchorMax = new Vector2(offsetPosX / totalWidth, height);
            image.rectTransform.SetAnchoredPosX(0);
            image.sprite = FlushImages[1];
        }

        step = 16;
        for (int i = step; i <= mMaxAttackCount; i += step)
        {
            if (i % 64 == 0) continue;

            float offsetPosX = i * stepWidth;
            Image image = Instantiate(FlushImagePrafab, SubGroup[2]);
            image.rectTransform.anchorMin = new Vector2(offsetPosX / totalWidth, height);
            image.rectTransform.anchorMax = new Vector2(offsetPosX / totalWidth, height);
            image.rectTransform.SetAnchoredPosX(0);
            image.sprite = FlushImages[2];
        }

        step = 64;
        for (int i = step; i <= mMaxAttackCount; i += step)
        {
            if (i % 256 == 0) continue;

            float offsetPosX = i * stepWidth;
            Image image = Instantiate(FlushImagePrafab, SubGroup[3]);
            image.rectTransform.anchorMin = new Vector2(offsetPosX / totalWidth, height);
            image.rectTransform.anchorMax = new Vector2(offsetPosX / totalWidth, height);
            image.rectTransform.SetAnchoredPosX(0);
            image.sprite = FlushImages[3];
        }

        step = 256;
        for (int i = step; i <= mMaxAttackCount; i += step)
        {
            if (i % 1024 == 0) continue;

            float offsetPosX = i * stepWidth;
            Image image = Instantiate(FlushImagePrafab, SubGroup[4]);
            image.rectTransform.anchorMin = new Vector2(offsetPosX / totalWidth, height);
            image.rectTransform.anchorMax = new Vector2(offsetPosX / totalWidth, height);
            image.rectTransform.SetAnchoredPosX(0);
            image.sprite = FlushImages[4];
        }
    }


    void InitZoomUISet()
    {
        mZoomIndex = 0;
        float totalWidth = UserSetting.ScorePerAttack * mMaxAttackCount * mWidthPerScore;
        Vector2 sizeDelta = flushImageRoot.rectTransform.sizeDelta;
        flushImageRoot.rectTransform.DOKill();
        CurrentScoreBar.transform.DOKill();
        flushImageRoot.rectTransform.sizeDelta = new Vector2(totalWidth, sizeDelta.y);
        CurrentScoreBar.transform.localScale = Vector3.one;
        foreach (Transform sub in SubGroup)
        {
            sub.gameObject.SetActive(true);
        }
    }
    IEnumerator CoZoomInOut()
    {
        while (true)
        {
            int nextZoomLevel = CalculateCurrentZoomLevel();
            if (mZoomIndex < nextZoomLevel)
            {
                SetZoomLevel(nextZoomLevel, 0.5f);
                mZoomIndex = nextZoomLevel;
                yield return new WaitForSeconds(0.5f);
            }
            else if (mZoomIndex > nextZoomLevel)
            {
                if(IsIdle && Mathf.Abs(CurrentScore) < UserSetting.ScorePerAttack)
                {
                    SetZoomLevel(0, 0.5f);
                    mZoomIndex = 0;
                    yield return new WaitForSeconds(0.5f);
                }
            }
            yield return null;
        }
    }

    void SetZoomLevel(int zoomLevel, float zommingDuration)
    {
        Vector2 sizeDelta = flushImageRoot.rectTransform.sizeDelta;
        float totalWidth = UserSetting.ScorePerAttack * mMaxAttackCount * mWidthPerScore;

        float pow = Mathf.Pow(4, zoomLevel);
        CurrentScoreBar.transform.DOScaleX(1f / pow, zommingDuration);
        flushImageRoot.rectTransform.DOSizeDelta(new Vector2(totalWidth / pow, sizeDelta.y), zommingDuration);
        BlocksZoomingEffect(zoomLevel - 1, 0, zommingDuration);
        BlocksZoomingEffect(zoomLevel, 1, zommingDuration);
        BlocksZoomingEffect(zoomLevel + 1, 1, zommingDuration);
    }
    void BlocksZoomingEffect(int targetZoomLevel, float zoomScale, float duration)
    {
        if(targetZoomLevel < 0 || targetZoomLevel >= SubGroup.Length)
            return;

        foreach(Transform block in SubGroup[targetZoomLevel])
        {
            block.DOScale(zoomScale, duration);
        }
    }
    int CalculateCurrentZoomLevel()
    {
        float zoomingTriggerRate = 0.2f;
        float currentScore = Mathf.Abs(CurrentScore);
        for (int zoomLevel = 0; zoomLevel < MaxZoomCount; zoomLevel++)
        {
            float scorePerBar = 50 * Mathf.Pow(4, zoomLevel + 1);
            float minScore = zoomLevel == 0 ? 0 :scorePerBar * (zoomingTriggerRate * 0.5f);
            float maxScore = zoomLevel == MaxZoomCount ? scorePerBar : scorePerBar * (1f - zoomingTriggerRate);
            if (minScore <= currentScore && currentScore <= maxScore)
            {
                return zoomLevel;
            }
        }

        return 0;
    }

    void UpdateCurrentScoreBar(int score)
    {
        float newWidth = Mathf.Abs(score) * mWidthPerScore;
        CurrentScoreBar.rectTransform.SetAnchoredWidth(newWidth);
        CurrentScoreBar.color = score > 0 ? SocreColorPlayer : SocreColorOpponent;
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
        float width = Mathf.Abs(score) * mWidthPerScore;
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
        Color startColor = Color.white;
        Color endColor = score > 0 ? SocreColorPlayer : SocreColorOpponent;
        newSubScoreBar.DOColor(endColor, 2.0f).From(startColor)
        .OnComplete(() =>
        {
            mIsTweening--;
            mTouchedTime = Time.time;
            float newWidth = CurrentScoreBar.rectTransform.sizeDelta.x + width;
            CurrentScoreBar.rectTransform.SetAnchoredWidth(newWidth);
            CurrentScoreBar.color = CurrentScore > 0 ? SocreColorPlayer : SocreColorOpponent;

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
        float width = Mathf.Abs(score) * mWidthPerScore;
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
        Color startColor = Color.white;
        Color endColor = Color.white;
        newSubScoreBar.DOColor(endColor, 2.0f).From(startColor)
        .OnComplete(() =>
        {
            mIsTweening--;
            mTouchedTime = Time.time;
            float newWidth = CurrentScoreBar.rectTransform.sizeDelta.x - width;
            CurrentScoreBar.rectTransform.SetAnchoredWidth(newWidth);
            CurrentScoreBar.color = Color.white;

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
        
        float width = Mathf.Abs(score) * mWidthPerScore;
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
            CurrentScoreBar.transform.SetAsFirstSibling();
            Destroy(newSubScoreBar.gameObject);
        });

        return score;
    }

    public bool IsLocked { get { return LockImage.gameObject.activeSelf; } }
    public void SetLock(float duration)
    {
        LockImage.gameObject.SetActive(true);
        StopCoroutine(nameof(CoFlickLockImage));
        LockImage.DOKill();
        LockImage.DOFade(1, duration * 0.7f).From(1).OnComplete(() =>
        {
            StopCoroutine(nameof(CoFlickLockImage));
            StartCoroutine(CoFlickLockImage(0.3f));
        });
        LockImage.DOFade(1, duration * 0.9f).From(1).OnComplete(() =>
        {
            StopCoroutine(nameof(CoFlickLockImage));
            StartCoroutine(CoFlickLockImage(0.1f));
        });
        LockImage.DOFade(1, duration).From(1).OnComplete(() =>
        {
            StopCoroutine(nameof(CoFlickLockImage));
            LockImage.gameObject.SetActive(false);
        });
    }
    IEnumerator CoFlickLockImage(float interval)
    {
        while(true)
        {
            LockImage.enabled = false;
            yield return new WaitForSeconds(interval);
            LockImage.enabled = true;
            yield return new WaitForSeconds(interval);
        }
    }
}
