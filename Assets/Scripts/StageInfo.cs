using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Android;
public enum StageGoalType { None, Score, Combo, ItemOneMore, ItemKeepCombo, ItemSameColor, Cover, Choco }
public class StageInfoCell
{
    public int ProductChocoCount;
    public int FrameCoverCount;
    public StageInfoCell(int chocoCount, int CoverCount)
    {
        ProductChocoCount = chocoCount;
        FrameCoverCount = CoverCount;
    }
}
public class StageInfo
{
    public const int Version = 3;
    public int Num = 0;
    public string GoalType = "";
    public int GoalValue = 0;
    public Sprite GoalTypeImage = null;
    public StageGoalType GoalTypeEnum = StageGoalType.None;
    public int MoveLimit = 0;
    public int TimeLimit = 0;
    public float ColorCount = 0;
    public int XCount = 0;
    public int YCount = 0;
    public Dictionary<int, ProductSkill> Items = new Dictionary<int, ProductSkill>();
    public StageInfoCell[,] Field = null;

    public static StageInfo Load(int stageNum)
    {
#if PLATFORM_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead))
            Permission.RequestUserPermission(Permission.ExternalStorageRead);
#endif

        //string fullname = GetPath() + stageNum + ".txt";
        //if(!File.Exists(fullname)) //최초 실행시 한번만 수행됨(각 스테이지 정보를 기록한 파일들 Save)
        //{
        //    CreateStageInfoFolder();
        //
        //    StageInfo defInfo = new StageInfo();
        //    defInfo.DefaultSetting(stageNum);
        //    Save(defInfo);
        //}
        //
        //string fileText = File.ReadAllText(fullname);
        //if (fileText == null || fileText.Length == 0)
        //    return null;

        TextAsset ta = Resources.Load<TextAsset>("StageInfo/Version"+ Version + "/" + stageNum);
        if (ta == null || ta.text.Length == 0)
            return null;


        StageInfo info = new StageInfo();
        info.Num = stageNum;

        int RowIndex = 0;
        string[] lines = ta.text.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string line in lines)
        {
            string[] tokens = line.Split(',');
            if (line[0] == '#' || tokens.Length < 2)
                continue;

            switch(tokens[0])
            {
                case "GoalType": info.GoalType = tokens[1]; break;
                case "GoalValue": info.GoalValue = int.Parse(tokens[1]); break;
                case "MoveLimit": info.MoveLimit = int.Parse(tokens[1]); break;
                case "TimeLimit": info.TimeLimit = int.Parse(tokens[1]); break;
                case "ColorCount": info.ColorCount = float.Parse(tokens[1]); break;
                case "XCount": info.XCount = int.Parse(tokens[1]); break;
                case "YCount": info.YCount = int.Parse(tokens[1]); RowIndex = info.YCount; break;
                case "Items": info.Items = Parse(tokens[1]); break;
                case "Rows": info.ParseRow(--RowIndex, tokens[1]); break;
            }
        }

        info.GoalTypeImage = TypeToImage(info.GoalType);
        info.GoalTypeEnum = StringToType(info.GoalType);

