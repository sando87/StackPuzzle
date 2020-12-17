using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using SkillPair = System.Tuple<PVPCommand, UnityEngine.Sprite>;

public class MenuWaitMatch : MonoBehaviour
{
    private const string UIObjName = "CanvasPopUp/MenuWaitMatch";
    private bool mIsSearching = false;
    private ProductColor mCurrentProductColor = ProductColor.None;

    public Text HeartTimer;
    public Text HeartCount;
    public Text MyUserInfo;
    public Text OppUserInfo;
    public Text CountDown;
    public GameObject BtnClose;
    public GameObject BtnMatch;
    public GameObject BtnCancle;

    public GameObject SkillSelector;
    public Image ProductBlueForSkill;
    public Image ProductGreenForSkill;
    public Image ProductOrangeForSkill;
    public Image ProductPurpleForSkill;
    public Image ProductRedForSkill;
    public Sprite[] Skillimages;

    public static void PopUp(bool autoPlay = false)
    {
        GameObject menuMatch = GameObject.Find("UIGroup").transform.Find(UIObjName).gameObject;
        MenuWaitMatch menu = menuMatch.GetComponent<MenuWaitMatch>();
        menuMatch.SetActive(true);
        menu.ResetMatchUI();

        if (autoPlay)
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
        MenuMain.PopUp();
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
        BtnCancle.SetActive(true);
        BtnMatch.SetActive(false);

        StartCoroutine("WaitOpponent");
    }

