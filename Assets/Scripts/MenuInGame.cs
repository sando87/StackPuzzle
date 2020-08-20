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
    public GameObject ParentPanel;
    public GameObject ComboText;

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
    private void UpdatePanel(int remainLimit, int totalScore, Product product)
    {
        if (totalScore > 0)
        {
            UpdateScore(totalScore);
            PlayComboAnimation(product);
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
    private void PlayComboAnimation(Product product)
    {
        GameObject comboTextObj = GameObject.Instantiate(ComboText, product.transform.position, Quaternion.identity, ParentPanel.transform);
        Text combo = comboTextObj.GetComponent<Text>();
        combo.text = product.Combo.ToString();
        StartCoroutine(ComboEffect(comboTextObj));
    }
    IEnumerator ComboEffect(GameObject obj)
    {
        float time = 0;
        while(time < 0.7)
        {
            float x = (time * 10) + 1;
            float y = (1 / x) * Time.deltaTime;
            Vector3 pos = obj.transform.position;
            pos.y += y;
            obj.transform.position = pos;
            time += Time.deltaTime;
            yield return null;
        }
        Destroy(obj);
    }

    public void OnPause()
    {
        MenuPause.PopUp();
    }
    public void OnLockMatch()
    {
        InGameManager.Inst.MatchLock = !InGameManager.Inst.MatchLock;
    }
}