        return info;
    }

    public int ComboTypeCount()
    {
        if (GoalTypeEnum != StageGoalType.Combo)
            return 0;

        return int.Parse(GoalType.Replace("Combo", ""));
    }

    private static Dictionary<int, ProductSkill> Parse(string token)
    {
        Dictionary<int, ProductSkill> infos = new Dictionary<int, ProductSkill>();
        string[] items = token.Split('/');
        foreach(string item in items)
        {
            string[] keyValue = item.Split(':');
            if (keyValue.Length != 2)
                continue;

            int matchCount = int.Parse(keyValue[0]);
            ProductSkill skill = (ProductSkill)Enum.Parse(typeof(ProductSkill), keyValue[1]);
            infos[matchCount] = skill;
        }

        return infos;
    }
    public static string ItemToString(Dictionary<int, ProductSkill> items)
    {
        string ret = "";
        foreach(var item in items)
        {
            ret += item.Key.ToString() + ":" + item.Value.ToString() + "/";
        }
        return ret;
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

    public static Sprite TypeToImage(string goalType)
    {
        Sprite image = null;
        switch(goalType)
        {
            case "Score": image = Resources.Load<Sprite>("Images/score"); break;
            case "Combo3": image = Resources.Load<Sprite>("Images/combo3"); break;
            case "Combo6": image = Resources.Load<Sprite>("Images/combo6"); break;
            case "Combo9": image = Resources.Load<Sprite>("Images/combo9"); break;
            case "Combo12": image = Resources.Load<Sprite>("Images/combo12"); break;
            case "Combo15": image = Resources.Load<Sprite>("Images/combo15"); break;
            case "Combo18": image = Resources.Load<Sprite>("Images/combo18"); break;
            case "ItemOneMore": image = Resources.Load<Sprite>("Images/itemOneMore"); break;
            case "ItemKeepCombo": image = Resources.Load<Sprite>("Images/itemKeepCombo"); break;
            case "ItemSameColor": image = Resources.Load<Sprite>("Images/itemSameColor"); break;
            case "Cover": image = Resources.Load<Sprite>("Images/cover"); break;
            case "Choco": image = Resources.Load<Sprite>("Images/choco"); break;
            default: break;
        }
        return image;
    }
    public static StageGoalType StringToType(string goalType)
    {
        StageGoalType type = StageGoalType.None;
        switch (goalType)
        {
            case "Score": type = StageGoalType.Score; break;
            case "Combo3": type = StageGoalType.Combo; break;
            case "Combo6": type = StageGoalType.Combo; break;
            case "Combo9": type = StageGoalType.Combo; break;
            case "Combo12": type = StageGoalType.Combo; break;
            case "Combo15": type = StageGoalType.Combo; break;
            case "Combo18": type = StageGoalType.Combo; break;
            case "ItemOneMore": type = StageGoalType.ItemOneMore; break;
            case "ItemKeepCombo": type = StageGoalType.ItemKeepCombo; break;
            case "ItemSameColor": type = StageGoalType.ItemSameColor; break;
            case "Cover": type = StageGoalType.Cover; break;
            case "Choco": type = StageGoalType.Choco; break;
            default: break;
        }
        return type;
    }
    public string ToCSVString()
    {
        string ret = ""
            + Num + ","
            + GoalType + ","
            + GoalValue + ","
            + MoveLimit + ","
            + TimeLimit + ","
            + ColorCount + ","
            + XCount + ","
            + YCount + ","
            + StageInfo.ItemToString(Items);
        return ret;
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
            Field[xIdx, rowIndex] = new StageInfoCell(int.Parse(keyValue[0]), int.Parse(keyValue[1]));
        }
    }
    public string RowToString(int rowIndex)
    {
        if (Field == null)
            return "";

        string rowString = "";
        for (int xIdx = 0; xIdx < XCount; ++xIdx)
        {
            rowString += Field[xIdx, rowIndex].ProductChocoCount.ToString() + "/" + Field[xIdx, rowIndex].FrameCoverCount.ToString() + " ";
        }
        return rowString + "\r\n";
    }
    public StageInfoCell GetCell(int idxX, int idxY)
    {
        if (Field == null || idxX < 0 || idxY < 0 || idxX >= XCount || idxY >= YCount)
            return new StageInfoCell(0, 0);

        return Field[idxX, idxY];
    }

    public static void SaveDeefault(int stageNum)
    {
        CreateStageInfoFolder();
        string fullname = GetPath() + stageNum + ".txt";
        string defaultData = 
        "GoalType,Score\r\n" +
        "GoalValue,3\r\n" +
        "MoveLimit,25\r\n" +
        "ColorCount,5.0\r\n" +
        "Items,4:OneMore/5:KeepCombo/6:SameColor/-1:SameColor\r\n" +
        "XCount,7\r\n" +
        "YCount,7\r\n" +
        "Rows,0/0 0/0 0/0 0/0 0/0 0/0 0/0\r\n" +
        "Rows,0/0 0/0 0/0 0/0 0/0 0/0 0/0\r\n" +
        "Rows,0/0 0/0 0/0 0/0 0/0 0/0 0/0\r\n" +
        "Rows,0/0 0/0 0/0 0/0 0/0 0/0 0/0\r\n" +
        "Rows,0/0 0/0 0/0 0/0 0/0 0/0 0/0\r\n" +
        "Rows,0/0 0/0 0/0 0/0 0/0 0/0 0/0\r\n" +
        "Rows,0/0 0/0 0/0 0/0 0/0 0/0 0/0\r\n";

        File.WriteAllText(fullname, defaultData);
    }

}
