using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class NetServerApp : MonoBehaviour
{
    public int ServerPort = 9435;

    NetServerModule mServer = new NetServerModule();

    static Dictionary<int, ServerField> mMatchingUsers = new Dictionary<int, ServerField>();
    static Header mRequestMsg = null;
    static MySession mSession = null;

    // Open Server
    void Awake()
    {
        mServer.OpenServer(ServerPort);
    }

    // Close Server
    private void OnDestroy()
    {
        mServer.CloseServer();
    }

    // Check if packet is arrived every Update call
    void Update()
    {
        MySession[] infos = mServer.GetRecvPackets();
        if (infos == null)
            return;

        foreach (MySession info in infos)
        {
            byte[] retData = ProcessPacket(info);
            if(retData != null)
            {
                info.data = retData;
                mServer.SendData(info);
            }
        }
    }

    private byte[] ProcessPacket(MySession session)
    {
        Header requestMsg = NetProtocol.ToMessage(session.data);
        if (requestMsg == null)
            return NetProtocol.ToArray(new Header());

        Debug.Log("recv cmd[" + requestMsg.Cmd + "]");
        mRequestMsg = requestMsg;
        mSession = session;
        object body = null;
        switch (requestMsg.Cmd)
        {
            case NetCMD.Undef:          body = "Undefied Command"; break;
            case NetCMD.AddUser:        body = ProcAddUser(requestMsg.body as UserInfo); break;
            case NetCMD.EditUserName:   body = ProcEditUserName(requestMsg.body as UserInfo); break;
            case NetCMD.GetUser:        body = ProcGetUser(requestMsg.body as UserInfo); break;
            case NetCMD.DelUser:        body = ProcDelUser(requestMsg.body as UserInfo); break;
            case NetCMD.RenewScore:     body = ProcRenewScore(requestMsg.body as UserInfo); break;
            case NetCMD.GetScores:      body = ProcGetUsers(); break;
            case NetCMD.AddLog:         body = ProcAddLog(requestMsg.body as LogInfo); break;
            case NetCMD.SearchOpponent: body = ProcSearchOpponent(requestMsg.body as SearchOpponentInfo); break;
            case NetCMD.StopMatching:   body = ProcStopMatching(requestMsg.body as SearchOpponentInfo); break;
            case NetCMD.GetInitField:   body = ProcGetInitField(requestMsg.body as InitFieldInfo); break;
            case NetCMD.NextProducts:   body = ProcNextProduct(requestMsg.body as NextProducts); break;
            case NetCMD.SendSwipe:      body = ProcSendSwipe(requestMsg.body as SwipeInfo); break;
            case NetCMD.EndGame:        body = ProcEndGame(requestMsg.body as EndGame); break;
            default:                    body = "Undefied Command"; break;
        }

        if (body == null)
            return null;

        Header responseMsg = new Header();
        responseMsg.Cmd = requestMsg.Cmd;
        responseMsg.RequestID = requestMsg.RequestID;
        responseMsg.body = body;
        return NetProtocol.ToArray(responseMsg);
    }


    private UserInfo ProcAddUser(UserInfo requestBody)
    {
        int usePk = DBManager.Inst().AddNewUser(requestBody, mSession.ipAddr);
        requestBody.userPk = usePk;
        return requestBody;
    }
    private string ProcEditUserName(UserInfo requestBody)
    {
        DBManager.Inst().EditUserName(requestBody);
        return "OK";
    }
    private string ProcDelUser(UserInfo requestBody)
    {
        DBManager.Inst().DeleteUser(requestBody.userPk);
        return "OK";
    }
    private UserInfo ProcGetUser(UserInfo requestBody)
    {
        requestBody = DBManager.Inst().GetUser(requestBody.userPk);
        return requestBody;
    }
    private string ProcRenewScore(UserInfo requestBody)
    {
        DBManager.Inst().RenewUserScore(requestBody);
        return "OK";
    }
    private UserInfo[] ProcGetUsers()
    {
        UserInfo[] users = DBManager.Inst().GetUsers();
        return users;
    }
    private string ProcAddLog(LogInfo requestBody)
    {
        DBManager.Inst().AddLog(requestBody);
        return "OK";
    }

    private SearchOpponentInfo ProcSearchOpponent(SearchOpponentInfo requestBody)
    {
        ServerField info = new ServerField();
        info.isMatching = false;
        info.userPK = requestBody.userPk;
        info.score = requestBody.userScore;
        info.sessionInfo = mSession;
        info.requestMsg = mRequestMsg;
        mMatchingUsers[requestBody.userPk] = info;
        StartCoroutine(SearchMatching(requestBody.userPk));
        return null;
    }
    private SearchOpponentInfo ProcStopMatching(SearchOpponentInfo requestBody)
    {
        mMatchingUsers.Remove(requestBody.userPk);
        return requestBody;
    }
    private InitFieldInfo ProcGetInitField(InitFieldInfo requestBody)
    {
        if(mMatchingUsers.ContainsKey(requestBody.userPk))
            requestBody.products = mMatchingUsers[requestBody.userPk].GetInitField(requestBody.XCount, requestBody.YCount);
        return requestBody;
    }
    private NextProducts ProcNextProduct(NextProducts requestBody)
    {
        if (mMatchingUsers.ContainsKey(requestBody.userPk))
            requestBody.nextProducts = mMatchingUsers[requestBody.userPk].GetNextColors(requestBody.offset, requestBody.requestCount);
        return requestBody;
    }
    private SwipeInfo ProcSendSwipe(SwipeInfo requestBody)
    {
        if (mMatchingUsers.ContainsKey(requestBody.fromUserPk))
        {
            MySession session = mMatchingUsers[requestBody.toUserPk].sessionInfo;

            Header responseMsg = new Header();
            responseMsg.Cmd = NetCMD.SendSwipe;
            responseMsg.RequestID = -1;
            responseMsg.body = requestBody;
            session.data = NetProtocol.ToArray(responseMsg);

            mServer.SendData(session);
        }

        return requestBody;
    }
    private EndGame ProcEndGame(EndGame requestBody)
    {
        mMatchingUsers.Remove(requestBody.userPk);
        return requestBody;
    }

    private IEnumerator SearchMatching(int userPK)
    {
        float time = 0;
        while (true) //search for 20sec
        {
            if (!mMatchingUsers.ContainsKey(userPK))
            {
                break;
            }

            ServerField user = mMatchingUsers[userPK];
            if (user.isMatching)
            {
                break;
            }
            if(time > 20)
            {
                SendOppoentInfo(user, null);
                mMatchingUsers.Remove(user.userPK);
                break;
            }

            foreach (var target in mMatchingUsers)
            {
                ServerField opp = target.Value;
                if (opp.userPK == user.userPK || opp.isMatching)
                    continue;

                if(Mathf.Abs(opp.score - user.score) < 5)
                {
                    user.isMatching = true;
                    opp.isMatching = true;
                    SendOppoentInfo(user, opp);
                    SendOppoentInfo(opp, user);
                    break;
                }
            }
            yield return new WaitForSeconds(1);
            time += 1;
        }
    }
    private void SendOppoentInfo(ServerField user, ServerField opponent)
    {
        MySession session = user.sessionInfo;

        SearchOpponentInfo body = new SearchOpponentInfo();
        body.userPk = user.userPK;
        body.userPk = user.score;
        body.opponentUserPk = opponent == null ? -1 : opponent.userPK;
        body.opponentUserScore = opponent == null ?  0 : opponent.score;
        body.isDone = true;

        Header responseMsg = new Header();
        responseMsg.Cmd = NetCMD.SearchOpponent;
        responseMsg.RequestID = user.requestMsg.RequestID;
        responseMsg.body = body;

        session.data = NetProtocol.ToArray(responseMsg);

        mServer.SendData(session);
    }
}
