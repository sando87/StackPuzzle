using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public enum NetCMD
{
    Undef, AddUser, EditUserName, GetUser, DelUser, AddLog, RenewScore, GetScores, 
    SearchOpponent, StopMatching, PVP
}
public enum PVPCommand
{
    Undef, StartGame, Click, Swipe, Destroy, Create, FlushAttacks, EndGame
}
public enum ProductColor
{
    None, Blue, Green, Orange, Purple, Red, Yellow
};
public enum SwipeDirection
{
    UP, DOWN, LEFT, RIGHT
};
public enum ProductSkill
{
    Nothing, OneMore, KeepCombo, SameColor
};

public class NetProtocol
{
    public const UInt32 MAGIC = 0x12345678;
    public const int recvBufSize = 1024 * 64;
    static public int HeadSize()
    {
        return Marshal.SizeOf(typeof(Header));
    }
    static public bool IsValid(byte[] msg, int offset = 0)
    {
        if (offset + 4 > msg.Length)
            return false;
        return NetProtocol.MAGIC == BitConverter.ToInt32(msg, offset);
    }
    static public int Length(byte[] msg, int offset = 0)
    {
        if (offset + 16 + 4 > msg.Length)
            return -1;
        return BitConverter.ToInt32(msg, offset + 16);
    }
    static public byte[] ToArray(Header msg, byte[] body)
    {
        try
        {
            List<byte> buf = new List<byte>();
            if (body == null)
            {
                msg.Length = HeadSize();
                buf.AddRange(Utils.Serialize(msg));
            }
            else
            {
                msg.Length = HeadSize() + body.Length;
                buf.AddRange(Utils.Serialize(msg));
                buf.AddRange(body);
            }

            return buf.ToArray();
        }
        catch(Exception ex)
        {
            LOG.warn(ex.Message);
        }
        return null;
    }
    static public Header ToMessage(byte[] buf, out byte[] body)
    {
        try
        {
            int headSize = HeadSize();
            Header msg = Utils.Deserialize<Header>(ref buf);
            int bodyLen = buf.Length - headSize;
            if (bodyLen > 0)
            {
                body = new byte[bodyLen];
                Array.Copy(buf, headSize, body, 0, bodyLen);
            }
            else
                body = null;

            return msg;
        }
        catch (Exception ex)
        {
            LOG.warn(ex.Message);
        }
        body = null;
        return null;
    }
    static public List<byte[]> SplitBuffer(byte[] buffer)
    {
        List<byte[]> messages = new List<byte[]>();

        int headerSize = HeadSize();
        if (buffer.Length < headerSize)
            return messages;

        int offset = 0;
        while (true)
        {
            if (!IsValid(buffer, offset))
                break;

            int len = Length(buffer, offset);
            if (len < 0 || offset + len > buffer.Length)
                break;

            byte[] msg = new byte[len];
            Array.Copy(buffer, offset, msg, 0, len);
            messages.Add(msg);
            offset += len;
        }

        return messages;
    }
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
public class Header
{
    public UInt32 Magic = NetProtocol.MAGIC;
    public NetCMD Cmd = NetCMD.Undef;
    public Int64 RequestID = -1;
    public int Length = 0;
    public int Ack = 0;
    public int UserPk = -1;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
    public string message = "";
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
public class UserInfo
{
    public int userPk = -1;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
    public string userName = "No Name";
    public int score = 100;
    public int win = 0;
    public int lose = 0;
    public int total = 0;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
    public string deviceName = "";
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
public struct ProductInfo
{
    public ProductColor color;
    public int idxX;
    public int idxY;
    public ProductInfo(int x, int y, ProductColor c) { idxX = x; idxY = y; color = c; }
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
public class LogInfo
{
    public int userPk;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
    public string message;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
public class SearchOpponentInfo
{
    public int userPk;
    public float colorCount;
    public UserInfo oppUser;
    public float oppColorCount;
    public bool isDone;
    public bool isBotPlayer;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
public class PVPInfo
{
    public PVPCommand cmd;
    public int oppUserPk;
    public int XCount;
    public int YCount;
    public int combo;
    public float colorCount;
    public bool success;
    public ProductSkill skill;
    public SwipeDirection dir;
    public UserInfo userInfo;
    public int ArrayCount;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100)]
    public ProductInfo[] products;
}