using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

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
    Dictionary<Int64, Action<byte[]>> mHandlerTable = new Dictionary<Int64, Action<byte[]>>();

    [Serializable]
    public class UnityEventClick : UnityEvent<Header, byte[]> { }
    public UnityEventClick EventMessage = null;

    private void OnDestroy()
    {
        LOG.UnInitialize();
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
        if (mInst == null)
            mInst = FindObjectOfType<NetClientApp>();
        return mInst;
    }
    public bool Request(NetCMD cmd, object body, Action<byte[]> response)
    {
        if (mSession == null)
            return false;

        try
        {
            Header head = new Header();
            head.Cmd = cmd;
            head.RequestID = mRequestID++;
            head.Ack = 0;
            head.UserPk = UserSetting.UserPK;

            byte[] data = NetProtocol.ToArray(head, Utils.Serialize(body));

            mStream.Write(data, 0, data.Length);

            if (response != null)
                mHandlerTable[head.RequestID] = response;
        }
        catch (SocketException ex) { LOG.warn(ex.Message); DisConnect(); return false; }
        catch (Exception ex) { LOG.warn(ex.Message); DisConnect(); return false; }
        return true;
    }
    public bool IsDisconnected()
    {
        return mSession == null;
    }
    public bool HeartCheck()
    {
        bool ret = Request(NetCMD.HeartCheck, UserSetting.UserInfo, (_body) =>
        {
            UserInfo res = Utils.Deserialize<UserInfo>(ref _body);
            if (res.userPk == UserSetting.UserPK)
                return;
        });
        return ret;
    }

    public bool ConnectSync(int timeoutSec)
    {
        if (mSession != null)
            return true;

        try
        {
            TcpClient session = new TcpClient();
            session.BeginConnect(ServerAddress, ServerPort, null, null);
            float st = Time.realtimeSinceStartup;
            while(Time.realtimeSinceStartup - st < timeoutSec)
            {
                if (session.Connected)
                {
                    mSession = session;
                    mStream = session.GetStream();
                    return true;
                }
            }

            throw new TimeoutException();
        }
        catch (SocketException ex) { LOG.warn(ex.Message); DisConnect(); }
        catch (Exception ex) { LOG.warn(ex.Message); DisConnect(); }
        return false;
    }
    public void ConnectASync(Action<bool> eventConnect)
    {
        if (mSession != null)
            return;

        try
        {
            TcpClient session = new TcpClient();
            session.BeginConnect(ServerAddress, ServerPort, null, null);
            StartCoroutine(CheckConnection(session, 10, eventConnect));
        }
        catch (SocketException ex) { LOG.warn(ex.Message); DisConnect(); }
        catch (Exception ex) { LOG.warn(ex.Message); DisConnect(); }
    }
    private IEnumerator CheckConnection(TcpClient tcpClient, float timeout, Action<bool> eventResult)
    {
        float time = 0;
        while (time < timeout)
        {
            try
            {
                if (tcpClient.Connected)
                {
                    mSession = tcpClient;
                    mStream = tcpClient.GetStream();
                    break;
                }
            }
            catch (SocketException ex) { LOG.warn(ex.Message); DisConnect(); break; }
            catch (Exception ex) { LOG.warn(ex.Message); DisConnect(); break; }

            yield return null;
            time += Time.deltaTime;
        }

        if (mSession == null)
            eventResult?.Invoke(false);
        else
            eventResult?.Invoke(true);
    }
    public void DisConnect()
    {
        if(mStream != null)
        {
            mStream.Close();
            mStream = null;
        }
        if (mSession != null)
        {
            mSession.Close();
            mSession = null;
        }
        mRecvBuffer.Clear();
        mHandlerTable.Clear();
        mRequestID = 0;
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
        catch (SocketException ex) { LOG.warn(ex.Message); DisConnect(); }
        catch (Exception ex) { LOG.warn(ex.Message); DisConnect(); }
    }
    private void ParseAndInvokeCallback()
    {
        if (mRecvBuffer.Count == 0)
            return;

        try
        {
            byte[] recvBuf = mRecvBuffer.ToArray();
            if (!NetProtocol.IsValid(recvBuf))
            {
                LOG.warn("Invalid data detected : Clear RecvBuffer");
                mRecvBuffer.Clear();
                return;
            }

            List<byte[]> messages = NetProtocol.SplitBuffer(recvBuf);
            foreach (byte[] msg in messages)
            {
                mRecvBuffer.RemoveRange(0, msg.Length);
                byte[] resBody = null;
                Header recvMsg = NetProtocol.ToMessage(msg, out resBody);
                if (recvMsg == null || recvMsg.Magic != 0x12345678)
                    continue;

                if (mHandlerTable.ContainsKey(recvMsg.RequestID))
                {
                    mHandlerTable[recvMsg.RequestID]?.Invoke(resBody);
                    mHandlerTable.Remove(recvMsg.RequestID);
                }

                EventMessage?.Invoke(recvMsg, resBody);
            }
        }
        catch (SocketException ex) { LOG.warn(ex.Message); DisConnect(); }
        catch (Exception ex) { LOG.warn(ex.Message); DisConnect(); }

    }
    
}
