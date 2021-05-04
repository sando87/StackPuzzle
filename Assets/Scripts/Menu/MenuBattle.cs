using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuBattle : MonoBehaviour
{
    private static MenuBattle mInst = null;
    private const string UIObjName = "UISpace/CanvasPopup/PVP";

    public GameObject EffectParent;
    public NumbersUI ComboPlayer;
    public NumbersUI ComboOpponent;
    public ScoreBar PVPScoreBarPlayer;
    public ScoreBar PVPScoreBarOpponent;
    public TextMeshProUGUI PlayerName;
    public TextMeshProUGUI PlayerScore;
    public TextMeshProUGUI OpponentName;
    public TextMeshProUGUI OpponentScore;
    public GameObject PlayerRect;
    public GameObject OpponentRect;
    public GameObject Limit;
    public GameObject[] ItemSlots;

    private MenuMessageBox mMenu;

    private void Update()
    {
#if PLATFORM_ANDROID
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnClose();
        }
#endif
    }

    public static MenuBattle Inst()
    {
        if (mInst == null)
            mInst = GameObject.Find(UIObjName).GetComponent<MenuBattle>();
        return mInst;
    }

    public static void PopUp()
    {
        Inst().gameObject.SetActive(true);
        Inst().Init();
    }

    public static void Hide()
    {
        Inst().gameObject.SetActive(false);
    }

#if (UNITY_ANDROID || UNITY_IPHONE) && !UNITY_EDITOR
    private void OnApplicationPause(bool pause)
    {
        //FinishGame(false);
    }

    private void OnApplicationFocus(bool focus)
    {
        //FinishGame(false);
    }

    private void OnApplicationQuit()
    {
        //FinishGame(false);
    }
