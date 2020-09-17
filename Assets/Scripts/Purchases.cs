using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class PurchaseInfo
{
    public int maxHeart;
    public int countHeart;
    public DateTime useTime;
    public bool infiniteHeart;
    public int countDiamond;
    public int countItemA;
    public int countItemB;
    public int countItemC;
    public int countItemD;
}

public class Purchases
{
    private static PurchaseInfo mInfo = null;

    public static void Initialize()
    {
        mInfo = LoadPurchaseInfo();
    }
    public static int CountHeart()
    {
        if(mInfo.countHeart < mInfo.maxHeart)
        {
            TimeSpan term = DateTime.Now - mInfo.useTime;
            int fiveMinite = 300;
            int gainHeart = term.Seconds / fiveMinite;
            int remainSec = term.Seconds % fiveMinite;
            mInfo.countHeart += gainHeart;
            mInfo.countHeart = Math.Min(mInfo.maxHeart, mInfo.countHeart);

            if (mInfo.countHeart < mInfo.maxHeart)
            {
                mInfo.useTime = DateTime.Now - new TimeSpan(0, remainSec / 60, remainSec % 60);
            }
            else
            {
                mInfo.useTime = DateTime.Now;
            }
            UpdatePurchaseInfo(mInfo);
        }
        return mInfo.countHeart;
    }
    public static void ChargeHeart(int cnt)
    {
        mInfo.countHeart += cnt;
        mInfo.useTime = DateTime.Now;
        UpdatePurchaseInfo(mInfo);
    }
    public static bool UseHeart()
    {
        if (mInfo.countHeart <= 0)
            return false;
        if (mInfo.countHeart == mInfo.maxHeart)
            mInfo.useTime = DateTime.Now;
        mInfo.countHeart--;
        UpdatePurchaseInfo(mInfo);
        return true;
    }
    public static int RemainSeconds()
    {
        if (mInfo.countHeart >= mInfo.maxHeart)
            return 0;
        return (DateTime.Now - mInfo.useTime).Seconds;
    }
    public static void ChargeDiamond(int cnt)
    {
        mInfo.countDiamond += cnt;
        UpdatePurchaseInfo(mInfo);
    }
    public static bool PurchaseItemA(int cnt)
    {
        if (mInfo.countDiamond < cnt)
            return false;
        mInfo.countDiamond -= cnt;
        mInfo.countItemA += cnt;
        UpdatePurchaseInfo(mInfo);
        return true;
    }
    public static bool UseItemA()
    {
        if (mInfo.countItemA <= 0)
            return false;
        mInfo.countItemA--;
        UpdatePurchaseInfo(mInfo);
        return true;
    }

    private static PurchaseInfo LoadPurchaseInfo()
    {
        if (PlayerPrefs.HasKey("pcInfo"))
        {
            string hexStr = PlayerPrefs.GetString("pcInfo");
            byte[] bytes = Utils.HexStringToByteArray(hexStr);
            PurchaseInfo info = Utils.Deserialize<PurchaseInfo>(bytes);
            return info;
        }
        else
        {
            PurchaseInfo info = new PurchaseInfo();
            info.maxHeart = 20;
            info.countHeart = 20;
            info.useTime = DateTime.Now;
            info.infiniteHeart = false;
            info.countDiamond = 100;
            info.countItemA = 0;
            info.countItemB = 0;
            info.countItemC = 0;
            info.countItemD = 0;
            UpdatePurchaseInfo(info);
            return info;
        }

    }
    private static void UpdatePurchaseInfo(PurchaseInfo info)
    {
        byte[] bInfo = Utils.Serialize(info);
        string hexStr = BitConverter.ToString(bInfo).Replace("-", string.Empty);
        PlayerPrefs.SetString("pcInfo", hexStr);
    }
}
