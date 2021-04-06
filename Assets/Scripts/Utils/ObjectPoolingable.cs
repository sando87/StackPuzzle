using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPoolingable : MonoBehaviour
{
    [HideInInspector]
    public int OriginPrefabInstanceID { get; set; } = 0;
    [HideInInspector]
    public bool IsDead { get; set; } = false;
}
