using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LogWriter : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(LoopWritingLog());
    }

    private void OnDestroy()
    {
        LOG.ProcessToFlushLog();
    }

    IEnumerator LoopWritingLog()
    {
        while(true)
        {
            yield return new WaitForSeconds(1);

            LOG.ProcessToFlushLog();
        }
    }
}
