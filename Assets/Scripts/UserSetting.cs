using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.UI;

public class UserSetting
{

    #region Game System Config Values
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
    private const string UserInfoVersion = "ui1";
    #endregion


    #region UserSetting Information in Local
    private static UserSettingInfo mUserSettingInfo = null;
    private static UserSettingInfo UserSettingInfo
    {
        get
        {
            if (mUserSettingInfo == null)
                mUserSettingInfo = UserSettingInfo.Load();
            return mUserSettingInfo;
        }
    }
    public static bool IsTermsAgreement { get { return UserSettingInfo.IsTermsAgreement; } set { UserSettingInfo.IsTermsAgreement = value; } }
    public static DateTime FirstLaunchDate { get { return UserSettingInfo.FirstLaunchDate; } }
    public static MatchingLevel MatchLevel { get { return UserSettingInfo.MatchLevel; } set { UserSettingInfo.MatchLevel = value; } }
    public static bool Mute { get { return UserSettingInfo.Mute; } set { UserSettingInfo.Mute = value; } }
    public static float VolumeSFX { get { return UserSettingInfo.VolumeSFX; } set { UserSettingInfo.VolumeSFX = value; } }
    public static float VolumeBackground { get { return UserSettingInfo.VolumeBackground; } set { UserSettingInfo.VolumeBackground = value; } }
    public static int TutorialNumber { get { return UserSettingInfo.TutorialNumber; } set { UserSettingInfo.TutorialNumber = value; } }
    public static DateTime GetLastExcuteTime(AdsType type) { return UserSettingInfo.GetLastExcuteTime(type); }
    public static void SetLastExcuteTime(AdsType type, DateTime time) { UserSettingInfo.SetLastExcuteTime(type, time); }
    public static bool StageIsLocked(int stageNum) { return UserSettingInfo.StageIsLocked(stageNum); }
    public static void StageUnLock(int stageNum) { UserSettingInfo.StageUnLock(stageNum); }
    public static byte GetStageStarCount(int stageNum) { return UserSettingInfo.GetStageStarCount(stageNum); }
    public static void SetStageStarCount(int stageNum, byte starCount) { UserSettingInfo.SetStageStarCount(stageNum, starCount); }
    public static int GetHighestStageNumber() { return UserSettingInfo.GetHighestStageNumber(); }
    #endregion


    #region UserInfo Network
    private static bool mIsBotPlayer = false;
    private static UserInfo mUserInfo = null;

    public static bool IsBotPlayer { get { return mIsBotPlayer; } }
    public static UserInfo UserInfo { get { return mUserInfo; } }
    public static int Latency { set { mUserInfo.NetworkLatency = value; } }
    public static int UserPK { get { return mUserInfo == null ? -1 : mUserInfo.userPk; } }
    public static int UserScore { get { return mUserInfo == null ? 0 : mUserInfo.score; } }
    public static float RankingRate { get { return mUserInfo == null ? 1 : mUserInfo.rankingRate; } }
    public static string UserName { get { return mUserInfo == null ? "" : mUserInfo.userName; } }

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
        if(PlayerPrefs.HasKey(UserInfoVersion))
        {
            UserInfo info = UnityUtils.LoadFromRegedit<UserInfo>(UserInfoVersion);
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
            UnityUtils.SaveToRegedit(UserInfoVersion, info);
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
    #endregion

}

class UserSettingInfo
{
    private const string KeyVersion = "usi2";

    [SerializeField] private bool mIsTermsAgreement = false;
    [SerializeField] private Int64 mFirstLaunchDate = 0;
    [SerializeField] private MatchingLevel mMatchLevel = MatchingLevel.Bronze;
    [SerializeField] private bool mMute = false;
    [SerializeField] private float mVolumeSFX = 1;
    [SerializeField] private float mVolumeBackground = 1;
    [SerializeField] private int mTutorialNumber = 1;
    [SerializeField] private Int64[] mAdsLastShowTime = null;
    [SerializeField] private byte[] mStageStarCount = null;

    public UserSettingInfo()
    {
        mIsTermsAgreement = false;
        mFirstLaunchDate = DateTime.Now.Ticks;
        mAdsLastShowTime = new Int64[Enum.GetValues(typeof(AdsType)).Length];
        mStageStarCount = new byte[UserSetting.StageTotalCount];

        for (int i = 0; i < mStageStarCount.Length; ++i)
            mStageStarCount[i] = 0xff;
        mStageStarCount[0] = 0;
    }

    public bool IsTermsAgreement
    {
        get { return mIsTermsAgreement; }
        set { mIsTermsAgreement = value; Save(); }
    }
    public DateTime FirstLaunchDate
    {
        get { return new DateTime(mFirstLaunchDate); }
    }
    public MatchingLevel MatchLevel
    {
        get { return mMatchLevel; }
        set { mMatchLevel = value; Save(); }
    }
    public bool Mute
    {
        get { return mMute; }
        set { mMute = value; Save(); }
    }
    public float VolumeSFX
    {
        get { return mVolumeSFX; }
        set { mVolumeSFX = value; Save(); }
    }
    public float VolumeBackground
    {
        get { return mVolumeBackground; }
        set { mVolumeBackground = value; Save(); }
    }
    public int TutorialNumber
    {
        get { return mTutorialNumber; }
        set { mTutorialNumber = value; Save(); }
    }
    public DateTime GetLastExcuteTime(AdsType type)
    {
        return new DateTime(mAdsLastShowTime[(int)type]);
    }
    public void SetLastExcuteTime(AdsType type, DateTime time)
    {
        mAdsLastShowTime[(int)type] = time.Ticks;
        Save();
    }
    public bool StageIsLocked(int stageNum)
    {
        return mStageStarCount[stageNum - 1] == 0xff;
    }
    public void StageUnLock(int stageNum)
    {
        byte cnt = mStageStarCount[stageNum - 1];
        if (cnt == 0xff)
            SetStageStarCount(stageNum, 0);
    }
    public byte GetStageStarCount(int stageNum)
    {
        byte cnt = mStageStarCount[stageNum - 1];
        return cnt == 0xff ? (byte)0 : cnt;
    }
    public int GetHighestStageNumber()
    {
        for (int i = 0; i < mStageStarCount.Length; ++i)
            if (mStageStarCount[i] == 0xff)
                return i;
        return mStageStarCount.Length;
    }
    public void SetStageStarCount(int stageNum, byte starCount)
    {
        mStageStarCount[stageNum - 1] = starCount;
        Save();
    }

    public static UserSettingInfo Load()
    {
        return UnityUtils.LoadFromRegedit<UserSettingInfo>(KeyVersion);
    }
    private void Save()
    {
        UnityUtils.SaveToRegedit(KeyVersion, this);
    }
}