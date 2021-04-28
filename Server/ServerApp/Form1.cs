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
        ConcurrentQueue<KeyValuePair<string, byte[]>> mMessages = new ConcurrentQueue<KeyValuePair<string, byte[]>>();
        ConcurrentDictionary<string, SessionUser> mUsers = new ConcurrentDictionary<string, SessionUser>();
        SessionUser mCurrentSession = null;
        string mFileLogPath = "./Log/";
        Timer mTimer = new Timer();
        object mLockObject = new object();

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

            LOG.LogWriterDB = null;
            LOG.IsNetworkAlive = () => { return false; };
            LOG.LogWriterConsole = (msg) => { WriteLog(msg); };

            mTimer.Interval = NetProtocol.ServerMatchingInterval;
            mTimer.Tick += MTimer_Tick;
            mTimer.Start();
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

        private void MTimer_Tick(object sender, EventArgs e)
        {
            CleanDeadSessions();
            ProcessMessages();
            ProcessMathching();
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
                    case NetCMD.HeartCheck: resBody = Utils.Deserialize<UserInfo>(ref body); break;
                    case NetCMD.AddUser: resBody = ProcAddUser(Utils.Deserialize<UserInfo>(ref body)); break;
                    case NetCMD.UpdateUserInfo: resBody = ProcUpdateUser(Utils.Deserialize<UserInfo>(ref body)); break;
                    case NetCMD.EditUserName: resBody = ProcEditUserName(Utils.Deserialize<UserInfo>(ref body)); break;
                    case NetCMD.GetUser: resBody = ProcGetUser(Utils.Deserialize<UserInfo>(ref body)); break;
                    case NetCMD.DelUser: resBody = ProcDelUser(Utils.Deserialize<UserInfo>(ref body)); break;
                    case NetCMD.RenewScore: resBody = ProcRenewScore(Utils.Deserialize<UserInfo>(ref body)); break;
                    case NetCMD.GetScores: resBody = ProcGetUsers(); break;
                    case NetCMD.AddLog: resBody = ProcAddLog(Utils.Deserialize<LogInfo>(ref body)); break;
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
        private UserInfo ProcAddUser(UserInfo requestBody)
        {
            int usePk = DBManager.Inst().AddNewUser(requestBody, mCurrentSession.Endpoint);
            requestBody.userPk = usePk;
            return requestBody;
        }
        private UserInfo ProcUpdateUser(UserInfo requestBody)
        {
            DBManager.Inst().UpdateUserInfo(requestBody);
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
        private SearchOpponentInfo ProcSearchOpponent(SearchOpponentInfo requestBody)
        {
            SessionUser MySession = mCurrentSession;
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

            SendMatchingInfoTo(userA.Endpoint, userB.UserInfo, MatchingState.FoundOpp);
            SendMatchingInfoTo(userB.Endpoint, userA.UserInfo, MatchingState.FoundOpp);

            await Task.Delay(NetProtocol.ServerMatchingInterval + 1000);

            if (userA.MatchState == MatchingState.FoundOppAck && userB.MatchState == MatchingState.FoundOppAck)
            {
                userA.SetOpp(userB.Endpoint, userB.UserInfo.score);
                userB.SetOpp(userA.Endpoint, userA.UserInfo.score);

                SendMatchingInfoTo(userA.Endpoint, userB.UserInfo, MatchingState.Matched);
                SendMatchingInfoTo(userB.Endpoint, userA.UserInfo, MatchingState.Matched);
            }
            else
            {
                userA.MatchState = MatchingState.TryMatching;
                userB.MatchState = MatchingState.TryMatching;
            }
        }
        private void SendMatchingInfoTo(string endPoint, UserInfo opponent, MatchingState state)
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
            body.State = state;

            Header responseMsg = new Header();
            responseMsg.Cmd = NetCMD.SearchOpponent;
            responseMsg.UserPk = destSession.UserInfo.userPk;

            byte[] response = NetProtocol.ToArray(responseMsg, Utils.Serialize(body));
            mServer.SendData(destSession.Endpoint, response);
        }
        private SessionUser FindOpponent(SessionUser me, SessionUser[] list)
        {
            if (me.MatchState != MatchingState.TryMatching)
                return null;

            foreach (SessionUser opp in list)
            {
                if (opp.MatchState != MatchingState.TryMatching)
                    continue;

                if (me.UserInfo.IsBot && opp.UserInfo.IsBot)
                    continue;

                if (me == opp)
                    continue;

                if (me.MatchLevel != opp.MatchLevel)
                    continue;

                float scoreDelta = Math.Abs(me.UserInfo.score - opp.UserInfo.score);
                float deltaMyScore = (me.MatchingTime() + 1) * 50;
                float deltaOppScore = (opp.MatchingTime() + 1) * 50;
                if (scoreDelta < deltaMyScore && scoreDelta < deltaOppScore)
                    return opp;
            }
            return null;
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

            int deltaScore = Utils.CalcDeltaScore(isWin, myScore, oppScore);

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

    }



    public class SessionUser
    {
        public string Endpoint { get; private set; }
        public string OppEndpoint { get; private set; }
        public int OppScore { get; private set; }
        public MatchingState MatchState { get; set; }
        public MatchingLevel MatchLevel { get; set; }
        public DateTime LastPulseTime { get; set; }
        public UserInfo UserInfo { get; set; }
        public Header LastMessage { get; set; }
        public DateTime MatchingStartTime { get; set; }
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
        public void SetOpp(string endPoint, int oppScore) { OppEndpoint = endPoint; MatchState = MatchingState.Matched; OppScore = oppScore; }
        public void ReleaseOpp() { OppEndpoint = ""; MatchState = MatchingState.Idle; }
        public void StartSearchOpp() { OppEndpoint = ""; MatchState = MatchingState.TryMatching; MatchingStartTime = DateTime.Now; }
        public float MatchingTime() { return (float)(DateTime.Now - MatchingStartTime).TotalSeconds; }
    }
}
