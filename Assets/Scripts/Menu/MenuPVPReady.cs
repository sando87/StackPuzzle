﻿using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using SkillPair = System.Tuple<PVPCommand, UnityEngine.Sprite>;

public class MenuPVPReady : MonoBehaviour
{
    private const string UIObjName = "UISpace/CanvasPopup/PlayPVPReady";

    public TextMeshProUGUI PlayerName;
    public TextMeshProUGUI PlayerLevel;
    public TextMeshProUGUI OpponentName;
    public TextMeshProUGUI OpponentLevel;

    private UserInfo mPlayer = null;
    private UserInfo mOpponent = null;
    private MatchingLevel mLevel = MatchingLevel.None;

    public static void PopUp(UserInfo player, UserInfo opponent, MatchingLevel level)
    {
        GameObject menuPlay = GameObject.Find(UIObjName);
        menuPlay.SetActive(true);
        MenuPVPReady menu = menuPlay.GetComponent<MenuPVPReady>();
        menu.mLevel = level;
        menu.UpdateUserInfo(player, opponent);

        menu.mPlayer = player;
        menu.mOpponent = opponent;

        menu.StartCoroutine(menu.StartBattle());
    }

    private void UpdateUserInfo(UserInfo player, UserInfo opponent)
    {
        PlayerName.text = player.userName;
        PlayerLevel.text = "Lv." + Utils.ToLevel(player.score);
        OpponentName.text = opponent.userName;
        OpponentLevel.text = "Lv." + Utils.ToLevel(opponent.score);
    }

    private IEnumerator StartBattle()
    {
        yield return new WaitForSeconds(1);
        StageInfo info = StageInfo.Load(mLevel);
        InGameManager.InstPVP_Opponent.StartGameInPVPOpponent(info, mOpponent);
        Vector3 pos = MenuBattle.Inst().OpponentRect.transform.position;
        pos.z = InGameManager.InstPVP_Opponent.transform.position.z;
        InGameManager.InstPVP_Opponent.transform.position = pos;

        yield return new WaitForSeconds(2);

        InGameManager.InstPVP_Player.StartGameInPVPPlayer(info, mPlayer);
        InGameManager.InstPVP_Player.InitProducts();
        pos = MenuBattle.Inst().PlayerRect.transform.position;
        pos.z = InGameManager.InstPVP_Player.transform.position.z;
        InGameManager.InstPVP_Player.transform.position = pos;

        SoundPlayer.Inst.PlayBackMusic(SoundPlayer.Inst.BackMusicInGame);
        gameObject.SetActive(false);
        MenuBattle.PopUp();
    }
}
