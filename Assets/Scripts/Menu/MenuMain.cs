using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuMain : MonoBehaviour
{
    private const string UIObjName = "MenuMain";

    private MenuMessageBox mMenu = null;

    private void Awake()
    {
        LOG.LogWriterConsole = (msg) => { Debug.Log(msg); };

        Application.targetFrameRate = 30;

        NetClientApp.GetInstance().Connect(3);
        UserSetting.Initialize();
        InitLogSystem();
        Purchases.Initialize();

        LOG.echo("Start StackPuzzle");
    }

    private void Update()
    {
        QuitProgram();
    }

    public static void PopUp()
    {
        GameObject.Find("UIGroup").transform.Find(UIObjName).gameObject.SetActive(true);
        GameObject.Find("WorldSpace").transform.Find("MainScreen").gameObject.SetActive(true);
    }

    public void PlayStageMode()
    {
        gameObject.SetActive(false);
        GameObject.Find("WorldSpace").transform.Find("MainScreen").gameObject.SetActive(false);

        MenuStages.PopUp();

        SoundPlayer.Inst.PlayBackMusic(SoundPlayer.Inst.BackMusicMap);
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
    }
    public void PlayBattleMode()
    {
        gameObject.SetActive(false);
        GameObject.Find("WorldSpace").transform.Find("MainScreen").gameObject.SetActive(false);

        MenuWaitMatch.PopUp();

        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
    }

    private void InitLogSystem()
    {
        LOG.IsNetworkAlive = () => { return !NetClientApp.GetInstance().IsDisconnected(); };
        LOG.LogWriterDB = (msg) => {
            LogInfo info = new LogInfo();
            info.userPk = UserSetting.UserPK;
            info.message = msg;
            return NetClientApp.GetInstance().Request(NetCMD.AddLog, info, null);
        };
        LOG.Initialize(Application.persistentDataPath);
    }

    private void QuitProgram()
    {
#if PLATFORM_ANDROID
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (mMenu == null)
            {
                mMenu = MenuMessageBox.PopUp("Quit??", true, (bool isOK) =>
                {
                    if (isOK)
                        Application.Quit();
                    else
                        mMenu = null;
                });
            }
            else
            {
                Application.Quit();
            }
        }
#endif
    }
}
