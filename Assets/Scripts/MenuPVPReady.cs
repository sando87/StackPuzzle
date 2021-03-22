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

    public TextMeshProUGUI PlayerName;
    public TextMeshProUGUI PlayerLevel;
    public TextMeshProUGUI OpponentName;
    public TextMeshProUGUI OpponentLevel;

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

        menu.StartCoroutine(menu.StartBattle());
    }

    private void UpdateUserInfo(UserInfo player, UserInfo opponent)
    {
        PlayerName.text = player.userName;
        PlayerLevel.text = "Lv." + UserSetting.ToLevel(player.score);
        OpponentName.text = opponent.userName;
        OpponentLevel.text = "Lv." + UserSetting.ToLevel(opponent.score);
    }

    private IEnumerator StartBattle()
    {
        yield return new WaitForSeconds(1);
        StageInfo info = StageInfo.Load(0);
        InGameManager.InstPVP_Opponent.StartGameInPVPOpponent(info, mOpponent);

        yield return new WaitForSeconds(2);

        InGameManager.InstPVP_Player.StartGameInPVPPlayer(info, mPlayer);
        InGameManager.InstPVP_Player.InitProducts();

        gameObject.SetActive(false);
        MenuBattle.PopUp();
    }
}
