using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuFailed : MonoBehaviour
{
    private const string UIObjName = "CanvasPopUp/MenuFailed";

    public Text Score;
    public Text TargetScore;
    public Text StageLevel;

    public static void PopUp(int level, int target, int score)
    {
        GameObject menuFailed = GameObject.Find("UIGroup").transform.Find(UIObjName).gameObject;

        MenuFailed menu = menuFailed.GetComponent<MenuFailed>();
        menu.Score.text = score.ToString();
        menu.StageLevel.text = level.ToString();
        menu.TargetScore.text = target.ToString();

        menuFailed.SetActive(true);
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectGameOver);
    }

    public void OnAgain()
    {
        gameObject.SetActive(false);
        MenuStages.PopUp();
        StageManager.Inst.Activate(true);
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
    }
}
