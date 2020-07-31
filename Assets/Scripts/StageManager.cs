using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StageManager : MonoBehaviour
{
    private static StageManager mInst = null;
    public static StageManager Inst
    {
        get
        {
            if (mInst == null)
                mInst = GameObject.Find("WorldSpace").transform.Find("StageManager").GetComponent<StageManager>();
            return mInst;
        }
    }

    public void Activate(bool isActivate)
    {
        gameObject.SetActive(isActivate);
    }

    public Stage GetStage(int stageNumber)
    {
        Transform stage = transform.Find("Level" + stageNumber);
        return stage == null ? null : stage.GetComponent<Stage>();
    }
}
