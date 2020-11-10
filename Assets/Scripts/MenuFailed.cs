using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuFailed : MonoBehaviour
{
    private const string UIObjName = "CanvasPopUp/MenuFailed";

    public Text Score;
    public Text TargetScore;
    public Image TargetType;
    public Text StageLevel;

    public static void PopUp(int level, int target, Sprite targetImg, int score)
    {
        GameObject menuFailed = GameObject.Find("UIGroup").transform.Find(UIObjName).gameObject;

        MenuFailed menu = menuFailed.GetComponent<MenuFailed>();
        menu.Score.text = score.ToString();
        menu.StageLevel.text = level.ToString();
        menu.TargetScore.text = target.ToString();
        menu.TargetType.sprite = targetImg;

        menuFailed.SetActive(true);

        if (UserSetting.IsBotPlayer)
            menu.StartCoroutine(menu.AutoEnd());
    }
    IEnumerator AutoEnd()
    {
        yield return new WaitForSeconds(1);
        OnAgain();
    }

    public void OnAgain()
    {
        gameObject.SetActive(false);
        MenuInGame.Hide();
        MenuStages.PopUp();
        StageManager.Inst.Activate(true);
        SoundPlayer.Inst.PlayBackMusic(SoundPlayer.Inst.BackMusicMap);
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
    }
}
