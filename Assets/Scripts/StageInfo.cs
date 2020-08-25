using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Android;
public class StageInfoCell
{
    public bool ProductMovable;
    public int FrameCoverCount;
    public StageInfoCell(bool movable, int count)
    {
        ProductMovable = movable;
        FrameCoverCount = count;
    }
}
public class StageInfo
{
    public const int Version = 2;
    public int Num;
    public int GoalScore;
    public bool IsLocked;
    public int StarCount;
    public int MoveLimit;
    public int ColorCount;
    public int XCount;
    public int YCount;
    public StageInfoCell[,] Field;

    public static StageInfo Load(int stageNum)
    {
#if PLATFORM_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead))
            Permission.RequestUserPermission(Permission.ExternalStorageRead);
#endif

        string fullname = GetPath() + stageNum + ".txt";
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

        int RowIndex = 0;
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
                case "ColorCount": info.ColorCount = int.Parse(tokens[1]); break;
                case "XCount": info.XCount = int.Parse(tokens[1]); break;
                case "YCount": info.YCount = int.Parse(tokens[1]); RowIndex = info.YCount; break;
                case "Rows": info.ParseRow(--RowIndex, tokens[1]); break;
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
            + "ColorCount," + info.ColorCount.ToString() + "\r\n"
            + "XCount," + info.XCount.ToString() + "\r\n"
            + "YCount," + info.YCount.ToString() + "\r\n"
            ;

        for(int yIdx = info.YCount - 1; yIdx >= 0; --yIdx)
            text += "Rows," + info.RowToString(yIdx);

        File.WriteAllText(GetPath() + info.Num + ".txt", text);
    }

    public static void CreateStageInfoFolder()
    {
        string sDirPath = GetPath();
        DirectoryInfo di = new DirectoryInfo(sDirPath);
        if (di.Exists == false)
            di.Create();
    }

    public static string GetPath()
    {
        return Application.persistentDataPath + "/StageInfo/Version" + Version + "/";
    }

    public void ParseRow(int rowIndex, string row)
    {
        if (Field == null)
            Field = new StageInfoCell[XCount, YCount];

        string[] columns = row.Trim().Split(' ');
        if (columns.Length != XCount || rowIndex < 0)
            return;

        for(int xIdx = 0; xIdx < columns.Length; ++xIdx)
        {
            string[] keyValue = columns[xIdx].Split('/');
            Field[xIdx, rowIndex] = new StageInfoCell(keyValue[0] == "o", int.Parse(keyValue[1]));
        }
    }
    public string RowToString(int rowIndex)
    {
        if (Field == null)
            return "";

        string rowString = "";
        for (int xIdx = 0; xIdx < XCount; ++xIdx)
        {
            string ox = Field[xIdx, rowIndex].ProductMovable ? "o" : "x";
            rowString += ox + "/" + Field[xIdx, rowIndex].FrameCoverCount.ToString() + " ";
        }
        return rowString + "\r\n";
    }
    public StageInfoCell GetCell(int idxX, int idxY)
    {
        if (Field == null || idxX < 0 || idxY < 0 || idxX >= XCount || idxY >= YCount)
            return new StageInfoCell(true, 0);

        return Field[idxX, idxY];
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
            ColorCount = 4;
            XCount = 6;
            YCount = 6;
            Field = new StageInfoCell[XCount, YCount];
            for (int y =0; y < YCount; ++y)
                for (int x = 0; x < XCount; ++x)
                    Field[x, y] = new StageInfoCell(true, 0);
        }
        else if (level == 2)
        {
            Num = level;
            GoalScore = 800;
            IsLocked = true;
            StarCount = 0;
            MoveLimit = 20;
            ColorCount = 4;
            XCount = 7;
            YCount = 7;
            Field = new StageInfoCell[XCount, YCount];
            for (int y = 0; y < YCount; ++y)
                for (int x = 0; x < XCount; ++x)
                    Field[x, y] = new StageInfoCell(true, 0);
        }
        else if (level == 3)
        {
            Num = level;
            GoalScore = 2000;
            IsLocked = true;
            StarCount = 0;
            MoveLimit = 1;
            ColorCount = 3;
            XCount = 8;
            YCount = 8;
            Field = new StageInfoCell[XCount, YCount];
            for (int y = 0; y < YCount; ++y)
                for (int x = 0; x < XCount; ++x)
                    Field[x, y] = new StageInfoCell(true, 0);
        }
        else if (level == 4)
        {
            Num = level;
            GoalScore = 500;
            IsLocked = true;
            StarCount = 0;
            MoveLimit = 30;
            ColorCount = 5;
            XCount = 8;
            YCount = 8;
            Field = new StageInfoCell[XCount, YCount];
            for (int y = 0; y < YCount; ++y)
                for (int x = 0; x < XCount; ++x)
                    Field[x, y] = new StageInfoCell(true, 0);
        }
        else if (level == 5)
        {
            Num = level;
            GoalScore = 800;
            IsLocked = true;
            StarCount = 0;
            MoveLimit = 30;
            ColorCount = 5;
            XCount = 8;
            YCount = 8;
            Field = new StageInfoCell[XCount, YCount];
            for (int y = 0; y < YCount; ++y)
                for (int x = 0; x < XCount; ++x)
                    Field[x, y] = new StageInfoCell(true, 0);
        }
        else if (level == 6)
        {
            Num = level;
            GoalScore = 1500;
            IsLocked = true;
            StarCount = 0;
            MoveLimit = 30;
            ColorCount = 5;
            XCount = 8;
            YCount = 8;
            Field = new StageInfoCell[XCount, YCount];
            for (int y = 0; y < YCount; ++y)
                for (int x = 0; x < XCount; ++x)
                    Field[x, y] = new StageInfoCell(true, 0);
        }
        else if (level == 7)
        {
            Num = level;
            GoalScore = 500;
            IsLocked = true;
            StarCount = 0;
            MoveLimit = 30;
            ColorCount = 6;
            XCount = 8;
            YCount = 8;
            Field = new StageInfoCell[XCount, YCount];
            for (int y = 0; y < YCount; ++y)
                for (int x = 0; x < XCount; ++x)
                    Field[x, y] = new StageInfoCell(true, 0);
        }
        else if (level == 8)
        {
            Num = level;
            GoalScore = 700;
            IsLocked = true;
            StarCount = 0;
            MoveLimit = 30;
            ColorCount = 6;
            XCount = 8;
            YCount = 8;
            Field = new StageInfoCell[XCount, YCount];
            for (int y = 0; y < YCount; ++y)
                for (int x = 0; x < XCount; ++x)
                    Field[x, y] = new StageInfoCell(true, 0);
        }
        else if (level == 9)
        {
            Num = level;
            GoalScore = 1000;
            IsLocked = true;
            StarCount = 0;
            MoveLimit = 30;
            ColorCount = 6;
            XCount = 8;
            YCount = 8;
            Field = new StageInfoCell[XCount, YCount];
            for (int y = 0; y < YCount; ++y)
                for (int x = 0; x < XCount; ++x)
                    Field[x, y] = new StageInfoCell(true, 0);
        }
        else if (level == 10)
        {
            Num = level;
            GoalScore = 1500;
            IsLocked = true;
            StarCount = 0;
            MoveLimit = 5;
            ColorCount = 4;
            XCount = 8;
            YCount = 8;
            Field = new StageInfoCell[XCount, YCount];
            for (int y = 0; y < YCount; ++y)
                for (int x = 0; x < XCount; ++x)
                    Field[x, y] = new StageInfoCell(true, 0);
        }
    }
}
