using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using SkillPair = System.Tuple<PVPCommand, UnityEngine.Sprite>;

public class MenuWaitMatch : MonoBehaviour
{
    private const string UIObjName = "UISpace/CanvasPopup/SearchBattle";
    private bool mIsSearching = false;
    private int mRoonMakeID = -1;
    private int mRoonJoinID = 1234;

    public TextMeshProUGUI RoomID;
    public TextMeshProUGUI WaitText;
    public TextMeshProUGUI Ranking;
    public TextMeshProUGUI ExpLevel;
    public TextMeshProUGUI Exp;
    public TextMeshProUGUI WinLose;
    public Slider ExpBar;
    public Image RankImage;
    public GameObject BtnFriend;
    public GameObject BtnMatch;
    public GameObject BtnCancle;
    public Button[] LeagueLevelBtns;

    public static MenuWaitMatch Inst()
    {
        return GameObject.Find(UIObjName).GetComponent<MenuWaitMatch>();
    }

    public static void PopUp()
    {
        GameObject menuMatch = GameObject.Find(UIObjName);
        MenuWaitMatch menu = menuMatch.GetComponent<MenuWaitMatch>();
        menuMatch.SetActive(true);

        if (UserSetting.IsBotPlayer)
            UserSetting.MatchLevel = MatchingLevel.All;

        menu.ResetMatchUI();

        if (UserSetting.IsBotPlayer)
            menu.StartCoroutine(menu.AutoMatch());
    }