    IEnumerator WaitOpponent()
    {
        int n = 0;
        while(true)
        {
            switch(n%3)
            {
                case 0: OppUserInfo.text = "Matching.."; break;
                case 1: OppUserInfo.text = "Matching..."; break;
                case 2: OppUserInfo.text = "Matching...."; break;
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
                    StartCoroutine(StartCountDown(res));
            }
            return;
        });
    }

    IEnumerator StartCountDown(SearchOpponentInfo opponentInfo)
    {
        if (!UserSetting.IsBotPlayer)
            Purchases.UseHeart();

        StageInfo info = StageInfo.Load(0);

        SkillPair[] oppSkillMap = InGameManager.InstPVP_Opponent.SkillMapping;
        oppSkillMap[(int)ProductColor.Blue] = new Tuple<PVPCommand, Sprite>(opponentInfo.skillBlue, GetSkillimage(opponentInfo.skillBlue));
        oppSkillMap[(int)ProductColor.Green] = new Tuple<PVPCommand, Sprite>(opponentInfo.skillGreen, GetSkillimage(opponentInfo.skillGreen));
        oppSkillMap[(int)ProductColor.Orange] = new Tuple<PVPCommand, Sprite>(opponentInfo.skillOrange, GetSkillimage(opponentInfo.skillOrange));
        oppSkillMap[(int)ProductColor.Purple] = new Tuple<PVPCommand, Sprite>(opponentInfo.skillPurple, GetSkillimage(opponentInfo.skillPurple));
        oppSkillMap[(int)ProductColor.Red] = new Tuple<PVPCommand, Sprite>(opponentInfo.skillRed, GetSkillimage(opponentInfo.skillRed));
        oppSkillMap[(int)ProductColor.Yellow] = new Tuple<PVPCommand, Sprite>(opponentInfo.skillYellow, GetSkillimage(opponentInfo.skillYellow));

        InGameManager.InstPVP_Opponent.StartGame(info, opponentInfo.UserInfo);
        InGameManager.InstPVP_Player.StartGame(info, UserSetting.UserInfo);

        mIsSearching = false;
        StopCoroutine("WaitOpponent");
        UpdateUserInfo(opponentInfo.UserInfo);
        BtnClose.SetActive(false);
        BtnMatch.SetActive(false);
        BtnCancle.SetActive(false);

        CountDown.gameObject.SetActive(true);
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectCountDown);
        CountDown.text = "3";
        yield return new WaitForSeconds(1);
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectCountDown);
        CountDown.text = "2";
        yield return new WaitForSeconds(1);
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectCountDown);
        CountDown.text = "1";
        yield return new WaitForSeconds(1);

        InGameManager.InstPVP_Player.InitProducts();

        gameObject.SetActive(false);
        MenuBattle.PopUp();
    }
    private void FailMatch()
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectAlarm);
        ResetMatchUI();
    }
    private IEnumerator AutoMatch()
    {
        yield return new WaitForSeconds(5);
        OnMatch();
    }
    private void ResetMatchUI()
    {
        mCurrentProductColor = ProductColor.None;
        mIsSearching = false;
        BtnCancle.SetActive(false);
        BtnMatch.SetActive(true);
        BtnClose.SetActive(true);
        UpdateUserInfo(UserSetting.UserInfo);
        OppUserInfo.text = "No Matched User";
        CountDown.text = "0";
        CountDown.gameObject.SetActive(false);
        StopCoroutine("WaitOpponent");
        StopCoroutine("UpdateHeartTimer");
        StartCoroutine("UpdateHeartTimer");
        UpdateSkillPanel();
    }
    private void UpdateUserInfo(UserInfo info)
    {
        string text =
            "ID : #" + info.userPk + "\n" +
            "Name : " + info.userName + "\n" +
            "Score : " + info.score + "\n" +
            "Win/Lose : " + info.win + "/" + info.lose;

        if (UserSetting.UserPK == info.userPk)
            MyUserInfo.text = text;
        else
            OppUserInfo.text = text;
    }
    IEnumerator UpdateHeartTimer()
    {
        while (true)
        {
            Purchases.UpdateHeartTimer();
            int remainSec = Purchases.RemainSeconds();
            int remainLife = Purchases.CountHeart();
            HeartCount.text = remainLife.ToString();
            if (Purchases.MaxHeart())
            {
                HeartTimer.text = "Full";
            }
            else
            {
                int min = remainSec / 60;
                int sec = remainSec % 60;
                string secStr = string.Format("{0:D2}", sec);
                HeartTimer.text = min + ":" + secStr;
            }
            yield return new WaitForSeconds(1);
        }
    }
    public void OnSelectProduct(ProductColor color)
    {
        mCurrentProductColor = color;
        SkillSelector.SetActive(true);
    }
    public void OnSelectSkill(PVPCommand skill)
    {
        InGameManager.InstPVP_Player.SkillMapping[(int)mCurrentProductColor] = new Tuple<PVPCommand, Sprite>(skill, GetSkillimage(skill));

        UpdateSkillPanel();
        SkillSelector.SetActive(false);
        mCurrentProductColor = ProductColor.None;
    }

    private void UpdateSkillPanel()
    {
        SkillPair[] map = InGameManager.InstPVP_Player.SkillMapping;

        ProductBlueForSkill.sprite = GetSkillimage(map[(int)ProductColor.Blue].Item1);
        ProductGreenForSkill.sprite = GetSkillimage(map[(int)ProductColor.Green].Item1);
        ProductOrangeForSkill.sprite = GetSkillimage(map[(int)ProductColor.Orange].Item1);
        ProductPurpleForSkill.sprite = GetSkillimage(map[(int)ProductColor.Purple].Item1);
        ProductRedForSkill.sprite = GetSkillimage(map[(int)ProductColor.Red].Item1);
    }
    private Sprite GetSkillimage(PVPCommand skill)
    {
        Sprite sprite = null;
        switch (skill)
        {
            case PVPCommand.SkillBomb: sprite = Skillimages[1]; break;
            case PVPCommand.SkillIce: sprite = Skillimages[2]; break;
            case PVPCommand.SkillShield: sprite = Skillimages[3]; break;
            case PVPCommand.SkillScoreBuff: sprite = Skillimages[4]; break;
            case PVPCommand.SkillCloud: sprite = Skillimages[5]; break;
            case PVPCommand.SkillUpsideDown: sprite = Skillimages[6]; break;
            case PVPCommand.SkillRemoveBadEffects: sprite = Skillimages[7]; break;
            case PVPCommand.Undef: sprite = Skillimages[0]; break;
        }
        return null;
    }
}
