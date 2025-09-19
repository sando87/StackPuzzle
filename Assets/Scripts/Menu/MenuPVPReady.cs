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
    private int mMapRandomSeed = 0;

    public static void PopUp(UserInfo player, UserInfo opponent, MatchingLevel level, int mapRandomSeed)
    {
        GameObject menuPlay = GameObject.Find(UIObjName);
        menuPlay.SetActive(true);
        MenuPVPReady menu = menuPlay.GetComponent<MenuPVPReady>();
        menu.mLevel = level;
        menu.mMapRandomSeed = mapRandomSeed;
        menu.UpdateUserInfo(player, opponent);

        menu.mPlayer = player;
        menu.mOpponent = opponent;

        menu.StartCoroutine(menu.StartBattle());
    }

    private void UpdateUserInfo(UserInfo player, UserInfo opponent)
    {
        PlayerLeague.sprite = player.maxLeague.GetSprite();
        PlayerLeague.gameObject.SetActive(false);
        PlayerName.text = player.userName;
        PlayerLevel.text = "Lv." + Utils.ToLevel(player.score);
        OpponentLeague.sprite = opponent.maxLeague.GetSprite();
        OpponentLeague.gameObject.SetActive(false);
        OpponentName.text = opponent.userName;
        OpponentLevel.text = "Lv." + Utils.ToLevel(opponent.score);
    }

    private IEnumerator StartBattle()
    {
        yield return new WaitForSeconds(1);
        LOG.trace(mMapRandomSeed);
        StageInfo info = StageInfo.Load(mLevel, mMapRandomSeed);
        Vector3 pos = MenuBattle.Inst().OpponentRect.transform.position;
        InGameManager.InstPVP_Opponent.transform.SetPosition2D(pos);
        InGameManager.InstPVP_Opponent.StartGameInPVPOpponent(info, mOpponent);
        
        Rect oppFieldArea = InGameManager.InstPVP_Opponent.FieldWorldRect;
        MenuBattle.Inst().OpponentPanel.transform.position = new Vector3(oppFieldArea.xMax, oppFieldArea.yMax, 0);
        
        yield return new WaitForSeconds(2);
        
        pos = MenuBattle.Inst().PlayerRect.transform.position;
        InGameManager.InstPVP_Player.transform.SetPosition2D(pos);
        // pos = MenuBattle.Inst().AttackPointFrame.transform.position;
        // InGameManager.InstPVP_Player.AttackPointFrame.transform.SetPosition2D(pos);

        InGameManager.InstPVP_Player.StartGameInPVPPlayer(info, mPlayer);
        InGameManager.InstPVP_Player.InitProducts();

        Rect playerFieldArea = InGameManager.InstPVP_Player.FieldWorldRect;
        MenuBattle.Inst().PlayerPanel.transform.position = new Vector3(playerFieldArea.xMax, playerFieldArea.yMax, 0);
        
        SoundPlayer.Inst.PlayBackMusic(SoundPlayer.Inst.BackMusicInGamePVP);
        gameObject.SetActive(false);
        MenuBattle.PopUp(info);
    }
}
