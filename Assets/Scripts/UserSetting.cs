using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.UI;

public class UserSetting
{
    public static bool IsBotPlayer { get { return mIsBotPlayer; } }
    public static UserInfo UserInfo { get { return mUserInfo; } }
    public static int UserPK { get { return mUserInfo == null ? -1 : mUserInfo.userPk; } }
    public static int UserScore
    {
        get { return mUserInfo == null ? -1 : mUserInfo.score; }
        set {
            mUserInfo.score = value;
            UpdateUserInfo(mUserInfo);
        }
    }
    public static bool Win {
        set
        {
            if (value)
                mUserInfo.win++;
            else
                mUserInfo.lose++;
            mUserInfo.total++;
            UpdateUserInfo(mUserInfo);
        }
    }
    public static bool Mute
    {
        get { return PlayerPrefs.GetInt("userMute", 0) == 1; }
        set { PlayerPrefs.SetInt("userMute", value ? 1 : 0); }
    }
    public static int TutorialNumber
    {
        get { return PlayerPrefs.GetInt("TutorialNumber", 1); }
        set { PlayerPrefs.SetInt("TutorialNumber", value); }
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
    public const float MatchInterval = 2.0f;
    public const float ChocoFlushInterval = 1.5f;
    public const int MatchCount = 3;
    public const int AttackScore = 10;
    public const int FlushCount = 20;
    public const int scorePerProduct = 1;
    public const float GridSize = 0.82f;
    public const int ScorePerBar = 300;
    public const int ScorePerSplitBar = 60;
    public const float BattleOppResize = 0.6f;
    public const int HeartChargingIntervalMin = 5;
    public const float InfoBoxDisplayTime = 2.0f;
    public const int ScorePerCoin = 50;

    private static bool mIsBotPlayer = false;
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
            NetClientApp.GetInstance().Request(NetCMD.AddUser, mUserInfo, (_body) =>
            {
                UserInfo res = Utils.Deserialize<UserInfo>(ref _body);
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
            info.userName = PlayerPrefs.GetString("userName");
            info.score = PlayerPrefs.GetInt("score");
            info.win = PlayerPrefs.GetInt("win");
            info.lose = PlayerPrefs.GetInt("lose");
            info.total = PlayerPrefs.GetInt("total");
            info.deviceName = PlayerPrefs.GetString("deviceName");
            return info;
        }
        else
        {
            UserInfo info = new UserInfo();
            info.userPk = -1;
            info.userName = "No Name";
            info.score = 100;
            info.win = 0;
            info.lose = 0;
            info.total = 0;
            info.deviceName = SystemInfo.deviceUniqueIdentifier;
            return info;
        }
    }
    public static UserInfo UpdateUserInfo(UserInfo info)
    {
        if (mIsBotPlayer)
        {
            string jsonUserInfo = JsonUtility.ToJson(info, true);
            string fullname = Application.persistentDataPath + "/" + info.deviceName + ".json";
            File.WriteAllText(fullname, jsonUserInfo);
        }
        else
        {
            PlayerPrefs.SetInt("userPk", info.userPk);
            PlayerPrefs.SetString("userName", info.userName);
            PlayerPrefs.SetInt("score", info.score);
            PlayerPrefs.SetInt("win", info.win);
            PlayerPrefs.SetInt("lose", info.lose);
            PlayerPrefs.SetInt("total", info.total);
            PlayerPrefs.SetString("deviceName", info.deviceName);
        }
        return info;
    }
    public static void SwitchBotPlayer(bool enable, string deviceName)
    {
        if (enable)
        {
#if PLATFORM_ANDROID
            if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead))
                Permission.RequestUserPermission(Permission.ExternalStorageRead);
#endif

            string fullname = Application.persistentDataPath + "/" + deviceName + ".json";
            if(!File.Exists(fullname))
            {
                MenuMessageBox.PopUp("No File. Do you want to create?", true, (isOK) => {
                    if(isOK)
                    {
                        UserInfo virtualUser = new UserInfo();
                        virtualUser.deviceName = deviceName;
                        string jsonUserInfo = JsonUtility.ToJson(virtualUser, true);
                        File.WriteAllText(fullname, jsonUserInfo);
                        SwitchBotPlayer(true, deviceName);
                    }
                });
                return;
            }
            
            string fileText = File.ReadAllText(fullname);
            if (fileText == null || fileText.Length == 0)
                return;
                        
            mUserInfo = JsonUtility.FromJson<UserInfo>(fileText);
            mIsBotPlayer = true;
            AutoBalancer.AutoBalance = true;
            if (mUserInfo.userPk <= 0)
            {
                NetClientApp.GetInstance().Request(NetCMD.AddUser, mUserInfo, (_body) =>
                {
                    UserInfo res = Utils.Deserialize<UserInfo>(ref _body);
                    if (res.userPk <= 0)
                        return;

                    mUserInfo = res;
                    UpdateUserInfo(mUserInfo);
                });
            }
        }
        else
        {
            mUserInfo = LoadUserInfo();
            mIsBotPlayer = false;
            AutoBalancer.AutoBalance = false;
        }
    }

}
