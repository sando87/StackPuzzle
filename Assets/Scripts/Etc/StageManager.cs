using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StageManager : MonoBehaviour
{
    public TextAsset[] LevelFiles = null;

    private static StageManager mInst = null;
    public static StageManager Inst
    {
        get
        {
            if (mInst == null)
                mInst = GameObject.Find("WorldSpace").transform.Find("StageScreen").GetComponent<StageManager>();
            return mInst;
        }
    }

    public void Activate(bool isActivate)
    {
        gameObject.SetActive(isActivate);
    }

    public Stage GetStage(int stageNumber)
    {
        Transform stage = transform.Find("Levels/Level" + stageNumber);
        return stage == null ? null : stage.GetComponent<Stage>();
    }
}
