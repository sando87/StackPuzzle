using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public enum NetCMD
{
    Undef, AddUser, UpdateUserInfo, EditUserName, GetUser, DelUser, AddLog, RenewScore, GetScores, 
    SearchOpponent, StopMatching, PVP, HeartCheck
}
public enum PVPCommand
{
    Undef, StartGame, Click, Swipe, Destroy, Create, FlushAttacks, EndGame, DropPause, DropResume, ChangeSkill, BreakIce,
    SkillBomb, SkillIce, SkillIceRes, SkillShield, SkillScoreBuff, SkillChangeProducts, SkillCloud, SkillUpsideDown, SkillRemoveBadEffects
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
    Nothing, Horizontal, Vertical, Bomb, SameColor
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
    static public Header ToMessage(byte[] buf, out byte[] body)
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
    public string userName = "";
    public int score = 0;
    public int win = 0;
    public int lose = 0;
    public int total = 0;
    public float rankingRate = 1;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
    public string deviceName = "";
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
public struct ProductInfo
{
    public ProductColor prvColor;
    public ProductColor nextColor;
    public ProductSkill skill;
    public int idxX;
    public int idxY;
    public int prvInstID;
    public int nextInstID;
    public ProductInfo(ProductColor prvColor, ProductColor nextColor, ProductSkill skill, int idxX, int idxY, int prvInstID, int nextInstID)
    { this.prvColor = prvColor; this.nextColor = nextColor; this.skill = skill; this.idxX = idxX; this.idxY = idxY; this.prvInstID = prvInstID; this.nextInstID = nextInstID; }
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
public class LogInfo
{
    public int userPk;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
    public string message;
    public LogInfo(string msg) { userPk = -1; message = msg; }
    public LogInfo() { userPk = -1; message = ""; }
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
public class SearchOpponentInfo
{
    public int userPk;
    public float colorCount;
    public UserInfo UserInfo;
    public bool isDone;
    public bool isBotPlayer;
    public PVPCommand skillBlue;
    public PVPCommand skillGreen;
    public PVPCommand skillOrange;
    public PVPCommand skillPurple;
    public PVPCommand skillRed;
    public PVPCommand skillYellow;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
public class PVPInfo
{
    public PVPCommand cmd;
    public int oppUserPk;
    public int XCount;
    public int YCount;
    public int combo;
    public int idxX;
    public int idxY;
    public int newDropCount;
    public float colorCount;
    public bool withLaserEffect;
    public bool success;
    public ProductSkill skill;
    public SwipeDirection dir;
    public UserInfo userInfo;
    public int ArrayCount;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100)]
    public ProductInfo[] products = new ProductInfo[100];
}