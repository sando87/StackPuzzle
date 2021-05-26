using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoogleMobileAds.Api;
using System;

public enum AdsType
{
    None, ChargeLifeA, ChargeLifeB, RewardItem, MissionFailed
}

public class GoogleADMob : MonoBehaviour
{
    private static GoogleADMob mInst = null;
    public static GoogleADMob Inst { get { if (mInst == null) mInst = FindObjectOfType<GoogleADMob>(); return mInst; } }

    private Dictionary<AdsType, AdsUnit> AdsUnits = new Dictionary<AdsType, AdsUnit>();
    private AdsType CurAdsType = AdsType.None;
    private Action<bool> EventReward = null;
    private bool Paused = false;

    // Start is called before the first frame update
    void Start()
    {
        //ca-app-pub-3940256099942544~3347511713 test android
        //ca-app-pub-3940256099942544~1458002511 test iOS
        //ca-app-pub-1906763424823821~4446405417 prod Android

        // Initialize the Google Mobile Ads SDK.
        MobileAds.Initialize(initStatus => {
            InitAdsUnits();
        });
    }

    private void InitAdsUnits()
    {
#if UNITY_ANDROID
        //string adUnitId = "ca-app-pub-3940256099942544/5224354917";  //for rewardAds test ID

        AdsUnits[AdsType.ChargeLifeA] = new AdsUnit("ca-app-pub-1906763424823821/9540811810", AdsType.ChargeLifeA, new TimeSpan(0, 15, 0));
        AdsUnits[AdsType.ChargeLifeB] = new AdsUnit("ca-app-pub-3940256099942544/5224354917", AdsType.ChargeLifeB, new TimeSpan(0, 60, 0));
        AdsUnits[AdsType.RewardItem] = new AdsUnit("ca-app-pub-3940256099942544/5224354917", AdsType.RewardItem, new TimeSpan(0, 0, 0));
        AdsUnits[AdsType.MissionFailed] = new AdsUnit("ca-app-pub-3940256099942544/5224354917", AdsType.MissionFailed, new TimeSpan(0, 10, 0));

        foreach (var unit in AdsUnits)
            unit.Value.Load();

#elif UNITY_IPHONE
#endif
    }

    public int RemainSec(AdsType type)
    {
        return (int)AdsUnits[type].RemainSec;
    }
    public bool IsReady(AdsType type)
    {
        return AdsUnits[type].IsReady;
    }
    public void Show(AdsType type, Action<bool> eventReward)
    {
        if(AdsUnits[type].IsReady)
        {
            CurAdsType = type;
            EventReward = eventReward;
            if (AdsUnits[type].Excute())
            {
                Paused = true;
                StopCoroutine("CheckRewardResponse");
                StartCoroutine("CheckRewardResponse");
            }
        }
    }
    private IEnumerator CheckRewardResponse()
    {
        while(Paused)
            yield return null;

        EventReward?.Invoke(AdsUnits[CurAdsType].RewardSuccess);
        EventReward = null;
        CurAdsType = AdsType.None;
    }

    private void OnApplicationPause(bool pause)
    {
        Paused = pause;
    }
}

public class AdsUnit
{
    private AdsType Type;
    private string AdsID;
    private TimeSpan CoolTime;
    private RewardedAd Unit;
    private DateTime LastTime;
    public bool RewardSuccess { get; private set; }
    public AdsUnit(string id, AdsType type, TimeSpan coolTime)
    {
        Type = type;
        AdsID = id;
        CoolTime = coolTime;
        Unit = null;
        LastTime = UserSetting.GetLastExcuteTime(type);
        RewardSuccess = false;
    }

    public double RemainSec
    {
        get
        {
            TimeSpan term = DateTime.Now - LastTime;
            return term > CoolTime ? 0 : CoolTime.TotalSeconds - term.TotalSeconds;
        }
    }
    public bool IsReady { get { return RemainSec == 0 && Unit != null; } }
    public bool Excute()
    {
#if (UNITY_ANDROID || UNITY_IPHONE) && !UNITY_EDITOR
        if (Unit.IsLoaded())
        {
            RewardSuccess = false;
            Unit.Show();
            return true;
        }
        else
        {
            Load();
        }
#endif
        return false;
    }
    public void Load()
    {
        Unit = new RewardedAd(AdsID);

        Unit.OnAdLoaded += HandleRewardedAdLoaded;
        Unit.OnAdFailedToLoad += HandleRewardedAdFailedToLoad;
        Unit.OnAdOpening += HandleRewardedAdOpening;
        Unit.OnAdFailedToShow += HandleRewardedAdFailedToShow;
        Unit.OnUserEarnedReward += HandleUserEarnedReward;
        Unit.OnAdClosed += HandleRewardedAdClosed;

        // Create an empty ad request.
        AdRequest request = new AdRequest
            .Builder()
            .AddTestDevice("ABE6080B6424CDCCDE0810199E2229A4")   //sjlee's test mobile device ID
            .Build();

        // Load the rewarded ad with the request.
        Unit.LoadAd(request);
    }


    private void HandleRewardedAdLoaded(object sender, EventArgs args)
    {
        LOG.echo("[" + Type + "] HandleRewardedAdLoaded event received");
    }

    private void HandleRewardedAdFailedToLoad(object sender, AdErrorEventArgs args)
    {
        LOG.echo("[" + Type + "] HandleRewardedAdFailedToLoad event received with message: "
                + args.Message);
    }

    private void HandleRewardedAdOpening(object sender, EventArgs args)
    {
        LOG.echo("[" + Type + "] HandleRewardedAdOpening event received");
    }

    private void HandleRewardedAdFailedToShow(object sender, AdErrorEventArgs args)
    {
        LOG.echo("[" + Type + "] HandleRewardedAdFailedToShow event received with message: "
                 + args.Message);
    }

    private void HandleRewardedAdClosed(object sender, EventArgs args)
    {
        LOG.echo("[" + Type + "] HandleRewardedAdClosed event received");
        Load();
    }

    private void HandleUserEarnedReward(object sender, Reward args)
    {
        string type = args.Type;
        double amount = args.Amount;
        LOG.echo("[" + Type + "] HandleRewardedAdRewarded event received for "
                 + amount.ToString() + " " + type);

        LastTime = DateTime.Now;
        UserSetting.SetLastExcuteTime(Type, LastTime);
        RewardSuccess = true;
    }
}
