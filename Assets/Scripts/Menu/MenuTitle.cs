using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Android;
using System;
using System.IO;

public class MenuTitle : MonoBehaviour
{

    private bool mIsTouched = false;

    public Slider LoadingBar;
    public TextMeshProUGUI VersionText;
    public TextMeshProUGUI LoadingText;
    public TextMeshProUGUI StartText;
    public GameObject StartButton;
    public Image ForegroundImage;

    [SerializeField] LocaleManager _LocaleManager;
    [SerializeField] LogWriter _LogWriter;
    [SerializeField] NetClientApp _NetworkObject;
    [SerializeField] LogToGoogleForms _LogToGoogleForms;
    [SerializeField] GameObject _SoundManager;
    [SerializeField] GameObject _ObjectPooling;
    [SerializeField] GameObject _AutoBotSystem;
    [SerializeField] GameObject _GoogleAd;
    [SerializeField] GameObject _IAPManager;
    [SerializeField] GameObject _ReviewManager;
    [SerializeField] GameObject _Tutorial;
    [SerializeField] PerformanceMonitering _Monitoring;
    [SerializeField] GameObject _MobileNotification;

    private void Awake()
    {
        StartCoroutine(CoIntializer());
    }

    IEnumerator CoIntializer()
    {
        LoadingBar.value = 0;
        yield return StartCoroutine(CoFadeInOut(true, 0.5f));
        StartCoroutine(nameof(Loading));
        LoadingText.text = "0%";

#if PLATFORM_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead))
            Permission.RequestUserPermission(Permission.ExternalStorageRead);
#endif

        // 로그 - 로컬파일
        InitLogSystem();
        LoadingText.text = "5%";
        yield return new WaitForSeconds(0.1f);
        LoadingText.text = "10%";

#if UNITY_STANDALONE_WIN || UNITY_EDITOR
        DirectoryInfo di = new DirectoryInfo("./VirtualSaveData/");
        if (di.Exists == false)
            di.Create();
#endif

        // 현지화로 언어 번역 기능
        _LocaleManager.gameObject.SetActive(true);
        yield return new WaitForSeconds(0.1f);
        LoadingText.text = "12%";

        // 로컬 파일 IO
        UserSetting.Initialize();
        yield return new WaitForSeconds(0.1f);
        LoadingText.text = "15%";
        VersionText.text = "v" + UserSetting.UserSettingInfo.Version;

        // 최초 1회 시스템 언어에 따라 디폴트 언어 세팅
        if (UserSetting.CurrentLang == LocaleSupportLangType.None)
        {
            // 우선 현지화 언어 대응은 영어권 국가만 지원
            UserSetting.CurrentLang = LocaleSupportLangType.English;
            // UserSetting.CurrentLang = ConvertLangType(Application.systemLanguage);
        }

        Purchases.Initialize();
        yield return new WaitForSeconds(0.1f);
        LoadingText.text = "20%";

        // 약정 동의
        if (!UserSetting.IsTermsAgreement)
        {
            MenuTermsAndConditions.PopUp(() =>
            {
                LOG.trace("TermsAgreed");
                UserSetting.IsTermsAgreement = true;
            });

            yield return new WaitUntil(() => UserSetting.IsTermsAgreement);
        }

        // 네트워크매니저
        _NetworkObject.EventConnection = OnNetConnected;
        _NetworkObject.gameObject.SetActive(true);
        yield return new WaitUntil(() => !_NetworkObject.IsTryingConnect);
        LoadingText.text = "45%";

        // 로그 - 구글폼
        _LogToGoogleForms.gameObject.SetActive(true);
        yield return new WaitForSeconds(0.1f);
        LoadingText.text = "50%";

        // 사운드매니저
        _SoundManager.SetActive(true);
        yield return new WaitForSeconds(0.1f);
        SoundPlayer.Inst.PlayBackMusic(SoundPlayer.Inst.BackMusicMap);
        LoadingText.text = "60%";

        // 오브젝트풀링
        _ObjectPooling.SetActive(true);
        yield return new WaitForSeconds(0.1f);
        LoadingText.text = "70%";

        // 자동봇 시스템 초기화
        _AutoBotSystem.SetActive(true);
        yield return new WaitForSeconds(0.1f);
        LoadingText.text = "80%";

        // 구글광고
        _GoogleAd.SetActive(true);
        yield return new WaitForSeconds(0.1f);
        LoadingText.text = "90%";

        // 인앱결제모듈
        _IAPManager.SetActive(true);
        yield return new WaitForSeconds(0.1f);
        LoadingText.text = "92%";

        // 리뷰 매니저
        _ReviewManager.SetActive(true);
        yield return new WaitForSeconds(0.1f);
        LoadingText.text = "94%";

        // 튜토리얼
        _Tutorial.SetActive(true);
        yield return new WaitForSeconds(0.1f);
        LoadingText.text = "95%";

        // 알람 기능
        _MobileNotification.SetActive(true);
        yield return new WaitForSeconds(0.1f);
        MobileNotificationManager.Inst.SetUpAlarm(UserSetting.IsAlarmOn);
        LoadingText.text = "97%";

        // 기타 시스템 설정 초기화
        Application.targetFrameRate = 30; //FPS 30프레임 고정
        Screen.sleepTimeout = SleepTimeout.NeverSleep; //화면꺼짐 방지
        yield return new WaitForSeconds(0.1f);
        LoadingText.text = "100%";

        uint tickSecond = (uint)(DateTime.Now.Ticks / TimeSpan.TicksPerSecond);
        string sessionID = tickSecond.ToString() + "." + UserSetting.UserInfo.userPk;
        UserSetting.SessionID = sessionID;
        string log = "GameStart"
        + "," + UserSetting.UserSettingInfo.Version
        + "," + UserSetting.SessionID
        + "," + Purchases.CountGold()
        + "," + Purchases.CountDiamond()
        + "," + Purchases.CountItem(PurchaseItemType.ExtendLimit)
        + "," + Purchases.CountItem(PurchaseItemType.RemoveIce)
        + "," + Purchases.CountItem(PurchaseItemType.MakeSkill1)
        + "," + Purchases.CountItem(PurchaseItemType.MakeSkill2)
        + "," + Purchases.CountItem(PurchaseItemType.Meteor)
        + "," + Purchases.CountItem(PurchaseItemType.KeepCombo);
        LOG.trace(log);

        LogToGoogleForms.Instance.LogGameStart(sessionID);

        // 모니터링 시스템 작동
        // _Monitoring.OnMonitering = (log) => LOG.trace(log);
        // _Monitoring.gameObject.SetActive(true);
        // yield return new WaitForSeconds(0.1f);
        // LoadingText.text = "100%";

        mIsTouched = false;
        ReadyAndWaitForTouch();
        yield return new WaitUntil(() => mIsTouched);
        UserSetting.CountingGameStart();
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
        gameObject.SetActive(false);
        MenuStages.PopUp();
    }



