using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuAttendance : MonoBehaviour
{
    private const string UIObjName = "UISpace/CanvasPopup/Attendance";
    public GameObject RewardsParent;

    public static MenuAttendance Inst()
    {
        return GameObject.Find(UIObjName).GetComponent<MenuAttendance>();
    }

    public static void PopUp()
    {
        GameObject menuMatch = GameObject.Find(UIObjName);
        MenuAttendance menu = menuMatch.GetComponent<MenuAttendance>();
        menuMatch.SetActive(true);
        menu.UpdateUI();
    }

    public void OnClose()
    {
        gameObject.SetActive(false);
        MenuStages.PopUp();
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton2);
    }

    public void OnClickAttendance()
    {
        Button btn = EventSystem.current.currentSelectedGameObject.GetComponent<Button>();
        btn.enabled = false;
        Purchases.SetAttendFlag(GetDayIndex());

        //Reward Attendance...
        string rewardText = btn.name;
        StageInfo.DoReward(rewardText);

        UpdateUI();
    }


    public bool IsAttendable()
    {
        return Purchases.IsAttend(GetDayIndex());
    }

    private void UpdateUI()
    {
        int todayIndex = GetDayIndex();

        Button[] btns = RewardsParent.GetComponentsInChildren<Button>();
        foreach(Button btn in btns)
        {
            int idx = btn.transform.GetSiblingIndex();
            string rewardText = btn.name;
            var rewardInfo = StageInfo.StringToRewardInfo(rewardText);
            btn.image.sprite = rewardInfo.Item2;
            btn.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = idx + " Day";
            btn.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = rewardInfo.Item3.ToString();

            //color on/off
            //button en/disable
            //mark on/off
        }
    }
    private int GetDayIndex()
    {
        TimeSpan span = DateTime.Now - UserSetting.FirstLaunchDate;
        return span.Days;
    }
}
