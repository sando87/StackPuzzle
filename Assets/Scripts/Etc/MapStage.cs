using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MapStage : MonoBehaviour
{
    [SerializeField] private Button BtnStage = null;
    [SerializeField] private Image Lock = null;
    [SerializeField] private Image Star1 = null;
    [SerializeField] private Image Star2 = null;
    [SerializeField] private Image Star3 = null;
    [SerializeField] private TextMeshPro NumberText = null;

    public int Number { get { return int.Parse(name); } }
    public bool Locked { get { return Lock.gameObject.activeSelf; } }

    void Start()
    {
        UpdateUI();
    }

    public void OnBtnClick()
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
        StageInfo stageInfo = StageInfo.Load(Number);
        if (stageInfo == null)
            MenuMessageBox.PopUp("No Stage Info.", false, null);
        else
            MenuPlay.PopUp(stageInfo);
    }

    public void UpdateStarCount(int starCount)
    {
        int currentStarCount = UserSetting.GetStageStarCount(Number);
        if (starCount < currentStarCount)
            return;

        UserSetting.SetStageStarCount(Number, (byte)starCount);

        UpdateUI();
    }
    public void UnLock()
    {
        UserSetting.StageUnLock(Number);

        UpdateUI();
    }
    private void UpdateUI()
    {
        bool isLocked = UserSetting.StageIsLocked(Number);
        byte starCount = UserSetting.GetStageStarCount(Number);

        NumberText.text = Number.ToString();
        Star1.gameObject.SetActive(starCount >= 1 ? true : false);
        Star2.gameObject.SetActive(starCount >= 2 ? true : false);
        Star3.gameObject.SetActive(starCount >= 3 ? true : false);
        Lock.gameObject.SetActive(isLocked);
        BtnStage.enabled = !isLocked;
    }
}