    public void OnClose()
    {
        SearchOpponentInfo info = new SearchOpponentInfo();
        info.MyUserInfo = UserSetting.UserInfo;
        NetClientApp.GetInstance().Request(NetCMD.StopMatching, info, null);

        mIsSearching = false;
        gameObject.SetActive(false);
        MenuStages.PopUp();
        if(!SoundPlayer.Inst.PlayerBack.isPlaying)
            SoundPlayer.Inst.PlayBackMusic(SoundPlayer.Inst.BackMusicMap);
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton2);
    }

    public void OnCancle()
    {
        ResetMatchUI();
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton2);

        SearchOpponentInfo info = new SearchOpponentInfo();
        info.MyUserInfo = UserSetting.UserInfo;
        NetClientApp.GetInstance().Request(NetCMD.StopMatching, info, null);
    }

    public void OnChangeLevel()
    {
        if (mIsSearching)
            return;

        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
        Button btn = EventSystem.current.currentSelectedGameObject.GetComponent<Button>();
        int idx = btn.transform.GetSiblingIndex();
        UserSetting.MatchLevel = (MatchingLevel)(idx + 1);
        UpdateLeagueLevelButtons();
    }

    public void OnMatchFriend()
    {
        if (NetClientApp.GetInstance().IsDisconnected())
        {
            MenuInformBox.PopUp("Server Disconnected.");
            return;
        }

        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);

        if (UserSetting.UserName.Length < UserSetting.NameLengthMin)
        {
            MenuSettings.EditUserName();
            return;
        }

        if (Purchases.CountHeart() <= 0)
        {
            MenuMessageBox.PopUp("No Life", false, null);
            return;
        }

        MenuPVPFriend.PopUp(mRoonJoinID, (type, roomID) => 
        {
            if (type == MatchingFriend.None)
                return;

            if (type == MatchingFriend.Make)
            {
                RequestMatchMake();
            }
            else if(type == MatchingFriend.Join)
            {
                mRoonJoinID = roomID;
                RequestMatchJoin(roomID);
                RoomID.text = "Searching\n" + roomID;
                RoomID.gameObject.SetActive(true);
            }

            mIsSearching = true;
            BtnFriend.SetActive(false);
            BtnMatch.SetActive(false);
            BtnCancle.SetActive(true);

            StartCoroutine("WaitOpponent");
        });
    }

    public void OnChangeItem()
    {
        if (mIsSearching)
            return;

        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
        ItemButton curBtn = EventSystem.current.currentSelectedGameObject.GetComponent<ItemButton>();
        MenuItemSelector.PopUp((item) =>
        {
            curBtn.SetItem(item);
            int idx = curBtn.transform.GetSiblingIndex();
            UserSetting.UserInfo.PvpItems[idx] = item;
        });
    }

    public void OnMatch()
    {
        if (NetClientApp.GetInstance().IsDisconnected())
        {
            MenuInformBox.PopUp("Server Disconnected.");
            return;
        }

        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);

        if (UserSetting.UserName.Length < UserSetting.NameLengthMin)
        {
            MenuSettings.EditUserName();
            return;
        }

        MatchingLevel currentPossibleLeague = Utils.ToLeagueLevel(UserSetting.UserScore);
        if (currentPossibleLeague < UserSetting.MatchLevel && UserSetting.MatchLevel != MatchingLevel.All)
        {
            int levelForNext = Utils.LevelForNextLeague(UserSetting.UserScore);
            MenuMessageBox.PopUp("Required\n" + levelForNext + " Level", false, null);
            return;
        }

        if (Purchases.CountHeart() <= 0)
        {
            MenuMessageBox.PopUp("No Life", false, null);
            return;
        }

        RequestMatch();

        mIsSearching = true;
        BtnFriend.SetActive(false);
        BtnMatch.SetActive(false);
        BtnCancle.SetActive(true);

        StartCoroutine("WaitOpponent");
    }

    IEnumerator WaitOpponent()
    {
        int n = 0;
        WaitText.gameObject.SetActive(true);
        while (true)
        {
            switch(n%3)
            {
                case 0: WaitText.text = "Matching.."; break;
                case 1: WaitText.text = "Matching..."; break;
                case 2: WaitText.text = "Matching...."; break;
            }
            n++;
            yield return new WaitForSeconds(1);
        }
    }

    public void HandlerMatchingResult(Header head, byte[] body)
    {
        if (head.Ack == 1 || head.Cmd != NetCMD.SearchOpponent)
            return;

        SearchOpponentInfo res = Utils.Deserialize<SearchOpponentInfo>(ref body);
        if (res.State == MatchingState.Matched)
        {
            ReadyToFight(res);
        }
        else if (res.State == MatchingState.FoundOpp)
        {
            if(mIsSearching)
            {
                SearchOpponentInfo info = new SearchOpponentInfo();
                info.MyUserInfo = UserSetting.UserInfo;
                info.OppUserInfo = res.OppUserInfo;
                info.State = MatchingState.FoundOppAck;
                NetClientApp.GetInstance().Request(NetCMD.SearchOpponent, info, null);
            }
        }
    }
    private void RequestMatch()
    {
        SearchOpponentInfo info = new SearchOpponentInfo();
        info.MyUserInfo = UserSetting.UserInfo;
        info.OppUserInfo = new UserInfo();
        info.State = MatchingState.TryMatching;
        info.Level = UserSetting.MatchLevel;
        NetClientApp.GetInstance().Request(NetCMD.SearchOpponent, info, null);
    }
    private void RequestMatchMake()
    {
        SearchOpponentInfo info = new SearchOpponentInfo();
        info.MyUserInfo = UserSetting.UserInfo;
        info.OppUserInfo = new UserInfo();
        info.State = MatchingState.TryMatching;
        info.Level = UserSetting.MatchLevel;
        info.WithFriend = MatchingFriend.Make;
        info.RoomNumber = mRoonMakeID;
        NetClientApp.GetInstance().Request(NetCMD.SearchOpponent, info, (body) =>
        {
            SearchOpponentInfo res = Utils.Deserialize<SearchOpponentInfo>(ref body);
            mRoonMakeID = res.RoomNumber;
            RoomID.text = "Room ID\n" + res.RoomNumber;
            RoomID.gameObject.SetActive(true);
        });
    }
    private void RequestMatchJoin(int roomNumber)
    {
        SearchOpponentInfo info = new SearchOpponentInfo();
        info.MyUserInfo = UserSetting.UserInfo;
        info.OppUserInfo = new UserInfo();
        info.State = MatchingState.TryMatching;
        info.Level = UserSetting.MatchLevel;
        info.WithFriend = MatchingFriend.Join;
        info.RoomNumber = roomNumber;
        NetClientApp.GetInstance().Request(NetCMD.SearchOpponent, info, null);
    }

    void ReadyToFight(SearchOpponentInfo pvpInfo)
    {
        if (!UserSetting.IsBotPlayer)
        {
            Purchases.UseHeart();

            string log = "[PVP Start] " + "OppUserPK:" + pvpInfo.OppUserInfo.userPk + ", HeartCount:" + Purchases.CountHeart();
            LOG.echo(log);
        }

        //SkillPair[] oppSkillMap = InGameManager.InstPVP_Opponent.SkillMapping;
        //oppSkillMap[(int)ProductColor.Blue] = new Tuple<PVPCommand, Sprite>(opponentInfo.skillBlue, GetSkillimage(opponentInfo.skillBlue));
        //oppSkillMap[(int)ProductColor.Green] = new Tuple<PVPCommand, Sprite>(opponentInfo.skillGreen, GetSkillimage(opponentInfo.skillGreen));
        //oppSkillMap[(int)ProductColor.Orange] = new Tuple<PVPCommand, Sprite>(opponentInfo.skillOrange, GetSkillimage(opponentInfo.skillOrange));
        //oppSkillMap[(int)ProductColor.Purple] = new Tuple<PVPCommand, Sprite>(opponentInfo.skillPurple, GetSkillimage(opponentInfo.skillPurple));
        //oppSkillMap[(int)ProductColor.Red] = new Tuple<PVPCommand, Sprite>(opponentInfo.skillRed, GetSkillimage(opponentInfo.skillRed));
        //oppSkillMap[(int)ProductColor.Yellow] = new Tuple<PVPCommand, Sprite>(opponentInfo.skillYellow, GetSkillimage(opponentInfo.skillYellow));

        ResetMatchUI();

        gameObject.SetActive(false);
        SoundPlayer.Inst.StopBackMusic();
        MenuPVPReady.PopUp(UserSetting.UserInfo, pvpInfo.OppUserInfo, pvpInfo.Level);
    }
    private void FailMatch()
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectWrongMatched);
        ResetMatchUI();
    }
    private IEnumerator AutoMatch()
    {
        yield return new WaitForSeconds(1);
        OnMatch();
    }
    private void ResetMatchUI()
    {
        mIsSearching = false;
        WaitText.text = "";
        WaitText.gameObject.SetActive(false);
        RoomID.text = "";
        RoomID.gameObject.SetActive(false);
        BtnFriend.SetActive(true);
        BtnMatch.SetActive(true);
        BtnCancle.SetActive(false);
        UpdateUserInfo(UserSetting.UserInfo);
        StopCoroutine("WaitOpponent");
        ItemButton[] btns = GetComponentsInChildren<ItemButton>();
        foreach (ItemButton btn in btns)
        {
            btn.UpdateItem();
            int idx = btn.transform.GetSiblingIndex();
            UserSetting.UserInfo.PvpItems[idx] = btn.GetItem();
        }
        UpdateLeagueLevelButtons();
    }
    private void UpdateUserInfo(UserInfo info)
    {
        WinLose.text = info.win + " / " + info.lose;
        int rank = (int)(info.rankingRate * 100.0f);
        Ranking.text = "Top " + rank + "%";
        UpdateExpBar(info.score);
        //string text =
        //    "ID : #" + info.userPk + "\n" +
        //    "Name : " + info.userName + "\n" +
        //    "Score : " + info.score + "\n" +
        //    "Win/Lose : " + info.win + "/" + info.lose;
        //
        //if (UserSetting.UserPK == info.userPk)
        //    MyUserInfo.text = text;
        //else
        //    OppUserInfo.text = text;
    }
    private void UpdateExpBar(int score)
    {
        int level = Utils.ToLevel(score);
        ExpLevel.text = level.ToString();
        Exp.text = score.ToString();
        int dd = score % Utils.ScorePerLevel;
        float rate = (float)dd / Utils.ScorePerLevel;
        ExpBar.normalizedValue = rate;
    }
    private void UpdateLeagueLevelButtons()
    {
        foreach(Button btn in LeagueLevelBtns)
        {
            int idx = btn.transform.GetSiblingIndex();
            MatchingLevel level = (MatchingLevel)(idx + 1);
            if(level == UserSetting.MatchLevel)
            {
                btn.transform.GetChild(0).GetChild(0).gameObject.SetActive(true);
            }
            else
            {
                btn.transform.GetChild(0).GetChild(0).gameObject.SetActive(false);
            }

            if (level <= UserSetting.UserInfo.maxLeague)
            {
                btn.transform.GetChild(0).gameObject.SetActive(true);
                btn.transform.GetChild(1).gameObject.SetActive(false);
                btn.enabled = true;
            }
            else
            {
                btn.transform.GetChild(0).gameObject.SetActive(false);
                btn.transform.GetChild(1).gameObject.SetActive(true);
                btn.enabled = false;
            }
        }
    }

    private ProductColor ToColor(string color)
    {
        string lowerColor = color.ToLower();
        switch(lowerColor)
        {
            case "blue": return ProductColor.Blue;
            case "green": return ProductColor.Green;
            case "orange": return ProductColor.Orange;
            case "purple": return ProductColor.Purple;
            case "red": return ProductColor.Red;
            case "yellow": return ProductColor.Yellow;
        }
        return ProductColor.None;
    }
    private PVPCommand ToSkill(string skill)
    {
        string lowerSkill = skill.ToLower();
        switch (lowerSkill)
        {
            case "bomb": return PVPCommand.SkillBomb;
            case "ice": return PVPCommand.SkillIce;
            case "shield": return PVPCommand.SkillShield;
            case "scorebuff": return PVPCommand.SkillScoreBuff;
            case "cloud": return PVPCommand.SkillCloud;
            case "upsidedown": return PVPCommand.SkillUpsideDown;
            case "remove": return PVPCommand.SkillRemoveBadEffects;
        }
        return PVPCommand.Undef;
    }
}
