using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuBattle : MonoBehaviour
{
    private const string UIObjName = "MenuBattle";

    public Text SavedCombo;
    public Image MatchLock;
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
        BattleFieldManager.Inst.MatchLock = false;
        MatchLock.color = Color.white;
        BattleFieldManager.Inst.EventOnChange = UpdatePanel;
    }
    private void UpdatePanel(Product product)
    {
        if(product.mSkill == ProductSkill.KeepCombo)
        {
            int combo = int.Parse(SavedCombo.text);
            combo = Mathf.Max(combo, product.Combo);
            SavedCombo.text = combo.ToString();
        }
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
                BattleFieldManager.Inst.FinishGame(false);
            }
        });
    }
    public void OnLockMatch()
    {
        bool lockState = !BattleFieldManager.Inst.MatchLock;
        BattleFieldManager.Inst.MatchLock = lockState;
        MatchLock.color = lockState ? Color.red : Color.white;
    }
}
