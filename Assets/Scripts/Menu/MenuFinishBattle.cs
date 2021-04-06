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

    public static void PopUp(bool win, UserInfo info, int deltaExp)
    {
        GameObject objMenu = GameObject.Find(UIObjName).gameObject;
        objMenu.SetActive(true);

        MenuFinishBattle menu = objMenu.GetComponent<MenuFinishBattle>();
        menu.WinEffect.SetActive(win);
        menu.LoseEffect.SetActive(!win);

        menu.StartCoroutine(menu.AnimateExp(info.score - deltaExp, info.score));

        if (UserSetting.IsBotPlayer)
            menu.StartCoroutine(menu.AutoNext());
    }

    private IEnumerator AnimateExp(int fromScore, int toScore)
    {
        int prvLevel = UserSetting.ToLevel(fromScore);
        int score = fromScore;
        UpdateExpBar(score);
        while (score != toScore)
        {
            yield return new WaitForSeconds(0.2f);
            score += toScore > fromScore ? 1 : -1;
            if(prvLevel < UserSetting.ToLevel(score))
            {
                prvLevel = UserSetting.ToLevel(score);
                //MenuLevel Pop UP here!!
            }
            UpdateExpBar(score);
        }
    }
    private void UpdateExpBar(int score)
    {
        int level = UserSetting.ToLevel(score);
        Level.text = level.ToString();
        Exp.text = score.ToString();
        int dd = score % UserSetting.ScorePerLevel;
        float rate = (float)dd / UserSetting.ScorePerLevel;
        ExpBar.normalizedValue = rate;
    }

    public void OnOK()
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
        gameObject.SetActive(false);
        MenuWaitMatch.PopUp();
    }

    public void OnClose()
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
        gameObject.SetActive(false);
        MenuStages.PopUp();
    }

    private IEnumerator AutoNext()
    {
        yield return new WaitForSeconds(1);
        OnOK();
    }
}
