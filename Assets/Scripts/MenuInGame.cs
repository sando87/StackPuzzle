using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuInGame : MonoBehaviour
{
    private const string UIObjName = "MenuInGame";
    
    public Text CurrentScore;
    public Text TargetScore;
    public Text Limit;
    public Text StageLevel;
    public Image ScoreBar;
    public Image BarStar1;
    public Image BarStar2;
    public Image BarStar3;

    private void Start()
    {
        InGameManager.Inst.EventOnChange = UpdatePanel;
    }

    public static void PopUp(StageInfo info)
    {
        GameObject menuPlay = GameObject.Find("UIGroup").transform.Find(UIObjName).gameObject;
        menuPlay.GetComponent<MenuInGame>().InitUIState(info);
        menuPlay.SetActive(true);
    }
    public static void Hide()
    {
        GameObject menuPlay = GameObject.Find("UIGroup").transform.Find(UIObjName).gameObject;
        menuPlay.SetActive(false);
    }
    private void UpdatePanel(int currentScore, int remainLimit)
    {
        int countStar = InGameManager.Inst.GetStarCount();
        int targetScore = int.Parse(TargetScore.text);
        float rateTarget = (float)currentScore / (float)targetScore;
        Limit.text = remainLimit.ToString();
        CurrentScore.text = currentScore.ToString();
        ScoreBar.fillAmount = rateTarget;
        BarStar1.gameObject.SetActive(countStar >= 1);
        BarStar2.gameObject.SetActive(countStar >= 2);
        BarStar3.gameObject.SetActive(countStar >= 3);
    }
    private void InitUIState(StageInfo info)
    {
        ScoreBar.fillAmount = 0;
        BarStar1.gameObject.SetActive(false);
        BarStar2.gameObject.SetActive(false);
        BarStar3.gameObject.SetActive(false);

        CurrentScore.text = "0";
        Limit.text = info.MoveLimit.ToString();
        TargetScore.text = info.GoalScore.ToString();
        StageLevel.text = info.Num.ToString();
    }

    public void OnPause()
    {
        MenuPause.PopUp();
    }
}
