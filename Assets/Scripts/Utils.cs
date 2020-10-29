using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class Utils
{
    public static void DisableAllChilds(GameObject parent)
    {
        int cnt = parent.transform.childCount;
        for(int i = 0; i < cnt; ++i)
        {
            parent.transform.GetChild(i).gameObject.SetActive(false);
        }
    }
    public static byte[] Serialize(object source)
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
            LOG.warn(ex.Message);
        }
        return null;
    }
    public static T Deserialize<T>(byte[] byteArray, int off = 0) where T : class
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
            LOG.warn(ex.Message);
        }
        return null;
    }
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

    public static IEnumerator AnimateConcave(GameObject obj, Vector3 worldDest, float duration, Action action = null)
    {
        float time = 0;
        Vector3 startPos = obj.transform.position;
        Vector3 destPos = worldDest;
        Vector3 dir = destPos - startPos;
        Vector3 offset = Vector3.zero;
        Vector3 axisZ = new Vector3(0, 0, 1);
        float slopeY = dir.y / (duration * duration);
        float slopeX = -dir.x / (duration * duration);
        while (time < duration)
        {
            float nowT = time - duration;
            offset.x = slopeX * nowT * nowT + dir.x;
            offset.y = slopeY * time * time;
            obj.transform.position = startPos + offset;
            obj.transform.Rotate(axisZ, offset.x - dir.x);
            time += Time.deltaTime;
            yield return null;
        }

        action?.Invoke();
    }

    public static IEnumerator AnimateConvex(GameObject obj, Vector3 worldDest, float duration, Action action = null)
    {
        float time = 0;
        Vector3 startPos = obj.transform.position;
        Vector3 destPos = worldDest;
        Vector3 dir = destPos - startPos;
        Vector3 offset = Vector3.zero;
        Vector3 axisZ = new Vector3(0, 0, 1);
        float slopeY = -dir.y / (duration * duration);
        float slopeX = -dir.x / (duration * duration);
        while (time < duration)
        {
            float nowT = time - duration;
            offset.x = slopeX * nowT * nowT + dir.x;
            offset.y = slopeY * nowT * nowT + dir.y;
            obj.transform.position = startPos + offset;
            obj.transform.Rotate(axisZ, offset.x - dir.x);
            time += Time.deltaTime;
            yield return null;
        }

        action?.Invoke();
    }
}
