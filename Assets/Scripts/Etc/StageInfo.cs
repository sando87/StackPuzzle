using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public enum StageGoalType { None, Score, Combo3, Combo6, Combo9, Combo12, Combo15, Combo18, ItemOneMore, ItemKeepCombo, ItemSameColor, Rope, Ice, Cap, Bush }

public class StageInfoCell
{
    public int IceCount;
    public int CapCount;
    public int RopeCount;
    public int BushCount;
    public int ProductType; // 0: Normal, -1: Empty, 1~6: ProductSkill

    public StageInfoCell(int productType, int iceCount, int ropeCount, int capCount, int bushCount)
    {
        IceCount = iceCount;
        RopeCount = ropeCount;
        CapCount = capCount;
        BushCount = bushCount;
        ProductType = productType;
    }
    public StageInfoCell()
    {
        IceCount = 0;
        RopeCount = 0;
        CapCount = 0;
        BushCount = 0;
        ProductType = 0;
    }

    public bool IsDisabled { get => ProductType == -1; }
}
public class StageInfo
{
    static public readonly string NewLine = Environment.NewLine;

    public const int Version = 4;
    public int Num = 0;
    public string GoalType = "";
    public int GoalValue = 0;
    public Sprite GoalTypeImage = null;
    public StageGoalType GoalTypeEnum = StageGoalType.None;
    public int MoveLimit = 0;
    public int TimeLimit = 0;
    public float ColorCount = 0;
    public int MatchingChance = 0; // -100 ~ 100 : 0이면 기본 랜덤 그대로, 100으로 갈수록 매칭 확률 증가, -100으로 갈수록 매칭 확률 감소
    public int StarPoint = 50;
    public int XCount { get { return BoardInfo[0].Length; } }
    public int YCount { get { return BoardInfo.Count; } }
    public MatchingLevel Difficulty { get; private set; }
    public int RandomSeed = -1;
    public List<string> Rewards = new List<string>();
    public Dictionary<int, ProductSkill> Items = new Dictionary<int, ProductSkill>();
    //public StageInfoCell[,] Field = null;
    public List<StageInfoCell[]> BoardInfo = new List<StageInfoCell[]>();

    public static StageInfo Load(int stageNum)
    {
#if UNITY_STANDALONE_WIN
        string path = "./" + stageNum + ".txt";
        if(File.Exists(path))
        {
            string[] tmpLines = File.ReadAllLines(path);
            if(tmpLines != null && tmpLines.Length > 0)
            {
                StageInfo tmpInfo = Load(tmpLines);
                tmpInfo.Num = stageNum;
                tmpInfo.Difficulty = MatchingLevel.None;
                return tmpInfo;
            }
        }
#endif

        TextAsset ta = Resources.Load<TextAsset>("StageInfo/Version" + Version + "/" + stageNum);
        if (ta == null || ta.text.Length == 0)
            return null;

        string[] lines = ta.text.Split(new string[] { NewLine }, StringSplitOptions.RemoveEmptyEntries);
        StageInfo info = Load(lines);
        info.Num = stageNum;
        info.Difficulty = MatchingLevel.None;
        return info;
    }

    public static int GetMaxStageNum()
    {
        string path = "./Assets/Resources/StageInfo/Version" + Version + "/";
        DirectoryInfo info = new DirectoryInfo(path);
        int maxNum = 0;
        foreach (System.IO.FileInfo file in info.GetFiles()) 
        {
            string[] pieces = file.Name.Split('.');
            if(pieces.Length == 2 && int.TryParse(pieces[0], out int num))
            {
                maxNum = Math.Max(maxNum, num);
            }
        }
        return maxNum;
    }

    public static StageInfo Load(MatchingLevel level, int mapRandomSeed)
    {
        string filename = level.ToString();
        TextAsset ta = Resources.Load<TextAsset>("StageInfo/Version" + Version + "/" + filename);
        if (ta == null || ta.text.Length == 0)
            return null;

        string[] lines = ta.text.Split(new string[] { NewLine }, StringSplitOptions.RemoveEmptyEntries);
        StageInfo info = Load(lines);
        info.Num = 0;
        info.Difficulty = level;
        info.RandomSeed = mapRandomSeed;
        return info;
    }

