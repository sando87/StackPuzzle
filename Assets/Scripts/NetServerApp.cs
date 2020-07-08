using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class NetServerApp : MonoBehaviour
{
    public int ServerPort = 9435;

    NetServerModule mServer = new NetServerModule();

    // Open Server
    void Awake()
    {
        mServer.OpenServer(ServerPort);
    }

    // Close Server
    private void OnDestroy()
    {
        mServer.CloseServer();
    }

    // Check if packet is arrived every Update call
    void Update()
    {
        MySession[] infos = mServer.GetRecvPackets();
        if (infos == null)
            return;

        foreach (MySession info in infos)
        {
            byte[] retData = NetProtocol.ProcessPacket(info.data);
            info.data = retData;
            mServer.SendData(info);
        }
    }

}
