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
    //public TextMeshProUGUI PlayerName;
    //public TextMeshProUGUI PlayerScore;
    //public TextMeshProUGUI OpponentName;
    //public TextMeshProUGUI OpponentScore;
    public GameObject PlayerRect;
    public GameObject OpponentRect;
    public TextMeshProUGUI PlayerLimit;
    public TextMeshProUGUI OpponentLimit;
    public ItemButton[] PlayerItemSlots;
    public ItemButton[] OpponentItemSlots;
    public GameObject TimeoutEffectAnim;
    public GameObject IceBlockUp1;
    public TextMeshProUGUI IceCountFrom;
    public TextMeshProUGUI IceCountTo;
    public GameObject IceBlockUp2;
    public GameObject IceBlockUp3;
    public PVPScoreBar PVPScoreBarPrefab;
    public GameObject PlayerPanel;
    public GameObject OpponentPanel;

    private MenuMessageBox mMenu;
    private StageInfo mStageInfo;

    public PVPScoreBar PVPScoreBar { get; private set; } = null;

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
        //FinishPVPGame(false);
    }

    private void OnApplicationFocus(bool focus)
    {
        //FinishPVPGame(false);
    }

    private void OnApplicationQuit()
    {
        //FinishPVPGame(false);
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
        TimeoutEffectAnim.SetActive(false);
        //PlayerName.text = InGameManager.InstPVP_Player.UserInfo.userName;
        //OpponentName.text = InGameManager.InstPVP_Opponent.UserInfo.userName;
        //PlayerScore.text = InGameManager.InstPVP_Player.UserInfo.score.ToString();
        //OpponentScore.text = InGameManager.InstPVP_Opponent.UserInfo.score.ToString();

        InitPVPScoreBar();

        bool isAdsAdded = false;
        PurchaseItemType[] items = InGameManager.InstPVP_Player.UserInfo.PvpItems;
        for (int i = 0; i < 3; ++i)
        {
            if (items[i] == PurchaseItemType.None)
            {
                if (isAdsAdded)
                {
                    PlayerItemSlots[i].name = "empty";
                    PlayerItemSlots[i].SetEmptyImage();
                    PlayerItemSlots[i].SetEnable(false);
                    PlayerItemSlots[i].HideItemCount();
                }
                else
                {
                    isAdsAdded = true;
                    PlayerItemSlots[i].name = "ads" + i;
                    PlayerItemSlots[i].SetAdsImage();
                    PlayerItemSlots[i].SetEnable(true);
                    PlayerItemSlots[i].HideItemCount();
                }
            }
            else
            {
                PlayerItemSlots[i].SetItem(items[i]);
                PlayerItemSlots[i].SetItemImage();
                PlayerItemSlots[i].SetEnable(true);
                PlayerItemSlots[i].HideItemCount();
            }
        }

        isAdsAdded = false;
        items = InGameManager.InstPVP_Opponent.UserInfo.PvpItems;
        for (int i = 0; i < 3; ++i)
        {
            if(items[i] == PurchaseItemType.None)
            {
                if (isAdsAdded)
                {
                    OpponentItemSlots[i].name = "empty";
                    OpponentItemSlots[i].SetEmptyImage();
                    OpponentItemSlots[i].SetEnable(false);
                    OpponentItemSlots[i].HideItemCount();
                    OpponentItemSlots[i].GetComponent<Button>().enabled = false;
                }
                else
                {
                    isAdsAdded = true;
                    OpponentItemSlots[i].name = "ads" + i;
                    OpponentItemSlots[i].SetAdsImage();
                    OpponentItemSlots[i].SetEnable(true);
                    OpponentItemSlots[i].HideItemCount();
                    OpponentItemSlots[i].GetComponent<Button>().enabled = false;
                }
            }
            else
            {
                OpponentItemSlots[i].SetItem(items[i]);
                OpponentItemSlots[i].SetItemImage();
                OpponentItemSlots[i].SetEnable(true);
                OpponentItemSlots[i].HideItemCount();
                OpponentItemSlots[i].GetComponent<Button>().enabled = false;
            }
        }


        InGameManager.InstPVP_Player.EventFinish = (success) =>
        {
            FinishPVPGame(success);
        };
        InGameManager.InstPVP_Player.EventCombo = (combo) =>
        {
            if (combo <= 0)
                ComboPlayer.BreakCombo();
            else
                ComboPlayer.SetNumber(combo);
        };
        InGameManager.InstPVP_Opponent.EventFinish = (success) =>
        {
            FinishPVPGame(!success);
        };
        InGameManager.InstPVP_Opponent.EventCombo = (combo) =>
        {
            if (combo <= 0)
                ComboOpponent.BreakCombo();
            else
                ComboOpponent.SetNumber(combo);
        };
        InGameManager.InstPVP_Player.EventRemainTime = (remainSec) =>
        {
            if (gameObject.activeInHierarchy)
                PlayerLimit.text = TimeToString(remainSec);
        };
        InGameManager.InstPVP_Opponent.EventRemainTime = (remainSec) =>
        {
            if (gameObject.activeInHierarchy)
            {
                StopCoroutine("DisplayOppTimeLimit");
                StartCoroutine("DisplayOppTimeLimit", remainSec);
            }
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

    private void FinishPVPGame(bool success)
    {
        if (mMenu != null)
        {
            Destroy(mMenu.gameObject);
            mMenu = null;
        }
        
        int prevScore = UserSetting.UserScore;

        EndPVP req = new EndPVP();
        req.cmd = PVPCommand.EndGame;
        req.oppUserPk = InGameManager.InstPVP_Opponent.UserPk;
        req.success = success;
        req.userInfo = UserSetting.UserInfo;
        bool ret = NetClientApp.GetInstance().Request(NetCMD.EndPVP, req, (_body) =>
        {
            EndPVP resBody = Utils.Deserialize<EndPVP>(ref _body);
            UserSetting.UpdateUserInfoToLocal(resBody.userInfo);

            // MatchingLevel currentLeague = Utils.ToLeagueLevel(UserSetting.UserScore);
            // if (UserSetting.UserInfo.maxLeague < currentLeague)
            //     UserSetting.SetMaxLeague(currentLeague);
        });

        if(!ret)
            MenuInformBox.PopUp("Network Disconnected");

        string log = "PVPEnd," + UserSetting.SessionID + "," + (success?"win":"lose") + "," + InGameManager.InstPVP_Opponent.UserPk;
        LOG.trace(log);

        if (success)
        {
            SoundPlayer.Inst.StopBackMusic();
            SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectSuccess);
            MenuFinishBattle.PopUp(success, prevScore, InGameManager.InstPVP_Player.ItWasToughBattle);

            InGameManager.InstPVP_Player.CleanUpGame();
            InGameManager.InstPVP_Opponent.CleanUpGame();
            Hide();
        }
        else
        {
            SoundPlayer.Inst.StopBackMusic();
            SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectGameOver);

            MenuFinishBattle.PopUp(success, prevScore, InGameManager.InstPVP_Player.ItWasToughBattle);
            InGameManager.InstPVP_Player.CleanUpGame();
            InGameManager.InstPVP_Opponent.CleanUpGame();
            Hide();
        }
    }

    public void OnClose()
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton2);
        if (mMenu != null)
        {
            Destroy(mMenu.gameObject);
            mMenu = null;
        }
        else
        {
            mMenu = MenuMessageBox.PopUp("Do you want to finish?", true, (bool isOK) =>
            {
                if (isOK)
                    FinishPVPGame(false);
            });

        }
    }

    public bool IsItemPossible(PurchaseItemType itemType)
    {
        foreach (ItemButton itemButton in PlayerItemSlots)
        {
            if (itemButton.GetItem() == itemType)
            {
                if (itemButton.IsEnabled())
                    return true;
            }
        }
        return false;
    }
    public void UseItemByAutoBot(PurchaseItemType itemType)
    {
        foreach (ItemButton itemButton in PlayerItemSlots)
        {
            if (itemButton.GetItem() == itemType && itemButton.IsEnabled())
            {
                UseItem(itemButton);
                itemButton.SetEnable(false);
                return;
            }
        }
    }

    public void OnClickItem()
    {
        ItemButton btn = EventSystem.current.currentSelectedGameObject.GetComponent<ItemButton>();
        if (btn.name.StartsWith("ads"))
        {
            MenuMessageBox.PopUp("Rewarded first\nAd will be paid later", true, (isOK) =>
            {
                if (isOK)
                {
                    int adsIndex = int.Parse(btn.name.Substring(3));
                    MenuItemSelector.PopUpByAds((itemType) =>
                    {
                        Purchases.AddAdsCount();

                        PlayerItemSlots[adsIndex].name = itemType.ToInt().ToString();
                        PlayerItemSlots[adsIndex].SetItem(itemType);
                        PlayerItemSlots[adsIndex].SetItemImage();
                        PlayerItemSlots[adsIndex].SetEnable(true);
                        PlayerItemSlots[adsIndex].HideItemCount();

                        InGameManager.InstPVP_Player.Network_GetItem(itemType, adsIndex);
                    });
                }
            });
        }
        else
        {
            UseItem(btn);
            btn.SetEnable(false);
        }
    }

    public void GetOpponentItem(PurchaseItemType itemType, int slotIndex)
    {
        if(slotIndex < 0 || slotIndex >= OpponentItemSlots.Length)
            return;

        OpponentItemSlots[slotIndex].SetItem(itemType);
        OpponentItemSlots[slotIndex].SetItemImage();
        OpponentItemSlots[slotIndex].SetEnable(true);
        OpponentItemSlots[slotIndex].HideItemCount();
        OpponentItemSlots[slotIndex].GetComponent<Button>().enabled = false;
    }

    void UseItem(ItemButton btn)
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton2);
        PurchaseItemType itemType = btn.GetItem();
        switch (itemType)
        {
            case PurchaseItemType.ExtendLimit:
                InGameManager.InstPVP_Player.UseItemExtendsLimits(btn.transform.position, PVPScoreBar.RootPosition);
                break;
            case PurchaseItemType.RemoveIce:
                {
                    bool ret = InGameManager.InstPVP_Player.UseItemBreakce(btn.transform.position, 20);
                    if (ret)
                        break;
                    else
                        return;
                }
            case PurchaseItemType.MakeSkill1:
                InGameManager.InstPVP_Player.UseItemMakeSkill1(btn.transform.position, 5);
                break;
            case PurchaseItemType.KeepCombo:
                {
                    bool ret = InGameManager.InstPVP_Player.UseItemMatch(btn.transform.position);
                    if (ret)
                        break;
                    else
                        return;
                }
            case PurchaseItemType.MakeSkill2:
                InGameManager.InstPVP_Player.UseItemMakeSkill2(btn.transform.position, 5);
                break;
            case PurchaseItemType.Meteor:
                InGameManager.InstPVP_Player.UseItemMeteor(10);
                break;
            default: break;
        }

        Purchases.UseItem(itemType);

        string log = "UseItem," + itemType + "," + itemType.GetCount();
        LOG.trace(log);
    }

    public void UseOpponentItem(PurchaseItemType itemType)
    {
        ItemButton btn = null;
        foreach(ItemButton itemBtn in OpponentItemSlots)
        {
            if(itemBtn.GetItem() == itemType)
            {
                btn = itemBtn;
                break;
            }
        }

        if(null == btn)
            return;

        switch (itemType)
        {
            case PurchaseItemType.ExtendLimit:
                InGameManager.InstPVP_Opponent.UseItemExtendsLimits(btn.transform.position, PVPScoreBar.RootPosition);
                break;
            case PurchaseItemType.RemoveIce:
                {
                    bool ret = InGameManager.InstPVP_Opponent.UseItemBreakce(btn.transform.position, 20);
                    if (ret)
                        break;
                    else
                        return;
                }
            case PurchaseItemType.MakeSkill1:
                InGameManager.InstPVP_Opponent.UseItemMakeSkill1(btn.transform.position, 5);
                break;
            case PurchaseItemType.KeepCombo:
                {
                    bool ret = InGameManager.InstPVP_Opponent.UseItemMatch(btn.transform.position);
                    if (ret)
                        break;
                    else
                        return;
                }
            case PurchaseItemType.MakeSkill2:
                InGameManager.InstPVP_Opponent.UseItemMakeSkill2(btn.transform.position, 5);
                break;
            case PurchaseItemType.Meteor:
                InGameManager.InstPVP_Opponent.UseItemMeteor(10);
                break;
            default: break;
        }

        btn.SetEnable(false);
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
    public void AnimTimeoutEffect(int iceBlockLevel)
    {
        if (iceBlockLevel <= 1)
            return;

        IceBlockUp1.SetActive(true);

        int addedIceBlockFrom = UserSetting.AddedIceBlockPerLevel * (iceBlockLevel - 2);
        int addedIceBlockTo = UserSetting.AddedIceBlockPerLevel * (iceBlockLevel - 1);
        IceCountFrom.text = "+" + addedIceBlockFrom;
        IceCountTo.text = "+" + addedIceBlockTo;

        PVPScoreBar.SetAddedIceBlockCount(addedIceBlockTo);

        StartCoroutine(AnimTimeout());
    }
    IEnumerator AnimTimeout()
    {
        TimeoutEffectAnim.SetActive(true);
        yield return new WaitForSeconds(3.0f);
        TimeoutEffectAnim.SetActive(false);
    }

    void InitPVPScoreBar()
    {
        if (PVPScoreBar != null)
        {
            Destroy(PVPScoreBar.gameObject);
        }

        PVPScoreBar = Instantiate(PVPScoreBarPrefab, transform);
        PVPScoreBar.Init();
    }
}
