using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class UserPurchaseInfo
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

public class UserSetting
{
    public static int UserPK { get { return mUserInfo == null ? -1 : mUserInfo.userPk; } }
    public static int UserScore { get { return mUserInfo == null ? -1 : mUserInfo.score; } }

    public const float MatchInterval = 1.5f;
    public const int MatchCount = 3;
    public const int scorePerProduct = 1;
    public const float GridSize = 0.8f;

    private static UserInfo mUserInfo = null;
    private static UserPurchaseInfo mUserPurchaseInfo = null;
    public static void Initialize()
    {
        mUserInfo = LoadUserInfo();
        mUserPurchaseInfo = LoadPurchaseInfo();
    }


    public static UserInfo LoadUserInfo()
    {
        if(PlayerPrefs.HasKey("userPk"))
        {
            UserInfo info = new UserInfo();
            info.userPk = PlayerPrefs.GetInt("userPk");
            info.score = PlayerPrefs.GetInt("score");
            info.deviceName = PlayerPrefs.GetString("deviceName");
            return info;
        }
        else
        {
            UserInfo info = new UserInfo();
            info.userPk = -1;
            info.score = 100;
            info.deviceName = SystemInfo.deviceUniqueIdentifier;
            NetClientApp.GetInstance().Request(NetCMD.AddUser, info, (_res) =>
            {
                UserInfo res = (UserInfo)_res;
                UpdateUserInfo(res);
            });
            return info;
        }
    }
    public static UserInfo UpdateUserInfo(UserInfo info)
    {
        PlayerPrefs.SetInt("userPk", info.userPk);
        PlayerPrefs.SetInt("score", info.score);
        PlayerPrefs.SetString("deviceName", info.deviceName);
        mUserInfo = info;
        return mUserInfo;
    }


    public static int CountHeart()
    {
        return 0;
    }
    public static void ChargeHeart(int cnt)
    {
    }
    public static bool UseHeart()
    {
        return true;
    }
    public static TimeSpan RemainTime()
    {
        return TimeSpan.Zero;
    }
    public static void ChargeDiamond(int cnt)
    {
        mUserPurchaseInfo.countDiamond += cnt;
        UpdatePurchaseInfo(mUserPurchaseInfo);
    }
    public static void PurchaseItemA(int cnt)
    {
    }
    public static bool UseItemA()
    {
        return true;
    }
    private static UserPurchaseInfo LoadPurchaseInfo()
    {
        if (PlayerPrefs.HasKey("pcInfo"))
        {
            string hexStr = PlayerPrefs.GetString("pcInfo");
            byte[] bytes = Utils.HexStringToByteArray(hexStr);
            UserPurchaseInfo info = Utils.Deserialize<UserPurchaseInfo>(bytes);
            return info;
        }
        else
        {
            UserPurchaseInfo info = new UserPurchaseInfo();
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
    private static void UpdatePurchaseInfo(UserPurchaseInfo info)
    {
        byte[] bInfo = Utils.Serialize(info);
        string hexStr = BitConverter.ToString(bInfo).Replace("-", string.Empty);
        PlayerPrefs.SetString("pcInfo", hexStr);
    }
}
