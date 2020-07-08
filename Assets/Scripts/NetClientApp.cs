using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using UnityEngine;

public class NetClientApp : MonoBehaviour
{
    public string ServerAddress = "localhost"; //"sjleeserver.iptime.org";
    public int ServerPort = 9435;

    const string GameObjectName = "NetClientObject";
    const int recvBufSize = 1024 * 8;

    TcpClient mSession = null;
    NetworkStream mStream = null;
    Dictionary<NetCMD, _FuncRecv> mHandlerTable = new Dictionary<NetCMD, _FuncRecv>();
    public delegate void _FuncRecv(object response);

    // Start is called before the first frame update
    void Start()
    {
        if (gameObject.name != GameObjectName)
        {
            Debug.Log("Name must be => " + GameObjectName);
            return;
        }

        Connect(ServerAddress, ServerPort);
    }

    private void OnDestroy()
    {
        DisConnect();
    }

    // Update is called once per frame
    void Update()
    {
        HandleResponse();
    }

    static public NetClientApp GetInstance()
    {
        return GameObject.Find(GameObjectName).GetComponent<NetClientApp>();
    }
    public void Request(NetCMD cmd, object body, _FuncRecv response)
    {
        if (mSession == null)
            return;

        try
        {
            Header head = new Header();
            head.Cmd = cmd;
            head.body = body;
            byte[] data = NetProtocol.Serialize(head);

            mStream.Write(data, 0, data.Length);

            mHandlerTable[cmd] = response;
        }
        catch (SocketException ex) { Debug.Log(ex.Message); DisConnect(); }
        catch (Exception ex) { Debug.Log(ex.Message); DisConnect(); }
    }




    private void Connect(string ipAddr, int port)
    {
        try
        {
            if (mSession != null)
                return;

            mSession = new TcpClient(ipAddr, port);
            mStream = mSession.GetStream();
        }
        catch (SocketException ex) { Debug.Log(ex.Message); }
        catch (Exception ex) { Debug.Log(ex.Message); }
    }
    private void DisConnect()
    {
        if (mSession != null)
        {
            mStream.Close();
            mSession.Close();
            mStream = null;
            mSession = null;
        }
        mHandlerTable.Clear();
    }
    private void HandleResponse()
    {
        if (mSession == null)
            return;

        try
        {
            if (mStream.DataAvailable)
            {
                byte[] recvBuf = new byte[recvBufSize];
                int nbytes = mStream.Read(recvBuf, 0, recvBuf.Length);
                if (nbytes <= 0)
                {
                    DisConnect();
                    return;
                }

                Header recvMsg = NetProtocol.Deserialize<Header>(recvBuf);
                if (recvMsg == null || recvMsg.Magic != 0x12345678)
                    return;

                if (mHandlerTable.ContainsKey(recvMsg.Cmd))
                {
                    mHandlerTable[recvMsg.Cmd]?.Invoke(recvMsg.body);
                    mHandlerTable.Remove(recvMsg.Cmd);
                }
                    
            }
        }
        catch (SocketException ex) { Debug.Log(ex.Message); DisConnect(); }
        catch (Exception ex) { Debug.Log(ex.Message); DisConnect(); }
    }


    private void ExampleSampleCode()
    {
        int random = UnityEngine.Random.Range(0, 4);
        if (random == 0)
        {
            //신규 사용자 추가 테스트 코드
            UserInfo user = new UserInfo();
            user.userName = "hong";
            user.score = 86;
            user.deviceName = SystemInfo.deviceName;
            user.ipAddress = NetworkInterface.GetAllNetworkInterfaces()[0].GetPhysicalAddress().ToString();
            NetClientApp.GetInstance().Request(NetCMD.AddUser, user, (object response) =>
            {
                UserInfo resUser = (UserInfo)response;
                Debug.Log(resUser.userPk);
            });
        }
        else if (random == 1)
        {
            //사용자 닉네임 바꾸기 테스트 코드
            UserInfo user = new UserInfo();
            user.userPk = 3;
            user.userName = "Kang";
            user.score = 12;
            user.deviceName = SystemInfo.deviceName;
            user.ipAddress = NetworkInterface.GetAllNetworkInterfaces()[0].GetPhysicalAddress().ToString();
            NetClientApp.GetInstance().Request(NetCMD.EditUserName, user, null);
        }
        else if (random == 2)
        {
            //다수의 사용자 정보 읽어오기 테스트 코드
            NetClientApp.GetInstance().Request(NetCMD.GetScores, null, (object response) =>
            {
                UserInfo[] infos = (UserInfo[])response;
                foreach (UserInfo info in infos)
                    Debug.Log(info.userName);
            });

        }
        else if (random == 3)
        {
            //로그 메시지 추가 테스트 코드
            LogInfo log = new LogInfo();
            log.userPk = 1;
            log.message = "log test object !!";
            NetClientApp.GetInstance().Request(NetCMD.AddLog, log, null);
        }

    }
}
