using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using SkillPair = System.Tuple<PVPCommand, UnityEngine.Sprite>;

public class MenuWaitMatch : MonoBehaviour
{
    private const string UIObjName = "UISpace/CanvasPopup/SearchBattle";
    private bool mIsSearching = false;

    public TextMeshProUGUI WaitText;
    public TextMeshProUGUI Ranking;
    public TextMeshProUGUI ExpLevel;
    public TextMeshProUGUI Exp;
    public TextMeshProUGUI WinLose;
    public Slider ExpBar;
    public Image RankImage;
    public GameObject BtnMatch;
    public GameObject BtnCancle;

    public static void PopUp()
    {
        NetClientApp.GetInstance().IsKeepConnection = true;
        GameObject menuMatch = GameObject.Find(UIObjName);
        MenuWaitMatch menu = menuMatch.GetComponent<MenuWaitMatch>();
        menuMatch.SetActive(true);
        menu.ResetMatchUI();

        if (UserSetting.IsBotPlayer)
            menu.StartCoroutine(menu.AutoMatch());
    }

    public void OnClose()
    {
        NetClientApp.GetInstance().IsKeepConnection = false;
        SearchOpponentInfo info = new SearchOpponentInfo();
        info.MyUserInfo = UserSetting.UserInfo;
        NetClientApp.GetInstance().Request(NetCMD.StopMatching, info, null);

        mIsSearching = false;
        gameObject.SetActive(false);
        MenuStages.PopUp();
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

    public void OnMatch()
    {
        if (NetClientApp.GetInstance().IsDisconnected())
        {
            MenuNetConnector.PopUp(() =>
            {
                UserSetting.UpdateUserInfoFromServer();
                OnMatch();
            });
            return;
        }

        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);

        if (UserSetting.UserName.Length < UserSetting.NameLengthMin)
        {
            MenuSettings.EditUserName();
            return;
        }

//#if (UNITY_ANDROID || UNITY_IPHONE) && !UNITY_EDITOR
        if (UserSetting.StageIsLocked(7))
        {
            MenuMessageBox.PopUp("Required\n7 Stages", false, null);
            return;
        }
        else if (UserSetting.MatchLevel == MatchingLevel.Normal)
        {
            int pvpLevel = Utils.ToLevel(UserSetting.UserScore);
            if (pvpLevel < 5)
            {
                MenuMessageBox.PopUp("Required\n5 Level", false, null);
                return;
            }
        }
        else if (UserSetting.MatchLevel == MatchingLevel.Hard)
        {
            int pvpLevel = Utils.ToLevel(UserSetting.UserScore);
            if (pvpLevel < 20)
            {
                MenuMessageBox.PopUp("Required\n20 Level", false, null);
                return;
            }
        }
        else if (UserSetting.MatchLevel == MatchingLevel.Hell)
        {
            int pvpLevel = Utils.ToLevel(UserSetting.UserScore);
            if (pvpLevel < 30)
            {
                MenuMessageBox.PopUp("Required\n30 Level", false, null);
                return;
            }
        }

        if (Purchases.CountHeart() <= 0)
        {
            MenuMessageBox.PopUp("No Life", false, null);
            return;
        }
//#endif

        RequestMatch();

        mIsSearching = true;
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
            ReadyToFight(res);
        else if(res.State == MatchingState.FoundOpp)
        {
            SearchOpponentInfo info = new SearchOpponentInfo();
            info.MyUserInfo = UserSetting.UserInfo;
            info.OppUserInfo = res.OppUserInfo;
            info.State = MatchingState.FoundOppAck;
            NetClientApp.GetInstance().Request(NetCMD.SearchOpponent, info, null);
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
        SoundPlayer.Inst.PlayerBack.Stop();
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
        BtnMatch.SetActive(true);
        BtnCancle.SetActive(false);
        UpdateUserInfo(UserSetting.UserInfo);
        StopCoroutine("WaitOpponent");
    }
    private void UpdateUserInfo(UserInfo info)
    {
        WinLose.text = info.win + " / " + info.lose;
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
