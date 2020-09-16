using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuBattle : MonoBehaviour
{
    private const string UIObjName = "MenuBattle";

    public Text SavedCombo;
    public GameObject ComboText;
    public GameObject ParentPanel;

    public static void PopUp()
    {
        GameObject menuPlay = GameObject.Find("UIGroup").transform.Find(UIObjName).gameObject;
        menuPlay.SetActive(true);
    }
    public static void Hide()
    {
        GameObject menuPlay = GameObject.Find("UIGroup").transform.Find(UIObjName).gameObject;
        menuPlay.SetActive(false);
    }
    private void UpdatePanel(int remainLimit, int totalScore, Product product)
    {
        PlayComboAnimation(product);
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

    public void OnClose()
    {
        MenuMessageBox.PopUp(gameObject, "Finish Game", true, (bool isOK) =>
        {
            if(isOK)
            {
                BattleFieldManager mgr = GameObject.Find("WorldSpace").transform.Find("BattleScreen").GetComponent<BattleFieldManager>();
                mgr.FinishGame(false);
                MenuBattle.Hide();
            }
        });
    }
    public void OnLockMatch()
    {
        BattleFieldManager mgr = GameObject.Find("WorldSpace").transform.Find("BattleScreen").GetComponent<BattleFieldManager>();
        mgr.MatchLock = !mgr.MatchLock;
    }
}