#endif

    private void Init()
    {
        for (int i = 0; i < EffectParent.transform.childCount; ++i)
            Destroy(EffectParent.transform.GetChild(i).gameObject);

        mMenu = null;
        ComboPlayer.Clear();
        ComboOpponent.Clear();
        PVPScoreBarPlayer.Clear();
        PVPScoreBarOpponent.Clear();
        PlayerName.text = InGameManager.InstPVP_Player.UserInfo.userName;
        OpponentName.text = InGameManager.InstPVP_Opponent.UserInfo.userName;
        PlayerScore.text = InGameManager.InstPVP_Player.UserInfo.score.ToString();
        OpponentScore.text = InGameManager.InstPVP_Opponent.UserInfo.score.ToString();

        PurchaseItemType[] items = MenuWaitMatch.Inst().GetSelectedItems();
        for (int i = 0; i < ItemSlots.Length; ++i)
        {
            if (i < items.Length && InGameManager.InstPVP_Player.Difficulty != MatchingLevel.Easy)
            {
                ItemSlots[i].name = items[i].ToInt().ToString();
                ItemSlots[i].GetComponentInChildren<Button>().enabled = true;
                ItemSlots[i].GetComponentInChildren<Image>().sprite = items[i].GetSprite();
                ItemSlots[i].GetComponentInChildren<Image>().color = Color.white;
                ItemSlots[i].GetComponentInChildren<TextMeshProUGUI>().text = items[i].GetName();
            }
            else
            {
                ItemSlots[i].GetComponentInChildren<Button>().enabled = false;
                ItemSlots[i].GetComponentInChildren<Image>().color = Color.gray;
                ItemSlots[i].GetComponentInChildren<Image>().sprite = PurchaseItemType.None.GetSprite();
                ItemSlots[i].GetComponentInChildren<TextMeshProUGUI>().text = "Empty";
            }
        }

        InGameManager.InstPVP_Player.EventMatched = (products) => {
            PVPScoreBarPlayer.SetScore(PVPScoreBarPlayer.CurrentScore + products[0].Combo * products.Length);
        };
        InGameManager.InstPVP_Player.EventFinish = (success) => {
            FinishGame(success);
        };
        InGameManager.InstPVP_Player.EventCombo = (combo) => {
            if (combo <= 0)
                ComboPlayer.BreakCombo();
            else
                ComboPlayer.SetNumber(combo);
        };
        InGameManager.InstPVP_Opponent.EventMatched = (products) => {
            PVPScoreBarOpponent.SetScore(PVPScoreBarOpponent.CurrentScore + products[0].Combo * products.Length);
        };
        InGameManager.InstPVP_Opponent.EventFinish = (success) => {
            FinishGame(!success);
        };
        InGameManager.InstPVP_Opponent.EventCombo = (combo) => {
            if (combo <= 0)
                ComboOpponent.BreakCombo();
            else
                ComboOpponent.SetNumber(combo);
        };
    }
    private void FinishGame(bool success)
    {
        int prevScore = UserSetting.UserScore;

        PVPInfo req = new PVPInfo();
        req.cmd = PVPCommand.EndGame;
        req.oppUserPk = InGameManager.InstPVP_Opponent.UserPk;
        req.success = success;
        req.userInfo = UserSetting.UserInfo;
        bool ret = NetClientApp.GetInstance().Request(NetCMD.PVP, req, (_body) =>
        {
            PVPInfo resBody = Utils.Deserialize<PVPInfo>(ref _body);
            UserSetting.UpdateUserInfo(resBody.userInfo);
        });

        if(!ret)
            MenuInformBox.PopUp("Network Disconnected");

        if (success)
        {
            SoundPlayer.Inst.PlayerBack.Stop();
            SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectSuccess);
            MenuFinishBattle.PopUp(success, prevScore);
        }
        else
        {
            SoundPlayer.Inst.PlayerBack.Stop();
            SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectGameOver);
            MenuFinishBattle.PopUp(success, prevScore);
        }

        string log = "[PVP] " + (success ? "win" : "lose") + ", oppPK:" + InGameManager.InstPVP_Opponent.UserPk;
        LOG.echo(log);

        InGameManager.InstPVP_Player.CleanUpGame();
        InGameManager.InstPVP_Opponent.CleanUpGame();
        Hide();
    }

    public void OnClose()
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton2);
        if (mMenu != null)
        {
            Destroy(mMenu);
            mMenu = null;
        }
        else
        {
            mMenu = MenuMessageBox.PopUp("Finish Game?", true, (bool isOK) =>
            {
                if (isOK)
                    FinishGame(false);
            });

        }
    }

    public void OnClickItem()
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton2);
        Button btn = EventSystem.current.currentSelectedGameObject.GetComponent<Button>();
        PurchaseItemType itemType = int.Parse(btn.transform.parent.name).ToItemType();
        switch (itemType)
        {
            case PurchaseItemType.ExtendLimit:
                InGameManager.InstStage.UseItemExtendsLimits(btn.transform.position, Limit.transform.position);
                break;
            case PurchaseItemType.RemoveIce:
                InGameManager.InstStage.UseItemBreakce(btn.transform.position, 10);
                break;
            case PurchaseItemType.MakeSkill1:
                InGameManager.InstStage.UseItemMakeSkill1(btn.transform.position, 10);
                break;
            case PurchaseItemType.MakeCombo:
                InGameManager.InstStage.UseItemMatch(btn.transform.position);
                break;
            case PurchaseItemType.MakeSkill2:
                InGameManager.InstStage.UseItemMakeSkill2(btn.transform.position, 10);
                break;
            case PurchaseItemType.PowerUp:
                InGameManager.InstStage.UseItemMeteor(5);
                break;
            default: break;
        }

        btn.GetComponent<Image>().color = Color.gray;
        btn.enabled = false;
        Purchases.UseItem(itemType);

        string log = "[UseItem] " + "PVP:" + OpponentName + ", Item:" + itemType + ", Count:" + itemType.GetCount();
        LOG.echo(log);
    }

}