    public static StageInfo Load(string[] lines)
    {
        StageInfo info = new StageInfo();
        foreach (string line in lines)
        {
            string[] tokens = line.Split(',');
            if (line[0] == '#' || tokens.Length < 2)
                continue;

            if (tokens[0].Equals("GoalType")) info.GoalType = tokens[1];
            else if (tokens[0].Equals("GoalValue")) info.GoalValue = int.Parse(tokens[1]);
            else if (tokens[0].Equals("MoveLimit")) info.MoveLimit = int.Parse(tokens[1]);
            else if (tokens[0].Equals("TimeLimit")) info.TimeLimit = int.Parse(tokens[1]);
            else if (tokens[0].Equals("ColorCount")) info.ColorCount = float.Parse(tokens[1]);
            else if (tokens[0].Equals("MatchingChance")) info.MatchingChance = int.Parse(tokens[1]);
            else if (tokens[0].Equals("RandomSeed")) info.RandomSeed = int.Parse(tokens[1]);
            else if (tokens[0].Equals("StarPoint")) info.StarPoint = int.Parse(tokens[1]);
            else if (tokens[0].Equals("Items")) info.Items = Parse(tokens[1]);
            else if (tokens[0].Equals("Reward")) info.Rewards.Add(tokens[1]);
            else if (tokens[0].Equals("Rows")) info.ParseRow(tokens[1]);
        }

        info.UpdateGoalInfo();

        return info;
    }

    public void UpdateGoalInfo()
    {
        GoalTypeImage = TypeToImage(GoalType);
        GoalTypeEnum = StringToType(GoalType);
        if (GoalValue <= 0)
        {
            if (GoalTypeEnum == StageGoalType.Ice)
                GoalValue = GetIceCount();
            else if (GoalTypeEnum == StageGoalType.Rope)
                GoalValue = GetRopeCount();
            else if (GoalTypeEnum == StageGoalType.Cap)
                GoalValue = GetCapCount();
            else if (GoalTypeEnum == StageGoalType.Bush)
                GoalValue = GetBushCount();
        }
    }

    public int ComboTypeCount()
    {
        if (GoalTypeEnum == StageGoalType.Combo3)
            return 3;
        else if (GoalTypeEnum == StageGoalType.Combo6)
            return 6;
        else if (GoalTypeEnum == StageGoalType.Combo9)
            return 9;
        else if (GoalTypeEnum == StageGoalType.Combo12)
            return 12;
        else if (GoalTypeEnum == StageGoalType.Combo15)
            return 15;
        else if (GoalTypeEnum == StageGoalType.Combo18)
            return 18;
        else
            return 0;
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
        return ret.Length > 0 ? "Items," + ret + NewLine : "";
    }


