using System.Collections;
using System.Collections.Generic;
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
}
