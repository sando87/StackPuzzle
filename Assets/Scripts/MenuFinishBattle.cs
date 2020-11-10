using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuFinishBattle : MonoBehaviour
{
    private const string UIObjName = "CanvasPopUp/MenuFinishBattle";

    public Image Star1;
    public Image Star2;
    public Image Star3;
    public Text Title;
    public Text DeltaScore;
    public Text PlayerInfo;

    public static void PopUp(bool win, UserInfo info, int deltaScore)
    {
        GameObject objMenu = GameObject.Find("UIGroup").transform.Find(UIObjName).gameObject;
        objMenu.SetActive(true);

        MenuFinishBattle menu = objMenu.GetComponent<MenuFinishBattle>();
        menu.Star1.gameObject.SetActive(win ? true : false);
        menu.Star2.gameObject.SetActive(deltaScore > 5 ? true : false);
        menu.Star3.gameObject.SetActive(deltaScore > 10 ? true : false);
        menu.Title.text = win ? "WIN" : "LOSE";
        menu.DeltaScore.text = deltaScore.ToString();

        string text =
            "ID : #" + info.userPk + "\n" +
            "Name : " + info.userName + "\n" +
            "Score : " + info.score + "\n" +
            "Win/Lose : " + info.win + "/" + info.lose;

        menu.PlayerInfo.text = text;

        if (UserSetting.IsBotPlayer)
            menu.StartCoroutine(menu.AutoNext());
    }

    public void OnOK(bool autoPlay)
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
        gameObject.SetActive(false);
        MenuBattle.Hide();
        MenuWaitMatch.PopUp(autoPlay);
    }

    private IEnumerator AutoNext()
    {
        yield return new WaitForSeconds(1);
        OnOK(true);
    }
}
