using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

public class NetClientApp : MonoBehaviour
{
    const int recvBufSize = 1024 * 64;

    public static NetClientApp mInst = null;
    public int ServerPort = 9435;
    private string ServerAddress = "192.168.219.109"; //"sjleeserver.iptime.org";
    // private string ServerAddress = "ec2-13-124-106-4.ap-northeast-2.compute.amazonaws.com";

    TcpClient mSession = null;
    NetworkStream mStream = null;
    private Int64 mRequestID = 0;
    private bool mIsTryingConnect = false;
    private bool mTryConnectImmediate = false; // 바로 연결 시도
    private List<byte> mRecvBuffer = new List<byte>();
    Dictionary<Int64, Action<byte[]>> mHandlerTable = new Dictionary<Int64, Action<byte[]>>();

    [Serializable]
    public class UnityEventClick : UnityEvent<Header, byte[]> { }
    public UnityEventClick EventMessage = null;
    public Action EventConnection = null;

    public bool IsTryingConnect { get { return mIsTryingConnect; } }

    static public NetClientApp GetInstance()
    {
        if (mInst == null)
            mInst = FindObjectOfType<NetClientApp>();
        return mInst;
    }

    private void OnDestroy()
    {
        DisConnect();
    }

    private void Start()
    {
        StartCoroutine(CheckHeart());
        StartCoroutine(AutoConnection());
    }

    void Update()
    {
        if (IsDisconnected())
            return;

        ReadRecvData();
        ParseAndInvokeCallback();
    }

    public bool IsNetworkAlive { get { return Application.internetReachability != NetworkReachability.NotReachable; } }
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

        }
        catch (SocketException ex) { LOG.warn(ex.Message); DisConnect(); return false; }
        catch (Exception ex) { LOG.warn(ex.Message); DisConnect(); return false; }
        return true;
    }
    public bool Request(NetCMD cmd, ByteSerializer body, Action<byte[]> response)
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

            byte[] data = NetProtocol.ToArray(head, body.Serialize());

            mStream.Write(data, 0, data.Length);

            if (response != null)
                mHandlerTable[head.RequestID] = response;

        }
        catch (SocketException ex) { LOG.warn(ex.Message); DisConnect(); return false; }
        catch (Exception ex) { LOG.warn(ex.Message); DisConnect(); return false; }
        return true;
    }

    private async void ConnectASync(float timeout = 10)
    {
        if (mIsTryingConnect)
            return;

        mIsTryingConnect = true;
        StartCoroutine("WaitTimeout", timeout);
        mSession = new TcpClient();
        var task1 = Task.Run(() => {
            try
            {
                mSession.Connect(ServerAddress, ServerPort);
                return mSession.Connected;
            }
            catch (SocketException ex) { LOG.warn(ex.Message); }
            catch (Exception ex) { LOG.warn(ex.Message); }
            return false;
        });

        await task1;

        StopCoroutine("WaitTimeout");
        if (mSession != null && mSession.Connected)
        {
            mStream = mSession.GetStream();
            LOG.trace(mSession.Client.LocalEndPoint.ToString());
            EventConnection?.Invoke();
        }
        else
        {
            DisConnect();
        }
        mIsTryingConnect = false;
    }
    private IEnumerator WaitTimeout(float timeout)
    {
        yield return new WaitForSeconds(timeout);
        mIsTryingConnect = false;
        DisConnect();
    }
    private void DisConnect()
    {
        if (mStream != null)
        {
            LOG.trace(mSession.Client.LocalEndPoint.ToString());
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
        mIsTryingConnect = false;
        mTryConnectImmediate = false;
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
            if (IsDisconnected())
            {
                UserSetting.Latency = -1;
            }
            else
            {
                DateTime reqTime = DateTime.Now;
                Request(NetCMD.HeartCheck, UserSetting.UserInfo, (body) =>
                {
                    TimeSpan latency = DateTime.Now - reqTime;
                    UserSetting.Latency = (int)(latency.TotalSeconds * 1000);
                });
            }
            
            yield return new WaitForSeconds(NetProtocol.HeartCheckInterval);
        }
    }
    private IEnumerator AutoConnection()
    {
        while(true)
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                if (!IsDisconnected())
                    DisConnect();
            }
            else
            {
                if (IsDisconnected())
                {
                    ConnectASync();
                    yield return new WaitUntil(() => !mIsTryingConnect);
                }
            }
            
            float startTime = Time.time;
            mIsTryingConnect = false;
            yield return new WaitUntil(() => mTryConnectImmediate || Time.time - startTime > 5 * 60);
            mTryConnectImmediate = false;
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

    public void TryConnectImmediate()
    {
        if(mIsTryingConnect)
            return;

        mTryConnectImmediate = true;
    }

}
