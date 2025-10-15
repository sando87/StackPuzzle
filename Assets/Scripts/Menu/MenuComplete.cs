using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuComplete : MonoBehaviour
{
    private const string UIObjName = "UISpace/CanvasGameUI/PlayComplete";

    public Image Star1;
    public Image Star2;
    public Image Star3;
    public TextMeshProUGUI StageLevel;
    public ScoreBar ScoreDisplay;
    public GameObject CoinPrefab;
    public GameObject FireworkPrefab;
    public RewardUISet _RewardUISet;
    public GameObject EventItemFXPrefab;

    private List<GameObject> Effects = new List<GameObject>();
    private int ScorePerCoin = UserSetting.ScorePerCoin;
    private int mScore = 0;
    private bool mIsFirstClear = false;
    private bool mIsFirst3StarClear = false;

    public static void PopUp(int level, int starCount, int score, bool isFirstClear, bool isFirstThreeStar)
    {
        GameObject menuComp = GameObject.Find(UIObjName);
        menuComp.SetActive(true);

        MenuComplete menu = menuComp.GetComponent<MenuComplete>();
        menu.UpdateUIState(level, starCount, score, isFirstClear, isFirstThreeStar);

        if (UserSetting.IsBotPlayer)
            menu.StartCoroutine(menu.AutoEnd());
    }
    IEnumerator AutoEnd()
    {
        yield return new WaitForSeconds(1);
        MenuStages.Inst.AutoStartAfterSec(1);
        OnNext();
    }

    private void UpdateUIState(int level, int starCount, int score, bool isFirstClear, bool isFirstThreeStar)
    {
        mScore = score;
        UpdateScorePerCoin();
        
        mIsFirstClear = isFirstClear;
        mIsFirst3StarClear = isFirstThreeStar;

        foreach (var effect in Effects)
            if (effect != null)
                Destroy(effect);
        Effects.Clear();

        StageInfo stageInfo = StageInfo.Load(level);

        ScoreDisplay.Init(score);
        ScoreDisplay.SetScore(score);
        StageLevel.text = string.Format(LocaleManager.Inst.DoLocaleText("STAGE {0} CLEAR!!", UserSetting.CurrentLang), level);

        _RewardUISet.UpdateForRewarding(stageInfo, isFirstClear, isFirstThreeStar);

        StartCoroutine(AnimateCollectCoins(score));
        StartCoroutine(AnimateStars(starCount));
        StartCoroutine(AnimateFireworkParticles());
    }

    IEnumerator AnimateFireworkParticles()
    {
        for (int i = 0; i < 6; ++i)
        {
            float xOff = Random.Range(-300.0f, 300.0f);
            float yOff = Random.Range(-300.0f, 300.0f);
            float size = Random.Range(50.0f, 150.0f);
            ParticleSystem particle = Instantiate(FireworkPrefab, transform).GetComponent<ParticleSystem>();
            particle.transform.localPosition = new Vector3(xOff, 300 + yOff, 0);
            particle.transform.localScale = new Vector3(size, size, 1);
            Effects.Add(particle.gameObject);
            SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectFirework);
            yield return new WaitForSeconds(Random.Range(5, 10) * 0.1f);
        }
    }
    IEnumerator AnimateStars(int starCount)
    {
        Star1.gameObject.SetActive(false);
        Star2.gameObject.SetActive(false);
        Star3.gameObject.SetActive(false);
        yield return new WaitForSeconds(0.5f);
        Star1.gameObject.SetActive(starCount >= 1);
        if (starCount >= 1)
            SoundPlayer.Inst.PlaySoundEffect(ClipSound.Star1);
        yield return new WaitForSeconds(1);
        Star2.gameObject.SetActive(starCount >= 2);
        if (starCount >= 2)
            SoundPlayer.Inst.PlaySoundEffect(ClipSound.Star2);
        yield return new WaitForSeconds(1);
        Star3.gameObject.SetActive(starCount >= 3);
        if (starCount >= 3)
            SoundPlayer.Inst.PlaySoundEffect(ClipSound.Star3);
        yield return new WaitForSeconds(1.5f);

        if (mIsFirst3StarClear)
            StartCoroutine(AnimateEventItem());
    }
    IEnumerator AnimateEventItem()
    {
        UserSetting.UserSettingInfo.GetRateRangeOfEventItem(UserSetting.EventItemExpPerWin, out float rateFrom, out float rateTo);
        GameObject targetObj = _RewardUISet.EventItemReward;
        SoundPlayer.Inst.PlaySoundEffect(ClipSound.Star1);

        GameObject starA = Instantiate(EventItemFXPrefab, Star1.transform.position, Quaternion.identity, targetObj.transform);
        starA.transform.DOLocalMove(Vector3.zero, 1.0f);
        Effects.Add(starA);
        GameObject starB = Instantiate(EventItemFXPrefab, Star2.transform.position, Quaternion.identity, targetObj.transform);
        starB.transform.DOLocalMove(Vector3.zero, 1.0f);
        Effects.Add(starB);
        GameObject starC = Instantiate(EventItemFXPrefab, Star3.transform.position, Quaternion.identity, targetObj.transform);
        starC.transform.DOLocalMove(Vector3.zero, 1.0f);
        Effects.Add(starC);

        yield return new WaitForSeconds(1f);
        
        SoundPlayer.Inst.PlaySoundEffect(ClipSound.Star2);
        _RewardUISet.SetEventItemRate(rateFrom, rateTo, 1.5f);
        yield return new WaitForSeconds(1.5f);

        if (rateTo >= 1)
        {
            Sprite nextEventItemImage = UserSetting.UserSettingInfo.GetNextEventItem().GetSprite();
            _RewardUISet.ChangeEventItemTween(nextEventItemImage);
        }

    }
    IEnumerator AnimateCollectCoins(int score)
    {
        yield return new WaitForSeconds(0.5f);
        float duration = 3.0f;
        float curScore = score;
        
        UpdateScorePerCoin();
        int prvCoinCount = score / ScorePerCoin;
        int curGold = 0;
        TextMeshProUGUI goldVal = _RewardUISet.GoldReward.parent.GetComponentInChildren<TextMeshProUGUI>();
        while (curScore > 0)
        {
            float step = score * Time.deltaTime / duration;
            curScore -= step;
            ScoreDisplay.SetScore((int)curScore);
            int curCoinCount = (int)(curScore / ScorePerCoin);
            if (prvCoinCount != curCoinCount)
            {
                prvCoinCount = curCoinCount;
                GameObject coinObj = Instantiate(CoinPrefab, ScoreDisplay.EndPosition, Quaternion.identity, _RewardUISet.GoldReward.transform);
                Effects.Add(coinObj);
                coinObj.transform.DOLocalMove(Vector3.zero, 1).OnComplete(() =>
                {
                    curGold += UserSetting.GoldPerCoin;
                    goldVal.text = curGold.ToString();
                    Destroy(coinObj);
                });
                SoundPlayer.Inst.PlaySoundEffect(ClipSound.Coin1);
            }
            yield return null;
        }
    }

    public void OnNext()
    {
        DoReward();

        TryRequestReview();

        foreach (var effect in Effects)
            if (effect != null)
                Destroy(effect);
        Effects.Clear();

        gameObject.SetActive(false);
        MenuInGame.Hide();
        MenuStages.PopUp();
        SoundPlayer.Inst.PlayBackMusic(SoundPlayer.Inst.BackMusicMap);
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
    }

    void UpdateScorePerCoin()
    {
        int coin = mScore / UserSetting.ScorePerCoin;
        if (coin < 12)
        {
            ScorePerCoin = mScore / UnityEngine.Random.Range(10, 14);
        }
        else
        {
            ScorePerCoin = UserSetting.ScorePerCoin;
        }
    }

    void DoReward()
    {
        UpdateScorePerCoin();
        int coin = mScore / ScorePerCoin;
        Purchases.AddGold(coin * UserSetting.GoldPerCoin);

        if (mIsFirstClear)
        {
            _RewardUISet.DoReward();
        }

        if (mIsFirst3StarClear)
        {
            UserSetting.UserSettingInfo.AddExpOfEventItem(UserSetting.EventItemExpPerWin);

            // 이벤트 게이지 완료시 아이템 획득 데이터 처리
            if (UserSetting.UserSettingInfo.IsDoneEventItem())
            {
                Purchases.AddItem(UserSetting.UserSettingInfo.CurrentEventItem);
                UserSetting.UserSettingInfo.ResetNextNewEventItem();
            }
        }
    }

    private void TryRequestReview()
    {
        if (UserSetting.IsReviewed) return;
        if (UserSetting.IsBotPlayer) return;
        if (UserSetting.UserInfo.StartCount < 3) return;
        if (UserSetting.GetHighestStageNumber() < 50) return;

        UserSetting.IsReviewed = true;
        StoreReviewManager.Inst.RequestReview();
    }
}
