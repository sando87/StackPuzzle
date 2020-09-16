using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuFinishBattle : MonoBehaviour
{
    private const string UIObjName = "CanvasPopUp/MenuFinishBattle";

    public Text Score;
    public Text TargetScore;
    public Text StageLevel;

    public static void PopUp(bool win, int currentScore, int deltaScore)
    {
        GameObject objMenu = GameObject.Find("UIGroup").transform.Find(UIObjName).gameObject;

        
        objMenu.SetActive(true);
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectGameOver);
    }

    public void OnConfirm()
    {
        gameObject.SetActive(false);
        MenuInGame.Hide();
        MenuStages.PopUp();
        StageManager.Inst.Activate(true);
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
    }
}
