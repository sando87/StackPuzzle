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
    public int RowCount;
    public int ColumnCount;

    public static StageInfo Load(int stageNum)
    {
        string fileText = File.ReadAllText(Application.persistentDataPath + "/StageInfo/" + stageNum + ".txt");
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
                case "RowCount": info.RowCount = int.Parse(tokens[1]); break;
                case "ColumnCount": info.ColumnCount = int.Parse(tokens[1]); break;
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
            + "RowCount," + info.RowCount.ToString() + "\r\n"
            + "ColumnCount," + info.ColumnCount.ToString();

        File.WriteAllText(Application.persistentDataPath + "/StageInfo/" + info.Num + ".txt", text);
    }
}
