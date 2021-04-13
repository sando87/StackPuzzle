using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServerApp
{
    public class ClientSession
    {
        private TcpClient tcpClient { get; set; }
        public NetworkStream streamReader { get; private set; }
        public NetworkStream streamWriter { get; private set; }
        public string endPoint { get; private set; }
        public RingBuffer ringBuffer { get; private set; }
        public byte[] recvBuffer { get; private set; }
        public ClientSession(TcpClient _tcpClient)
        {
            tcpClient = _tcpClient;
            streamReader = _tcpClient.GetStream();
            streamWriter = _tcpClient.GetStream();
            IPEndPoint ep = (System.Net.IPEndPoint)_tcpClient.Client.RemoteEndPoint;
            endPoint = ep.Address.ToString() + ":" + ep.Port;
            ringBuffer = new RingBuffer();
            recvBuffer = new byte[ServerModule.RECV_BUFSIZE];
            LOG.echo("Connect " + endPoint);
        }
        public void Disconnect()
        {
            streamWriter.Close();
            streamReader.Close();
            tcpClient.Close();
            LOG.echo("Disconnect " + endPoint);
        }
    }
    class ServerModule
    {
        public const int RECV_BUFSIZE = 8 * 1024;

        private TcpListener mListener = null;
        private ConcurrentDictionary<string, ClientSession> mClients = new ConcurrentDictionary<string, ClientSession>();

        public Func<byte[], bool> IsValid;
        public Func<byte[], int> Length;
        public Func<int> HeaderSize;
        public Action<byte[], int, string> EventRecvRow;
        public Action<byte[], string> EventRecvMsg;
        public Action<string> EventConnect;
        public Action<string> EventDisConnect;

        public void OpenServer(int port)
        {
            if (IsValid == null)
                IsValid = (d) => { return true; };

            if (mListener == null)
            {
                LOG.echo("Open Server");
                new Thread(() => AcceptClient(port)).Start();
            }
                
        }
        public void CloseServer()
        {
            string[] endpoints = new List<string>(mClients.Keys).ToArray();
            foreach (string ep in endpoints)
                Disconnect(ep);
            mClients.Clear();

            if (mListener != null)
            {
                mListener.Stop();
                mListener = null;
                LOG.echo("Close Server");
            }
        }
        public int SendData(string endPoint, byte[] data)
        {
            try
            {
                ClientSession info = mClients[endPoint];
                info.streamWriter.Write(data, 0, data.Length);
                info.streamWriter.Flush();
                return data.Length;
            }
            catch (SocketException ex) { LOG.warn(ex.Message); Disconnect(endPoint); }
            catch (Exception ex) { LOG.warn(ex.Message); Disconnect(endPoint); }
            return 0;
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
                    ClientSession info = new ClientSession(tc);
                    if (mClients.ContainsKey(info.endPoint))
                        LOG.warn();

                    mClients[info.endPoint] = info;
                    EventConnect?.Invoke(info.endPoint);
                    info.streamReader.BeginRead(info.recvBuffer, 0, info.recvBuffer.Length, new AsyncCallback(HandlerReadData), info);
                }
            }
            catch (SocketException ex) { LOG.warn(ex.Message); }
            catch (Exception ex) { LOG.warn(ex.Message); }
        }
        private void HandlerReadData(IAsyncResult ar)
        {
            ClientSession retInfo = (ClientSession)ar.AsyncState;
            try
            {
                int recvBytes = retInfo.streamReader.EndRead(ar);
                if (recvBytes <= 0)
                {
                    Disconnect(retInfo.endPoint);
                    return;
                }

                EventRecvRow?.Invoke(retInfo.recvBuffer, recvBytes, retInfo.endPoint);

                retInfo.ringBuffer.Push(retInfo.recvBuffer, recvBytes);

                List<byte[]> messages = ParseToMessages(retInfo.ringBuffer);
                foreach (byte[] message in messages)
                    EventRecvMsg?.Invoke(message, retInfo.endPoint);

                retInfo.streamReader.BeginRead(retInfo.recvBuffer, 0, retInfo.recvBuffer.Length, new AsyncCallback(HandlerReadData), retInfo);
            }
            catch (SocketException ex) { LOG.warn(ex.Message); Disconnect(retInfo.endPoint); }
            catch (Exception ex) { LOG.warn(ex.Message); Disconnect(retInfo.endPoint); }
        }
        public void Disconnect(string endPoint)
        {
            if (!mClients.ContainsKey(endPoint))
                return;

            ClientSession info;
            mClients.TryRemove(endPoint, out info);
            EventDisConnect?.Invoke(info.endPoint);
            info.Disconnect();
        }
        private List<byte[]> ParseToMessages(RingBuffer ringBuffer)
        {
            List<byte[]> messages = new List<byte[]>();

            int headerSize = HeaderSize();
            if (ringBuffer.Size < headerSize)
                return messages;

            while (true)
            {
                byte[] head = ringBuffer.readSize(headerSize);
                if (head == null)
                    break;

                if (!IsValid.Invoke(head))
                {
                    LOG.warn("ParseToMessages Error");
                    ringBuffer.Clear();
                    break;
                }

                int len = Length.Invoke(head);
                byte[] msg = ringBuffer.Pop(len);
                if (msg == null)
                    break;

                messages.Add(msg);
            }

            return messages;
        }
    }
}
