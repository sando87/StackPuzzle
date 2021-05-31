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

    public static int LevelForNextLeague(int score)
    {
        int level = ToLevel(score);
        if (level < PlayerLevelMinNormal)
            return PlayerLevelMinNormal;
        else if (level < PlayerLevelMinHard)
            return PlayerLevelMinHard;
        else if (level < PlayerLevelMinHell)
            return PlayerLevelMinHell;
        else
            return int.MaxValue;
    }
    public static MatchingLevel ToLeagueLevel(int score)
    {
        int level = ToLevel(score);
        if (level < PlayerLevelMinNormal)
            return MatchingLevel.Bronze;
        else if (level < PlayerLevelMinHard)
            return MatchingLevel.Silver;
        else if (level < PlayerLevelMinHell)
            return MatchingLevel.Gold;
        else
            return MatchingLevel.Master;
    }
    public static int ToLevel(int score) { return (score / ScorePerLevel) + 1; }
    public static int ToScore(int level) { return (level - 1) * ScorePerLevel; }


    public static int CalcDeltaScore(bool isWin, int playerScore, int opponentScore, MatchingLevel matchlevel)
    {
        /*
         * #승리시 계산(리니어)
         *  - lv 1~30: +40 ~ +10 (lv 1~30구간 상승되는 점수는 40-lv로 계산)
         *  - lv 30~: Random +5~+10 고정
         *  - 상대방점수가 나보다 높을시 2배 점수 획득
         *  
         * #패배시 계산
         *  - lv 1~20: +20 ~ -20 (lv 1~20구간 감소되는 점수는 20-lv*2로 계산)
         *  - lv 20~: Random -20~-25 고정
         *  - 상대방점수는 무관
         * */

        int level = Utils.ToLevel(playerScore);
        if (isWin)
        {
            if (level < 30)
            {
                int ret = 40 - level;
                return new Random().Next(ret - 5, ret + 5);
            }
            else
            {
                int ret = new Random().Next(5, 10);
                return opponentScore > playerScore ? ret * 2 : ret;
            }
        }
        else
        {
            if (level < 20)
            {
                int ret = 20 - (level * 2);
                return new Random().Next(ret - 5, ret + 5);
            }
            else
            {
                int ret = new Random().Next(-25, -20);
                return ret;
            }
        }
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
