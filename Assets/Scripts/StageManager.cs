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
                mInst = GameObject.Find("WorldSpace").transform.Find("StageScreen").GetComponent<StageManager>();
            return mInst;
        }
    }

    private void Start()
    {
        SoundPlayer.Inst.PlayBackMusic(SoundPlayer.Inst.BackMusicMap);
    }

    public void Activate(bool isActivate)
    {
        gameObject.SetActive(isActivate);
        if(isActivate)
            SoundPlayer.Inst.PlayBackMusic(SoundPlayer.Inst.BackMusicMap);
    }

    public Stage GetStage(int stageNumber)
    {
        Transform stage = transform.Find("Levels/Level" + stageNumber);
        return stage == null ? null : stage.GetComponent<Stage>();
    }
}
