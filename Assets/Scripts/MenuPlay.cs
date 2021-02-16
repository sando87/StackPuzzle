using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuPlay : MonoBehaviour
{
    private const string UIObjName = "UISpace/CanvasPopup/StageDetail";
    private StageInfo mStageInfo;

    public TextMeshProUGUI StageLevel;
    public TextMeshProUGUI TargetScore;
    public Image TargetType;

    public static void PopUp(StageInfo info)
    {
        GameObject menuPlay = GameObject.Find(UIObjName);
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

        SoundPlayer.Inst.Player.Stop();
        SoundPlayer.Inst.PlayBackMusic(SoundPlayer.Inst.BackMusicInGame);
        InGameManager.InstStage.StartGameInStageMode(mStageInfo, UserSetting.UserInfo);
        InGameManager.InstStage.InitProducts();
        MenuInGame.PopUp(mStageInfo);
        MenuStages.Hide();
        StageManager.Inst.Activate(false);
        gameObject.SetActive(false);

        if (!UserSetting.IsBotPlayer)
            Purchases.UseHeart();
    }
}
