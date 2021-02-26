using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class UnityUtils
{
    public static void DisableAllChilds(GameObject parent)
    {
        int cnt = parent.transform.childCount;
        for(int i = 0; i < cnt; ++i)
        {
            parent.transform.GetChild(i).gameObject.SetActive(false);
        }
    }
    public static IEnumerator CallAfterSeconds(float delay, Action func)
    {
        if (delay > 0)
            yield return new WaitForSeconds(delay);

        func.Invoke();

        yield return null;
    }

    //오목함수
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
            obj.transform.Rotate(axisZ, (offset - dir).magnitude);
            time += Time.deltaTime;
            yield return null;
        }

        action?.Invoke();
    }

    //볼록함수
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
            obj.transform.Rotate(axisZ, (offset - dir).magnitude);
            time += Time.deltaTime;
            yield return null;
        }

        action?.Invoke();
    }
    public static IEnumerator AnimateThrow(GameObject obj)
    {
        float dragCoeff = 0.05f;
        int ranDegree = UnityEngine.Random.Range(0, 360);
        int power = UnityEngine.Random.Range(10, 15);
        Vector3 startPos = obj.transform.position;
        Vector3 dir = new Vector3(Mathf.Cos(ranDegree * Mathf.Deg2Rad), Mathf.Sin(ranDegree * Mathf.Deg2Rad), 0);
        dir.Normalize();
        Vector3 speed = power * dir;
        while (true)
        {
            yield return null;
            if (obj == null)
                break;

            obj.transform.position += speed * Time.deltaTime;
            speed -= dragCoeff * speed.sqrMagnitude * dir;
            if (Vector3.Dot(dir, speed) < 0)
                break;
        }
    }
    public static float AccelPlus(float time, float duration)
    {
        return time * time / (duration * duration);
    }
    public static float AccelMinus(float time, float duration)
    {
        return 1 - AccelPlus(time, duration);
    }
    public static float DecelPlus(float time, float duration)
    {
        return 1 - DecelMinus(time, duration);
    }
    public static float DecelMinus(float time, float duration)
    {
        return (time - duration) * (time - duration) / (duration * duration);
    }
}