    IEnumerator Loading()
    {
        float time = 0;
        while (true)
        {
            float rate = time - (int)time;
            LoadingBar.value = rate;
            yield return null;
            time += Time.deltaTime;
        }
    }
    private void ReadyAndWaitForTouch()
    {
        StopCoroutine(nameof(Loading));
        LoadingBar.gameObject.SetActive(false);
        StartText.gameObject.SetActive(true);
        StartButton.gameObject.SetActive(true);
        StartCoroutine(FlinkerStartText());
    }

    public void OnTouchScreen()
    {
        mIsTouched = true;
    }

    IEnumerator CoFadeInOut(bool isFadeIn, float duration = 1f)
    {
        float time = 0;
        while (time < duration)
        {
            if (isFadeIn)
                ForegroundImage.color = new Color(0, 0, 0, (duration - time) / duration);
            else
                ForegroundImage.color = new Color(0, 0, 0, time / duration);

            time += Time.deltaTime;
            yield return null;
        }
        ForegroundImage.color = new Color(0, 0, 0, isFadeIn ? 0 : 1);
    }

    public void OnNetConnected()
    {
        LOG.trace("NetConnection[ OK ]");
        if (UserSetting.UserInfo.userPk < 0)
            UserSetting.AddNewUserInfoToServer();
        else
            UserSetting.LoadUserInfoFromServer();

        LOG.IsNetworkAlive = () => { return !NetClientApp.GetInstance().IsDisconnected(); };
    }


    private void InitLogSystem()
    {
        LOG.LogWriterConsole = (msg) => { Debug.Log(msg); };
        // LOG.IsNetworkAlive = () => { return !NetClientApp.GetInstance().IsDisconnected(); };
        LOG.LogStringWriterDB = (msg) => {
            LogInfo info = new LogInfo();
            info.userPk = UserSetting.UserPK;
            info.message = msg;
            return NetClientApp.GetInstance().Request(NetCMD.AddLog, info, null);
        };
        LOG.LogBytesWriterDB = (data) => {
            LogFile info = new LogFile();
            info.userPk = UserSetting.UserPK;
            info.data = data;
            return NetClientApp.GetInstance().Request(NetCMD.AddLogFile, info, null);
        };

#if UNITY_STANDALONE_WIN || UNITY_EDITOR
        LOG.Initialize(".");
#else
        LOG.Initialize(Application.persistentDataPath);
#endif

        _LogWriter.gameObject.SetActive(true);
    }


    IEnumerator FlinkerStartText()
    {
        bool isShow = true;
        while (true)
        {
            StartText.gameObject.SetActive(isShow);
            isShow = !isShow;
            yield return new WaitForSeconds(1);
        }
    }

    LocaleSupportLangType ConvertLangType(SystemLanguage systemLang)
    {
        switch (systemLang)
        {
            case SystemLanguage.Korean: return LocaleSupportLangType.Korean;
            case SystemLanguage.Japanese: return LocaleSupportLangType.Japanese;
            case SystemLanguage.ChineseSimplified: return LocaleSupportLangType.Chinese;
            default: return LocaleSupportLangType.English;
        }
    }

}
