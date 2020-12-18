using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class EffectCloud : MonoBehaviour
{
    public float LimitWorldPosX = 0;
    private Vector3 vel = new Vector3(0.5f, 0, 0);

    private void Update()
    {
        transform.localPosition += (vel * Time.deltaTime);
        if (transform.transform.position.x > LimitWorldPosX)
            Destroy(gameObject);
    }
}
