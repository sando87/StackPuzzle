using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PerformanceMonitering : MonoBehaviour
{
    public Action<string> OnMonitering = null;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR
    private PerformanceMonitor mPerformanceMonitor = null;

    void Awake()
    {
        mPerformanceMonitor = new PerformanceMonitor();
        mPerformanceMonitor.Init();
    }
    void Start()
    {
        StartCoroutine(DoMonitering());
    }

    IEnumerator DoMonitering()
    {
        yield return new WaitForSeconds(1);

        while (true)
        {
            string log = mPerformanceMonitor.GetInfo();
            if (log.Length > 0)
            {
                OnMonitering?.Invoke(log);
            }

            yield return new WaitForSeconds(60);
        }
    }
#endif
}
