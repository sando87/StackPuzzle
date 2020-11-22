using System;

public class InGameBillboard
{
    public int MaxCombo;
    public int ItemOneMoreCount;
    public int ItemKeepComboCount;
    public int ItemSameColorCount;
    public int CoverCount;
    public int ChocoCount;
    public int CurrentScore;
    public int CurrentCombo;
    public int DestroyCount;
    public int MoveCount;
    public int KeepCombo;
    public byte[] ComboCounter = new byte[1024];
    public void Reset()
    {
        MaxCombo = 0;
        ItemOneMoreCount = 0;
        ItemKeepComboCount = 0;
        ItemSameColorCount = 0;
        CoverCount = 0;
        ChocoCount = 0;
        CurrentScore = 0;
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
        + CoverCount + ","
        + ChocoCount + ","
        + CurrentScore + ","
        + DestroyCount + ","
        + MoveCount + ","
        + KeepCombo;
        return ret;
    }
}