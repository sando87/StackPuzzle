using System;

public class InGameBillboard
{
    public InGameManager Mgr;
    public int MaxCombo;
    public int ItemOneMoreCount;
    public int ItemKeepComboCount;
    public int ItemSameColorCount;
    public int RopeCount;
    public int IceCount;
    public int CapCount;
    public int BushCount;
    public int CurrentScore;
    public int CurrentCombo;
    public int DestroyCount;
    public int MoveCount;
    public int KeepCombo;
    public byte[] ComboCounter = new byte[1024];
    public void Reset(InGameManager mgr)
    {
        Mgr = mgr;
        MaxCombo = 0;
        ItemOneMoreCount = 0;
        ItemKeepComboCount = 0;
        ItemSameColorCount = 0;
        RopeCount = 0;
        IceCount = 0;
        CapCount = 0;
        BushCount = 0;
        CurrentScore = 0;
        CurrentCombo = 1;
        DestroyCount = 0;
        MoveCount = 0;
        KeepCombo = 0;
        Array.Clear(ComboCounter, 0, 1024);
    }
    public string ToCSVString()
    {
        string ret = ""
        + MaxCombo + ","
        + ItemOneMoreCount + ","
        + ItemKeepComboCount + ","
        + ItemSameColorCount + ","
        + RopeCount + ","
        + IceCount + ","
        + CapCount + ","
        + BushCount + ","
        + CurrentScore + ","
        + DestroyCount + ","
        + MoveCount + ","
        + KeepCombo;
        return ret;
    }
    public int GetGrade(StageInfo info)
    {
        float totlaRemain = info.MoveLimit;
        float currentRemin = info.MoveLimit - MoveCount;
        if (totlaRemain < 5)
            return 3;
        else if (totlaRemain * 0.4f < currentRemin)
            return 3;
        else if (totlaRemain * 0.2f < currentRemin)
            return 2;
        else if (0 < currentRemin)
            return 1;

        return 0;
    }
    public int GetGoalValue(StageGoalType type)
    {
        switch (type)
        {
            case StageGoalType.Score: return CurrentScore;
            case StageGoalType.Combo3: return CurrentCombo;
            case StageGoalType.Combo6: return CurrentCombo;
            case StageGoalType.Combo9: return CurrentCombo;
            case StageGoalType.Combo12: return CurrentCombo;
            case StageGoalType.Combo15: return CurrentCombo;
            case StageGoalType.Combo18: return CurrentCombo;
            case StageGoalType.ItemOneMore: return ItemOneMoreCount;
            case StageGoalType.ItemKeepCombo: return ItemKeepComboCount;
            case StageGoalType.ItemSameColor: return ItemSameColorCount;
            case StageGoalType.Rope: return RopeCount;
            case StageGoalType.Ice: return IceCount;
            case StageGoalType.Cap: return CapCount;
            case StageGoalType.Bush: return BushCount;
            default: return 0;
        }
    }
}