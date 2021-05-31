using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ServerApp
{
    public partial class Form1 : Form
    {
        ServerModule mServer = new ServerModule();
        ServerMonitoringInfo mMonitoringInfo = new ServerMonitoringInfo();
        ConcurrentQueue<KeyValuePair<string, byte[]>> mMessages = new ConcurrentQueue<KeyValuePair<string, byte[]>>();
        ConcurrentDictionary<string, SessionUser> mUsers = new ConcurrentDictionary<string, SessionUser>();
        SessionUser mCurrentSession = null;
        string mFileLogPath = "./Log/";
        Timer mTimer = new Timer();
        Timer mTimerForPVP = new Timer();
        Timer mTimerForLog = new Timer();
        object mLockObject = new object();
        Random mRandomForBotMatching = new Random();

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                DirectoryInfo di = new DirectoryInfo(mFileLogPath);
                if (di.Exists == false)
                    di.Create();
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }

            LOG.LogStringWriterDB = (data) => { return false; };
            LOG.LogBytesWriterDB = (data) => { return false; };
            LOG.IsNetworkAlive = () => { return false; };
            LOG.LogWriterConsole = (msg) => { WriteLog(msg); };

            Utils.InitNextRan(1132, 9978);

            mTimer.Interval = 1;
            mTimer.Tick += MTimer_Tick;
            mTimer.Start();

            mTimerForPVP.Interval = NetProtocol.ServerMatchingInterval * 1000;
            mTimerForPVP.Tick += MTimerForPVP_Tick;
            mTimerForPVP.Start();

            mTimerForLog.Interval = NetProtocol.ServerMonitoringInterval * 1000; //5min
            mTimerForLog.Tick += MTimerForLog_Tick;
            mTimerForLog.Start();
        }

        private void MTimer_Tick(object sender, EventArgs e)
        {
            ProcessMessages();
        }

        private void MTimerForPVP_Tick(object sender, EventArgs e)
        {
            CleanDeadSessions();
            ProcessMathching();
            ProcessMathchingWithFriend();
        }

        private void MTimerForLog_Tick(object sender, EventArgs e)
        {
            ServerMonitoring();
        }

        private void btnOpen_Click(object sender, EventArgs e)
        {
            int port = int.Parse(tbPort.Text);

            mServer.HeaderSize = () => { return NetProtocol.HeadSize(); };
            mServer.IsValid = (byte[] data) => { return NetProtocol.IsValid(data); };
            mServer.Length = (byte[] data) => { return NetProtocol.Length(data); };
            mServer.EventRecvRow = (data, len, addr) => {
                mMonitoringInfo.networkReadBytes += len;
            };
            mServer.EventSendRow = (data, len, addr) => {
                mMonitoringInfo.networkWriteBytes += len;
            };
            mServer.EventRecvMsg = (byte[] data, string client) => {
                mMessages.Enqueue(new KeyValuePair<string, byte[]>(client, data));
                BeginInvoke(new Action(() => ProcessMessages()));
            };
            mServer.EventConnect = (endPoint) => {
                BeginInvoke(new Action(() => ConnectedClient(endPoint, true)));
            };
            mServer.EventDisConnect = (endPoint) => {
                BeginInvoke(new Action(() => ConnectedClient(endPoint, false)));
            };
            mServer.OpenServer(port);

            btnOpen.Enabled = false;
            btnClose.Enabled = true;
            listBox1.Items.Add("Open Server");
            mMonitoringInfo.Reset();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            mServer.CloseServer();

            btnOpen.Enabled = true;
            btnClose.Enabled = false;
            listBox1.Items.Add("Close Server");
        }

        private void ConnectedClient(string endPoint, bool isConnected)
        {
            if (isConnected)
            {
                if (mUsers.ContainsKey(endPoint))
                    LOG.warn();

                mUsers[endPoint] = new SessionUser(endPoint);
            }
            else
                DisconnectUser(endPoint);
        }

        private void DisconnectUser(string endPoint)
        {
            if (!mUsers.ContainsKey(endPoint))
                return;

            SessionUser user;
            mUsers.TryRemove(endPoint, out user);
            if (user.MatchState == MatchingState.Matched)
                UpdateUserPVPRecord(user, false);
            mServer.Disconnect(user.Endpoint);
        }

        private SessionUser GetUser(string endPoint)
        {
            if (!mUsers.ContainsKey(endPoint))
            {
                LOG.echo(endPoint);
                return null;
            }
            return mUsers[endPoint];
        }



        private void ProcessMessages()
        {
            KeyValuePair<string, byte[]> pack = new KeyValuePair<string, byte[]>();
            while (mMessages.TryDequeue(out pack))
            {
                byte[] body = null;
                Header requestMsg = NetProtocol.ToMessage(pack.Value, out body);

                mCurrentSession = GetUser(pack.Key);
                if (mCurrentSession == null)
                {
                    LOG.echo(pack.Key);
                    LOG.echo(requestMsg.Cmd);
                    LOG.echo(body.Length);
                    continue;
                }

                mCurrentSession.LastMessage = requestMsg;
                mCurrentSession.LastPulseTime = DateTime.Now;

                object resBody = null;
                switch (requestMsg.Cmd)
                {
                    case NetCMD.Undef: resBody = new LogInfo("Undefied Command"); break;
                    case NetCMD.HeartCheck: resBody = ProcHeartCheck(Utils.Deserialize<UserInfo>(ref body)); break;
                    case NetCMD.UpdateUser: resBody = ProcUpdateUser(Utils.Deserialize<UserInfo>(ref body)); break;
                    case NetCMD.DelUser: resBody = ProcDelUser(Utils.Deserialize<UserInfo>(ref body)); break;
                    case NetCMD.RenewScore: resBody = ProcRenewScore(Utils.Deserialize<UserInfo>(ref body)); break;
                    case NetCMD.GetScores: resBody = ProcGetUsers(); break;
                    case NetCMD.AddLog: resBody = ProcAddLog(Utils.Deserialize<LogInfo>(ref body)); break;
                    case NetCMD.AddLogFile: resBody = ProcAddLogFile(body); break;
                    case NetCMD.SearchOpponent: resBody = ProcSearchOpponent(Utils.Deserialize<SearchOpponentInfo>(ref body)); break;
                    case NetCMD.StopMatching: resBody = ProcStopMatching(Utils.Deserialize<SearchOpponentInfo>(ref body)); break;
                    case NetCMD.PVP: resBody = ProcPVPCommand(Utils.Deserialize<PVPInfo>(ref body)); break;

                    default: resBody = new LogInfo("Undefied Command"); break;
                }

                if (resBody != null)
                {
                    Header responseMsg = new Header();
                    responseMsg.Cmd = requestMsg.Cmd;
                    responseMsg.RequestID = requestMsg.RequestID;
                    responseMsg.Ack = 1;
                    responseMsg.UserPk = requestMsg.UserPk;

                    byte[] responseData = NetProtocol.ToArray(responseMsg, Utils.Serialize(resBody));
                    mServer.SendData(mCurrentSession.Endpoint, responseData);
                }
            }
        }
        private UserInfo ProcHeartCheck(UserInfo requestBody)
        {
            if (requestBody.NetworkLatency > 0)
                mCurrentSession.Pings.Add(requestBody.NetworkLatency);
            return requestBody;
        }
        private UserInfo ProcAddUser(UserInfo requestBody)
        {
            int usePk = DBManager.Inst().AddNewUser(requestBody, mCurrentSession.Endpoint);
            requestBody.userPk = usePk;
            return requestBody;
        }
        private UserInfo ProcUpdateUser(UserInfo requestBody)
        {
            UserInfo preUser = DBManager.Inst().GetUser(requestBody.deviceName);
            if (preUser == null)
            {
                int newUserPk = DBManager.Inst().AddNewUser(requestBody, mCurrentSession.Endpoint);
                requestBody.userPk = newUserPk;
            }
            else
            {
                int userPk = DBManager.Inst().UpdateUserInfo(requestBody);
                requestBody.userPk = userPk;
            }
            requestBody.rankingRate = DBManager.Inst().GetRankingRate(requestBody.score);
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
            requestBody.rankingRate = DBManager.Inst().GetRankingRate(requestBody.score);
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
        private LogInfo ProcAddLogFile(byte[] requestBody)
        {
            LogFile fileMsg = new LogFile();
            fileMsg.Deserialize(requestBody);
            string[] logs = LOG.LogFileToString(fileMsg.data);

            LogInfo info = new LogInfo();
            info.userPk = fileMsg.userPk;
            foreach (string log in logs)
            {
                info.message = log;
                DBManager.Inst().AddLog(info);
            }

            info.message = "ok";
            return info;
        }
        private SearchOpponentInfo ProcSearchOpponent(SearchOpponentInfo requestBody)
        {
            SessionUser MySession = mCurrentSession;
            if (requestBody.WithFriend != MatchingFriend.None)
            {
                if (requestBody.WithFriend == MatchingFriend.Make)
                {
                    if (MySession.RoomNumber < 0)
                        MySession.RoomNumber = Utils.NextRan();
                    MySession.WithFriend = MatchingFriend.Make;
                    requestBody.RoomNumber = MySession.RoomNumber;
                }
                else
                {
                    MySession.WithFriend = MatchingFriend.Join;
                    MySession.RoomNumber = requestBody.RoomNumber;
                }
            }
            else
            {
                if (MySession.MatchState == MatchingState.FoundOpp && requestBody.State == MatchingState.FoundOppAck)
                {
                    MySession.MatchState = MatchingState.FoundOppAck;
                }
                else
                {
                    MySession.StartSearchOpp();
                    MySession.MatchLevel = requestBody.Level;
                    MySession.UserInfo = requestBody.MyUserInfo;
                }
            }
            
            return requestBody;
        }
        private SearchOpponentInfo ProcStopMatching(SearchOpponentInfo requestBody)
        {
            mCurrentSession.ReleaseOpp();
            return requestBody;
        }
        private PVPInfo ProcPVPCommand(PVPInfo requestBody)
        {
            bool isOK = false;
            switch (requestBody.cmd)
            {
                case PVPCommand.StartGame:
                case PVPCommand.Click:
                case PVPCommand.Swipe:
                case PVPCommand.Destroy:
                case PVPCommand.Create:
                case PVPCommand.FlushAttacks:
                case PVPCommand.SkillBomb:
                case PVPCommand.SkillIce:
                case PVPCommand.SkillIceRes:
                    isOK = BypassToOppPlayer(requestBody);
                    break;
                case PVPCommand.EndGame:
                    isOK = BypassToOppPlayer(requestBody);
                    requestBody.userInfo = EndPVPGame(requestBody);
                    break;
                default:
                    isOK = BypassToOppPlayer(requestBody);
                    break;
            }
            requestBody.oppDisconnected = !isOK;
            return requestBody;
        }
        private UserInfo EndPVPGame(PVPInfo requestBody)
        {
            UserInfo renewUserInfo = UpdateUserPVPRecord(mCurrentSession, requestBody.success);
            mCurrentSession.ReleaseOpp();
            return renewUserInfo;
        }
        private bool BypassToOppPlayer(PVPInfo requestBody)
        {
            SessionUser oppSessoion = GetUser(mCurrentSession.OppEndpoint);
            if (oppSessoion == null)
                return false;

            Header responseMsg = new Header();
            responseMsg.Cmd = NetCMD.PVP;
            responseMsg.UserPk = mCurrentSession.UserInfo.userPk;

            byte[] response = NetProtocol.ToArray(responseMsg, Utils.Serialize(requestBody));
            if (mServer.SendData(oppSessoion.Endpoint, response) <= 0)
                return false;

            return true;
        }



        private void ProcessMathchingWithFriend()
        {
            foreach (var joiner in mUsers)
            {
                SessionUser joinUser = joiner.Value;
                if (joinUser.WithFriend != MatchingFriend.Join)
                    continue;

                foreach (var maker in mUsers)
                {
                    SessionUser makeUser = maker.Value;
                    if (makeUser.WithFriend != MatchingFriend.Make)
                        continue;

                    if(joinUser.RoomNumber == makeUser.RoomNumber)
                    {
                        mMonitoringInfo.pvpMatchingCount++;

                        makeUser.SetOpp(joinUser.Endpoint, joinUser.UserInfo.score);
                        joinUser.SetOpp(makeUser.Endpoint, makeUser.UserInfo.score);

                        MatchingLevel level = makeUser.MatchLevel;
                        SendMatchingInfoTo(makeUser.Endpoint, joinUser.UserInfo, level, MatchingState.Matched);
                        SendMatchingInfoTo(joinUser.Endpoint, makeUser.UserInfo, level, MatchingState.Matched);
                    }
                }
            }
        }
        private void ProcessMathching()
        {
            List<SessionUser> _waitUsers = new List<SessionUser>();
            foreach (var each in mUsers)
            {
                if (each.Value.MatchState == MatchingState.TryMatching)
                    _waitUsers.Add(each.Value);
            }

            SessionUser[] waitUsers = _waitUsers.ToArray();
            foreach (SessionUser user in waitUsers)
            {
                SessionUser opp = FindOpponent(user, waitUsers);
                if (opp != null)
                    GetReadyMatching(user, opp);
            }
        }
        async void GetReadyMatching(SessionUser userA, SessionUser userB)
        {
            userA.MatchState = MatchingState.FoundOpp;
            userB.MatchState = MatchingState.FoundOpp;

            SendMatchingInfoTo(userA.Endpoint, userB.UserInfo, userB.MatchLevel, MatchingState.FoundOpp);
            SendMatchingInfoTo(userB.Endpoint, userA.UserInfo, userA.MatchLevel, MatchingState.FoundOpp);

            await Task.Delay(NetProtocol.ServerMatchingInterval * 1000);

            if (userA.MatchState == MatchingState.FoundOppAck && userB.MatchState == MatchingState.FoundOppAck)
            {
                mMonitoringInfo.pvpMatchingCount++;

                userA.SetOpp(userB.Endpoint, userB.UserInfo.score);
                userB.SetOpp(userA.Endpoint, userA.UserInfo.score);

                SendMatchingInfoTo(userA.Endpoint, userB.UserInfo, userB.MatchLevel, MatchingState.Matched);
                SendMatchingInfoTo(userB.Endpoint, userA.UserInfo, userA.MatchLevel, MatchingState.Matched);
            }
            else
            {
                userA.MatchState = MatchingState.TryMatching;
                userB.MatchState = MatchingState.TryMatching;
            }
        }
        private void SendMatchingInfoTo(string endPoint, UserInfo opponent, MatchingLevel oppLevel, MatchingState state)
        {
            SessionUser destSession = GetUser(endPoint);
            if (destSession == null)
            {
                LOG.echo(endPoint);
                return;
            }

            SearchOpponentInfo body = new SearchOpponentInfo();
            body.MyUserInfo = destSession.UserInfo;
            body.OppUserInfo = opponent;
            body.Level = destSession.MatchLevel != MatchingLevel.All ? destSession.MatchLevel : (oppLevel != MatchingLevel.All ? oppLevel : MatchingLevel.Silver);
            body.State = state;

            Header responseMsg = new Header();
            responseMsg.Cmd = NetCMD.SearchOpponent;
            responseMsg.UserPk = destSession.UserInfo.userPk;

            byte[] response = NetProtocol.ToArray(responseMsg, Utils.Serialize(body));
            mServer.SendData(destSession.Endpoint, response);
        }
        private SessionUser FindOpponent(SessionUser me, SessionUser[] list)
        {
            if (me.UserInfo.IsBot || me.MatchState != MatchingState.TryMatching)
                return null;

            bool botSkip = me.MatchingTime() < mRandomForBotMatching.Next(100);

            foreach (SessionUser opp in list)
            {
                if (opp.MatchState != MatchingState.TryMatching)
                    continue;

                if (botSkip && opp.UserInfo.IsBot)
                    continue;

                if (me == opp)
                    continue;

                if(me.MatchLevel != MatchingLevel.All && opp.MatchLevel != MatchingLevel.All)
                {
                    if (me.MatchLevel != opp.MatchLevel)
                        continue;
                }

                float detectRange = (me.MatchingTime() + 1) * 50;
                float scoreDelta = Math.Abs(me.UserInfo.score - opp.UserInfo.score);
                if (scoreDelta < detectRange)
                    return opp;
            }
            return null;
        }


        private void WriteLog(string msg)
        {
            try
            {
                lock (mLockObject)
                {
                    string filename = DateTime.Now.ToString("yyMMdd") + ".txt";
                    StreamWriter writer = File.AppendText(mFileLogPath + filename);
                    writer.WriteLine(msg);
                    writer.Close();
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }
        private void CleanDeadSessions()
        {
            SessionUser[] users = new List<SessionUser>(mUsers.Values).ToArray();
            foreach (SessionUser user in users)
            {
                if (user.IsPulseTimeout())
                {
                    LOG.echo("DeadSession : " + user.Endpoint);
                    DisconnectUser(user.Endpoint);
                }
            }
        }
        private UserInfo UpdateUserPVPRecord(SessionUser user, bool isWin)
        {
            int myScore = user.UserInfo.score;
            int oppScore = user.OppScore;

            int deltaScore = Utils.CalcDeltaScore(isWin, myScore, oppScore, user.MatchLevel);

            user.UserInfo.score += deltaScore;
            user.UserInfo.score = Math.Max(user.UserInfo.score, 0);
            if (isWin) user.UserInfo.win++;
            if (!isWin) user.UserInfo.lose++;
            user.UserInfo.total++;
            float rankingRate = DBManager.Inst().GetRankingRate(user.UserInfo.score);
            user.UserInfo.rankingRate = rankingRate;
            DBManager.Inst().UpdateUserInfo(user.UserInfo);
            return user.UserInfo;
        }
        private void ServerMonitoring()
        {
            mMonitoringInfo.userCount = mUsers.Count;
            foreach(var user in mUsers)
            {
                int sum = 0;
                foreach (int latency in user.Value.Pings)
                    sum += latency;
                int avg = sum / user.Value.Pings.Count;
                mMonitoringInfo.userPings.Add(avg.ToString());
            }

            string msg = mMonitoringInfo.ToMessage();
            LOG.echo(msg);
            mMonitoringInfo.Reset();
        }
    }



    public class SessionUser
    {
        public string Endpoint { get; private set; }
        public string OppEndpoint { get; private set; }
        public int OppScore { get; private set; }
        public MatchingState MatchState { get; set; }
        public MatchingLevel MatchLevel { get; set; }
        public MatchingFriend WithFriend { get; set; } = MatchingFriend.None;
        public DateTime LastPulseTime { get; set; }
        public UserInfo UserInfo { get; set; }
        public Header LastMessage { get; set; }
        public DateTime MatchingStartTime { get; set; }
        public int RoomNumber { get; set; } = -1;
        public List<int> Pings = new List<int>();
        public SessionUser(string endPoint)
        {
            Endpoint = endPoint;
            OppEndpoint = "";
            MatchState = MatchingState.Idle;
            LastPulseTime = DateTime.Now;
            UserInfo = null;
            LastMessage = null;
            MatchingStartTime = DateTime.Now;
        }
        public bool IsPulseTimeout() { return (DateTime.Now - LastPulseTime).TotalSeconds > NetProtocol.DeadSessionMaxTime; }
        public void SetOpp(string endPoint, int oppScore) { OppEndpoint = endPoint; MatchState = MatchingState.Matched; OppScore = oppScore; WithFriend = MatchingFriend.None; }
        public void ReleaseOpp() { OppEndpoint = ""; MatchState = MatchingState.Idle; WithFriend = MatchingFriend.None; }
        public void StartSearchOpp() { OppEndpoint = ""; MatchState = MatchingState.TryMatching; MatchingStartTime = DateTime.Now; }
        public float MatchingTime() { return (float)(DateTime.Now - MatchingStartTime).TotalSeconds; }
    }

    public class ServerMonitoringInfo
    {
        public int networkReadBytes;
        public int networkWriteBytes;
        public int userCount;
        public int pvpMatchingCount;
        public List<string> userPings = new List<string>();
        
        public void Reset()
        {
            networkReadBytes = 0;
            networkWriteBytes = 0;
            userCount = 0;
            pvpMatchingCount = 0;
            userPings.Clear();
        }

        public string ToMessage()
        {
            string pings = String.Join<string>("/", userPings);
            //return "[read : 123] [write : 123] [user : 123] [pvp : 123] [pings : 25/15/35/25/36/47]";
            return "[read : "+networkReadBytes+ "] [write : "+networkWriteBytes+ "] [user : "+userCount+ "] [pvp : "+pvpMatchingCount+ "] [pings : "+ pings + "]";
        }
    }
}
