using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ServerApp
{
    public partial class Form1 : Form
    {
        ServerModule mServer = new ServerModule();
        ConcurrentQueue<KeyValuePair<string, byte[]>> mMessages = new ConcurrentQueue<KeyValuePair<string, byte[]>>();
        Dictionary<int, MatchingInfo> mMatchingUsers = new Dictionary<int, MatchingInfo>();
        Timer mTimerMessageHandler = new Timer();
        Timer mTimerMatchingSystem = new Timer();
        Header mCurrentRequestMsg = null;
        string mCurrentEndPoint = "";

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LOG.LogWriterDB = null;
            LOG.LogWriterConsole = (msg) => { Console.WriteLine(msg); };

            mTimerMessageHandler.Interval = 20;
            mTimerMessageHandler.Tick += MTimer_Tick1;
            mTimerMessageHandler.Start();

            mTimerMatchingSystem.Interval = 2000;
            mTimerMatchingSystem.Tick += MTimer_Tick2;
            mTimerMatchingSystem.Start();
        }

        private void MTimer_Tick1(object sender, EventArgs e)
        {
            ProcessMessages();
        }
        private void MTimer_Tick2(object sender, EventArgs e)
        {
            DoMatchingSystem();
        }

        private void btnOpen_Click(object sender, EventArgs e)
        {
            int port = int.Parse(tbPort.Text);

            mServer.HeaderSize = () => { return NetProtocol.HeadSize(); };
            mServer.IsValid = (byte[] data) => { return NetProtocol.IsValid(data); };
            mServer.Length = (byte[] data) => { return NetProtocol.Length(data); };
            mServer.EventRecvRow = null;
            mServer.EventRecvMsg = (byte[] data, string client) => {
                mMessages.Enqueue(new KeyValuePair<string, byte[]>(client, data));
            };
            mServer.OpenServer(port);
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            mServer.CloseServer();
        }

        private void ProcessMessages()
        {
            KeyValuePair<string, byte[]> pack = new KeyValuePair<string, byte[]>();
            while(mMessages.TryDequeue(out pack))
            {
                byte[] body = null;
                Header requestMsg = NetProtocol.ToMessage(pack.Value, out body);

                mCurrentRequestMsg = requestMsg;
                mCurrentEndPoint = pack.Key;

                object resBody = null;
                switch (requestMsg.Cmd)
                {
                    case NetCMD.Undef: resBody = "Undefied Command"; break;
                    case NetCMD.AddUser: resBody = ProcAddUser(Utils.Deserialize<UserInfo>(ref body)); break;
                    case NetCMD.EditUserName: resBody = ProcEditUserName(Utils.Deserialize<UserInfo>(ref body)); break;
                    case NetCMD.GetUser: resBody = ProcGetUser(Utils.Deserialize<UserInfo>(ref body)); break;
                    case NetCMD.DelUser: resBody = ProcDelUser(Utils.Deserialize<UserInfo>(ref body)); break;
                    case NetCMD.RenewScore: resBody = ProcRenewScore(Utils.Deserialize<UserInfo>(ref body)); break;
                    case NetCMD.GetScores: resBody = ProcGetUsers(); break;
                    case NetCMD.AddLog: resBody = ProcAddLog(Utils.Deserialize<LogInfo>(ref body)); break;
                    case NetCMD.SearchOpponent: resBody = ProcSearchOpponent(Utils.Deserialize<SearchOpponentInfo>(ref body)); break;
                    case NetCMD.StopMatching: resBody = ProcStopMatching(Utils.Deserialize<SearchOpponentInfo>(ref body)); break;
                    case NetCMD.PVP: resBody = ProcPVPCommand(Utils.Deserialize<PVPInfo>(ref body)); break;
                    default: resBody = "Undefied Command"; break;
                }

                if (resBody != null)
                {
                    Header responseMsg = new Header();
                    responseMsg.Cmd = requestMsg.Cmd;
                    responseMsg.RequestID = requestMsg.RequestID;
                    responseMsg.Ack = 1;
                    responseMsg.UserPk = requestMsg.UserPk;

                    byte[] responseData = NetProtocol.ToArray(responseMsg, Utils.Serialize(resBody));
                    mServer.SendData(mCurrentEndPoint, responseData);
                }
            }
        }




        private UserInfo ProcAddUser(UserInfo requestBody)
        {
            int usePk = DBManager.Inst().AddNewUser(requestBody, mCurrentEndPoint);
            requestBody.userPk = usePk;
            return requestBody;
        }
        private UserInfo ProcEditUserName(UserInfo requestBody)
        {
            DBManager.Inst().EditUserName(requestBody);
            return requestBody;
        }
        private UserInfo ProcDelUser(UserInfo requestBody)
        {
            DBManager.Inst().DeleteUser(requestBody.userPk);
            return requestBody;
        }
        private UserInfo ProcGetUser(UserInfo requestBody)
        {
            requestBody = DBManager.Inst().GetUser(requestBody.userPk);
            return requestBody;
        }
        private UserInfo ProcRenewScore(UserInfo requestBody)
        {
            DBManager.Inst().RenewUserScore(requestBody.userPk, requestBody.score);
            return requestBody;
        }
        private UserInfo[] ProcGetUsers()
        {
            UserInfo[] users = DBManager.Inst().GetUsers();
            return users;
        }
        private LogInfo ProcAddLog(LogInfo requestBody)
        {
            DBManager.Inst().AddLog(requestBody);
            return requestBody;
        }

        private SearchOpponentInfo ProcSearchOpponent(SearchOpponentInfo requestBody)
        {
            MatchingInfo info = new MatchingInfo();
            info.isPlaying = false;
            info.userPK = requestBody.userPk;
            info.colorCount = requestBody.colorCount;
            info.userInfo = DBManager.Inst().GetUser(requestBody.userPk);
            info.oppUserPK = -1;
            info.isBotPlayer = requestBody.isBotPlayer;
            info.endPoint = mCurrentEndPoint;
            info.startTick = DateTime.Now.Ticks;
            info.requestID = mCurrentRequestMsg.RequestID;
            mMatchingUsers[requestBody.userPk] = info;
            return null;
        }
        private SearchOpponentInfo ProcStopMatching(SearchOpponentInfo requestBody)
        {
            mMatchingUsers.Remove(requestBody.userPk);
            return requestBody;
        }
        private PVPInfo ProcPVPCommand(PVPInfo requestBody)
        {
            switch (requestBody.cmd)
            {
                case PVPCommand.StartGame:
                case PVPCommand.Click:
                case PVPCommand.Swipe:
                case PVPCommand.Destroy:
                case PVPCommand.Create:
                case PVPCommand.FlushAttacks:
                    BypassToOppPlayer(requestBody);
                    break;
                case PVPCommand.EndGame:
                    EndPVPGame(requestBody);
                    BypassToOppPlayer(requestBody);
                    break;
            }
            return requestBody;
        }
        private void BypassToOppPlayer(PVPInfo requestBody)
        {
            if (mMatchingUsers.ContainsKey(requestBody.oppUserPk))
            {
                string oppEndPoint = mMatchingUsers[requestBody.oppUserPk].endPoint;

                Header responseMsg = new Header();
                responseMsg.Cmd = NetCMD.PVP;
                responseMsg.RequestID = -1;
                responseMsg.Ack = 0;
                responseMsg.UserPk = mCurrentRequestMsg.UserPk;

                byte[] response = NetProtocol.ToArray(responseMsg, Utils.Serialize(requestBody));
                mServer.SendData(oppEndPoint, response);
            }
        }
        private void EndPVPGame(PVPInfo requestBody)
        {
            mMatchingUsers.Remove(requestBody.userInfo.userPk);
            DBManager.Inst().UpdateUserInfo(requestBody.userInfo);
        }

        private void DoMatchingSystem()
        {
            var list = mMatchingUsers.ToArray();
            foreach (var each in list)
            {
                int userPk = each.Key;
                MatchingInfo user = each.Value;
                if (user.isPlaying || user.isBotPlayer)
                    continue;

                if (!mMatchingUsers.ContainsKey(userPk))
                    continue;

                float waitSec = (float) new TimeSpan(DateTime.Now.Ticks - user.startTick).TotalSeconds;
                if (waitSec > 20)
                {
                    SendOppoentInfo(user, null);
                    mMatchingUsers.Remove(userPk);
                    continue;
                }

                float detectRange = waitSec < 18 ? waitSec * 30.0f : 10000.0f;
                List<MatchingInfo> tmpList = new List<MatchingInfo>();
                foreach (var target in mMatchingUsers)
                {
                    MatchingInfo opp = target.Value;
                    if (user.isBotPlayer && opp.isBotPlayer)
                        continue;

                    if (opp.userPK != user.userPK && !opp.isPlaying)
                        if (Math.Abs(user.userInfo.score - opp.userInfo.score) < detectRange)
                            tmpList.Add(opp);
                }


                tmpList.Sort((lhs, rhs) => {
                    return Math.Abs(user.colorCount - lhs.colorCount) > Math.Abs(user.colorCount - rhs.colorCount) ? 1 : -1;
                });

                if (tmpList.Count > 0)
                {
                    MatchingInfo opp = tmpList[0];
                    user.isPlaying = true;
                    opp.isPlaying = true;
                    SendOppoentInfo(user, opp);
                    SendOppoentInfo(opp, user);
                    break;
                }
            }
        }
        private void SendOppoentInfo(MatchingInfo user, MatchingInfo opponent)
        {
            string userEndPoint = user.endPoint;

            SearchOpponentInfo body = new SearchOpponentInfo();
            body.userPk = user.userPK;
            body.colorCount = user.colorCount;
            body.oppUser = opponent == null ? new UserInfo() : opponent.userInfo;
            body.oppColorCount = opponent == null ? -1 : opponent.colorCount;
            body.isDone = true;
            body.isBotPlayer = user.isBotPlayer;

            Header responseMsg = new Header();
            responseMsg.Cmd = NetCMD.SearchOpponent;
            responseMsg.RequestID = user.requestID;
            responseMsg.Ack = 1;
            responseMsg.UserPk = user.userPK;

            byte[] response = NetProtocol.ToArray(responseMsg, Utils.Serialize(body));
            mServer.SendData(userEndPoint, response);
        }
    }

    public class MatchingInfo
    {
        public bool isPlaying = false;
        public int userPK = 0;
        public UserInfo userInfo = null;
        public int oppUserPK = 0;
        public float colorCount = 0;
        public bool isBotPlayer = false;
        public string endPoint = "";
        public long startTick = 0;
        public Int64 requestID = -1;
    }
}
