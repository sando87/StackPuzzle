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
    public Image RankImage;
    public Image ExpBar;
    public GameObject BtnMatch;
    public GameObject BtnCancle;

    public static void PopUp()
    {
        GameObject menuMatch = GameObject.Find(UIObjName);
        MenuWaitMatch menu = menuMatch.GetComponent<MenuWaitMatch>();
        menuMatch.SetActive(true);
        menu.ResetMatchUI();

        if (UserSetting.IsBotPlayer)
            menu.StartCoroutine(menu.AutoMatch());
    }

    public void OnClose()
    {
        if(mIsSearching)
        {
            SearchOpponentInfo info = new SearchOpponentInfo();
            info.userPk = UserSetting.UserPK;
            NetClientApp.GetInstance().Request(NetCMD.StopMatching, info, null);
        }

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
        info.userPk = UserSetting.UserPK;
        NetClientApp.GetInstance().Request(NetCMD.StopMatching, info, null);
    }

    public void OnMatch()
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);

        if (NetClientApp.GetInstance().IsDisconnected())
        {
            MenuMessageBox.PopUp("Network Disconnected", false, null);
            return;
        }

#if (UNITY_ANDROID || UNITY_IPHONE) && !UNITY_EDITOR
        //if (UserSetting.StageIsLocked(16))
        //{
        //    MenuMessageBox.PopUp("Required\n15 Stages", false, null);
        //    return;
        //}
        //
        //if (Purchases.CountHeart() <= 0)
        //{
        //    MenuMessageBox.PopUp("No Life", false, null);
        //    return;
        //}
#endif

        UserInfo userInfo = UserSetting.LoadUserInfo();
        if (userInfo.userPk <= 0)
            StartCoroutine(WaitForAddingUserInfo());
        else
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

    IEnumerator WaitForAddingUserInfo()
    {
        int limit = 60;
        while(0 < limit--)
        {
            yield return new WaitForSeconds(1);
            if (UserSetting.UserPK > 0)
            {
                RequestMatch();
                break;
            }
        }
    }

    private void RequestMatch()
    {
        SkillPair[] skillMap = InGameManager.InstPVP_Player.SkillMapping;
        SearchOpponentInfo info = new SearchOpponentInfo();
        info.userPk = UserSetting.UserPK;
        info.colorCount = 4.2f; // 4~6.0f
        info.UserInfo = UserSetting.UserInfo;
        info.isBotPlayer = UserSetting.UserInfo.deviceName.Contains("home") ? true : false;
        info.isDone = false;
        info.skillBlue = skillMap[(int)ProductColor.Blue].Item1;
        info.skillGreen = skillMap[(int)ProductColor.Green].Item1;
        info.skillOrange = skillMap[(int)ProductColor.Orange].Item1;
        info.skillPurple = skillMap[(int)ProductColor.Purple].Item1;
        info.skillRed = skillMap[(int)ProductColor.Red].Item1;
        info.skillYellow = PVPCommand.Undef;

        NetClientApp.GetInstance().Request(NetCMD.SearchOpponent, info, (_body) =>
        {
            SearchOpponentInfo res = Utils.Deserialize<SearchOpponentInfo>(ref _body);
            if (res.isDone && mIsSearching)
            {
                if (res.userPk == -1 || res.userPk == UserSetting.UserPK)
                {
                    FailMatch();

                    if (UserSetting.IsBotPlayer)
                        StartCoroutine(AutoMatch());
                }
                else
                    ReadyToFight(res);
            }
            return;
        });
    }

    void ReadyToFight(SearchOpponentInfo opponentInfo)
    {
        if (!UserSetting.IsBotPlayer)
            Purchases.UseHeart();


        //SkillPair[] oppSkillMap = InGameManager.InstPVP_Opponent.SkillMapping;
        //oppSkillMap[(int)ProductColor.Blue] = new Tuple<PVPCommand, Sprite>(opponentInfo.skillBlue, GetSkillimage(opponentInfo.skillBlue));
        //oppSkillMap[(int)ProductColor.Green] = new Tuple<PVPCommand, Sprite>(opponentInfo.skillGreen, GetSkillimage(opponentInfo.skillGreen));
        //oppSkillMap[(int)ProductColor.Orange] = new Tuple<PVPCommand, Sprite>(opponentInfo.skillOrange, GetSkillimage(opponentInfo.skillOrange));
        //oppSkillMap[(int)ProductColor.Purple] = new Tuple<PVPCommand, Sprite>(opponentInfo.skillPurple, GetSkillimage(opponentInfo.skillPurple));
        //oppSkillMap[(int)ProductColor.Red] = new Tuple<PVPCommand, Sprite>(opponentInfo.skillRed, GetSkillimage(opponentInfo.skillRed));
        //oppSkillMap[(int)ProductColor.Yellow] = new Tuple<PVPCommand, Sprite>(opponentInfo.skillYellow, GetSkillimage(opponentInfo.skillYellow));

        ResetMatchUI();

        gameObject.SetActive(false);
        MenuPVPReady.PopUp(UserSetting.UserInfo, opponentInfo.UserInfo);
    }
    private void FailMatch()
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectAlarm);
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
