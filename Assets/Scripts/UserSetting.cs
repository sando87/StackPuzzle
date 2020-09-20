using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UserSetting
{
    public static int UserPK { get { return mUserInfo == null ? -1 : mUserInfo.userPk; } }
    public static int UserScore
    {
        get { return mUserInfo == null ? -1 : mUserInfo.score; }
        set { mUserInfo.score = value; if (mUserInfo.score < 0) mUserInfo.score = 0; UpdateUserInfo(mUserInfo); }
    }

    public const float MatchInterval = 1.5f;
    public const int MatchCount = 3;
    public const int scorePerProduct = 1;
    public const float GridSize = 0.8f;

    private static UserInfo mUserInfo = null;
    public static void Initialize()
    {
        mUserInfo = LoadUserInfo();
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
                mUserInfo = (UserInfo)_res;
                UpdateUserInfo(mUserInfo);
            });
            return info;
        }
    }
    public static UserInfo UpdateUserInfo(UserInfo info)
    {
        PlayerPrefs.SetInt("userPk", info.userPk);
        PlayerPrefs.SetInt("score", info.score);
        PlayerPrefs.SetString("deviceName", info.deviceName);
        return mUserInfo;
    }
    
}