    public static Sprite TypeToImage(string goalType)
    {
        StageGoalType typeEnum = StringToType(goalType);
        Sprite image = null;
        switch(typeEnum)
        {
            case StageGoalType.Score: image = Resources.Load<Sprite>("Images/score"); break;
            case StageGoalType.Combo3: image = Resources.Load<Sprite>("Images/combo3"); break;
            case StageGoalType.Combo6: image = Resources.Load<Sprite>("Images/combo6"); break;
            case StageGoalType.Combo9: image = Resources.Load<Sprite>("Images/combo9"); break;
            case StageGoalType.Combo12: image = Resources.Load<Sprite>("Images/combo12"); break;
            case StageGoalType.Combo15: image = Resources.Load<Sprite>("Images/combo15"); break;
            case StageGoalType.Combo18: image = Resources.Load<Sprite>("Images/combo18"); break;
            case StageGoalType.ItemOneMore: image = Resources.Load<Sprite>("Images/itemOneMore"); break;
            case StageGoalType.ItemKeepCombo: image = Resources.Load<Sprite>("Images/itemKeepCombo"); break;
            case StageGoalType.ItemSameColor: image = Resources.Load<Sprite>("Images/itemSameColor"); break;
            case StageGoalType.Rope: image = Resources.Load<Sprite>("Images/rope"); break;
            case StageGoalType.Ice: image = Resources.Load<Sprite>("Images/ice"); break;
            case StageGoalType.Cap: image = Resources.Load<Sprite>("Images/cap"); break;
            case StageGoalType.Bush: image = Resources.Load<Sprite>("Images/bush"); break;
            default: break;
        }
        return image;
    }
    public static StageGoalType StringToType(string goalType)
    {
        return Enum.TryParse(goalType, out StageGoalType type) ? type : StageGoalType.None;
    }
    public static Tuple<string, Sprite, int> StringToRewardInfo(string rewardText)
    {
        string[] package = rewardText.Split(' ');
        if (package.Length > 1)
        {
            return new Tuple<string, Sprite, int>(rewardText, PurchaseItemTypeExtensions.GetChestSprite(), 1);
        }
        else
        {
            string[] sub = rewardText.Split('/');
            if (sub[0] == "life")
                return new Tuple<string, Sprite, int>(rewardText, PurchaseItemTypeExtensions.GetLifeSprite(), int.Parse(sub[1]));
            else if (sub[0] == "gold")
                return new Tuple<string, Sprite, int>(rewardText, PurchaseItemTypeExtensions.GetGoldSprite(), int.Parse(sub[1]));
            else if (sub[0] == "dia")
                return new Tuple<string, Sprite, int>(rewardText, PurchaseItemTypeExtensions.GetDiaSprite(), int.Parse(sub[1]));
            else
            {
                PurchaseItemType rewardType = int.Parse(sub[0]).ToItemType();
                return new Tuple<string, Sprite, int>(rewardText, rewardType.GetSprite(), int.Parse(sub[1]));
            }
        }
    }
    public Tuple<string, Sprite, int>[] GetRewardInfos()
    {
        List<Tuple<string, Sprite, int>> rets = new List<Tuple<string, Sprite, int>>();
        foreach(string reward in Rewards)
        {
            rets.Add(StringToRewardInfo(reward));
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
            + MatchingChance + ","
            + RandomSeed + ","
            + StarPoint + ","
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
            if(keyValue.Length == 5)
            {
                int productType = keyValue[0] == "x" ? -1 : keyValue[0] == "o" ? 0 : int.Parse(keyValue[0]);
                int productCapCount = int.Parse(keyValue[1]);
                int productIceCount = int.Parse(keyValue[2]);
                int frameBushCount = int.Parse(keyValue[3]);
                int frameRopeCount = int.Parse(keyValue[4]);
                cells[xIdx] = new StageInfoCell(productType, productIceCount, frameRopeCount, productCapCount, frameBushCount);
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
            rowString += cell.ProductType + "/" + cell.CapCount + "/" + cell.IceCount + "/" + cell.BushCount + "/" + cell.RopeCount + " ";
        }
        return "Rows," + rowString + NewLine;
    }
    public StageInfoCell GetCell(int idxX, int idxY)
    {
        return BoardInfo[idxY][idxX];
    }
    public int GetIceCount()
    {
        int count = 0;
        foreach(StageInfoCell[] row in BoardInfo)
        {
            foreach(StageInfoCell cell in row)
            {
                if (cell.IceCount > 0 && !cell.IsDisabled)
                    count++;
            }
        }
        return count;
    }
    public int GetRopeCount()
    {
        int count = 0;
        foreach (StageInfoCell[] row in BoardInfo)
        {
            foreach (StageInfoCell cell in row)
            {
                if (cell.RopeCount > 0 && !cell.IsDisabled)
                    count++;
            }
        }
        return count;
    }
    public int GetCapCount()
    {
        int count = 0;
        foreach (StageInfoCell[] row in BoardInfo)
        {
            foreach (StageInfoCell cell in row)
            {
                if (cell.CapCount > 0 && !cell.IsDisabled)
                    count++;
            }
        }
        return count;
    }
    public int GetBushCount()
    {
        int count = 0;
        foreach (StageInfoCell[] row in BoardInfo)
        {
            foreach (StageInfoCell cell in row)
            {
                if (cell.BushCount > 0 && !cell.IsDisabled)
                    count++;
            }
        }
        return count;
    }

    private string RewardsToString()
    {
        string ret = "";
        foreach(string reward in Rewards)
        {
            ret += "Reward," + reward + NewLine;
        }
        return ret;

    }
    private string RowsToString()
    {
        string ret = "";
        int cnt = BoardInfo.Count;
        for (int i = 0; i < cnt; ++i)
        {
            ret += RowToString(i);
        }
        return ret;
    }

#if UNITY_EDITOR
    public void SaveToFile()
    {
        string fullname = "Assets/Resources/StageInfo/Version" + Version + "/" + Num + ".txt";
        string data =
        // comments
        "# 0/0/0/0/0 => productType/cap(b)/ice(b)/bush(f)/rope(f)" + NewLine +
        "# GoalType : Score,ComboN, ItemOneMore, ItemKeepCombo, ItemSameColor, Cap, Ice, Bush, Rope" + NewLine +
        "# Reward,gold/100" + NewLine +
        "# Reward,dia/5" + NewLine +
        "# Reward,life/1" + NewLine +
        "# Reward,1/1 2/1 3/1" + NewLine +
        "# Reward,PurchaseItemType/Count" + NewLine +
        "# PurchaseItemType : None, ExtendLimit, RemoveIce, MakeSkill1, KeepCombo, MakeSkill2, Meteor" + NewLine +

        // data
        "GoalType," + GoalType + NewLine +
        "GoalValue," + GoalValue + NewLine +
        "MoveLimit," + MoveLimit + NewLine +
        "TimeLimit," + TimeLimit + NewLine +
        "ColorCount," + ColorCount + NewLine +
        "MatchingChance," + MatchingChance + NewLine +
        "StarPoint," + StarPoint + NewLine +
        "RandomSeed," + RandomSeed + NewLine +
        ItemToString(Items) +
        RewardsToString() +
        RowsToString();

        File.WriteAllText(fullname, data);
        AssetDatabase.Refresh();
    }
#endif
}
