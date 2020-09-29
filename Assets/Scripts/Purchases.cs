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
    public int[] countItem = new int[4];
    public bool infiniteHeart;
    public byte[] Serialize()
    {
        List<byte> bytes = new List<byte>();
        bytes.AddRange(BitConverter.GetBytes(random));
        bytes.AddRange(BitConverter.GetBytes(maxHeart));
        bytes.AddRange(BitConverter.GetBytes(countHeart));
        bytes.AddRange(BitConverter.GetBytes(useTimeTick));
        bytes.AddRange(BitConverter.GetBytes(countDiamond));
        bytes.AddRange(BitConverter.GetBytes(countItem[0]));
        bytes.AddRange(BitConverter.GetBytes(countItem[1]));
        bytes.AddRange(BitConverter.GetBytes(countItem[2]));
        bytes.AddRange(BitConverter.GetBytes(countItem[3]));
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
        countItem[0] = BitConverter.ToInt32(data, 24);
        countItem[1] = BitConverter.ToInt32(data, 28);
        countItem[2] = BitConverter.ToInt32(data, 32);
        countItem[3] = BitConverter.ToInt32(data, 36);
        infiniteHeart = BitConverter.ToBoolean(data, 40);
    }
}

public class Purchases
{
    private const string prefsKeyName = "pcInfo2";
    private static PurchaseInfo mInfo = null;
    private const int chargingIntervalMin = 5;

    public static void Initialize()
    {
        mInfo = LoadPurchaseInfo();
    }
    public static bool MaxHeart()
    {
        return mInfo.countHeart >= mInfo.maxHeart;
    }
    public static int CountHeart()
    {
        UpdateHeartTimer();
        return mInfo.countHeart;
    }
    public static bool ChargeHeartLimit(int cnt)
    {
        mInfo.countHeart += cnt;
        if (mInfo.countHeart > mInfo.maxHeart)
            mInfo.countHeart = mInfo.maxHeart;
        mInfo.useTimeTick = DateTime.Now.Ticks;
        UpdatePurchaseInfo(mInfo);
        return true;
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
    public static bool ChargeHeartInfinite()
    {
        mInfo.infiniteHeart = true;
        mInfo.countHeart = mInfo.maxHeart;
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
        if (mInfo.countHeart >= mInfo.maxHeart || mInfo.useTimeTick <= 0)
            return 0;
        DateTime nextTime = new DateTime(mInfo.useTimeTick) + new TimeSpan(0, chargingIntervalMin, 0);
        return (int)(nextTime - DateTime.Now).TotalSeconds;
    }
    public static void UpdateHeartTimer()
    {
        if (mInfo.countHeart >= mInfo.maxHeart || mInfo.useTimeTick <= 0)
            return;

        TimeSpan term = DateTime.Now - new DateTime(mInfo.useTimeTick);
        int fiveMinite = chargingIntervalMin * 60;
        int gainHeart = (int)term.TotalSeconds / fiveMinite;
        int remainSec = (int)term.TotalSeconds % fiveMinite;
        if (gainHeart > 0)
        {
            mInfo.countHeart += gainHeart;
            if (mInfo.countHeart < mInfo.maxHeart)
            {
                mInfo.useTimeTick = (DateTime.Now - new TimeSpan(0, remainSec / 60, remainSec % 60)).Ticks;
            }
            else
            {
                mInfo.countHeart = mInfo.maxHeart;
                mInfo.useTimeTick = 0;
            }
            UpdatePurchaseInfo(mInfo);
        }
    }
    public static int CountDiamond()
    {
        return mInfo.countDiamond;
    }
    public static void PurchaseDiamond(int cnt)
    {
        mInfo.countDiamond += cnt;
        UpdatePurchaseInfo(mInfo);
    }
    public static bool ChargeItem(int type, int cnt, int cost)
    {
        if (mInfo.countDiamond < cost)
            return false;
        mInfo.countDiamond -= cost;
        mInfo.countItem[type] += cnt;
        UpdatePurchaseInfo(mInfo);
        return true;
    }
    public static int CountItem(int type)
    {
        return mInfo.countItem[type];
    }
    public static bool UseItem(int type)
    {
        if (mInfo.countItem[type] <= 0)
            return false;
        mInfo.countItem[type]--;
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
            info.countItem[0] = 0;
            info.countItem[1] = 0;
            info.countItem[2] = 0;
            info.countItem[3] = 0;
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
