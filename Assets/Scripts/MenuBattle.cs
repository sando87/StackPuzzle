using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuBattle : MonoBehaviour
{
    private const string UIObjName = "MenuBattle";

    public Text SavedCombo;
    public Image MatchLock;
    public Image MatchUnLock;
    public GameObject ComboText;
    public GameObject ParentPanel;

    public static void PopUp()
    {
        GameObject menuPlay = GameObject.Find("UIGroup").transform.Find(UIObjName).gameObject;
        menuPlay.SetActive(true);
        menuPlay.GetComponent<MenuBattle>().Init();
    }
    public static void Hide()
    {
        GameObject menuPlay = GameObject.Find("UIGroup").transform.Find(UIObjName).gameObject;
        menuPlay.SetActive(false);
    }
    private void Init()
    {
        SavedCombo.text = "0";
        Lock(false);
        BattleFieldManager.Me.EventOnChange = PlayComboEffect;
        BattleFieldManager.Me.EventOnKeepCombo = SetKeepCombo;
        //BattleFieldManager.Opp.EventOnChange = UpdatePanel;
    }
    private void PlayComboEffect(Product product)
    {
        PlayComboAnimation(product);
    }
    private void SetKeepCombo(int combo)
    {
        SavedCombo.text = combo.ToString();
    }
    private void Lock(bool locked)
    {
        BattleFieldManager.Me.MatchLock = locked;
        MatchLock.gameObject.SetActive(locked);
        MatchUnLock.gameObject.SetActive(!locked);
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
        MenuMessageBox.PopUp("Finish Game", true, (bool isOK) =>
        {
            if(isOK)
            {
                BattleFieldManager.FinishGame(false);
            }
        });
    }
    public void OnLockMatch(bool locked)
    {
        Lock(locked);
    }
}
