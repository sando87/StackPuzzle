using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Stage : MonoBehaviour
{
    private Vector3 OriginalScale;

    // Start is called before the first frame update
    void Start()
    {
        OriginalScale = transform.localScale;
        UpdateStageInfo();
        //StageInfo.SaveDeefault(Number);
    }

    #region Properties
    public int Number { get { return int.Parse(name.Replace("Level", "")); } }
    public bool Locked { get { return transform.Find("Lock").gameObject.activeSelf; } }
    #endregion

    #region Mouse/Touch Event
    private void OnMouseEnter()
    {
        if (EventSystem.current.IsPointerOverGameObject(-1))
            return;

        transform.localScale = OriginalScale * 1.05f;
    }

    private void OnMouseDown()
    {
        if (EventSystem.current.IsPointerOverGameObject(-1))
            return;

        transform.localScale = OriginalScale * 0.95f;
    }

    private void OnMouseExit()
    {
        transform.localScale = OriginalScale;
    }

    private void OnDisable()
    {
        transform.localScale = OriginalScale;
    }
    private void OnMouseUpAsButton()
    {
        if (EventSystem.current.IsPointerOverGameObject(-1))
            return;

        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectButton1);
        transform.localScale = OriginalScale;
        StageInfo stageInfo = StageInfo.Load(Number);
        if (stageInfo == null)
            MenuMessageBox.PopUp("No Stage Info.", false, null);
        else
            MenuPlay.PopUp(stageInfo);
    }
    #endregion

    public void UpdateStarCount(int starCount)
    {
        int currentStarCount = UserSetting.GetStageStarCount(Number);
        if (starCount < currentStarCount)
            return;

        UserSetting.SetStageStarCount(Number, (byte)starCount);
        transform.Find("Stars/Separated/Star1").gameObject.SetActive(starCount >= 1 ? true : false);
        transform.Find("Stars/Separated/Star2").gameObject.SetActive(starCount >= 2 ? true : false);
        transform.Find("Stars/Separated/Star3").gameObject.SetActive(starCount >= 3 ? true : false);
    }
    public void UnLock()
    {
        UserSetting.StageUnLock(Number);
        transform.Find("Lock").gameObject.SetActive(false);
        GetComponent<BoxCollider2D>().enabled = true;
    }
    private void UpdateStageInfo()
    {
        bool isLocked = UserSetting.StageIsLocked(Number);
        byte starCount = UserSetting.GetStageStarCount(Number);
        transform.Find("Number").GetComponentInChildren<Text>().text = Number.ToString();
        transform.Find("Stars/Separated/Star1").gameObject.SetActive(starCount >= 1 ? true : false);
        transform.Find("Stars/Separated/Star2").gameObject.SetActive(starCount >= 2 ? true : false);
        transform.Find("Stars/Separated/Star3").gameObject.SetActive(starCount >= 3 ? true : false);
        transform.Find("Lock").gameObject.SetActive(isLocked);
        GetComponent<BoxCollider2D>().enabled = !isLocked;
        
    }
}
