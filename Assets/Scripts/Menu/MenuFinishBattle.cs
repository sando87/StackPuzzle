using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuFinishBattle : MonoBehaviour
{
    private const string UIObjName = "UISpace/CanvasPopup/PlayFinishPVP";

    public GameObject WinEffect;
    public GameObject LoseEffect;
    public Slider ExpBar;
    public TextMeshProUGUI Level;
    public TextMeshProUGUI Exp;
    private bool IsWin = false;

    public static void PopUp(bool win, int prevScore)
    {
        GameObject objMenu = GameObject.Find(UIObjName).gameObject;
        objMenu.SetActive(true);

        MenuFinishBattle menu = objMenu.GetComponent<MenuFinishBattle>();
        menu.Init(win, prevScore);
    }

    private void Init(bool win, int prevScore)
    {
        IsWin = win;
        WinEffect.SetActive(win);
        LoseEffect.SetActive(!win);
        StartCoroutine("AnimateExp", prevScore);

        if (UserSetting.IsBotPlayer)
            StartCoroutine("AutoNext");
    }

    private IEnumerator AnimateExp(int prevScore)
    {
        UpdateExpBar(prevScore);
        while (prevScore == UserSetting.UserScore)
            yield return null;

        int fromScore = prevScore;
        int toScore = UserSetting.UserScore;

        int prvLevel = Utils.ToLevel(fromScore);
        int score = fromScore;
        UpdateExpBar(score);
        while (score != toScore)
        {
            yield return new WaitForSeconds(0.05f);
            score += toScore > fromScore ? 1 : -1;
            if(prvLevel < Utils.ToLevel(score))
            {
                prvLevel = Utils.ToLevel(score);
                //MenuLevel Pop UP here!!
            }
            UpdateExpBar(score);
        }
    }
    private void UpdateExpBar(int score)
    {
        int level = Utils.ToLevel(score);
        Level.text = level.ToString();
        Exp.text = score.ToString();
        int dd = score % Utils.ScorePerLevel;
        float rate = (float)dd / Utils.ScorePerLevel;
        ExpBar.normalizedValue = rate;
    }

    public void OnOK()
    {
        NextMenu();
    }

    public void OnClose()
    {
        NextMenu();
    }


    private void NextMenu()
    {
        if (IsWin)
        {
            //이겼을때 광고 스킵
            GoNext();
        }
        else
        {
            //졌을때 광고 재생
            if (GoogleADMob.Inst.RemainSec(AdsType.MissionFailed) <= 0
                    && GoogleADMob.Inst.IsLoaded(AdsType.MissionFailed)
                    && !Purchases.IsAdsSkip())
            {
                GoogleADMob.Inst.Show(AdsType.MissionFailed, (reward) =>
                {
                    GoNext();
                });
            }
            else //졌지만 보여줄 광고가 없을때 스킵
                GoNext();
        }
    }

    private void GoNext()
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
        StopCoroutine("AnimateExp");
        StopCoroutine("AutoNext");
        gameObject.SetActive(false);
        MenuWaitMatch.PopUp();
        SoundPlayer.Inst.PlayBackMusic(SoundPlayer.Inst.BackMusicMap);
    }

    private IEnumerator AutoNext()
    {
        yield return new WaitForSeconds(1);
        OnOK();
    }
}
