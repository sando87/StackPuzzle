using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UserSetting
{
    public static int UserPK { get; set; }
    public static int UserScore { get; set; }

    public const float MatchInterval = 1.5f;
    public const int MatchCount = 3;
    public const int scorePerProduct = 1;
    public const float GridSize = 0.8f;

    public static UserInfo mUserInfo = null;
    public static UserInfo UserInfo
    {
        get
        {
            if (mUserInfo == null)
                mUserInfo = LoadUserInfo();
            return mUserInfo;
        }
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
}
