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
    public int ProductCapCount;
    public int FrameCoverCount;
    public int FrameBushCount;
    public StageInfoCell(int chocoCount, int CoverCount, int capCount, int bushCount)
    {
        ProductChocoCount = chocoCount;
        FrameCoverCount = CoverCount;
        ProductCapCount = capCount;
        FrameBushCount = bushCount;
    }
}
public class StageInfo
{
    public const int Version = 4;
    public int Num = 0;
    public string GoalType = "";
    public int GoalValue = 0;
    public Sprite GoalTypeImage = null;
    public StageGoalType GoalTypeEnum = StageGoalType.None;
    public int MoveLimit = 0;
    public int TimeLimit = 0;
    public float ColorCount = 0;
    public int XCount { get { return BoardInfo[0].Length; } }
    public int YCount { get { return BoardInfo.Count; } }
    public MatchingLevel Difficulty { get { return Num >= 0 ? MatchingLevel.None : ((MatchingLevel)(-Num)); } }
    public int RandomSeed = -1;
    public List<string> Rewards = new List<string>();
    public Dictionary<int, ProductSkill> Items = new Dictionary<int, ProductSkill>();
    //public StageInfoCell[,] Field = null;
    public List<StageInfoCell[]> BoardInfo = new List<StageInfoCell[]>();

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
                case "RandomSeed": info.RandomSeed = int.Parse(tokens[1]); break;
                //case "XCount": info.XCount = int.Parse(tokens[1]); break;
                //case "YCount": info.YCount = int.Parse(tokens[1]); RowIndex = info.YCount; break;
                case "Items": info.Items = Parse(tokens[1]); break;
                case "Reward": info.Rewards.Add(tokens[1]); break;
                case "Rows": info.ParseRow(tokens[1]); break;
            }
        }

        info.BoardInfo.Reverse();
        info.GoalTypeImage = TypeToImage(info.GoalType);
        info.GoalTypeEnum = StringToType(info.GoalType);
        if(info.GoalValue <= 0)
        {
            if (info.GoalTypeEnum == StageGoalType.Choco)
                info.GoalValue = info.GetChocoCount();
            else if(info.GoalTypeEnum == StageGoalType.Cover)
                info.GoalValue = info.GetCoverCount();
        }

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
    public Tuple<string, Sprite, int>[] GetRewardInfos()
    {
        List<Tuple<string, Sprite, int>> rets = new List<Tuple<string, Sprite, int>>();
        foreach(string reward in Rewards)
        {
            string[] package = reward.Split(' ');
            if (package.Length > 1)
            {
                rets.Add(new Tuple<string, Sprite, int>(reward, PurchaseItemTypeExtensions.GetChestSprite(), 1));
            }
            else
            {
                string[] sub = reward.Split('/');
                if (sub[0] == "life")
                    rets.Add(new Tuple<string, Sprite, int>(reward, PurchaseItemTypeExtensions.GetLifeSprite(), int.Parse(sub[1])));
                else if (sub[0] == "gold")
                   rets.Add(new Tuple<string, Sprite, int>(reward, PurchaseItemTypeExtensions.GetGoldSprite(), int.Parse(sub[1])));
                else if (sub[0] == "dia")
                    rets.Add(new Tuple<string, Sprite, int>(reward, PurchaseItemTypeExtensions.GetDiaSprite(), int.Parse(sub[1])));
                else
                {
                    PurchaseItemType rewardType = int.Parse(sub[0]).ToItemType();
                    rets.Add(new Tuple<string, Sprite, int>(reward, rewardType.GetSprite(), int.Parse(sub[1])));
                }
            }
        }
        return rets.ToArray();
    }
    public static void DoReward(string rewardPair)
    {
        string[] sub = rewardPair.Split('/');
        int count = int.Parse(sub[1]);
        if (sub[0] == "life")
            Purchases.ChargeHeart(count, 0);
        else if (sub[0] == "gold")
            Purchases.AddGold(count);
        else if (sub[0] == "dia")
            Purchases.PurchaseDiamond(count);
        else
        {
            PurchaseItemType rewardType = int.Parse(sub[0]).ToItemType();
            Purchases.ChargeItemUseGold(rewardType, count, 0);
        }
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
            + RandomSeed + ","
            + XCount + ","
            + YCount + ","
            + StageInfo.ItemToString(Items);
        return ret;
    }
    public void ParseRow(string row)
    {
        string[] columns = row.Trim().Split(' ');
        StageInfoCell[] cells = new StageInfoCell[columns.Length];

        for (int xIdx = 0; xIdx < columns.Length; ++xIdx)
        {
            string[] keyValue = columns[xIdx].Split('/');
            if(keyValue.Length == 2)
            {
                int productChocoCount = keyValue[0] == "*" ? -1 : int.Parse(keyValue[0]);
                int frameCoverCount = keyValue[1] == "x" ? -1 : int.Parse(keyValue[1]);
                cells[xIdx] = new StageInfoCell(productChocoCount, frameCoverCount, 0, 0);
            }
            else if(keyValue.Length == 4)
            {
                int productCapCount = int.Parse(keyValue[0]);
                int productChocoCount = keyValue[1] == "*" ? -1 : int.Parse(keyValue[0]);
                int frameBushCount = int.Parse(keyValue[2]);
                int frameCoverCount = keyValue[3] == "x" ? -1 : int.Parse(keyValue[1]);
                cells[xIdx] = new StageInfoCell(productChocoCount, frameCoverCount, productCapCount, frameBushCount);
            }
        }
        BoardInfo.Add(cells);
    }
    public string RowToString(int rowIndex)
    {
        string rowString = "";
        for (int xIdx = 0; xIdx < XCount; ++xIdx)
        {
            StageInfoCell cell = GetCell(xIdx, rowIndex);
            string productChoco = cell.FrameCoverCount < 0 ? "*" : cell.FrameCoverCount.ToString();
            string frameCover = cell.FrameCoverCount < 0 ? "x" : cell.FrameCoverCount.ToString();
            rowString += cell.ProductCapCount + "/" + productChoco + "/" + cell.FrameBushCount + "/" + frameCover + " ";
        }
        return rowString + "\r\n";
    }
    public StageInfoCell GetCell(int idxX, int idxY)
    {
        return BoardInfo[idxY][idxX];
    }
    public int GetChocoCount()
    {
        int count = 0;
        foreach(StageInfoCell[] row in BoardInfo)
        {
            foreach(StageInfoCell cell in row)
            {
                if (cell.ProductChocoCount > 0)
                    count++;
            }
        }
        return count;
    }
    public int GetCoverCount()
    {
        int count = 0;
        foreach (StageInfoCell[] row in BoardInfo)
        {
            foreach (StageInfoCell cell in row)
            {
                if (cell.FrameCoverCount > 0)
                    count++;
            }
        }
        return count;
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
        "RandomSeed,-1\r\n" +
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
