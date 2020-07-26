using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuPlay : MonoBehaviour
{
    private StageInfo mStageInfo;

    public static void PopUp(StageInfo info)
    {
        GameObject menuPlay = GameObject.Find("PopUpMenu").transform.Find("MenuPlay").gameObject;
        menuPlay.GetComponent<MenuPlay>().UpdateUIState(info);
        menuPlay.SetActive(true);
    }
    public void UpdateUIState(StageInfo info)
    {
        mStageInfo = info;
        transform.Find("Image/Level").GetComponent<Text>().text = info.Num.ToString();
        transform.Find("Image/Star1").gameObject.SetActive(info.StarCount >= 1);
        transform.Find("Image/Star2").gameObject.SetActive(info.StarCount >= 2);
        transform.Find("Image/Star3").gameObject.SetActive(info.StarCount >= 3);
        transform.Find("Image/Score").GetComponent<Text>().text = info.GoalScore.ToString();
    }

    public void OnClose()
    {
        gameObject.SetActive(false);
    }

    public void OnPlay()
    {
        InGameManager.Inst.Init(mStageInfo);
        InGameManager.Inst.Show = true;
        GameObject.Find("StageScreen").SetActive(false);
        gameObject.SetActive(false);
    }
}
