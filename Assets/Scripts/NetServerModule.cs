using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class MySession
{
    public TcpClient client;
    public NetworkStream stream;
    public string ipAddr;
    public byte[] data;
};

public class NetServerModule
{
    private ConcurrentQueue<MySession> mQueue = new ConcurrentQueue<MySession>();
    private TcpListener mListener = null;


    public void OpenServer(int port)
    {
        if(mListener == null)
            new Thread(() => AcceptClient(port)).Start();
    }
    public void CloseServer()
    {
        if (mListener != null)
        {
            mListener.Stop();
            mListener = null;
        }
    }
    public MySession[] GetRecvPackets()
    {
        if (mQueue.Count <= 0)
            return null;

        List<MySession> infos = new List<MySession>();
        for (int i = 0; i < mQueue.Count; ++i)
        {
            MySession client = null;
            if (mQueue.TryDequeue(out client))
                infos.Add(client);
        }
        return infos.ToArray();
    }
    public void SendData(MySession info)
    {
        try
        {
            info.stream.Write(info.data, 0, info.data.Length);
            Debug.Log("ip[" + info.ipAddr + "], size[" + info.data.Length + "]");
        }
        catch (SocketException ex) { Debug.Log(ex.Message); DisConnectClinet(info); }
        catch (Exception ex) { Debug.Log(ex.Message); DisConnectClinet(info); }
    }


    private void AcceptClient(int port)
    {
        try
        {
            mListener = new TcpListener(IPAddress.Any, port);
            mListener.Start();

            while (true)
            {
                TcpClient tc = mListener.AcceptTcpClient();

                MySession info = new MySession();
                info.client = tc;
                info.stream = tc.GetStream();
                info.ipAddr = ((System.Net.IPEndPoint)tc.Client.RemoteEndPoint).Address.ToString();
                info.data = new byte[NetProtocol.recvBufSize];

                info.stream.BeginRead(info.data, 0, info.data.Length, new AsyncCallback(HandlerReadData), info);
            }
        }
        catch (SocketException ex) { Debug.Log(ex.Message); }
        catch (Exception ex) { Debug.Log(ex.Message); }
    }
    private void HandlerReadData(IAsyncResult ar)
    {
        MySession retInfo = (MySession)ar.AsyncState;
        try
        {
            int recvBytes = retInfo.stream.EndRead(ar);
            if (recvBytes <= 0)
            {
                DisConnectClinet(retInfo);
                return;
            }

            byte[] recvBuf = new byte[recvBytes];
            Array.Copy(retInfo.data, recvBuf, recvBytes);
            List<byte[]> messages = NetProtocol.Split(recvBuf);
            foreach(byte[] msg in messages)
            {
                MySession packet = new MySession();
                packet.client = retInfo.client;
                packet.stream = retInfo.stream;
                packet.ipAddr = retInfo.ipAddr;
                packet.data = msg;
                mQueue.Enqueue(packet);
            }

            retInfo.stream.BeginRead(retInfo.data, 0, retInfo.data.Length, new AsyncCallback(HandlerReadData), retInfo);
        }
        catch (SocketException ex) { Debug.Log(ex.Message); DisConnectClinet(retInfo); }
        catch (Exception ex) { Debug.Log(ex.Message); DisConnectClinet(retInfo); }
    }
    private void DisConnectClinet(MySession clinet)
    {
        clinet.stream.Close();
        clinet.client.Close();
        clinet.stream = null;
        clinet.client = null;
    }
}
