using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SystemMain : MonoBehaviour
{
    private void Awake()
    {
        LOG.LogWriterConsole = (msg) => { Debug.Log(msg); };

        Application.targetFrameRate = 30;

        NetClientApp.GetInstance().Connect(3);
        UserSetting.Initialize();
        InitLogSystem();
        Purchases.Initialize();

        SoundPlayer.Inst.PlayBackMusic(SoundPlayer.Inst.BackMusicMap);
        MenuStages.PopUp();

        LOG.echo("Start StackPuzzle");
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
}
