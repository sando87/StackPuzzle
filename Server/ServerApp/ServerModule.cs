using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServerApp
{
    class ClientInfo
    {
        public TcpClient tcpClient = null;
        public string endPoint = "";
        public RingBuffer ringBuffer = null;
        public byte[] recvBuffer = null;
    }
    class ServerModule
    {
        private const int RECV_BUFSIZE = 8 * 1024;

        private TcpListener mListener = null;
        private ConcurrentDictionary<string, ClientInfo> mClients = new ConcurrentDictionary<string, ClientInfo>();

        public Func<byte[], bool> IsValid;
        public Func<byte[], int> Length;
        public Func<int> HeaderSize;
        public Action<byte[], int, string> EventRecvRow;
        public Action<byte[], string> EventRecvMsg;

        public void OpenServer(int port)
        {
            if (IsValid == null)
                IsValid = (d) => { return true; };

            if (mListener == null)
                new Thread(() => AcceptClient(port)).Start();
        }
        public void CloseServer()
        {
            foreach (var each in mClients)
                each.Value.tcpClient.Close();
            mClients.Clear();

            if (mListener != null)
            {
                mListener.Stop();
                mListener = null;
            }
        }
        public int SendData(string endPoint, byte[] data)
        {
            if (!mClients.ContainsKey(endPoint))
                return 0;

            try
            {
                ClientInfo info = mClients[endPoint];
                NetworkStream stream = info.tcpClient.GetStream();
                stream.Write(data, 0, data.Length);
                stream.Close();
                return data.Length;
            }
            catch (SocketException ex) { LOG.warn(ex.Message); DisConnectClinet(endPoint); }
            catch (Exception ex) { LOG.warn(ex.Message); DisConnectClinet(endPoint); }
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

                    ClientInfo info = new ClientInfo();
                    info.tcpClient = tc;
                    info.endPoint = ((System.Net.IPEndPoint)tc.Client.RemoteEndPoint).Address.ToString();
                    info.ringBuffer = new RingBuffer();
                    info.recvBuffer = new byte[RECV_BUFSIZE];
                    mClients[info.endPoint] = info;

                    NetworkStream stream = tc.GetStream();
                    stream.BeginRead(info.recvBuffer, 0, info.recvBuffer.Length, new AsyncCallback(HandlerReadData), info);
                    stream.Close();
                }
            }
            catch (SocketException ex) { LOG.warn(ex.Message); }
            catch (Exception ex) { LOG.warn(ex.Message); }
        }
        private void HandlerReadData(IAsyncResult ar)
        {
            ClientInfo retInfo = (ClientInfo)ar.AsyncState;
            try
            {
                NetworkStream stream = retInfo.tcpClient.GetStream();
                int recvBytes = stream.EndRead(ar);
                if (recvBytes <= 0)
                {
                    DisConnectClinet(retInfo.endPoint);
                    return;
                }

                EventRecvRow?.Invoke(retInfo.recvBuffer, recvBytes, retInfo.endPoint);

                retInfo.ringBuffer.Push(retInfo.recvBuffer, recvBytes);

                List<byte[]> messages = ParseToMessages(retInfo.ringBuffer);
                foreach (byte[] message in messages)
                    EventRecvMsg?.Invoke(message, retInfo.endPoint);

                stream.BeginRead(retInfo.recvBuffer, 0, retInfo.recvBuffer.Length, new AsyncCallback(HandlerReadData), retInfo);
                stream.Close();
            }
            catch (SocketException ex) { LOG.warn(ex.Message); DisConnectClinet(retInfo.endPoint); }
            catch (Exception ex) { LOG.warn(ex.Message); DisConnectClinet(retInfo.endPoint); }
        }
        private void DisConnectClinet(string endPoint)
        {
            if (!mClients.ContainsKey(endPoint))
                return;

            ClientInfo info;
            mClients.TryRemove(endPoint, out info);
            info = mClients[endPoint];
            info.tcpClient.Close();
            LOG.warn("Disconnect " + endPoint);
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
