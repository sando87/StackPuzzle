using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuPVPFriend : MonoBehaviour
{
    private Action<MatchingFriend, int> EventClick = null;
    private int mDefaultJoinID = -1;

    public static MenuPVPFriend PopUp(int defaultJoinID, Action<MatchingFriend, int> eventClick)
    {
        GameObject prefab = (GameObject)Resources.Load("Prefabs/MenuPVPFriend", typeof(GameObject));
        GameObject objMenu = GameObject.Instantiate(prefab, GameObject.Find("UISpace/CanvasPopup").transform);
        MenuPVPFriend box = objMenu.GetComponent<MenuPVPFriend>();
        box.EventClick = eventClick;
        box.mDefaultJoinID = defaultJoinID;
        return box;
    }

    public void OnMake()
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
        Destroy(gameObject);
        EventClick?.Invoke(MatchingFriend.Make, 0);
    }
    public void OnJoin()
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
        MenuEditBox.PopUp("Write Room ID", mDefaultJoinID.ToString(), (isOK, inputText) =>
        {
            if(isOK)
            {
                int roomID = 0;
                bool isNumberOK = int.TryParse(inputText, out roomID);
                if(isNumberOK)
                {
                    Destroy(gameObject);
                    EventClick?.Invoke(MatchingFriend.Join, roomID);
                }
                else
                {
                    MenuInformBox.PopUp("Write the numbers.");
                }
            }
        });
    }
    public void OnCancle()
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton2);
        Destroy(gameObject);
    }
}
