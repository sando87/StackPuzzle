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
        string mCurrentEndPoint = "";
        string mFileLogPath = "./Log/";
        Timer mTimer = new Timer();

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
            catch (Exception ex) { listBox1.Items.Add(ex.Message); }

            LOG.LogWriterDB = null;
            LOG.IsNetworkAlive = () => { return false; };
            LOG.LogWriterConsole = (msg) => { WriteLog(msg); };

            mTimer.Interval = 5000;
            mTimer.Tick += MTimer_Tick;
            mTimer.Start();
        }

        private void WriteLog(string msg)
        {
            try
            {
                string filename = DateTime.Now.ToString("yyMMdd") + ".txt";
                StreamWriter writer = File.AppendText(mFileLogPath + filename);
                writer.WriteLine(msg);
                writer.Close();
            }
            catch (Exception ex) { listBox1.Items.Add(ex.Message); }
        }

        private void MTimer_Tick(object sender, EventArgs e)
        {
            ProcessMessages();
            //CleanDeadSessions();
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
            if(isConnected)
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
            if(user.OppEndpoint.Length > 0)
            {
                InformWinTo(mUsers[user.OppEndpoint], user);
            }
            mServer.Disconnect(user.Endpoint);
        }

        private void CleanDeadSessions()
        {
            SessionUser[] users = new List<SessionUser>(mUsers.Values).ToArray();
            foreach(SessionUser user in users)
            {
                if (user.UserState == UserState.Matched && user.IsPulseTimeout())
                {
                    LOG.echo("DeadSession : " + user.Endpoint);
                    DisconnectUser(user.Endpoint);
                }
            }
        }

        private void ProcessMessages()
        {
            try
            {
                KeyValuePair<string, byte[]> pack = new KeyValuePair<string, byte[]>();
                while (mMessages.TryDequeue(out pack))
                {
                    byte[] body = null;
                    Header requestMsg = NetProtocol.ToMessage(pack.Value, out body);

                    mCurrentEndPoint = pack.Key;
                    mUsers[mCurrentEndPoint].LastMessage = requestMsg;
                    mUsers[mCurrentEndPoint].LastPulseTime = DateTime.Now;

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
                        mServer.SendData(mCurrentEndPoint, responseData);
                    }
                }
            }
            catch(Exception ex)
            {
                LOG.warn(ex.Message);
            }
        }




        private UserInfo ProcAddUser(UserInfo requestBody)
        {
            int usePk = DBManager.Inst().AddNewUser(requestBody, mCurrentEndPoint);
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
            SessionUser MySession = mUsers[mCurrentEndPoint];
            MySession.UserState = UserState.Matching;
            MySession.UserInfo = requestBody.MyUserInfo;
            SessionUser OppSession = FindOpponent(requestBody.DeltaScore);
            if (OppSession != null)
            {
                MySession.SetOpp(OppSession.Endpoint);
                OppSession.SetOpp(MySession.Endpoint);
                requestBody.OppUserInfo = OppSession.UserInfo;
                SendUserInfoTo(OppSession.Endpoint, MySession.UserInfo);
            }
            return requestBody;
        }
        private SearchOpponentInfo ProcStopMatching(SearchOpponentInfo requestBody)
        {
            mUsers[mCurrentEndPoint].ReleaseOpp();
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
                case PVPCommand.SkillBomb:
                case PVPCommand.SkillIce:
                case PVPCommand.SkillIceRes:
                    BypassToOppPlayer(requestBody);
                    break;
                case PVPCommand.EndGame:
                    EndPVPGame(requestBody);
                    BypassToOppPlayer(requestBody);
                    break;
                default:
                    BypassToOppPlayer(requestBody);
                    break;
            }
            return requestBody;
        }
        private void BypassToOppPlayer(PVPInfo requestBody)
        {
            SessionUser curSession = mUsers[mCurrentEndPoint];
            if (curSession.OppEndpoint.Length > 0)
            {
                Header responseMsg = new Header();
                responseMsg.Cmd = NetCMD.PVP;
                responseMsg.RequestID = -1;
                responseMsg.Ack = 0;
                responseMsg.UserPk = curSession.UserInfo.userPk;

                byte[] response = NetProtocol.ToArray(responseMsg, Utils.Serialize(requestBody));
                mServer.SendData(curSession.OppEndpoint, response);
            }
        }
        private void EndPVPGame(PVPInfo requestBody)
        {
            mUsers[mCurrentEndPoint].ReleaseOpp();
            float rankingRate = DBManager.Inst().GetRankingRate(requestBody.userInfo.score);
            requestBody.userInfo.rankingRate = rankingRate;
            DBManager.Inst().UpdateUserInfo(requestBody.userInfo);
        }

        private SessionUser FindOpponent(float deltaScore)
        {
            SessionUser me = mUsers[mCurrentEndPoint];
            foreach (var opp in mUsers)
            {
                SessionUser oppInfo = opp.Value;
                if (oppInfo.UserState != UserState.Matching)
                    continue;

                if (me.UserInfo.IsBot && oppInfo.UserInfo.IsBot)
                    continue;

                if (me == oppInfo)
                    continue;

                if (Math.Abs(me.UserInfo.score - oppInfo.UserInfo.score) < deltaScore)
                    return oppInfo;
            }
            return null;
        }
        private void SendUserInfoTo(string endPoint, UserInfo opponent)
        {
            SearchOpponentInfo body = new SearchOpponentInfo();
            body.MyUserInfo = mUsers[endPoint].UserInfo;
            body.OppUserInfo = opponent;
            body.DeltaScore = 0;

            Header responseMsg = new Header();
            responseMsg.Cmd = NetCMD.SearchOpponent;
            responseMsg.UserPk = mUsers[endPoint].UserInfo.userPk;

            byte[] response = NetProtocol.ToArray(responseMsg, Utils.Serialize(body));
            mServer.SendData(endPoint, response);
        }
        private void InformWinTo(SessionUser winUser, SessionUser loseUser)
        {
            PVPInfo body = new PVPInfo();
            body.cmd = PVPCommand.EndGame;
            body.oppUserPk = winUser.UserInfo.userPk;
            body.success = false;
            body.userInfo = loseUser.UserInfo;

            Header responseMsg = new Header();
            responseMsg.Cmd = NetCMD.PVP;
            responseMsg.UserPk = loseUser.UserInfo.userPk;

            byte[] response = NetProtocol.ToArray(responseMsg, Utils.Serialize(body));
            mServer.SendData(winUser.Endpoint, response);
        }
    }

    public enum UserState { None, Idle, Matching, Matched }
    public class SessionUser
    {
        public string Endpoint { get; private set; }
        public string OppEndpoint { get; private set; }
        public UserState UserState { get; set; }
        public DateTime LastPulseTime { get; set; }
        public UserInfo UserInfo { get; set; }
        public Header LastMessage { get; set; }
        public SessionUser(string endPoint)
        {
            Endpoint = endPoint;
            UserState = UserState.Idle;
            LastPulseTime = DateTime.Now;
            UserInfo = null;
            LastMessage = null;
        }
        public bool IsPulseTimeout() { return (DateTime.Now - LastPulseTime).TotalSeconds > 12; }
        public void SetOpp(string endPoint) { OppEndpoint = endPoint; UserState = UserState.Matched; }
        public void ReleaseOpp() { OppEndpoint = ""; UserState = UserState.Idle; }
    }
}
