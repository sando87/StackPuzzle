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

    public static void PopUp(bool win, int prevScore)
    {
        GameObject objMenu = GameObject.Find(UIObjName).gameObject;
        objMenu.SetActive(true);

        MenuFinishBattle menu = objMenu.GetComponent<MenuFinishBattle>();
        menu.Init(win, prevScore);
    }

    private void Init(bool win, int prevScore)
    {
        WinEffect.SetActive(win);
        LoseEffect.SetActive(!win);
        StartCoroutine("AnimateExp", prevScore);

        if (UserSetting.IsBotPlayer)
            StartCoroutine("AutoNext");
    }

    private IEnumerator AnimateExp(int prevScore)
    {
        while (prevScore == UserSetting.UserScore)
            yield return null;

        int fromScore = prevScore;
        int toScore = UserSetting.UserScore;

        int prvLevel = Utils.ToLevel(fromScore);
        int score = fromScore;
        UpdateExpBar(score);
        while (score != toScore)
        {
            yield return new WaitForSeconds(0.1f);
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
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
        StopCoroutine("AnimateExp");
        StopCoroutine("AutoNext");
        gameObject.SetActive(false);
        MenuWaitMatch.PopUp();
        SoundPlayer.Inst.PlayBackMusic(SoundPlayer.Inst.BackMusicMap);
    }

    public void OnClose()
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
