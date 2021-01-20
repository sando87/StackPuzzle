using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using SkillPair = System.Tuple<PVPCommand, UnityEngine.Sprite>;

public class MenuPVPReady : MonoBehaviour
{
    private const string UIObjName = "UISpace/CanvasPopup/PlayPVPReady";

    public static void PopUp(UserInfo player, UserInfo opponent)
    {
        GameObject menuPlay = GameObject.Find(UIObjName);
        MenuPVPReady menu = menuPlay.GetComponent<MenuPVPReady>();
        menu.UpdateUserInfo(player, opponent);

        StageInfo info = StageInfo.Load(0);
        InGameManager.InstPVP_Opponent.StartGame(info, opponent);
        InGameManager.InstPVP_Player.StartGame(info, player);

        menu.Invoke("StartBattle", 3.0f);
    }

    private void UpdateUserInfo(UserInfo player, UserInfo opponent)
    {

    }

    private void StartBattle()
    {
        InGameManager.InstPVP_Player.InitProducts();

        gameObject.SetActive(false);
        MenuBattle.PopUp();
    }
}
