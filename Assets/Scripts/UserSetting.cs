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
    public static bool StageIsLocked(int stageNum)
    {
        byte cnt = StageStarCount[stageNum - 1];
        return cnt == 0xff;
    }
    public static void StageUnLock(int stageNum)
    {
        byte cnt = StageStarCount[stageNum - 1];
        if (cnt == 0xff)
            SetStageStarCount(stageNum, 0);
    }
    public static byte GetStageStarCount(int stageNum)
    {
        byte cnt = StageStarCount[stageNum - 1];
        return cnt == 0xff ? (byte)0 : cnt;
    }
    public static void SetStageStarCount(int stageNum, byte starCount)
    {
        StageStarCount[stageNum - 1] = starCount;
        byte[] encryptInfo = Utils.Encrypt(StageStarCount);
        string hexStr = BitConverter.ToString(encryptInfo).Replace("-", string.Empty);
        PlayerPrefs.SetString("StageStarCount", hexStr);
    }

    public const int StageTotalCount = 100;
    public const float MatchInterval = 1.5f;
    public const int MatchCount = 3;
    public const int scorePerProduct = 1;
    public const float GridSize = 0.82f;

    private static UserInfo mUserInfo = null;
    private static byte[] mStageStarCount = null;
    private static byte[] StageStarCount
    {
        get
        {
            if (mStageStarCount == null)
            {
                if (PlayerPrefs.HasKey("StageStarCount"))
                {
                    string hexStr = PlayerPrefs.GetString("StageStarCount");
                    byte[] bytes = Utils.HexStringToByteArray(hexStr);
                    byte[] originData = Utils.Decrypt(bytes);
                    mStageStarCount = originData;
                }
                else
                {
                    mStageStarCount = new byte[StageTotalCount];
                    for (int i = 0; i < StageTotalCount; ++i)
                        mStageStarCount[i] = 0xff; //lock
                    SetStageStarCount(1, 0);
                }
            }
            return mStageStarCount;
        }
    }

    public static void Initialize()
    {
        mUserInfo = LoadUserInfo();
        if(mUserInfo.userPk <= 0)
        {
            NetClientApp.GetInstance().Request(NetCMD.AddUser, mUserInfo, (_res) =>
            {
                UserInfo res = (UserInfo)_res;
                if (res.userPk <= 0)
                    return;

                mUserInfo = res;
                UpdateUserInfo(mUserInfo);
            });
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
            return info;
        }
    }
    public static UserInfo UpdateUserInfo(UserInfo info)
    {
        PlayerPrefs.SetInt("userPk", info.userPk);
        PlayerPrefs.SetInt("score", info.score);
        PlayerPrefs.SetString("deviceName", info.deviceName);
        return info;
    }
    
}
