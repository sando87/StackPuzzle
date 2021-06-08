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
    public static int Latency { set { mUserInfo.NetworkLatency = value; } }
    public static int UserPK { get { return mUserInfo == null ? -1 : mUserInfo.userPk; } }
    public static int UserScore { get { return mUserInfo == null ? 0 : mUserInfo.score; } }
    public static float RankingRate { get { return mUserInfo == null ? 1 : mUserInfo.rankingRate; } }
    public static string UserName { get { return mUserInfo == null ? "" : mUserInfo.userName; } }
    

    public static DateTime FirstLaunchDate
    {
        get
        {
            string ticks = PlayerPrefs.GetString("FirstLaunchDate", DateTime.Now.Ticks.ToString());
            return new DateTime(long.Parse(ticks));
        }
    }
    public static MatchingLevel MatchLevel
    {
        get { return (MatchingLevel)PlayerPrefs.GetInt("matchLevel", 1); }
        set { PlayerPrefs.SetInt("matchLevel", (int)value); }
    }
    public static bool Mute
    {
        get { return PlayerPrefs.GetInt("userMute", 0) == 1; }
        set { PlayerPrefs.SetInt("userMute", value ? 1 : 0); }
    }
    public static float VolumeSFX
    {
        get { return PlayerPrefs.GetFloat("VolumeSFX", 1); }
        set { PlayerPrefs.SetFloat("VolumeSFX", value); }
    }
    public static float VolumeBackground
    {
        get { return PlayerPrefs.GetFloat("VolumeBackground", 1); }
        set { PlayerPrefs.SetFloat("VolumeBackground", value); }
    }
    public static int TutorialNumber
    {
        get { return PlayerPrefs.GetInt("TutorialNumber", 1); }
        set { PlayerPrefs.SetInt("TutorialNumber", value); }
    }
    public static DateTime GetLastExcuteTime(AdsType type)
    {
        return new DateTime(long.Parse(PlayerPrefs.GetString(type.ToString(), "0")));
    }
    public static void SetLastExcuteTime(AdsType type, DateTime time)
    {
        PlayerPrefs.SetString(type.ToString(), time.Ticks.ToString());
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

    public const int NameLengthMin = 3;
    public const int StageTotalCount = 100;
    public const float SameSkillInterval = 0.3f; //SameSkill2 매칭 간격
    public const float ComboMatchInterval = 0.3f; //콤보 매칭간 시간
    public const float MatchReadyInterval = 0.3f; //매칭되고 실제 터지기까지 시간
    public const float ProductDropGravity = -50.0f; //떨어지는 속도
    public const float AutoMatchInterval = 0.1f; //매칭후 자동 매칭간 시간
    public const float SkillDestroyInterval = 0.2f; //1단계 스킬블럭들 터지는 시간
    public const float ChocoFlushInterval = 1.5f;
    public const int MatchCount = 3;
    public const int ScorePerAttack = 50;
    public const int FlushCount = 20;
    public const int scorePerProduct = 1;
    public const float GridSize = 0.82f;
    public const int ScorePerBar = 140;
    public const int ScorePerSplitBar = 20;
    public const float BattleOppResize = 0.5f;
    public const int HeartChargingIntervalMin = 5;
    public const float InfoBoxDisplayTime = 2.0f;
    public const int ScorePerCoin = 50;
    public const int GoldPerCoin = 1;

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
    }
    public static void AddNewUserInfoToServer()
    {
        NetClientApp.GetInstance().Request(NetCMD.AddUser, UserSetting.UserInfo, (_body) =>
        {
            UserInfo res = Utils.Deserialize<UserInfo>(ref _body);
            if (res == null || res.userPk <= 0)
                return;

            UpdateUserInfoToLocal(res);
        });
    }
    public static void LoadUserInfoFromServer()
    {
        NetClientApp.GetInstance().Request(NetCMD.UpdateUser, UserSetting.UserInfo, (_body) =>
        {
            UserInfo res = Utils.Deserialize<UserInfo>(ref _body);
            if (res == null || res.userPk != mUserInfo.userPk)
                return;

            UpdateUserInfoToLocal(res);
        });
    }
    public static void EditUserName(string name)
    {
        mUserInfo.userName = name;
        SaveUserInfo(mUserInfo);
        NetClientApp.GetInstance().Request(NetCMD.EditName, UserSetting.UserInfo, null);
    }
    public static void UpdateUserInfoToLocal(UserInfo info)
    {
        mUserInfo = info;
        SaveUserInfo(info);
    }
    public static void SetMaxLeague(MatchingLevel level)
    {
        mUserInfo.maxLeague = level;
        SaveUserInfo(mUserInfo);
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
            info.rankingRate = PlayerPrefs.GetFloat("rankingRate");
            info.deviceName = PlayerPrefs.GetString("deviceName");
            info.maxLeague = (MatchingLevel)PlayerPrefs.GetInt("maxLeague");
            return info;
        }
        else
        {
            UserInfo info = new UserInfo();
            info.deviceName = SystemInfo.deviceUniqueIdentifier;
            return info;
        }
    }
    private static UserInfo SaveUserInfo(UserInfo info)
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
            PlayerPrefs.SetFloat("rankingRate", info.rankingRate);
            PlayerPrefs.SetString("deviceName", info.deviceName);
            PlayerPrefs.SetInt("maxLeague", (int)info.maxLeague);
        }
        return info;
    }
    public static void SwitchBotPlayer(bool enable, string deviceName)
    {
        if (enable)
        {
            string fullname = Application.persistentDataPath + "/" + deviceName + ".json";
            if(!File.Exists(fullname))
            {
                MenuMessageBox.PopUp("No File. Do you want to create?", true, (isOK) => {
                    if(isOK)
                    {
                        UserInfo virtualUser = new UserInfo();
                        virtualUser.userName = deviceName;
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


            mIsBotPlayer = true;
            UserInfo info = JsonUtility.FromJson<UserInfo>(fileText);
            UpdateUserInfoToLocal(info);
            AutoBalancer.AutoBalance = true;

            if (!NetClientApp.GetInstance().IsDisconnected())
            {
                if (UserSetting.UserPK < 0)
                    UserSetting.AddNewUserInfoToServer();
                else
                    UserSetting.LoadUserInfoFromServer();
            }
        }
        else
        {
            mIsBotPlayer = false;
            mUserInfo = LoadUserInfo();
            AutoBalancer.AutoBalance = false;

            if (!NetClientApp.GetInstance().IsDisconnected())
            {
                if (UserSetting.UserPK < 0)
                    UserSetting.AddNewUserInfoToServer();
                else
                    UserSetting.LoadUserInfoFromServer();
            }

        }
    }

}
