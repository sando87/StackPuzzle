using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Android;

public class MenuTitle : MonoBehaviour
{
    private const float mMaxTimeout = 3;

    public Slider LoadingBar;
    public TextMeshProUGUI StartText;
    public GameObject StartButton;
    public GameObject NetworkObject;

    private void Awake()
    {
#if PLATFORM_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead))
            Permission.RequestUserPermission(Permission.ExternalStorageRead);
#endif

        Consolation.ConsoleInGame cig = FindObjectOfType<Consolation.ConsoleInGame>();
        if (cig != null)
            cig.Init();

        LOG.LogWriterConsole = (msg) => { Debug.Log(msg); };

        Application.targetFrameRate = 30; //FPS 30프레임 고정
        Screen.sleepTimeout = SleepTimeout.NeverSleep; //화면꺼짐 방지

        SoundPlayer.Inst.PlayBackMusic(SoundPlayer.Inst.BackMusicMap);

        if (UserSetting.IsTermsAgreement)
        {
            InitOnAwake();
        }
        else
        {
            MenuTermsAndConditions.PopUp(() =>
            {
                LOG.echo("TermsAgreed");
                InitOnAwake();
            });
        }
    }

    private void InitOnAwake()
    {
        LOG.echo("Start App");
        NetworkObject.SetActive(true);
        NetClientApp.GetInstance().EventConnection = OnNetConnected;
        InitLogSystem();
        UserSetting.Initialize();
        Purchases.Initialize();

        StartCoroutine("Loading");
    }

    public void OnNetConnected()
    {
        LOG.echo("NetConnection[ OK ]");
        if (UserSetting.UserInfo.userPk < 0)
            UserSetting.AddNewUserInfoToServer();
        else
            UserSetting.LoadUserInfoFromServer();
        
        if(gameObject.activeInHierarchy)
        {
            Ready();
        }
    }

    private void Ready()
    {
        StopCoroutine("Loading");
        LoadingBar.gameObject.SetActive(false);
        StartText.gameObject.SetActive(true);
        StartButton.gameObject.SetActive(true);
        StartCoroutine(FlinkerStartText());
    }

    IEnumerator Loading()
    {
        float time = 0;
        TextMeshProUGUI loadingText = LoadingBar.GetComponentInChildren<TextMeshProUGUI>();
        while (time < mMaxTimeout)
        {
            float rate = time / mMaxTimeout;
            LoadingBar.value = rate;
            int percent = (int)(rate * 100.0f);
            loadingText.text = percent.ToString() + "%";
            yield return null;
            time += Time.deltaTime;
        }
        LoadingBar.value = 1.0f;
        loadingText.text = "100%";

        Ready();
    }

    public void OnTouchScreen()
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
        gameObject.SetActive(false);
        MenuStages.PopUp();
    }

    private void InitLogSystem()
    {
        LOG.IsNetworkAlive = () => { return !NetClientApp.GetInstance().IsDisconnected(); };
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
        LOG.Initialize(Application.persistentDataPath);
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

}
