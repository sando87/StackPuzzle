using System;

public class InGameBillboard
{
    public int MaxCombo;
    public int ItemOneMoreCount;
    public int ItemKeepComboCount;
    public int ItemSameColorCount;
    public int ItemReduceColorCount;
    public int CoverCount;
    public int ChocoCount;
    public int CurrentScore;
    public int RemainLimit;
    public int KeepCombo;
    public void Reset()
    {
        MaxCombo = 0;
        ItemOneMoreCount = 0;
        ItemKeepComboCount = 0;
        ItemSameColorCount = 0;
        ItemReduceColorCount = 0;
        CoverCount = 0;
        ChocoCount = 0;
        CurrentScore = 0;
        RemainLimit = 0;
        KeepCombo = 0;
    }
    public float GetAchievementRate(StageInfo target)
    {
        if (target == null)
            return 0;

        string type = target.GoalType;
        int value = target.GoalValue;
        switch (type)
        {
            case "Score": return CurrentScore / (float)value;
            case "Combo": return MaxCombo / (float)value;
            case "ItemOneMore": return ItemOneMoreCount / (float)value;
            case "ItemKeepCombo": return ItemKeepComboCount / (float)value;
            case "ItemSameColor": return ItemSameColorCount / (float)value;
            case "Cover": return CoverCount / (float)value;
            case "Choco": return ChocoCount / (float)value;
            default: break;
        }

        return 0;
    }
    public int GetStarCount(StageInfo target)
    {
        if (target == null)
            return 0;

        int point = 0;
        string type = target.GoalType;
        int value = target.GoalValue;
        switch (type)
        {
            case "Score":
                if (CurrentScore >= value * 3)
                    point += 100;
                else if (CurrentScore >= value * 2)
                    point += 70;
                else if (CurrentScore >= value * 1)
                    point += 40;
                break;
            case "Combo":
                if (MaxCombo >= value * 2)
                    point += 100;
                else if (MaxCombo >= value * 1.5f)
                    point += 70;
                else if (MaxCombo >= value)
                    point += 40;
                break;
            case "ItemOneMore":
                if (ItemOneMoreCount >= value)
                    point += 100;
                break;
            case "ItemKeepCombo":
                if (ItemKeepComboCount >= value)
                    point += 100;
                break;
            case "ItemSameColor":
                if (ItemSameColorCount >= value)
                    point += 100;
                break;
            case "Cover":
                if (CoverCount >= value)
                    point += 100;
                break;
            case "Choco":
                if (ChocoCount >= value)
                    point += 100;
                break;
            default: break;
        }

        if (RemainLimit <= target.MoveLimit * 0.25f)
            point += 50;
        else if (RemainLimit <= target.MoveLimit * 0.5f)
            point += 75;
        else
            point += 100;

        float avgPoint = (float)point / 2.0f;
        int starCount = Math.Min(3, (int)avgPoint / 30);
        return starCount;
    }
    public InGameState CheckState(StageInfo target)
    {
        if (target == null)
            return InGameState.Noting;

        if (MenuPause.IsPopped())
            return InGameState.Paused;

        bool isAchieved = false;
        string type = target.GoalType;
        int value = target.GoalValue;
        switch (type)
        {
            case "Score":
                if (CurrentScore >= value)
                    isAchieved = true;
                break;
            case "Combo":
                if (MaxCombo >= value)
                    isAchieved = true;
                break;
            case "ItemOneMore":
                if (ItemOneMoreCount >= value)
                    isAchieved = true;
                break;
            case "ItemKeepCombo":
                if (ItemKeepComboCount >= value)
                    isAchieved = true;
                break;
            case "ItemSameColor":
                if (ItemSameColorCount >= value)
                    isAchieved = true;
                break;
            case "Cover":
                if (CoverCount >= value)
                    isAchieved = true;
                break;
            case "Choco":
                if (ChocoCount >= value)
                    isAchieved = true;
                break;
            default:
                break;
        }

        if (isAchieved)
            return InGameState.Win;

        if (RemainLimit <= 0)
            return InGameState.Lose;

        return InGameState.Running;
    }
}