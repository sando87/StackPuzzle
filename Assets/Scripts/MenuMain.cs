using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuMain : MonoBehaviour
{
    private const string UIObjName = "MenuMain";

    public static void PopUp()
    {
        GameObject.Find("UIGroup").transform.Find(UIObjName).gameObject.SetActive(true);
        GameObject.Find("WorldSpace").transform.Find("MainScreen").gameObject.SetActive(true);
    }

    public void PlayStageMode()
    {
        gameObject.SetActive(false);
        GameObject.Find("WorldSpace").transform.Find("MainScreen").gameObject.SetActive(false);

        MenuStages.PopUp();
        
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
    }
    public void PlayBattleMode()
    {
        gameObject.SetActive(false);
        GameObject.Find("WorldSpace").transform.Find("MainScreen").gameObject.SetActive(false);

        MenuWaitMatch.PopUp();

        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
    }
}
