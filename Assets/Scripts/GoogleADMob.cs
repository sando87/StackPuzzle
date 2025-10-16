using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// using GoogleMobileAds.Api;
using System;
using UnityEngine.Advertisements;
using UnityEngine.UI;

public enum AdsType
{
    None, ChargeLifeA, ChargeLifeB, RewardItem, MissionFailed, InGameItemA, InGameItemB, InGameItemC
}

public class GoogleADMob : MonoBehaviour, IUnityAdsInitializationListener
{
    private static GoogleADMob mInst = null;
    public static GoogleADMob Inst { get { if (mInst == null) mInst = FindObjectOfType<GoogleADMob>(); return mInst; } }

    [SerializeField] private string _androidGameId = "YOUR_ANDROID_GAME_ID";
    [SerializeField] private string _iOSGameId = "YOUR_IOS_GAME_ID";
    [SerializeField] private bool _testMode = true;
    [SerializeField] List<AdsUnit> AdsList = new List<AdsUnit>();
    private Dictionary<AdsType, AdsUnit> AdsUnits = new Dictionary<AdsType, AdsUnit>();

    public bool IsDoneInit { get; private set; } = false;
    public bool IsSuceessInit { get; private set; } = false;
    public string GameId { get { return (Application.platform == RuntimePlatform.IPhonePlayer) ? _iOSGameId : _androidGameId; } }

    void Awake()
    {
        foreach (AdsUnit ads in AdsList)
        {
            if (ads.AdsType != AdsType.None)
                AdsUnits[ads.AdsType] = ads;
        }

        StartCoroutine(StartInit());
    }

    IEnumerator StartInit()
    {
        while (true)
        {
            if (!Advertisement.isInitialized && NetClientApp.GetInstance().IsNetworkAlive)
            {
                IsDoneInit = false;
                IsSuceessInit = false;
                Advertisement.Initialize(GameId, _testMode, this);
                yield return new WaitUntil(() => IsDoneInit);

                if (IsSuceessInit)
                {
                    foreach (var ads in AdsUnits)
                    {
                        ads.Value.Load();
                    }

                    StopCoroutine(nameof(CheckAdsUnitLoading));
                    StartCoroutine(nameof(CheckAdsUnitLoading));
                }
            }

            yield return new WaitForSeconds(5);
        }
    }
    public void OnInitializationComplete()
    {
        IsDoneInit = true;
        IsSuceessInit = true;
    }
    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        IsDoneInit = true;
        IsSuceessInit = false;
    }



    public int RemainSec(AdsType type)
    {
#if UNITY_ANDROID || UNITY_IOS
        if (!IsSuceessInit || !AdsUnits.ContainsKey(type))
            return -1;

        return (int)AdsUnits[type].RemainSec;
#else
        return (int)AdsUnits[type].RemainSec;
#endif
    }
    public bool IsLoaded(AdsType type)
    {
#if UNITY_ANDROID || UNITY_IOS
        if (!IsSuceessInit || !AdsUnits.ContainsKey(type))
            return false;

        return AdsUnits[type].IsLoaded;
#else
        return true;
#endif
    }
    public void Show(AdsType type, Action<bool> eventReward)
    {
#if UNITY_ANDROID || UNITY_IOS
        if (!IsSuceessInit || !AdsUnits.ContainsKey(type))
            return;

        if (!AdsUnits[type].IsLoaded)
        {
            eventReward.Invoke(false);
            return;
        }

        AdsUnits[type].Show(eventReward);
#else
        AdsUnits[type].LastTime = DateTime.Now;
        eventReward?.Invoke(true);
#endif
    }

    private IEnumerator CheckAdsUnitLoading()
    {
        while(true)
        {
            yield return new WaitForSeconds(3);

            if (!NetClientApp.GetInstance().IsNetworkAlive)
                continue;

            if (IsSuceessInit)
            {
                foreach (var unit in AdsUnits)
                {
                    if (unit.Value.State == AdsUnitState.UnLoaded)
                        unit.Value.Load();
                }
            }
        }
    }

    // private void OnApplicationPause(bool pause)
    // {
    //     Paused = pause;
    // }
}

public enum AdsUnitState
{
    None, UnLoaded, Loading, Loaded, Showing
}

[Serializable]
public class AdsUnit : IUnityAdsLoadListener, IUnityAdsShowListener
{
    public AdsType AdsType = AdsType.None;
    public string AdsID_Adroid = "";
    public string AdsID_IOS = "";
    public double Cooltime = 0;

    public string AdsUnitID { get { return Application.platform == RuntimePlatform.IPhonePlayer ? AdsID_IOS : AdsID_Adroid; } }
    public DateTime LastTime = new DateTime();
    public AdsUnitState State { get; private set; } = AdsUnitState.UnLoaded;

    private Action<bool> mEventOnReward = null; // 인자로는 보상 성공 여부를 전달

    public double RemainSec
    {
        get
        {
            double seconds = (DateTime.Now - LastTime).TotalSeconds;
            return seconds > Cooltime ? 0 : Cooltime - seconds;
        }
    }

    public bool IsLoaded { get { return State == AdsUnitState.Loaded; } }

    public void Load()
    {
        if (IsLoaded)
            return;

        State = AdsUnitState.Loading;
        Advertisement.Load(AdsUnitID, this);
    }

    public void Show(Action<bool> onReward)
    {
        if (!IsLoaded)
            return;
            
        LOG.trace(AdsUnitID);
        State = AdsUnitState.Showing;
        mEventOnReward = onReward;
        Advertisement.Show(AdsUnitID, this);
    }


    public void OnUnityAdsAdLoaded(string placementId)
    {
        if (AdsUnitID.Equals(placementId))
        {
            State = AdsUnitState.Loaded;
            mEventOnReward = null;
        }
    }

    public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message)
    {
        if (AdsUnitID.Equals(placementId))
        {
            State = AdsUnitState.UnLoaded;
            mEventOnReward = null;
        }
    }

    public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message)
    {
        if (AdsUnitID.Equals(placementId))
        {
            State = AdsUnitState.UnLoaded;
            mEventOnReward?.Invoke(false);
            mEventOnReward = null;
        }
    }

    public void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState showCompletionState)
    {
        LOG.trace(placementId + ":" + showCompletionState);
        if (AdsUnitID.Equals(placementId) && showCompletionState == UnityAdsShowCompletionState.COMPLETED)
        {
            State = AdsUnitState.UnLoaded;
            LastTime = DateTime.Now;
            mEventOnReward?.Invoke(true);
            mEventOnReward = null;
        }
        else
        {
            State = AdsUnitState.UnLoaded;
            mEventOnReward?.Invoke(false);
            mEventOnReward = null;
        }
    }

    public void OnUnityAdsShowStart(string placementId)
    {
    }

    public void OnUnityAdsShowClick(string placementId)
    {
    }

}
