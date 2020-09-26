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
    public static NetClientApp mInst = null;
    public string ServerAddress = "localhost"; //"sjleeserver.iptime.org";
    public int ServerPort = 9435;

    const string GameObjectName = "NetClientObject";
    const int recvBufSize = 1024 * 64;

    TcpClient mSession = null;
    NetworkStream mStream = null;
    private Int64 mRequestID = 0;
    private List<byte> mRecvBuffer = new List<byte>();
    Dictionary<Int64, Action<object>> mHandlerTable = new Dictionary<Int64, Action<object>>();
    public Action<Header> EventResponse;
    
    private void OnDestroy()
    {
        DisConnect();
    }

    // Update is called once per frame
    void Update()
    {
        ReadRecvData();
        ParseAndInvokeCallback();
    }

    static public NetClientApp GetInstance()
    {
        if(mInst == null)
        {
            mInst = GameObject.Find(GameObjectName).GetComponent<NetClientApp>();
            mInst.Connect();
        }
        return mInst;
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
            head.Ack = 0;
            head.UserPk = UserSetting.UserPK;
            head.body = body;
            byte[] data = NetProtocol.ToArray(head);

            mStream.Write(data, 0, data.Length);

            if(response != null)
                mHandlerTable[head.RequestID] = response;
        }
        catch (SocketException ex) { Debug.Log(ex.Message); DisConnect(); }
        catch (Exception ex) { Debug.Log(ex.Message); DisConnect(); }
    }
    public bool IsDisconnected()
    {
        return mSession == null;
    }


    private void Connect()
    {
        try
        {
            if (mSession != null)
                return;

            mSession = new TcpClient(ServerAddress, ServerPort);
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
        mRecvBuffer.Clear();
        mHandlerTable.Clear();
    }


    private void ReadRecvData()
    {
        if (mSession == null)
            return;

        try
        {
            while (mStream.DataAvailable)
            {
                byte[] recvBuf = new byte[NetProtocol.recvBufSize];
                int readLen = mStream.Read(recvBuf, 0, recvBuf.Length);
                if (readLen <= 0)
                {
                    DisConnect();
                    return;
                }

                byte[] subBuf = new byte[readLen];
                Array.Copy(recvBuf, subBuf, readLen);
                mRecvBuffer.AddRange(subBuf);
            }
        }
        catch (SocketException ex) { Debug.Log(ex.Message); DisConnect(); }
        catch (Exception ex) { Debug.Log(ex.Message); DisConnect(); }
    }
    private void ParseAndInvokeCallback()
    {
        if (mRecvBuffer.Count == 0)
            return;

        try
        {
            List<byte[]> messages = NetProtocol.Split(mRecvBuffer.ToArray());
            foreach (byte[] msg in messages)
            {
                mRecvBuffer.RemoveRange(0, msg.Length);
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

            if(mRecvBuffer.Count != 0)
                Debug.Log("UnKnown Network Packet : " + mRecvBuffer.Count);
        }
        catch (SocketException ex) { Debug.Log(ex.Message); DisConnect(); }
        catch (Exception ex) { Debug.Log(ex.Message); DisConnect(); }

    }
    
}
