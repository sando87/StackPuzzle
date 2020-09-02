using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class NetServerApp : MonoBehaviour
{
    public int ServerPort = 9435;

    NetServerModule mServer = new NetServerModule();

    static Dictionary<int, ServerSideMatchingUser> mMatchingUsers = new Dictionary<int, ServerSideMatchingUser>();

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
            info.data = retData;
            mServer.SendData(info);
        }
    }

    private byte[] ProcessPacket(MySession session)
    {
        Header requestMsg = NetProtocol.Deserialize<Header>(session.data);
        if (requestMsg == null)
            return NetProtocol.Serialize(new Header());

        Header responseMsg = new Header();
        responseMsg.Cmd = requestMsg.Cmd;
        switch (requestMsg.Cmd)
        {
            case NetCMD.Undef:          responseMsg.body = "Undefied Command"; break;
            case NetCMD.AddUser:        responseMsg.body = ProcAddUser(requestMsg.body as UserInfo); break;
            case NetCMD.EditUserName:   responseMsg.body = ProcEditUserName(requestMsg.body as UserInfo); break;
            case NetCMD.GetUser:        responseMsg.body = ProcGetUser(requestMsg.body as UserInfo); break;
            case NetCMD.DelUser:        responseMsg.body = ProcDelUser(requestMsg.body as UserInfo); break;
            case NetCMD.RenewScore:     responseMsg.body = ProcRenewScore(requestMsg.body as UserInfo); break;
            case NetCMD.GetScores:      responseMsg.body = ProcGetUsers(); break;
            case NetCMD.AddLog:         responseMsg.body = ProcAddLog(requestMsg.body as LogInfo); break;
            case NetCMD.SearchOpponent: responseMsg.body = ProcSearchOpponent(requestMsg.body as SearchOpponentInfo, session); break;
            case NetCMD.GetInitField:   responseMsg.body = ProcGetInitField(requestMsg.body as InitFieldInfo); break;
            case NetCMD.NextProducts:   responseMsg.body = ProcNextProduct(requestMsg.body as NextProducts); break;
            case NetCMD.SendSwipe:      responseMsg.body = ProcSendSwipe(requestMsg.body as SwipeInfo); break;
            case NetCMD.EndGame:        responseMsg.body = ProcEndGame(requestMsg.body as EndGame); break;
            default:                    responseMsg.body = "Undefied Command"; break;
        }
        return NetProtocol.Serialize(responseMsg);
    }


    private UserInfo ProcAddUser(UserInfo requestBody)
    {
        int usePk = DBManager.Inst().AddNewUser(requestBody);
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

    private SearchOpponentInfo ProcSearchOpponent(SearchOpponentInfo requestBody, MySession session)
    {
        ServerSideMatchingUser info = new ServerSideMatchingUser();
        info.isMatching = false;
        info.userPK = requestBody.userPk;
        info.score = requestBody.userScore;
        info.sessionInfo = session;
        mMatchingUsers[requestBody.userPk] = info;
        StartCoroutine(SearchMatching(requestBody.userPk));
        requestBody.isDone = false;
        return requestBody;
    }
    private InitFieldInfo ProcGetInitField(InitFieldInfo requestBody)
    {
        requestBody.products = mMatchingUsers[requestBody.userPk].GetInitField(requestBody.XCount, requestBody.YCount);
        return requestBody;
    }
    private NextProducts ProcNextProduct(NextProducts requestBody)
    {
        requestBody.nextProducts = mMatchingUsers[requestBody.userPk].GetNextColors(requestBody.offset, requestBody.requestCount);
        return requestBody;
    }
    private SwipeInfo ProcSendSwipe(SwipeInfo requestBody)
    {
        MySession session = mMatchingUsers[requestBody.opponentUserPk].sessionInfo;

        Header responseMsg = new Header();
        responseMsg.Cmd = NetCMD.SendSwipe;
        responseMsg.body = requestBody;
        session.data = NetProtocol.Serialize(responseMsg);

        mServer.SendData(session);

        return requestBody;
    }
    private EndGame ProcEndGame(EndGame requestBody)
    {
        mMatchingUsers.Remove(requestBody.userPk);
        return requestBody;
    }

    private IEnumerator SearchMatching(int userPK)
    {
        ServerSideMatchingUser user = mMatchingUsers[userPK];
        float time = 0;
        while (time < 20) //search for 20sec
        {
            if (user.isMatching)
                break;

            foreach(var target in mMatchingUsers)
            {
                ServerSideMatchingUser opp = target.Value;
                if (opp == user || opp.isMatching)
                    continue;

                if(Mathf.Abs(opp.score - user.score) < 5)
                {
                    user.isMatching = true;
                    opp.isMatching = true;
                    SendOppoentInfo(user.userPK, opp.userPK);
                    SendOppoentInfo(opp.userPK, user.userPK);
                    break;
                }
            }
            yield return new WaitForSeconds(1);
            time += Time.deltaTime;
        }

        if (!user.isMatching)
        {
            SendOppoentInfo(user.userPK, -1);
            mMatchingUsers.Remove(user.userPK);
        }
    }
    private void SendOppoentInfo(int userPK, int opponentPK)
    {
        MySession session = mMatchingUsers[userPK].sessionInfo;

        SearchOpponentInfo body = new SearchOpponentInfo();
        body.userPk = userPK;
        body.userPk = mMatchingUsers[userPK].score;
        body.opponentUserPk = opponentPK;
        body.opponentUserScore = opponentPK > 0 ? mMatchingUsers[opponentPK].score : 0;
        body.isDone = true;

        Header responseMsg = new Header();
        responseMsg.Cmd = NetCMD.SearchOpponent;
        responseMsg.body = body;

        session.data = NetProtocol.Serialize(responseMsg);

        mServer.SendData(session);
    }
}
