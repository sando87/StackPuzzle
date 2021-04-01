using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuComplete : MonoBehaviour
{
    private const string UIObjName = "UISpace/CanvasPopup/PlayComplete";

    public Image Star1;
    public Image Star2;
    public Image Star3;
    public Animator RewardCoin;
    public TextMeshProUGUI StageLevel;
    public TextMeshProUGUI GoldValue;
    public ScoreBar ScoreDisplay;
    public GameObject CoinPrefab;
    public GameObject FireworkPrefab;
    public GameObject RewardPrefab;
    public GameObject RewardParent;
    private List<GameObject> Effects = new List<GameObject>();
    private int ScorePerCoin = UserSetting.ScorePerCoin;

    public static void PopUp(int level, int starCount, int score, bool isFirstClear)
    {
        GameObject menuComp = GameObject.Find(UIObjName);

        MenuComplete menu = menuComp.GetComponent<MenuComplete>();
        menu.UpdateUIState(level, starCount, score, isFirstClear);

        if (UserSetting.IsBotPlayer)
            menu.StartCoroutine(menu.AutoEnd());
    }
    IEnumerator AutoEnd()
    {
        yield return new WaitForSeconds(1);
        MenuStages.Inst.AutoStartAfterSec(1);
        OnNext();
    }

    private void UpdateUIState(int level, int starCount, int score, bool isFirstClear)
    {
        foreach (var effect in Effects)
            if(effect != null)
                Destroy(effect);
        Effects.Clear();

        StageInfo stageInfo = StageInfo.Load(level);

        if (stageInfo.GoalTypeEnum == StageGoalType.Score)
            ScoreDisplay.ScorePerBar = stageInfo.GoalValue;
        else
            ScoreDisplay.ScorePerBar = UserSetting.ScorePerBar;

        ScoreDisplay.Clear();
        ScoreDisplay.SetScore(score);
        StageLevel.text = "STAGE " + level.ToString() + " Clear!!";
        GoldValue.text = "0";

        gameObject.SetActive(true);

        int coin = score / UserSetting.ScorePerCoin;
        if (coin < 12)
        {
            ScorePerCoin = score / UnityEngine.Random.Range(10, 14);
            coin = score / ScorePerCoin;
        }
        else
        {
            ScorePerCoin = UserSetting.ScorePerCoin;
            coin = score / ScorePerCoin;
        }
        Purchases.AddGold(coin * UserSetting.GoldPerCoin);

        StartCoroutine(AnimateReward(score));
        StartCoroutine(AnimateStars(starCount));
        StartCoroutine(AnimateFireworkParticles());
    }

    IEnumerator AnimateFireworkParticles()
    {
        for(int i = 0; i < 6; ++i)
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
        if(starCount >= 1)
            SoundPlayer.Inst.PlaySoundEffect(ClipSound.Star1);
        yield return new WaitForSeconds(1);
        Star2.gameObject.SetActive(starCount >= 2);
        if(starCount >= 2)
            SoundPlayer.Inst.PlaySoundEffect(ClipSound.Star2);
        yield return new WaitForSeconds(1);
        Star3.gameObject.SetActive(starCount >= 3);
        if(starCount >= 3)
            SoundPlayer.Inst.PlaySoundEffect(ClipSound.Star3);
    }
    IEnumerator AnimateReward(int score)
    {
        yield return new WaitForSeconds(0.5f);
        float duration = 3.0f;
        float curScore = score;
        int prvCoinCount = score / ScorePerCoin;
        while (curScore > 0)
        {
            float step = score * Time.deltaTime / duration;
            curScore -= step;
            ScoreDisplay.SetScore((int)curScore);
            int curCoinCount = (int)(curScore / ScorePerCoin);
            if(prvCoinCount != curCoinCount)
            {
                prvCoinCount = curCoinCount;
                GameObject coinObj = Instantiate(CoinPrefab, ScoreDisplay.transform.position, Quaternion.identity, RewardCoin.transform);
                Effects.Add(coinObj);
                StartCoroutine(UnityUtils.AnimateThrow(coinObj));
                SoundPlayer.Inst.PlaySoundEffect(ClipSound.Coin1);
            }
            yield return null;
        }
        yield return new WaitForSeconds(1);
        StartCoroutine(AnimateCollectCoins());
    }
    private IEnumerator AnimateCollectCoins()
    {
        int speed = 0;
        Image[] coins = RewardCoin.GetComponentsInChildren<Image>();
        while (true)
        {
            yield return null;
            bool isAllDone = true;
            foreach(Image coin in coins)
            {
                if (coin == null || coin.gameObject == RewardCoin.gameObject)
                    continue;

                isAllDone = false;
                Vector3 dir = coin.transform.localPosition;
                dir.Normalize();
                coin.transform.localPosition -= speed * dir * Time.deltaTime;
                if(Vector3.Dot(dir, coin.transform.localPosition) < 0)
                {
                    SoundPlayer.Inst.PlaySoundEffect(ClipSound.Coin2);
                    RewardCoin.Play("push", -1, 0);
                    int curGold = int.Parse(GoldValue.text);
                    GoldValue.text = (curGold + UserSetting.GoldPerCoin).ToString();
                    Destroy(coin.gameObject);
                }
            }

            speed += 50;
            if (isAllDone)
                break;
        }
    }

    public void OnNext()
    {
        foreach (var effect in Effects)
            if (effect != null)
                Destroy(effect);
        Effects.Clear();

        gameObject.SetActive(false);
        MenuInGame.Hide();
        MenuStages.PopUp();
        StageManager.Inst.Activate(true);
        SoundPlayer.Inst.PlayBackMusic(SoundPlayer.Inst.BackMusicMap);
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
    }
}
