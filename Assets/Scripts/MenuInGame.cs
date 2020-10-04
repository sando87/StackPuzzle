using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuInGame : MonoBehaviour
{
    private const string UIObjName = "MenuInGame";
    private StageInfo mStageInfo;

    public Text CurrentScore;
    public Text KeepCombo;
    public Text Limit;
    public Text StageLevel;
    public Text GoalType;
    public Image ScoreBar;
    public Image BarStar1;
    public Image BarStar2;
    public Image BarStar3;
    public GameObject ParentPanel;
    public GameObject ComboText;
    public GameObject GameField;

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
        mStageInfo = info;
        ScoreBar.fillAmount = 0;
        BarStar1.gameObject.SetActive(false);
        BarStar2.gameObject.SetActive(false);
        BarStar3.gameObject.SetActive(false);

        CurrentScore.text = "0";
        Limit.text = info.MoveLimit.ToString();
        GoalType.text = info.Goals[0].Split('/')[0];
        KeepCombo.text = "0";
        StageLevel.text = info.Num.ToString();

        GameField.GetComponent<InGameManager>().EventOnChange = UpdatePanel;
        GameField.GetComponent<InGameManager>().EventOnKeepCombo = UpdateKeepCombo;
    }

    private void UpdateKeepCombo(int keepCombo)
    {
        KeepCombo.text = keepCombo.ToString();
    }
    private void UpdateScore(int totalScore)
    {
        string goal = mStageInfo.Goals[0].Split('/')[1];
        float targetScore = float.Parse(goal);
        float rateTarget = totalScore / targetScore;
        CurrentScore.text = totalScore.ToString();
        ScoreBar.fillAmount = rateTarget;
        //int starCount = InGameManager.GetStarCount(totalScore, int.Parse(goal));
        //BarStar1.gameObject.SetActive(starCount >= 1);
        //BarStar2.gameObject.SetActive(starCount >= 2);
        //BarStar3.gameObject.SetActive(starCount >= 3);
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
        InGameManager mgr = GameField.GetComponent<InGameManager>();
        mgr.MatchLock = !mgr.MatchLock;
    }
}
