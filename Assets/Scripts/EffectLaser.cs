using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class EffectLaser : MonoBehaviour
{
    private const float length = 2.0f;

    public void SetDestination(Vector3 worldPos)
    {
        Vector3 dir = worldPos - transform.position;
        dir.z = 0;
        float dist = dir.magnitude;
        dir.Normalize();
        Quaternion qua = Quaternion.FromToRotation(new Vector3(0, -1, 0), dir);
        transform.localRotation = qua;

        float scale = dist / length;
        transform.localScale = new Vector3(1, scale, 1);

        Destroy(gameObject, 1.0f);
    }


    //public Vector3 Destination = Vector3.zero;
    //
    //public void Burst(Vector3 destWorldPos, float duration)
    //{
    //    Vector3 dir = destWorldPos - transform.position;
    //    dir.z = 0;
    //    float dist = dir.magnitude;
    //    dir.Normalize();
    //    Quaternion qua = Quaternion.FromToRotation(new Vector3(1, 0, 0), dir);
    //    transform.localRotation = qua;
    //
    //    StartCoroutine(AnimateEffectLaser(dist, duration));
    //}
    //
    //IEnumerator AnimateEffectLaser(float distance, float duration)
    //{
    //    float time = 0;
    //    Vector3 size = new Vector3(1, 1, 1);
    //    while(time < duration)
    //    {
    //        float width = UnityUtils.AccelMinus(time, duration); // 1 -> 0
    //        float length = UnityUtils.DecelPlus(time, duration) * distance; // 0 -> distance
    //        size.x = length;
    //        size.y = width;
    //        transform.localScale = size;
    //
    //        time += Time.deltaTime;
    //        yield return null;
    //    }
    //
    //    Destroy(gameObject);
    //}
}
