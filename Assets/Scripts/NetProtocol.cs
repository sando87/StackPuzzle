﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using UnityEngine;


public enum NetCMD
{
    Undef, AddUser, EditUserName, GetUser, DelUser, AddLog, RenewScore, GetScores, 
    SearchOpponent, StopMatching, GetInitField, NextProducts, SendSwipe, EndGame, SendChoco
}

public class ServerField
{
    public bool isMatching = false;
    public int userPK = 0;
    public UserInfo userInfo = null;
    public float colorCount = 0;
    public MySession sessionInfo = null;
    public Header requestMsg = null;
    public ProductColor[,] initField = null;
    public List<ProductColor> nextColors = new List<ProductColor>();
    public ProductColor[,] GetInitField(int xCount, int yCount)
    {
        if(initField == null)
        {
            initField = new ProductColor[xCount, yCount];
            for(int y = 0; y < yCount; ++y)
                for (int x = 0; x < xCount; ++x)
                    initField[x, y] = (ProductColor)RandomNextColor();
        }

        return initField;
    }
    public ProductColor[] GetNextColors(int offset, int count)
    {
        if (nextColors.Count < offset + count)
        {
            int n = offset + count - nextColors.Count;
            for (int i = 0; i < n; ++i)
                nextColors.Add((ProductColor)RandomNextColor());
        }
        return nextColors.GetRange(offset, count).ToArray();
    }
    private int RandomNextColor()
    {
        int count = (int)(colorCount + 0.99f);
        float remain = colorCount - (int)colorCount;
        int idx = UnityEngine.Random.Range(0, count);
        if(remain > 0 && idx == count - 1)
        {
            if(remain <= UnityEngine.Random.Range(0, 10) * 0.1f)
                idx = UnityEngine.Random.Range(0, count - 1);
        }
        return idx + 1;
    }
}

public class NetProtocol
{
    public const int recvBufSize = 1024 * 64;
    static public int HeadSize()
    {
        return 28;
    }
    static public int Length(byte[] msg, int offset = 0)
    {
        if (offset + HeadSize() > msg.Length)
            return -1;

        return BitConverter.ToInt32(msg, offset + 16);
    }
    static public List<byte[]> Split(byte[] recvBuf)
    {
        int offset = 0;
        List<byte[]> messages = new List<byte[]>();
        while (true)
        {
            int length = NetProtocol.Length(recvBuf, offset);
            if (length <= 0 || offset + length > recvBuf.Length)
                break;

            byte[] buf = new byte[length];
            Array.Copy(recvBuf, offset, buf, 0, length);
            messages.Add(buf);
            offset += length;
        }
        return messages;
    }
    static public byte[] ToArray(Header msg)
    {
        List<byte> buf = new List<byte>();
        try
        {
            byte[] body = Utils.Serialize(msg.body);
            msg.Length = HeadSize() + body.Length;

            buf.AddRange(BitConverter.GetBytes(msg.Magic));
            buf.AddRange(BitConverter.GetBytes((UInt32)msg.Cmd));
            buf.AddRange(BitConverter.GetBytes(msg.RequestID));
            buf.AddRange(BitConverter.GetBytes(msg.Length));
            buf.AddRange(BitConverter.GetBytes(msg.Ack));
            buf.AddRange(BitConverter.GetBytes(msg.UserPk));
            buf.AddRange(body);
        }
        catch(Exception ex)
        {
            LOG.warn(ex.Message);
            return null;
        }
        return buf.ToArray();
    }
    static public Header ToMessage(byte[] buf)
    {
        Header msg = new Header();
        try
        {
            msg.Magic = BitConverter.ToUInt32(buf, 0);
            msg.Cmd = (NetCMD)BitConverter.ToUInt32(buf, 4);
            msg.RequestID = BitConverter.ToInt64(buf, 8);
            msg.Length = BitConverter.ToInt32(buf, 16);
            msg.Ack = BitConverter.ToInt32(buf, 20);
            msg.UserPk = BitConverter.ToInt32(buf, 24);
            msg.body = Utils.Deserialize<object>(buf, 28);
        }
        catch (Exception ex)
        {
            LOG.warn(ex.Message);
            return null;
        }
        return msg;
    }
}

[Serializable]
public class Header
{
    public UInt32 Magic = 0x12345678;
    public NetCMD Cmd = NetCMD.Undef;
    public Int64 RequestID = -1;
    public int Length = 0;
    public int Ack = 0;
    public int UserPk = -1;
    public object body = null;
}

[Serializable]
public class UserInfo
{
    public int userPk = -1;
    public String userName = "No Name";
    public int score = 100;
    public int win = 0;
    public int lose = 0;
    public int total = 0;
    public String deviceName = "";
}

[Serializable]
public class LogInfo
{
    public int userPk;
    public String message;
}

[Serializable]
public class SearchOpponentInfo
{
    public int userPk;
    public float colorCount;
    public UserInfo oppUser;
    public float oppColorCount;
    public bool isDone;
}

[Serializable]
public class InitFieldInfo
{
    public int userPk;
    public int XCount;
    public int YCount;
    public float colorCount;
    public ProductColor[,] products;
}

[Serializable]
public class NextProducts
{
    public int userPk;
    public int offset;
    public int requestCount;
    public ProductColor[] nextProducts;
}

[Serializable]
public class SwipeInfo
{
    public int fromUserPk;
    public int toUserPk;
    public int idxX;
    public int idxY;
    public bool matchLock;
    public SwipeDirection dir;
}

[Serializable]
public class EndGame
{
    public int fromUserPk;
    public int toUserPk;
    public bool win;
    public UserInfo userInfo;
}

[Serializable]
public class ChocoInfo
{
    public int fromUserPk;
    public int toUserPk;
    public int[] xIndicies;
    public int[] yIndicies;
}