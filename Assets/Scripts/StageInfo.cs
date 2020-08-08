using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class StageInfo
{
    public int Num;
    public int GoalScore;
    public bool IsLocked;
    public int StarCount;
    public int MoveLimit;
    public int XCount;
    public int YCount;
    public int ColorCount;

    public static StageInfo Load(int stageNum)
    {
        string fullname = Application.persistentDataPath + "/StageInfo/" + stageNum + ".txt";
        if(!File.Exists(fullname))
        {
            StageInfo defInfo = new StageInfo();
            defInfo.DefaultSetting(stageNum);
            Save(defInfo);
        }

        string fileText = File.ReadAllText(fullname);
        if (fileText == null || fileText.Length == 0)
            return null;

        StageInfo info = new StageInfo();
        info.Num = stageNum;

        string[] lines = fileText.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string line in lines)
        {
            string[] tokens = line.Split(',');
            if (tokens.Length < 2)
                continue;

            switch(tokens[0])
            {
                case "GoalScore": info.GoalScore = int.Parse(tokens[1]); break;
                case "IsLocked": info.IsLocked = bool.Parse(tokens[1]); break;
                case "StarCount": info.StarCount = int.Parse(tokens[1]); break;
                case "MoveLimit": info.MoveLimit = int.Parse(tokens[1]); break;
                case "XCount": info.XCount = int.Parse(tokens[1]); break;
                case "YCount": info.YCount = int.Parse(tokens[1]); break;
                case "ColorCount": info.ColorCount = int.Parse(tokens[1]); break;
            }
        }

        return info;
    }
    public static void Save(StageInfo info)
    {
        string text = "GoalScore," + info.GoalScore.ToString() + "\r\n"
            + "IsLocked," + info.IsLocked.ToString() + "\r\n"
            + "StarCount," + info.StarCount.ToString() + "\r\n"
            + "MoveLimit," + info.MoveLimit.ToString() + "\r\n"
            + "XCount," + info.XCount.ToString() + "\r\n"
            + "YCount," + info.YCount.ToString() + "\r\n"
            + "ColorCount," + info.ColorCount.ToString() + "\r\n";

        File.WriteAllText(Application.persistentDataPath + "/StageInfo/" + info.Num + ".txt", text);
    }

    public void DefaultSetting(int level)
    {
        Num = level;
        GoalScore = level * 200 + 500;
        IsLocked = level == 1 ? false : true;
        StarCount = 0;
        MoveLimit = 30;
        XCount = 6;
        YCount = 6;
        ColorCount = 4;
    }
}
