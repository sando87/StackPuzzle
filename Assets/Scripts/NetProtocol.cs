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
    SearchOpponent, GetInitField, NextProducts, SendSwipe, EndGame
}

public class ServerSideMatchingUser
{
    public bool isMatching = false;
    public int userPK = 0;
    public int score = 0;
    public MySession sessionInfo = null;
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
    public int userPk;
    public int opponentUserPk;
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
