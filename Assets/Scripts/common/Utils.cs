using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;

public class Utils
{
    public const int ScorePerLevel = 50;
    public const int PlayerLevelMinEasy = 1;
    public const int PlayerLevelMinNormal = 5;
    public const int PlayerLevelMinHard = 30;
    public const int PlayerLevelMinHell = 50;

    public static int ToLevel(int score) { return (score / ScorePerLevel) + 1; }
    public static int ToScore(int level) { return (level - 1) * ScorePerLevel; }
    public static int CalcDeltaScore(bool isWin, int playerScore, int opponentScore, MatchingLevel matchlevel)
    {
        int refLevel = matchlevel == MatchingLevel.Hard ? PlayerLevelMinHard : 
            (matchlevel == MatchingLevel.Hell ? PlayerLevelMinHell : 0);

        int ret = 0;
        if (isWin)
        {
            int level = Utils.ToLevel(playerScore);
            level = Math.Max(0, level - refLevel);
            float weight = 20.0f / (level + 20); //level이 올라갈수록 얻는 경험치가 낮아지는 요소
            float gap = (opponentScore - (playerScore - 100)) * 0.1f * weight;
            float exp = gap < 2 ? 2 :(gap > 30 ? 30 : gap);
            ret = (int)exp;
        }
        else
        {
            int level = Utils.ToLevel(playerScore);
            level = Math.Max(0, level - refLevel);
            float weight = 20.0f / (level + 20); //level이 올라갈수록 얻는 경험치가 낮아지는 요소
            float gap = (opponentScore - (playerScore + 100)) * 0.1f * weight;
            float exp = gap < -30 ? -30 : (gap > -2 ? -2 : gap);
            ret = (int)exp;
        }


        if (matchlevel == MatchingLevel.Easy)
        {
            int playerLevel = Utils.ToLevel(playerScore);
            if (playerLevel < PlayerLevelMinNormal)
                ret = (ret + 50) / 2; //뉴비들은 이기든 지든 10~40점 획득
            else
                ret = (int)(ret * 0.3f); //중급자가 Easy모드로 할 경우 획득 점수 낮추기
        }

        return ret;
    }

    static public byte[] Serialize(object obj)
    {
        try
        {
            int size = Marshal.SizeOf(obj);
            byte[] arr = new byte[size];
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(obj, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);
            return arr;
        }
        catch (Exception ex)
        {
            LOG.error(ex.Message);
        }
        return null;
    }
    static public T Deserialize<T>(ref byte[] data) where T : new()
    {
        try
        {
            T str = new T();
            int size = Marshal.SizeOf(str);
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.Copy(data, 0, ptr, size);
            str = (T)Marshal.PtrToStructure(ptr, str.GetType());
            Marshal.FreeHGlobal(ptr);
            return str;
        }
        catch (Exception ex)
        {
            LOG.error(ex.Message);
        }
        return default(T);
    }
    //public static byte[] Serialize(object source)
    //{
    //    try
    //    {
    //        var formatter = new BinaryFormatter();
    //        using (var stream = new MemoryStream())
    //        {
    //            formatter.Serialize(stream, source);
    //            return stream.ToArray();
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        LOG.warn(ex.Message);
    //    }
    //    return null;
    //}
    //public static T Deserialize<T>(byte[] byteArray, int off = 0) where T : class
    //{
    //    try
    //    {
    //        using (var memStream = new MemoryStream())
    //        {
    //            var binForm = new BinaryFormatter();
    //            memStream.Write(byteArray, off, byteArray.Length - off);
    //            memStream.Seek(0, SeekOrigin.Begin);
    //            var obj = (T)binForm.Deserialize(memStream);
    //            return obj;
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        LOG.warn(ex.Message);
    //    }
    //    return null;
    //}
    public static byte[] HexStringToByteArray(string hex)
    {
        return Enumerable.Range(0, hex.Length)
                         .Where(x => x % 2 == 0)
                         .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                         .ToArray();
    }
    public static byte[] Encrypt(byte[] input)
    {
        byte[] output = new byte[input.Length];
        byte key = 0x4f;
        output[0] = (byte)(key ^ input[0]);
        for (int i = 1; i< input.Length; ++i)
        {
            output[i] = (byte)(output[i - 1] ^ (key ^ input[i]));
        }
        return output;
    }
    public static byte[] Decrypt(byte[] input)
    {
        byte[] output = new byte[input.Length];
        byte key = 0x4f;
        for (int i = input.Length - 1; i > 0; --i)
        {
            output[i] = (byte)((input[i] ^ input[i-1]) ^ key);
        }
        output[0] = (byte)(key ^ input[0]);
        return output;
    }

}
