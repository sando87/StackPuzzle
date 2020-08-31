using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using UnityEngine;


public enum NetCMD
{
    Undef, AddUser, EditUserName, GetUser, DelUser, AddLog, RenewScore, GetScores, 
    InitField, NextProducts, AttackSwipe
}
public class NetProtocol
{
    static public byte[] ProcessPacket(byte[] buf)
    {
        Header requestMsg = Deserialize<Header>(buf);
        if (requestMsg == null)
            return Serialize(new Header());

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
            case NetCMD.InitField:      responseMsg.body = ProcInitField(requestMsg.body as InitFieldInfo); break;
            case NetCMD.NextProducts:   responseMsg.body = ProcNextProduct(requestMsg.body as NextProducts); break;
            case NetCMD.AttackSwipe:    responseMsg.body = ProcAttackSwipe(requestMsg.body as SwipeInfo); break;
            default:                    responseMsg.body = "Undefied Command"; break;
        }
        return Serialize(responseMsg);
    }


    static UserInfo ProcAddUser(UserInfo requestBody)
    {
        int usePk = DBManager.Inst().AddNewUser(requestBody);
        requestBody.userPk = usePk;
        return requestBody;
    }
    static string ProcEditUserName(UserInfo requestBody)
    {
        DBManager.Inst().EditUserName(requestBody);
        return "OK";
    }
    static string ProcDelUser(UserInfo requestBody)
    {
        DBManager.Inst().DeleteUser(requestBody.userPk);
        return "OK";
    }
    static UserInfo ProcGetUser(UserInfo requestBody)
    {
        requestBody = DBManager.Inst().GetUser(requestBody.userPk);
        return requestBody;
    }
    static string ProcRenewScore(UserInfo requestBody)
    {
        DBManager.Inst().RenewUserScore(requestBody);
        return "OK";
    }
    static UserInfo[] ProcGetUsers()
    {
        UserInfo[] users = DBManager.Inst().GetUsers();
        return users;
    }
    static string ProcAddLog(LogInfo requestBody)
    {
        DBManager.Inst().AddLog(requestBody);
        return "OK";
    }
    static InitFieldInfo ProcInitField(InitFieldInfo requestBody)
    {
        return requestBody;
    }
    static NextProducts ProcNextProduct(NextProducts requestBody)
    {
        return requestBody;
    }
    static string ProcAttackSwipe(SwipeInfo requestBody)
    {
        return "OK";
    }

    static public byte[] Serialize(object source)
    {
        try
        {
            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream())
            {
                formatter.Serialize(stream, source);
                return stream.ToArray();
            }
        }
        catch (Exception ex)
        {
            Debug.Log(ex.Message);
        }
        return null;
    }
    static public T Deserialize<T>(byte[] byteArray) where T : class
    {
        try
        {
            using (var memStream = new MemoryStream())
            {
                var binForm = new BinaryFormatter();
                memStream.Write(byteArray, 0, byteArray.Length);
                memStream.Seek(0, SeekOrigin.Begin);
                var obj = (T)binForm.Deserialize(memStream);
                return obj;
            }
        }
        catch (Exception ex)
        {
            Debug.Log(ex.Message);
        }
        return null;
    }
}

[Serializable]
public class Header
{
    public UInt32 Magic = 0x12345678;
    public NetCMD Cmd = NetCMD.Undef;
    public object body = null;
}

[Serializable]
public class UserInfo
{
    public int userPk = -1;
    public String userName;
    public int score;
    public String deviceName;
    public String ipAddress;
}

[Serializable]
public class LogInfo
{
    public int userPk;
    public String message;
}

[Serializable]
public class InitFieldInfo
{
    public int userPk;
    public int xCount;
    public int yCount;
    public ProductColor[] products;
}

[Serializable]
public class NextProducts
{
    public int userPk;
    public int requestCount;
    public ProductColor[] products;
}

[Serializable]
public class SwipeInfo
{
    public int userPk;
    public int idxX;
    public int idxY;
    public SwipeDirection dir;
}
