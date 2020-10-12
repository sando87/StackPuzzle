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
    public Text TargetValue;
    public Image TargetType;
    public Image Lock;
    public Image UnLock;
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
    private void UpdatePanel(InGameBillboard inGameInfo, Product product)
    {
        CurrentScore.text = inGameInfo.CurrentScore.ToString();
        ScoreBar.fillAmount = inGameInfo.GetAchievementRate(mStageInfo);
        Limit.text = inGameInfo.RemainLimit.ToString();
        KeepCombo.text = inGameInfo.KeepCombo.ToString();

        if (product != null)
            PlayComboAnimation(product);
    }
    private void InitUIState(StageInfo info)
    {
        mStageInfo = info;
        ScoreBar.fillAmount = 0;
        BarStar1.gameObject.SetActive(false);
        BarStar2.gameObject.SetActive(false);
        BarStar3.gameObject.SetActive(false);
        Lock.gameObject.SetActive(false);
        UnLock.gameObject.SetActive(true);

        CurrentScore.text = "0";
        Limit.text = info.MoveLimit.ToString();
        TargetType.sprite = info.GoalTypeImage;
        TargetValue.text = info.GoalValue.ToString();
        KeepCombo.text = "0";
        StageLevel.text = info.Num.ToString();

        GameField.GetComponent<InGameManager>().EventOnChange = UpdatePanel;
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
    public void OnLockMatch(bool enableLock)
    {
        GameField.GetComponent<InGameManager>().MatchLock = enableLock;
        Lock.gameObject.SetActive(enableLock);
        UnLock.gameObject.SetActive(!enableLock);
    }
}
