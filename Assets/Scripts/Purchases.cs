﻿using System;
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
    public int countGold;
    public int countDiamond;
    public bool infiniteHeart;
    public int[] countItem = new int[16];
    public PurchaseInfo()
    {
        maxHeart = 20;
        countHeart = 20;
        useTimeTick = DateTime.Now.Ticks;
        countGold = 200;
        countDiamond = 10;
        infiniteHeart = false;

        for (int i = 0; i < countItem.Length; ++i)
            countItem[i] = 0;
    }
    public byte[] Serialize()
    {
        List<byte> bytes = new List<byte>();
        bytes.AddRange(BitConverter.GetBytes(random));
        bytes.AddRange(BitConverter.GetBytes(maxHeart));
        bytes.AddRange(BitConverter.GetBytes(countHeart));
        bytes.AddRange(BitConverter.GetBytes(useTimeTick));
        bytes.AddRange(BitConverter.GetBytes(countGold));
        bytes.AddRange(BitConverter.GetBytes(countDiamond));
        bytes.AddRange(BitConverter.GetBytes(infiniteHeart));

        for (int i = 0; i < countItem.Length; ++i)
            bytes.AddRange(BitConverter.GetBytes(countItem[i]));

        return bytes.ToArray();
    }
    public void DeSerialize(byte[] data)
    {
        random = BitConverter.ToInt32(data, 0);
        maxHeart = BitConverter.ToInt32(data, 4);
        countHeart = BitConverter.ToInt32(data, 8);
        useTimeTick = BitConverter.ToInt64(data, 12);
        countGold =    BitConverter.ToInt32(data, 20);
        countDiamond = BitConverter.ToInt32(data, 24);
        infiniteHeart = BitConverter.ToBoolean(data, 28);

        for(int i = 0; i < countItem.Length; ++i)
            countItem[i] = BitConverter.ToInt32(data, 32 + i * 4);
    }
}

public class Purchases
{
    private const string prefsKeyName = "pcInfo3";
    private static PurchaseInfo mInfo = null;

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
        DateTime nextTime = new DateTime(mInfo.useTimeTick) + new TimeSpan(0, UserSetting.HeartChargingIntervalMin, 0);
        return (int)(nextTime - DateTime.Now).TotalSeconds;
    }
    public static void UpdateHeartTimer()
    {
        if (mInfo.countHeart >= mInfo.maxHeart || mInfo.useTimeTick <= 0)
            return;

        TimeSpan term = DateTime.Now - new DateTime(mInfo.useTimeTick);
        int fiveMinite = UserSetting.HeartChargingIntervalMin * 60;
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

    public static int CountGold()
    {
        return mInfo.countGold;
    }
    public static int CountDiamond()
    {
        return mInfo.countDiamond;
    }
    public static void AddGold(int gold)
    {
        mInfo.countGold += gold;
        UpdatePurchaseInfo(mInfo);
    }
    public static void PurchaseDiamond(int cnt)
    {
        mInfo.countDiamond += cnt;
        UpdatePurchaseInfo(mInfo);
    }
    public static bool ChargeItemUseGold(int type, int cnt, int gold)
    {
        if (mInfo.countGold < gold)
            return false;
        mInfo.countGold -= gold;
        mInfo.countItem[type] += cnt;
        UpdatePurchaseInfo(mInfo);
        return true;
    }
    public static bool ChargeItemUseDia(int type, int cnt, int diamond)
    {
        if (mInfo.countDiamond < diamond)
            return false;
        mInfo.countDiamond -= diamond;
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
