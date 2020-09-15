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
    const int recvBufSize = 1024 * 64;

    TcpClient mSession = null;
    NetworkStream mStream = null;
    private Int64 mRequestID = 0;
    Dictionary<Int64, Action<object>> mHandlerTable = new Dictionary<Int64, Action<object>>();
    public Action<Header> EventResponse;

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
    public void Request(NetCMD cmd, object body, Action<object> response)
    {
        if (mSession == null)
            return;

        try
        {
            Header head = new Header();
            head.Cmd = cmd;
            head.RequestID = mRequestID++;
            head.body = body;
            byte[] data = NetProtocol.ToArray(head);

            mStream.Write(data, 0, data.Length);

            if(response != null)
                mHandlerTable[head.RequestID] = response;
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
                byte[] recvBuf = new byte[NetProtocol.recvBufSize];
                int recvLen = mStream.Read(recvBuf, 0, recvBuf.Length);
                if (recvLen <= 0)
                {
                    DisConnect();
                    return;
                }

                byte[] temp = new byte[recvLen];
                Array.Copy(recvBuf, temp, recvLen);
                List<byte[]> messages = NetProtocol.Split(temp);
                foreach (byte[] msg in messages)
                {
                    Header recvMsg = NetProtocol.ToMessage(msg);
                    if (recvMsg == null || recvMsg.Magic != 0x12345678)
                        continue;

                    if (mHandlerTable.ContainsKey(recvMsg.RequestID))
                    {
                        mHandlerTable[recvMsg.RequestID]?.Invoke(recvMsg.body);
                        mHandlerTable.Remove(recvMsg.RequestID);
                    }

                    EventResponse?.Invoke(recvMsg);
                }
            }
        }
        catch (SocketException ex) { Debug.Log(ex.Message); DisConnect(); }
        catch (Exception ex) { Debug.Log(ex.Message); DisConnect(); }
    }
}
