using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuPlay : MonoBehaviour
{
    private const string UIObjName = "MenuPlay";
    private StageInfo mStageInfo;

    public Text StageLevel;
    public Text TargetScore;
    public Image Star1;
    public Image Star2;
    public Image Star3;

    public static void PopUp(StageInfo info)
    {
        GameObject menuPlay = GameObject.Find("UIGroup").transform.Find(UIObjName).gameObject;
        menuPlay.GetComponent<MenuPlay>().UpdateUIState(info);
        menuPlay.SetActive(true);
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
    }

    public void OnPlay()
    {
        InGameManager.Inst.StartGame(mStageInfo);
        GameObject.Find("StageScreen").SetActive(false);
        gameObject.SetActive(false);
    }
}
