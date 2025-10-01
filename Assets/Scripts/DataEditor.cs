using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

#if UNITY_EDITOR

public class DataEditor : EditorWindow
{
    [MenuItem("/Users/DataEditor")]
    private static void ShowWind기ow()
    {
        var window = GetWindow<DataEditor>();
        window.titleContent = new GUIContent("Data Editor");
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.BeginVertical();

        if (GUILayout.Button("Delete UserSettingInfo", new GUILayoutOption[] { GUILayout.Width(150) }))
        {
            UserSettingInfo.Delete();
        }
        if (GUILayout.Button("Delete UserInfo", new GUILayoutOption[] { GUILayout.Width(150) }))
        {
            UserSetting.DeleteUserInfo();
        }
        if (GUILayout.Button("Delete Purchase", new GUILayoutOption[] { GUILayout.Width(150) }))
        {
            Purchases.DeletePurchaseInfo();
        }
        if (GUILayout.Button("Purchases", new GUILayoutOption[] { GUILayout.Width(150) }))
        {
            Purchases.AddGold(1000);
            Purchases.PurchaseDiamond(10);
        }
        if (GUILayout.Button("AddTutorial", new GUILayoutOption[] { GUILayout.Width(150) }))
        {
            UserSetting.TutorialNumber++;
        }
        if (GUILayout.Button("SubTutorial", new GUILayoutOption[] { GUILayout.Width(150) }))
        {
            UserSetting.TutorialNumber--;
        }
        if (GUILayout.Button("AddStage", new GUILayoutOption[] { GUILayout.Width(150) }))
        {
            UserSetting.StageUnLock(UserSetting.GetHighestStageNumber() + 1);
        }

        GUILayout.EndVertical();
    }

    [MenuItem("CONTEXT/Transform/RenameAllChild")]
    static void RenameAllChild(MenuCommand command)
    {
        Transform objTr = (Transform)command.context;
        if (objTr.childCount <= 0)
            return;

        int startNum = int.Parse(objTr.GetChild(0).name);
        for (int i = 0; i < objTr.childCount; ++i)
        {
            objTr.GetChild(i).name = startNum.ToString();
            startNum++;
        }
    }

}
#endif