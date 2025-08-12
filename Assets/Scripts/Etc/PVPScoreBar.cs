using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
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
    public Vector3 RootPosition { get { return RootScoreArea.position; } }
    public Vector3 HitPointPosition { get { return HitPoint.transform.position; } }
    public bool IsIdle
    {
        get
        {
            return Time.time > mTouchedTime + UserSetting.IceFlushInterval && !mIsOnWaitting && mTweenCounter == 0;
        }
    }

    private Image mPrevSub = null;
    private float mTouchedTime = 0;
    private int mZoomIndex = 0;
    private float mWidthPerScore = 4.0f; // 스코어 1점을 UI상 표현하는 너비
    private int mMaxAttackCount = 256; // UI창에서 표현할 수 있는 최대 얼음 조각 개수
    private int MaxZoomCount { get { return SubGroup.Length - 1; } }
    private float mAccScoreOnWaiting = 0;
    private bool mIsOnWaitting = false;
    private int mTweenCounter = 0;

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

        // HitPoint.transform.SetParent(CurrentScoreBar.transform);
        // HitPoint.rectTransform.anchorMin = new Vector2(1, 0.5f);
        // HitPoint.rectTransform.anchorMax = new Vector2(1, 0.5f);
        HitPoint.rectTransform.SetAnchoredPosX(0);

        mPrevSub = CurrentScoreBar;
        mZoomIndex = 0;
        RootScoreArea.DOKill();
        RootScoreArea.localScale = Vector3.one;

        mTouchedTime = 0;
        mAccScoreOnWaiting = 0;
        mIsOnWaitting = false;
        mTweenCounter = 0;

        InitZoomUISet();
        InitFlushImageObjects();
    }

    void InitFlushImageObjects()
    {
        if (SubGroup[0].childCount > 0)
            return;

        float stepWidth = UserSetting.ScorePerAttack * mWidthPerScore;
        float totalWidth = UserSetting.ScorePerAttack * mMaxAttackCount * mWidthPerScore;
        flushImageRoot.rectTransform.SetAnchoredWidth(totalWidth);
        int step = 1;
        float height = 0.25f;

        for (int i = step; i <= mMaxAttackCount; i += step)
        {
            if (i % 4 == 0) continue;

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
                if (IsIdle && Mathf.Abs(CurrentScore) < UserSetting.ScorePerAttack)
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
        RootScoreArea.transform.DOScaleX(1f / pow, zommingDuration);
        flushImageRoot.rectTransform.DOSizeDelta(new Vector2(totalWidth / pow, sizeDelta.y), zommingDuration);
        BlocksZoomingEffect(zoomLevel - 1, 0, zommingDuration);
        BlocksZoomingEffect(zoomLevel, 1, zommingDuration);
        BlocksZoomingEffect(zoomLevel + 1, 1, zommingDuration);
    }
    void BlocksZoomingEffect(int targetZoomLevel, float zoomScale, float duration)
    {
        if (targetZoomLevel < 0 || targetZoomLevel >= SubGroup.Length)
            return;

        foreach (Transform block in SubGroup[targetZoomLevel])
        {
            block.DOScale(zoomScale, duration);
        }
    }
    int CalculateCurrentZoomLevel()
    {
        float currentScore = Mathf.Abs(CurrentScore);
        for (int zoomIdx = 0; zoomIdx < MaxZoomCount; zoomIdx++)
        {
            float scorePerBar = UserSetting.ScorePerAttack * Mathf.Pow(4, zoomIdx + 1);
            float minScore = zoomIdx == 0 ? 0 : scorePerBar * 0.1f;
            float maxScore = zoomIdx == MaxZoomCount ? scorePerBar : scorePerBar * 0.75f;
            if (minScore <= currentScore && currentScore <= maxScore)
            {
                return zoomIdx;
            }
        }

        return 0;
    }

    public void AddScore(int score)
    {
        int newScore = CurrentScore + score;
        if (CurrentScore != 0 && newScore * CurrentScore <= 0)
        {
            mTweenCounter = 0;
            UpdateScoreBar(newScore);
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
        int absScore = Mathf.Abs(score);
        if (mIsOnWaitting)
        {
            mAccScoreOnWaiting += absScore;
        }

        float newWidth = Mathf.Abs(CurrentScore) * mWidthPerScore;
        HitPoint.rectTransform.SetAnchoredPosX(newWidth);
    }

    void UpdateScoreBar(int score)
    {
        CurrentScoreBar.DOKill();
        CurrentScoreBar.rectTransform.DOKill();
        for (int i = CurrentScoreBar.transform.childCount - 1; i >= 0; --i)
        {
            Destroy(CurrentScoreBar.transform.GetChild(i).gameObject);
        }

        float newWidth = Mathf.Abs(score) * mWidthPerScore;
        CurrentScoreBar.rectTransform.SetAnchoredWidth(newWidth);
        CurrentScoreBar.color = score > 0 ? SocreColorPlayer : SocreColorOpponent;
    }

    void DoEffectAddScore(int score)
    {
        int nextNewScore = CurrentScore + score;
        UpdateScoreBar(CurrentScore);

        float width = Mathf.Abs(score) * mWidthPerScore;
        Image newSubScoreBar = Instantiate(ScoreSubBar, CurrentScoreBar.transform);
        newSubScoreBar.name = "Add";

        newSubScoreBar.rectTransform.pivot = new Vector2(0, 0.5f);
        newSubScoreBar.rectTransform.anchorMin = new Vector2(1, 0.5f);
        newSubScoreBar.rectTransform.anchorMax = new Vector2(1, 0.5f);
        newSubScoreBar.rectTransform.SetAnchoredPosX(0);
        newSubScoreBar.rectTransform.SetAnchoredWidth(width);
        newSubScoreBar.color = Color.white;

        Vector2 prevSize = CurrentScoreBar.rectTransform.sizeDelta;
        Vector2 addedSize = newSubScoreBar.rectTransform.sizeDelta;
        CurrentScoreBar.rectTransform.DOSizeDelta(new Vector2(prevSize.x + addedSize.x, prevSize.y), 2.0f).SetEase(Ease.OutQuad).SetDelay(0.5f);

        mTweenCounter = 1;
        newSubScoreBar.rectTransform.DOSizeDelta(new Vector2(0, addedSize.y), 2.0f).SetEase(Ease.OutQuad).SetDelay(0.5f)
        .OnComplete(() =>
        {
            mTweenCounter = 0;
            UpdateScoreBar(nextNewScore);
        });
    }
    void DoEffectSubScore(int score)
    {
        UpdateScoreBar(CurrentScore + score);

        float width = Mathf.Abs(score) * mWidthPerScore;
        Image newSubScoreBar = Instantiate(ScoreSubBar, CurrentScoreBar.transform);
        newSubScoreBar.name = "Sub";

        newSubScoreBar.rectTransform.pivot = new Vector2(0, 0.5f);
        newSubScoreBar.rectTransform.anchorMin = new Vector2(1, 0.5f);
        newSubScoreBar.rectTransform.anchorMax = new Vector2(1, 0.5f);
        newSubScoreBar.rectTransform.SetAnchoredPosX(0);
        newSubScoreBar.rectTransform.SetAnchoredWidth(width);
        newSubScoreBar.color = Color.white;

        mTweenCounter = 1;
        Vector2 subSize = newSubScoreBar.rectTransform.sizeDelta;
        newSubScoreBar.rectTransform.DOSizeDelta(new Vector2(0, subSize.y), 2.0f).SetEase(Ease.OutQuad).SetDelay(0.5f)
        .OnComplete(() =>
        {
            mTweenCounter = 0;
            if (newSubScoreBar == null)
                Destroy(newSubScoreBar.gameObject);

        });
    }
    public int DoFlush(int score)
    {
        if (Mathf.Abs(score) > Mathf.Abs(CurrentScore))
            return 0;

        float width = Mathf.Abs(score) * mWidthPerScore;
        Image newSubScoreBar = Instantiate(ScoreSubBar, RootScoreArea);
        newSubScoreBar.color = Color.blue;
        newSubScoreBar.rectTransform.pivot = new Vector2(0, 0.5f);
        newSubScoreBar.rectTransform.anchorMin = new Vector2(0, 0.5f);
        newSubScoreBar.rectTransform.anchorMax = new Vector2(0, 0.5f);
        newSubScoreBar.rectTransform.SetAnchoredPosX(0);
        newSubScoreBar.rectTransform.SetAnchoredWidth(width);

        CurrentScore = CurrentScore > 0 ? CurrentScore - score : CurrentScore + score;
        UpdateScoreBar(CurrentScore);
        CurrentScoreBar.transform.SetParent(newSubScoreBar.transform);
        CurrentScoreBar.rectTransform.SetAnchoredPosX(width);

        mTouchedTime = Time.time;
        mTweenCounter = 1;
        newSubScoreBar.rectTransform.DOAnchorPosX(-width, 2.0f).SetEase(Ease.OutQuad).SetDelay(0.5f)
        .OnComplete(() =>
        {
            mTweenCounter = 0;
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
        while (true)
        {
            LockImage.enabled = false;
            yield return new WaitForSeconds(interval);
            LockImage.enabled = true;
            yield return new WaitForSeconds(interval);
        }
    }

    public void WaitStart()
    {
        if (!mIsOnWaitting)
        {
            mAccScoreOnWaiting = 0;
            mIsOnWaitting = true;
        }
    }
    public void WaitEnd()
    {
        float refScoreForTouch = UserSetting.ScorePerAttack * Mathf.Pow(4, mZoomIndex);
        if (mAccScoreOnWaiting > refScoreForTouch)
        {
            mTouchedTime = Time.time;
        }

        mAccScoreOnWaiting = 0;
        mIsOnWaitting = false;
    }
    public void SetIceBlockLevel(int level)
    {
        foreach (Transform subGroup in flushImageRoot.transform)
        {
            int count = 0;
            foreach (Transform child in subGroup)
            {
                child.GetComponentInChildren<TextMeshProUGUI>().text = level == 1 ? " " : "x" + level;
                count++;
                if (count >= 4)
                    break;
            }
        }
    }
}
