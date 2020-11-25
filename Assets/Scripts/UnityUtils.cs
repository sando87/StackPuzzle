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
}
