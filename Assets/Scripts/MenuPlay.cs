using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuPlay : MonoBehaviour
{
    private const string UIObjName = "CanvasPopUp/MenuPlay";
    private StageInfo mStageInfo;

    public Text StageLevel;
    public Text TargetScore;
    public Image Star1;
    public Image Star2;
    public Image Star3;
    public GameObject GameField;

    public static void PopUp(StageInfo info)
    {
        GameObject menuPlay = GameObject.Find("UIGroup").transform.Find(UIObjName).gameObject;
        menuPlay.GetComponent<MenuPlay>().UpdateUIState(info);
        menuPlay.SetActive(true);
        StageManager.Inst.gameObject.SetActive(false);
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton2);
    }
    public void UpdateUIState(StageInfo info)
    {
        mStageInfo = info;
        StageLevel.text = info.Num.ToString();
        Star1.gameObject.SetActive(info.StarCount >= 1);
        Star2.gameObject.SetActive(info.StarCount >= 2);
        Star3.gameObject.SetActive(info.StarCount >= 3);
        TargetScore.text = info.GoalScore.ToString();
    }

    public void OnClose()
    {
        gameObject.SetActive(false);
        StageManager.Inst.gameObject.SetActive(true);
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
    }

    public void OnPlay()
    {
        if(Purchases.CountHeart() <= 0)
        {
            MenuMessageBox.PopUp(gameObject, "No Life", false, null);
            return;
        }

        Purchases.UseHeart();
        GameField.GetComponent<InGameManager>().StartGame(mStageInfo);
        MenuInGame.PopUp(mStageInfo);
        MenuStages.Hide();
        StageManager.Inst.Activate(false);
        gameObject.SetActive(false);
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
    }
}
