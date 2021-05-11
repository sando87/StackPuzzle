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
    public TextMeshProUGUI PlayerLimit;
    public TextMeshProUGUI OpponentLimit;
    public GameObject AttackPointFrame;
    public Sprite ItemEmptyImage;
    public ItemButton[] PlayerItemSlots;
    public ItemButton[] OpponentItemSlots;

    private MenuMessageBox mMenu;
    private StageInfo mStageInfo;

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

    public static void PopUp(StageInfo stageInfo)
    {
        Inst().gameObject.SetActive(true);
        Inst().Init(stageInfo);
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

    private void Init(StageInfo stageInfo)
    {
        mStageInfo = stageInfo;

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

        PurchaseItemType[] items = MenuWaitMatch.GetSelectedItems(InGameManager.InstPVP_Player.UserInfo);
        for (int i = 0; i < PlayerItemSlots.Length; ++i)
        {
            if (i < items.Length && InGameManager.InstPVP_Player.Difficulty != MatchingLevel.Easy)
            {
                PlayerItemSlots[i].SetItem(items[i]);
                PlayerItemSlots[i].SetEnable(true);
            }
            else
            {
                PlayerItemSlots[i].SetItem(PurchaseItemType.None);
                PlayerItemSlots[i].SetEnable(false);
            }
        }

        items = MenuWaitMatch.GetSelectedItems(InGameManager.InstPVP_Opponent.UserInfo);
        for (int i = 0; i < OpponentItemSlots.Length; ++i)
        {
            if (i < items.Length && InGameManager.InstPVP_Opponent.Difficulty != MatchingLevel.Easy)
            {
                OpponentItemSlots[i].SetItem(items[i]);
                OpponentItemSlots[i].SetEnable(true);
            }
            else
            {
                OpponentItemSlots[i].SetItem(PurchaseItemType.None);
                OpponentItemSlots[i].SetEnable(false);
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
        InGameManager.InstPVP_Player.EventRemainTime = (remainSec) => {
            PlayerLimit.text = TimeToString(remainSec);
        };
        InGameManager.InstPVP_Opponent.EventRemainTime = (remainSec) => {
            StopCoroutine("DisplayOppTimeLimit");
            StartCoroutine("DisplayOppTimeLimit", remainSec);
        };
    }

    static public string TimeToString(int second)
    {
        if (second < 0)
            return "00:00";

        int min = second / 60;
        int sec = second % 60;
        return string.Format("{0:00}:{1:00}", min, sec);
    }
    static public int StringToSec(string timerText)
    {
        string[] piece = timerText.Split(':');
        int min = int.Parse(piece[0]);
        int sec = int.Parse(piece[1]);
        return min * 60 + sec;
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
        ItemButton btn = EventSystem.current.currentSelectedGameObject.GetComponent<ItemButton>();
        PurchaseItemType itemType = btn.GetItem();
        switch (itemType)
        {
            case PurchaseItemType.ExtendLimit:
                InGameManager.InstPVP_Player.UseItemExtendsLimits(btn.transform.position, PlayerLimit.transform.position);
                break;
            case PurchaseItemType.RemoveIce:
                InGameManager.InstPVP_Player.UseItemBreakce(btn.transform.position, 10);
                break;
            case PurchaseItemType.MakeSkill1:
                InGameManager.InstPVP_Player.UseItemMakeSkill1(btn.transform.position, 10);
                break;
            case PurchaseItemType.MakeCombo:
                InGameManager.InstPVP_Player.UseItemMatch(btn.transform.position);
                break;
            case PurchaseItemType.MakeSkill2:
                InGameManager.InstPVP_Player.UseItemMakeSkill2(btn.transform.position, 10);
                break;
            case PurchaseItemType.PowerUp:
                InGameManager.InstPVP_Player.UseItemMeteor(5);
                break;
            default: break;
        }

        btn.SetEnable(false);
        Purchases.UseItem(itemType);

        string log = "[UseItem] " + "PVP:" + OpponentName + ", Item:" + itemType + ", Count:" + itemType.GetCount();
        LOG.echo(log);
    }
    IEnumerator DisplayOppTimeLimit(int _remain)
    {
        int remain = _remain;
        while (remain >= 0)
        {
            OpponentLimit.text = TimeToString(remain);
            remain--;
            yield return new WaitForSeconds(1);
        }
    }

}
