using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class EffectMerge : MonoBehaviour
{
    private const float width = 1.28f; // image width 128 pixel
    private Product mProductA;
    private Product mProductB;

    public void SetProucts(Product proA, Product proB)
    {
        mProductA = proA;
        mProductB = proB;

        Vector3 dir = proA.transform.position - proB.transform.position;
        dir.z = 0;
        dir.Normalize();
        Quaternion qua = Quaternion.FromToRotation(new Vector3(1, 0, 0), dir);
        transform.localRotation = qua;
    }

    private void Update()
    {
        if(mProductA == null || mProductB == null)
        {
            Destroy(gameObject);
            return;
        }

        float z = transform.position.z;
        Vector3 center = (mProductB.transform.position + mProductA.transform.position) * 0.5f;
        center.z = z;
        transform.position = center;

        Vector3 dir = mProductB.transform.position - mProductA.transform.position;
        dir.z = 0;
        float dist = dir.magnitude;
        dir.Normalize();
        Quaternion qua = Quaternion.FromToRotation(new Vector3(1, 0, 0), dir);
        transform.localRotation = qua;

        float scale = dist / width;
        transform.localScale = new Vector3(scale, 1.3f, 1);
    }


}
