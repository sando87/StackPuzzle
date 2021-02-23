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
    public TextMeshProUGUI Score;
    public TextMeshProUGUI StageLevel;
    public GameObject CoinPrefab;
    public GameObject FireworkPrefab;
    private List<GameObject> Effects = new List<GameObject>();

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

        Score.text = score.ToString();
        StageLevel.text = "Stage " + level.ToString();

        gameObject.SetActive(true);

        int gold = score / UserSetting.ScorePerCoin;
        Purchases.AddGold(gold);

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
        float duration = 3.0f;
        float curScore = score;
        int prvCoinCount = score / UserSetting.ScorePerCoin;
        while (curScore > 0)
        {
            float step = score * Time.deltaTime / duration;
            curScore -= step;
            int curCoinCount = (int)(curScore / UserSetting.ScorePerCoin);
            if(prvCoinCount != curCoinCount)
            {
                prvCoinCount = curCoinCount;
                GameObject coinObj = Instantiate(CoinPrefab, Score.transform.position, Quaternion.identity, transform);
                Effects.Add(coinObj);
                StartCoroutine(UnityUtils.AnimateConvex(coinObj, RewardCoin.transform.position, 0.5f, () =>
                {
                    Destroy(coinObj);
                    RewardCoin.Play("push", -1, 0);
                }));
                SoundPlayer.Inst.PlaySoundEffect(ClipSound.Coin);
            }
            yield return null;
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
