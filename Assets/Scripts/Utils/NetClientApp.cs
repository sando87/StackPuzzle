using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.Events;

public class NetClientApp : MonoBehaviour
{
    public static NetClientApp mInst = null;
    public int ServerPort = 9435;

    const int recvBufSize = 1024 * 64;

    TcpClient mSession = null;
    NetworkStream mStream = null;
    private Int64 mRequestID = 0;
    private DateTime mLastRequest = DateTime.Now;
    private Action<bool> mEventConnection = null;
    private List<byte> mRecvBuffer = new List<byte>();
    Dictionary<Int64, Action<byte[]>> mHandlerTable = new Dictionary<Int64, Action<byte[]>>();

    [Serializable]
    public class UnityEventClick : UnityEvent<Header, byte[]> { }
    public UnityEventClick EventMessage = null;
    public bool IsKeepConnection { get; set; } = false;


    private string GetServerAddr()
    {
        string serverAddr = "sjleeserver.iptime.org";
        //string serverAddr = "ec2-3-35-208-197.ap-northeast-2.compute.amazonaws.com";
        if (IsAccessableDomain(serverAddr))
            return serverAddr;

        return "27.117.158.3";
        //return "3.35.208.197";
    }

    private void OnDestroy()
    {
        LOG.UnInitialize();
        DisConnect();
    }

    private void Start()
    {
        StartCoroutine(CheckHeart());
        StartCoroutine(CheckKeepSession());
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
    public bool IsDisconnected()
    {
        return mSession == null || !mSession.Connected || mStream == null;
    }
    public bool Request(NetCMD cmd, object body, Action<byte[]> response)
    {
        if (IsDisconnected())
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

            mLastRequest = DateTime.Now;
        }
        catch (SocketException ex) { LOG.warn(ex.Message); DisConnect(); return false; }
        catch (Exception ex) { LOG.warn(ex.Message); DisConnect(); return false; }
        return true;
    }

    public bool ConnectSync(int timeoutSec)
    {
        if (mSession != null)
            return true;

        try
        {
            mSession = new TcpClient();
            mSession.BeginConnect(GetServerAddr(), ServerPort, null, null);
            float st = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - st < timeoutSec)
            {
                if (mSession.Connected)
                {
                    mStream = mSession.GetStream();
                    mLastRequest = DateTime.Now;
                    LOG.echo(mSession.Client.LocalEndPoint.ToString());
                    return true;
                }
            }

            mSession.Close();
            mSession = null;
            throw new TimeoutException();
        }
        catch (SocketException ex) { LOG.warn(ex.Message); DisConnect(); }
        catch (Exception ex) { LOG.warn(ex.Message); DisConnect(); }
        return false;
    }
    public void ConnectASync(Action<bool> eventConnect, float timeout = 10)
    {
        mEventConnection = eventConnect;
        if (mSession != null)
            return;

        try
        {
            mSession = new TcpClient();
            mSession.BeginConnect(GetServerAddr(), ServerPort, null, null);
            StartCoroutine(CheckConnection(timeout));
        }
        catch (SocketException ex) { LOG.warn(ex.Message); DisConnect(); }
        catch (Exception ex) { LOG.warn(ex.Message); DisConnect(); }
    }
    private IEnumerator CheckConnection(float timeout)
    {
        float time = 0;
        while (time < timeout)
        {
            if (mSession == null)
                break;
            else if (mSession.Connected)
                break;

            yield return null;
            time += Time.deltaTime;
        }

        if (mSession != null && mSession.Connected)
        {
            mStream = mSession.GetStream();
            mLastRequest = DateTime.Now;
            LOG.echo(mSession.Client.LocalEndPoint.ToString());
            mEventConnection?.Invoke(true);
        }
        else
        {
            DisConnect();
            mEventConnection?.Invoke(false);
        }
    }
    public void DisConnect()
    {
        if (mStream != null)
        {
            LOG.echo(mSession.Client.LocalEndPoint.ToString());
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
        if (mStream == null)
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

    private IEnumerator CheckHeart()
    {
        while (true)
        {
            try
            {
                if (!IsDisconnected())
                {
                    Header head = new Header();
                    head.Cmd = NetCMD.HeartCheck;
                    head.RequestID = mRequestID++;
                    head.Ack = 0;
                    head.UserPk = UserSetting.UserPK;

                    byte[] data = NetProtocol.ToArray(head, Utils.Serialize(UserSetting.UserInfo));

                    mStream.Write(data, 0, data.Length);
                }
            }
            catch (SocketException ex) { LOG.warn(ex.Message); DisConnect(); }
            catch (Exception ex) { LOG.warn(ex.Message); DisConnect(); }

            yield return new WaitForSeconds(NetProtocol.HeartCheckInterval);
        }
    }
    private IEnumerator CheckKeepSession()
    {
        while (true)
        {
            if (!IsKeepConnection)
            {
                if (!IsDisconnected())
                {
                    if ((DateTime.Now - mLastRequest).TotalSeconds > NetProtocol.ClientSessionKeepTime)
                        DisConnect();
                }
            }
            yield return new WaitForSeconds(NetProtocol.ClientSessionKeepTime);
        }
    }

    public static bool IsAccessableDomain(string domain)
    {
        try
        {
            IPHostEntry entry = Dns.GetHostEntry(domain);
            if (entry != null && entry.AddressList.Length > 0)
                return true;
        }
        catch (SocketException ex) { LOG.warn(ex.Message); }
        catch (Exception ex) { LOG.warn(ex.Message); }
        return false;
    }

}
