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
    public TextMeshProUGUI Score;
    public TextMeshProUGUI StageLevel;

    public static void PopUp(int level, int starCount, int score, bool isFirstClear)
    {
        GameObject menuComp = GameObject.Find(UIObjName);

        MenuComplete menu = menuComp.GetComponent<MenuComplete>();
        menu.Star1.gameObject.SetActive(starCount >= 1);
        menu.Star2.gameObject.SetActive(starCount >= 2);
        menu.Star3.gameObject.SetActive(starCount >= 3);
        menu.Score.text = score.ToString();
        menu.StageLevel.text = level.ToString();

        menuComp.SetActive(true);

        if (UserSetting.IsBotPlayer)
            menu.StartCoroutine(menu.AutoEnd());
    }
    IEnumerator AutoEnd()
    {
        yield return new WaitForSeconds(1);
        OnNext();
    }

    public void OnNext()
    {
        gameObject.SetActive(false);
        MenuInGame.Hide();
        MenuStages.PopUp();
        StageManager.Inst.Activate(true);
        SoundPlayer.Inst.PlayBackMusic(SoundPlayer.Inst.BackMusicMap);
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
    }
}
