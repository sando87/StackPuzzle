using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class PurchaseItemTypeExtensions
{
    private static Sprite LifeImage = null;
    private static Sprite GoldImage = null;
    private static Sprite DiaImage = null;
    private static Sprite ChestImage = null;
    private static Sprite[] ItemImages = null;

    static PurchaseItemTypeExtensions()
    {
        int count = System.Enum.GetValues(typeof(PurchaseItemType)).Length;
        ItemImages = new Sprite[count];
        for (int i = 0; i < count; ++i)
        {
            switch (i.ToItemType())
            {
                case PurchaseItemType.ExtendLimit: ItemImages[i] = Resources.Load<Sprite>("Images/Items/item_time"); break;
                case PurchaseItemType.RemoveIce: ItemImages[i] = Resources.Load<Sprite>("Images/Items/item_missile"); break;
                case PurchaseItemType.MakeSkill1: ItemImages[i] = Resources.Load<Sprite>("Images/Items/item_bombs"); break;
                case PurchaseItemType.MakeCombo: ItemImages[i] = Resources.Load<Sprite>("Images/Items/item_up"); break;
                case PurchaseItemType.MakeSkill2: ItemImages[i] = Resources.Load<Sprite>("Images/Items/item_same"); break;
                case PurchaseItemType.PowerUp: ItemImages[i] = Resources.Load<Sprite>("Images/Items/item_arrow"); break;
                default: ItemImages[i] = Resources.Load<Sprite>("Images/Items/equipment_equip_icon"); break; 
            }
        }

        LifeImage = Resources.Load<Sprite>("Images/life");
        GoldImage = Resources.Load<Sprite>("Images/gold");
        DiaImage = Resources.Load<Sprite>("Images/diamond");
        ChestImage = Resources.Load<Sprite>("Images/chest");
    }

    public static Sprite GetSprite(this PurchaseItemType type)
    {
        return ItemImages[type.ToInt()];
    }
    public static string GetDescription(this PurchaseItemType type)
    {
        switch (type)
        {
            case PurchaseItemType.ExtendLimit: return "Extends limits(Move/Time).";
            case PurchaseItemType.RemoveIce: return "Break freezed blocks.";
            case PurchaseItemType.MakeSkill1: return "Make skill blocks.";
            case PurchaseItemType.MakeCombo: return "Make combo block.";
            case PurchaseItemType.MakeSkill2: return "Make great skill block.";
            case PurchaseItemType.PowerUp: return "Power UP!!";
            default: return "Unknown Item.";
        }
    }
    public static string GetName(this PurchaseItemType type)
    {
        switch (type)
        {
            case PurchaseItemType.ExtendLimit: return "TimeKeeper";
            case PurchaseItemType.RemoveIce: return "Missile";
            case PurchaseItemType.MakeSkill1: return "Bomb";
            case PurchaseItemType.MakeCombo: return "Combo";
            case PurchaseItemType.MakeSkill2: return "Rainbow";
            case PurchaseItemType.PowerUp: return "Arrow";
            default: return "Unknown Item.";
        }
    }
    public static int GetCount(this PurchaseItemType type)
    {
        return Purchases.CountItem(type);
    }
    public static int ToInt(this PurchaseItemType type)
    {
        return (int)type;
    }
    public static PurchaseItemType ToItemType(this int type)
    {
        return (PurchaseItemType)type;
    }
    public static Sprite GetLifeSprite() { return LifeImage; }
    public static Sprite GetGoldSprite() { return GoldImage; }
    public static Sprite GetDiaSprite() { return DiaImage; }
    public static Sprite GetChestSprite() { return ChestImage; }
}

