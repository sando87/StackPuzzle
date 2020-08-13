using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Android;

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
#if PLATFORM_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead))
            Permission.RequestUserPermission(Permission.ExternalStorageRead);
#endif

        string fullname = Application.persistentDataPath + "/StageInfo/" + stageNum + ".txt";
        if(!File.Exists(fullname)) //최초 실행시 한번만 수행됨(각 스테이지 정보를 기록한 파일들 Save)
        {
            CreateStageInfoFolder();

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

    public static void CreateStageInfoFolder()
    {
        string sDirPath = Application.persistentDataPath + "/StageInfo";
        DirectoryInfo di = new DirectoryInfo(sDirPath);
        if (di.Exists == false)
            di.Create();
    }

    public void DefaultSetting(int level)
    {
        //테스트를 위한 임시 스테이지 구성
        if(level == 1)
        {
            Num = level;
            GoalScore = 500;
            IsLocked = false;
            StarCount = 0;
            MoveLimit = 20;
            XCount = 6;
            YCount = 6;
            ColorCount = 4;
        }
        else if (level == 2)
        {
            Num = level;
            GoalScore = 800;
            IsLocked = true;
            StarCount = 0;
            MoveLimit = 20;
            XCount = 7;
            YCount = 7;
            ColorCount = 4;
        }
        else if (level == 3)
        {
            Num = level;
            GoalScore = 2000;
            IsLocked = true;
            StarCount = 0;
            MoveLimit = 1;
            XCount = 8;
            YCount = 8;
            ColorCount = 3;
        }
        else if (level == 4)
        {
            Num = level;
            GoalScore = 500;
            IsLocked = true;
            StarCount = 0;
            MoveLimit = 30;
            XCount = 8;
            YCount = 8;
            ColorCount = 5;
        }
        else if (level == 5)
        {
            Num = level;
            GoalScore = 800;
            IsLocked = true;
            StarCount = 0;
            MoveLimit = 30;
            XCount = 8;
            YCount = 8;
            ColorCount = 5;
        }
        else if (level == 6)
        {
            Num = level;
            GoalScore = 1500;
            IsLocked = true;
            StarCount = 0;
            MoveLimit = 30;
            XCount = 8;
            YCount = 8;
            ColorCount = 5;
        }
        else if (level == 7)
        {
            Num = level;
            GoalScore = 500;
            IsLocked = true;
            StarCount = 0;
            MoveLimit = 30;
            XCount = 8;
            YCount = 8;
            ColorCount = 6;
        }
        else if (level == 8)
        {
            Num = level;
            GoalScore = 700;
            IsLocked = true;
            StarCount = 0;
            MoveLimit = 30;
            XCount = 8;
            YCount = 8;
            ColorCount = 6;
        }
        else if (level == 9)
        {
            Num = level;
            GoalScore = 1000;
            IsLocked = true;
            StarCount = 0;
            MoveLimit = 30;
            XCount = 8;
            YCount = 8;
            ColorCount = 6;
        }
        else if (level == 10)
        {
            Num = level;
            GoalScore = 1500;
            IsLocked = true;
            StarCount = 0;
            MoveLimit = 5;
            XCount = 8;
            YCount = 8;
            ColorCount = 4;
        }
    }
}
