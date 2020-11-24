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
    public Image TargetType;
    public Image Star1;
    public Image Star2;
    public Image Star3;

    public static void PopUp(StageInfo info)
    {
        GameObject menuPlay = GameObject.Find("UIGroup").transform.Find(UIObjName).gameObject;
        MenuPlay menu = menuPlay.GetComponent<MenuPlay>();
        menu.UpdateUIState(info);
        menuPlay.SetActive(true);
        StageManager.Inst.gameObject.SetActive(false);

        if (UserSetting.IsBotPlayer)
            menu.StartCoroutine(menu.AutoStart());
    }
    IEnumerator AutoStart()
    {
        yield return new WaitForSeconds(1);
        OnPlay();
    }
    public void UpdateUIState(StageInfo info)
    {
        int starCount = UserSetting.GetStageStarCount(info.Num);
        mStageInfo = info;
        StageLevel.text = info.Num.ToString();
        Star1.gameObject.SetActive(starCount >= 1);
        Star2.gameObject.SetActive(starCount >= 2);
        Star3.gameObject.SetActive(starCount >= 3);
        TargetScore.text = info.GoalValue.ToString();
        TargetType.sprite = info.GoalTypeImage;
    }

    public void OnClose()
    {
        gameObject.SetActive(false);
        StageManager.Inst.gameObject.SetActive(true);
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton2);
    }

    public void OnPlay()
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
        if (Purchases.CountHeart() <= 0)
        {
            MenuMessageBox.PopUp("No Life", false, null);
            return;
        }

        if (!UserSetting.IsBotPlayer)
            Purchases.UseHeart();

        SoundPlayer.Inst.Player.Stop();
        SoundPlayer.Inst.PlayBackMusic(SoundPlayer.Inst.BackMusicInGame);
        InGameManager.InstStage.StartGame(mStageInfo, UserSetting.UserInfo);
        InGameManager.InstStage.InitProducts();
        MenuInGame.PopUp(mStageInfo);
        MenuStages.Hide();
        StageManager.Inst.Activate(false);
        gameObject.SetActive(false);
    }
}
