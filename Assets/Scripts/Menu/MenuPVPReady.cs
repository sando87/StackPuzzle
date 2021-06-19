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

    public Image PlayerLeague;
    public TextMeshProUGUI PlayerName;
    public TextMeshProUGUI PlayerLevel;
    public Image OpponentLeague;
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
        PlayerLeague.sprite = player.maxLeague.GetSprite();
        PlayerName.text = player.userName;
        PlayerLevel.text = "Lv." + Utils.ToLevel(player.score);
        OpponentLeague.sprite = opponent.maxLeague.GetSprite();
        OpponentName.text = opponent.userName;
        OpponentLevel.text = "Lv." + Utils.ToLevel(opponent.score);
    }

    private IEnumerator StartBattle()
    {
        yield return new WaitForSeconds(1);
        StageInfo info = StageInfo.Load(mLevel);
        Vector3 pos = MenuBattle.Inst().OpponentRect.transform.position;
        InGameManager.InstPVP_Opponent.transform.SetPosition2D(pos);
        InGameManager.InstPVP_Opponent.StartGameInPVPOpponent(info, mOpponent);
        
        yield return new WaitForSeconds(2);

        pos = MenuBattle.Inst().PlayerRect.transform.position;
        InGameManager.InstPVP_Player.transform.SetPosition2D(pos);
        pos = MenuBattle.Inst().AttackPointFrame.transform.position;
        InGameManager.InstPVP_Player.AttackPointFrame.transform.SetPosition2D(pos);

        InGameManager.InstPVP_Player.StartGameInPVPPlayer(info, mPlayer);
        InGameManager.InstPVP_Player.InitProducts();
        
        SoundPlayer.Inst.PlayBackMusic(SoundPlayer.Inst.BackMusicInGamePVP);
        gameObject.SetActive(false);
        MenuBattle.PopUp(info);
    }
}
