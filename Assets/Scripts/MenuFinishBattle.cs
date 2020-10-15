using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuFinishBattle : MonoBehaviour
{
    private const string UIObjName = "CanvasPopUp/MenuFinishBattle";

    public Text Result;
    public Text CurrentScore;
    public Text DeltaScore;

    public static void PopUp(bool win, int currentScore, int deltaScore)
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectGameOver);
        GameObject objMenu = GameObject.Find("UIGroup").transform.Find(UIObjName).gameObject;
        objMenu.SetActive(true);

        MenuFinishBattle menu = objMenu.GetComponent<MenuFinishBattle>();
        menu.CurrentScore.text = currentScore.ToString();
        menu.DeltaScore.text = deltaScore.ToString();
        menu.Result.text = win ? "WIN" : "LOSE";

        if (UserSetting.IsBotPlayer)
            menu.StartCoroutine(menu.AutoNext());
    }

    public void OnOK(bool autoPlay)
    {
        gameObject.SetActive(false);
        MenuBattle.Hide();
        MenuWaitMatch.PopUp(autoPlay);
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
    }

    private IEnumerator AutoNext()
    {
        yield return new WaitForSeconds(1);
        OnOK(true);
    }
}
