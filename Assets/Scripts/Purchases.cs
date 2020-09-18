using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PurchaseInfo
{
    public int random;
    public int maxHeart;
    public int countHeart;
    public long useTimeTick;
    public int countDiamond;
    public int countItemA;
    public int countItemB;
    public int countItemC;
    public int countItemD;
    public bool infiniteHeart;
    public byte[] Serialize()
    {
        List<byte> bytes = new List<byte>();
        bytes.AddRange(BitConverter.GetBytes(random));
        bytes.AddRange(BitConverter.GetBytes(maxHeart));
        bytes.AddRange(BitConverter.GetBytes(countHeart));
        bytes.AddRange(BitConverter.GetBytes(useTimeTick));
        bytes.AddRange(BitConverter.GetBytes(countDiamond));
        bytes.AddRange(BitConverter.GetBytes(countItemA));
        bytes.AddRange(BitConverter.GetBytes(countItemB));
        bytes.AddRange(BitConverter.GetBytes(countItemC));
        bytes.AddRange(BitConverter.GetBytes(countItemD));
        bytes.AddRange(BitConverter.GetBytes(infiniteHeart));
        return bytes.ToArray();
    }
    public void DeSerialize(byte[] data)
    {
        random = BitConverter.ToInt32(data, 0);
        maxHeart = BitConverter.ToInt32(data, 4);
        countHeart = BitConverter.ToInt32(data, 8);
        useTimeTick = BitConverter.ToInt64(data, 12);
        countDiamond = BitConverter.ToInt32(data, 20);
        countItemA = BitConverter.ToInt32(data, 24);
        countItemB = BitConverter.ToInt32(data, 28);
        countItemC = BitConverter.ToInt32(data, 32);
        countItemD = BitConverter.ToInt32(data, 36);
        infiniteHeart = BitConverter.ToBoolean(data, 40);
    }
}

public class Purchases
{
    private const string prefsKeyName = "pcInfo2";
    private static PurchaseInfo mInfo = null;

    public static void Initialize()
    {
        mInfo = LoadPurchaseInfo();
    }
    public static int CountHeart()
    {
        if(mInfo.countHeart < mInfo.maxHeart)
        {
            TimeSpan term = DateTime.Now - new DateTime(mInfo.useTimeTick);
            int fiveMinite = 300;
            int gainHeart = term.Seconds / fiveMinite;
            int remainSec = term.Seconds % fiveMinite;
            mInfo.countHeart += gainHeart;
            mInfo.countHeart = Math.Min(mInfo.maxHeart, mInfo.countHeart);

            if (mInfo.countHeart < mInfo.maxHeart)
            {
                mInfo.useTimeTick = (DateTime.Now - new TimeSpan(0, remainSec / 60, remainSec % 60)).Ticks;
            }
            else
            {
                mInfo.useTimeTick = DateTime.Now.Ticks;
            }
            UpdatePurchaseInfo(mInfo);
        }
        return mInfo.countHeart;
    }
    public static bool ChargeHeart(int cnt, int cost)
    {
        if (mInfo.countDiamond < cost)
            return false;
        mInfo.countDiamond -= cost;
        mInfo.countHeart += cnt;
        mInfo.useTimeTick = DateTime.Now.Ticks;
        UpdatePurchaseInfo(mInfo);
        return true;
    }
    public static bool UseHeart()
    {
        if (mInfo.countHeart <= 0)
            return false;
        if (mInfo.countHeart == mInfo.maxHeart)
            mInfo.useTimeTick = DateTime.Now.Ticks;
        mInfo.countHeart--;
        UpdatePurchaseInfo(mInfo);
        return true;
    }
    public static int RemainSeconds()
    {
        if (mInfo.countHeart >= mInfo.maxHeart)
            return 0;
        return (DateTime.Now - new DateTime(mInfo.useTimeTick)).Seconds;
    }
    public static void PurchaseDiamond(int cnt)
    {
        mInfo.countDiamond += cnt;
        UpdatePurchaseInfo(mInfo);
    }
    public static bool ChargeItemA(int cnt, int cost)
    {
        if (mInfo.countDiamond < cost)
            return false;
        mInfo.countDiamond -= cost;
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
        if (PlayerPrefs.HasKey(prefsKeyName))
        {
            string hexStr = PlayerPrefs.GetString(prefsKeyName);
            byte[] bytes = Utils.HexStringToByteArray(hexStr);
            byte[] originData = Utils.Decrypt(bytes);
            PurchaseInfo info = new PurchaseInfo();
            info.DeSerialize(originData);
            return info;
        }
        else
        {
            PurchaseInfo info = new PurchaseInfo();
            info.maxHeart = 20;
            info.countHeart = 20;
            info.useTimeTick = DateTime.Now.Ticks;
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
        info.random = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        byte[] bInfo = info.Serialize();
        byte[] encryptInfo = Utils.Encrypt(bInfo);
        string hexStr = BitConverter.ToString(encryptInfo).Replace("-", string.Empty);
        PlayerPrefs.SetString(prefsKeyName, hexStr);
    }
}
