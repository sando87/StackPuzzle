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
    SearchOpponent, StopMatching, GetInitField, NextProducts, SendSwipe, EndGame
}

public class ServerField
{
    public bool isMatching = false;
    public int userPK = 0;
    public int score = 0;
    public MySession sessionInfo = null;
    public Header requestMsg = null;
    public ProductColor[,] initField = null;
    public List<ProductColor> nextColors = new List<ProductColor>();
    public ProductColor[,] GetInitField(int xCount, int yCount)
    {
        if(initField == null)
        {
            int colorTypeCount = System.Enum.GetValues(typeof(ProductColor)).Length;
            initField = new ProductColor[xCount, yCount];
            for(int y = 0; y < yCount; ++y)
                for (int x = 0; x < xCount; ++x)
                    initField[x, y] = (ProductColor)UnityEngine.Random.Range(1, colorTypeCount);
        }

        return initField;
    }
    public ProductColor[] GetNextColors(int offset, int count)
    {
        if (nextColors.Count < offset + count)
        {
            int colorTypeCount = System.Enum.GetValues(typeof(ProductColor)).Length;
            int n = offset + count - nextColors.Count;
            for (int i = 0; i < n; ++i)
                nextColors.Add((ProductColor)UnityEngine.Random.Range(1, colorTypeCount));
        }
        return nextColors.GetRange(offset, count).ToArray();
    }
}

public class NetProtocol
{
    static public int HeadSize()
    {
        return 20;
    }
    static public int Length(byte[] msg, int offset = 0)
    {
        return BitConverter.ToInt32(msg, offset + 16);
    }
    static public byte[] ToArray(Header msg)
    {
        List<byte> buf = new List<byte>();
        try
        {
            byte[] body = Serialize(msg.body);
            msg.Length = HeadSize() + body.Length;

            buf.AddRange(BitConverter.GetBytes(msg.Magic));
            buf.AddRange(BitConverter.GetBytes((UInt32)msg.Cmd));
            buf.AddRange(BitConverter.GetBytes(msg.RequestID));
            buf.AddRange(BitConverter.GetBytes(msg.Length));
            buf.AddRange(body);
        }
        catch(Exception ex)
        {
            Debug.Log(ex.Message);
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
            msg.body = Deserialize<object>(buf, 20);
        }
        catch (Exception ex)
        {
            Debug.Log(ex.Message);
            return null;
        }
        return msg;
    }
    static private byte[] Serialize(object source)
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
    static private T Deserialize<T>(byte[] byteArray, int off = 0) where T : class
    {
        try
        {
            using (var memStream = new MemoryStream())
            {
                var binForm = new BinaryFormatter();
                memStream.Write(byteArray, off, byteArray.Length - off);
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
    public Int64 RequestID = -1;
    public int Length = 0;
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
public class SearchOpponentInfo
{
    public int userPk;
    public int userScore;
    public int opponentUserPk;
    public int opponentUserScore;
    public bool isDone;
}

[Serializable]
public class InitFieldInfo
{
    public int userPk;
    public int XCount;
    public int YCount;
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
    public bool matchable;
    public SwipeDirection dir;
}

[Serializable]
public class EndGame
{
    public int userPk;
    public bool win;
    public int maxCombo;
    public int score;
}
