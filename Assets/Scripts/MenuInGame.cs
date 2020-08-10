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
    public Text Combo;
    public Animation ComboAnim;

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
    private void UpdatePanel(int remainLimit, int currentScore, int combo, ProductColor color)
    {
        if (currentScore > 0)
        {
            UpdateScore(currentScore);
            PlayComboAnimation(combo, color);
        }
        else
        {
            Limit.text = remainLimit.ToString();
        }
        
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

        InGameManager.Inst.EventOnChange = UpdatePanel;
        ComboAnim = Combo.GetComponent<Animation>();
        Combo.gameObject.SetActive(false);
    }

    private void UpdateScore(int totalScore)
    {
        int countStar = InGameManager.Inst.GetStarCount();
        int targetScore = int.Parse(TargetScore.text);
        float rateTarget = (float)totalScore / (float)targetScore;
        CurrentScore.text = totalScore.ToString();
        ScoreBar.fillAmount = rateTarget;
        BarStar1.gameObject.SetActive(countStar >= 1);
        BarStar2.gameObject.SetActive(countStar >= 2);
        BarStar3.gameObject.SetActive(countStar >= 3);
    }
    private void PlayComboAnimation(int combo, ProductColor color)
    {
        Color comboColor = Color.white;
        switch(color)
        {
            case ProductColor.Blue: comboColor = Color.cyan; break;
            case ProductColor.Green: comboColor = Color.green; break;
            case ProductColor.Orange: comboColor = new Color(1.0f, 0.5f, 0); break;
            case ProductColor.Purple: comboColor = Color.magenta; break;
            case ProductColor.Red: comboColor = Color.red; break;
            case ProductColor.Yellow: comboColor = Color.yellow; break;
            default: comboColor = Color.white; break;
        }
        Combo.color = comboColor;
        Combo.gameObject.SetActive(true);
        Combo.text = combo + " Combo";
        ComboAnim.Play("combo");
        StopCoroutine("ClearCombo");
        StartCoroutine("ClearCombo");
    }
    IEnumerator ClearCombo()
    {
        yield return new WaitForSeconds(InGameManager.ComboDuration);
        Combo.gameObject.SetActive(false);
        int curScore = int.Parse(CurrentScore.text);
        int curCombo = int.Parse(Combo.text.Replace("Combo", " ").Trim());
        curScore += (curCombo * 1);
        UpdateScore(curScore);
        InGameManager.Inst.ClearCombo(1);
    }

    public void OnPause()
    {
        MenuPause.PopUp();
    }
}