public class PurchaseInfo
{
    public int random;
    public int maxHeart;
    public int countHeart;
    public long useTimeTick;
    public int countGold;
    public int countDiamond;
    public int infiniteHeart;
    public int adsSkip;
    public int[] countItem = new int[16];
    public int[] attendFlags = new int[30];
    public PurchaseInfo()
    {
        maxHeart = 20;
        countHeart = 20;
        useTimeTick = DateTime.Now.Ticks;
        countGold = 200;
        countDiamond = 10;
        infiniteHeart = 0;
        adsSkip = 0;

        for (int i = 0; i < countItem.Length; ++i)
            countItem[i] = 0;
        for (int i = 0; i < attendFlags.Length; ++i)
            attendFlags[i] = 0;
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
        bytes.AddRange(BitConverter.GetBytes(adsSkip));

        for (int i = 0; i < countItem.Length; ++i)
            bytes.AddRange(BitConverter.GetBytes(countItem[i]));
        for (int i = 0; i < attendFlags.Length; ++i)
            bytes.AddRange(BitConverter.GetBytes(attendFlags[i]));

        return bytes.ToArray();
    }
    public void DeSerialize(byte[] data)
    {
        int off = 0;
        random = BitConverter.ToInt32(data, off); off += 4;
        maxHeart = BitConverter.ToInt32(data, off); off += 4;
        countHeart = BitConverter.ToInt32(data, off); off += 4;
        useTimeTick = BitConverter.ToInt64(data, off); off += 8;
        countGold =    BitConverter.ToInt32(data, off); off += 4;
        countDiamond = BitConverter.ToInt32(data, off); off += 4;
        infiniteHeart = BitConverter.ToInt32(data, off); off += 4;
        adsSkip = BitConverter.ToInt32(data, off); off += 4;

        for (int i = 0; i < countItem.Length; ++i)
            countItem[i] = BitConverter.ToInt32(data, off + i * 4);
        off += 4 * countItem.Length;
        for (int i = 0; i < attendFlags.Length; ++i)
            attendFlags[i] = BitConverter.ToInt32(data, off + i * 4);
        off += 4 * attendFlags.Length;
    }
}

public class Purchases
{
    private const string prefsKeyName = "pcInfo5";
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
    public static bool IsHeartMax()
    {
        return mInfo.countHeart >= mInfo.maxHeart;
    }
    public static bool ChargeHeart(int cnt, int diamond)
    {
        if (mInfo.countDiamond < diamond)
            return false;
        mInfo.countDiamond -= diamond;
        mInfo.countHeart += cnt;
        if (mInfo.countHeart > mInfo.maxHeart)
            mInfo.countHeart = mInfo.maxHeart;
        mInfo.useTimeTick = DateTime.Now.Ticks;
        UpdatePurchaseInfo(mInfo);
        return true;
    }
    public static bool ChargeHeartInfinite(int diamond)
    {
        if (mInfo.countDiamond < diamond || IsInfinite())
            return false;
        mInfo.countDiamond -= diamond;
        mInfo.infiniteHeart = 1;
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

    public static bool IsInfinite()
    {
        return mInfo.infiniteHeart == 1;
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
    public static bool PurchaseGold(int gold, int costDiamond)
    {
        if (mInfo.countDiamond < costDiamond)
            return false;
        mInfo.countGold += gold;
        mInfo.countDiamond -= costDiamond;
        UpdatePurchaseInfo(mInfo);
        return true;
    }
    public static void PurchaseDiamond(int cnt)
    {
        mInfo.countDiamond += cnt;
        UpdatePurchaseInfo(mInfo);
    }
    public static bool ChargeItemUseGold(PurchaseItemType type, int cnt, int gold)
    {
        if (mInfo.countGold < gold)
            return false;
        mInfo.countGold -= gold;
        mInfo.countItem[type.ToInt()] += cnt;
        UpdatePurchaseInfo(mInfo);
        return true;
    }
    public static bool ChargeItemUseDia(PurchaseItemType type, int cnt, int diamond)
    {
        if (mInfo.countDiamond < diamond)
            return false;
        mInfo.countDiamond -= diamond;
        mInfo.countItem[type.ToInt()] += cnt;
        UpdatePurchaseInfo(mInfo);
        return true;
    }
    public static int CountItem(PurchaseItemType type)
    {
        return mInfo.countItem[type.ToInt()];
    }
    public static bool UseItem(PurchaseItemType type)
    {
        if (mInfo.countItem[type.ToInt()] <= 0)
            return false;
        mInfo.countItem[type.ToInt()]--;
        UpdatePurchaseInfo(mInfo);
        return true;
    }
    public static void SetAttendFlag(int dayIdx)
    {
        if(dayIdx < mInfo.attendFlags.Length)
            mInfo.attendFlags[dayIdx] = 1;
    }
    public static bool IsAttend(int dayIdx)
    {
        if (dayIdx < mInfo.attendFlags.Length)
            return mInfo.attendFlags[dayIdx] == 1;
        return false;
    }
    public static void PurchaseAdsSkip()
    {
        mInfo.adsSkip = 1;
        UpdatePurchaseInfo(mInfo);
    }
    public static bool IsAdsSkip()
    {
        return mInfo.adsSkip == 1;
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
