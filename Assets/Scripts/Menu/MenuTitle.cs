using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuTitle : MonoBehaviour
{
    private const float mMaxTimeout = 3;

    public Slider LoadingBar;
    public TextMeshProUGUI StartText;
    public GameObject StartButton;

    private void Awake()
    {
        Consolation.ConsoleInGame cig = FindObjectOfType<Consolation.ConsoleInGame>();
        if (cig != null)
            cig.Init();

        LOG.LogWriterConsole = (msg) => { Debug.Log(msg); };

        Application.targetFrameRate = 30;

        SoundPlayer.Inst.PlayBackMusic(SoundPlayer.Inst.BackMusicMap);
        NetClientApp.GetInstance().EventConnection = OnNetConnected;
        InitLogSystem();
        UserSetting.Initialize();
        Purchases.Initialize();

        StartCoroutine("Loading");
    }

    public void OnNetConnected(bool isConnected)
    {
        LOG.echo("NetConnection[" + isConnected + "]");
        if(isConnected)
            UserSetting.SyncUserInfoToDB();
        
        if(gameObject.activeInHierarchy)
        {
            StopCoroutine("Loading");
            LoadingBar.gameObject.SetActive(false);
            StartText.gameObject.SetActive(true);
            StartButton.gameObject.SetActive(true);
            StartCoroutine(FlinkerStartText());
        }
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
