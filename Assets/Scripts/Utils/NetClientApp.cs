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
    private string ServerAddress = "sjleeserver.iptime.org"; //27.117.158.3
    //private string ServerAddress = "ec2-3-35-208-197.ap-northeast-2.compute.amazonaws.com"; //3.35.208.197

    TcpClient mSession = null;
    NetworkStream mStream = null;
    private Int64 mRequestID = 0;
    private bool mIsTryingConnect = false;
    private List<byte> mRecvBuffer = new List<byte>();
    Dictionary<Int64, Action<byte[]>> mHandlerTable = new Dictionary<Int64, Action<byte[]>>();

    [Serializable]
    public class UnityEventClick : UnityEvent<Header, byte[]> { }
    public UnityEventClick EventMessage = null;
    public Action EventConnection = null;


    static public NetClientApp GetInstance()
    {
        if (mInst == null)
            mInst = FindObjectOfType<NetClientApp>();
        return mInst;
    }

    private void OnDestroy()
    {
        LOG.UnInitialize();
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
            LOG.echo(mSession.Client.LocalEndPoint.ToString());
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
        DisConnect();
    }
    private void DisConnect()
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
                    ConnectASync();
            }
            
            yield return new WaitForSeconds(1);
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
