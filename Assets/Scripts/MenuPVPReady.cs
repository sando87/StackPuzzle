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

    private UserInfo mPlayer = null;
    private UserInfo mOpponent = null;

    public static void PopUp(UserInfo player, UserInfo opponent)
    {
        GameObject menuPlay = GameObject.Find(UIObjName);
        menuPlay.SetActive(true);
        MenuPVPReady menu = menuPlay.GetComponent<MenuPVPReady>();
        menu.UpdateUserInfo(player, opponent);

        menu.mPlayer = player;
        menu.mOpponent = opponent;
        
        menu.Invoke("StartBattle", 3.0f);
    }

    private void UpdateUserInfo(UserInfo player, UserInfo opponent)
    {

    }

    private void StartBattle()
    {
        StageInfo info = StageInfo.Load(0);
        InGameManager.InstPVP_Opponent.StartGameInPVPOpponent(info, mOpponent);
        InGameManager.InstPVP_Player.StartGameInPVPPlayer(info, mPlayer);

        InGameManager.InstPVP_Player.InitProducts();

        gameObject.SetActive(false);
        MenuBattle.PopUp();
    }
}