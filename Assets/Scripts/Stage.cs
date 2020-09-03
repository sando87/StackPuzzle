using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Stage : MonoBehaviour
{
    private Vector3 OriginalScale;
    private StageInfo mStageInfo;

    // Start is called before the first frame update
    void Start()
    {
        OriginalScale = transform.localScale;
        mStageInfo = StageInfo.Load(Number);
        if (mStageInfo == null)
            gameObject.SetActive(false);
        else
            UpdateStageInfo();
    }

    #region Properties
    public int Number { get { return int.Parse(name.Replace("Level", "")); } }
    public bool Locked { get { return transform.Find("Lock").gameObject.activeSelf; } }
    #endregion

    #region Mouse/Touch Event
    private void OnMouseEnter()
    {
        transform.localScale = OriginalScale * 1.05f;
    }

    private void OnMouseDown()
    {
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
        transform.localScale = OriginalScale;
        //MenuPlay.PopUp(mStageInfo);
        MenuWaitMatch.PopUp();
    }
    #endregion

    public void UpdateStarCount(int starCount)
    {
        mStageInfo.StarCount = starCount;
        transform.Find("Stars/Separated/Star1").gameObject.SetActive(mStageInfo.StarCount >= 1 ? true : false);
        transform.Find("Stars/Separated/Star2").gameObject.SetActive(mStageInfo.StarCount >= 2 ? true : false);
        transform.Find("Stars/Separated/Star3").gameObject.SetActive(mStageInfo.StarCount >= 3 ? true : false);
        StageInfo.Save(mStageInfo);
    }
    public void UnLock()
    {
        mStageInfo.IsLocked = false;
        transform.Find("Lock").gameObject.SetActive(mStageInfo.IsLocked);
        GetComponent<BoxCollider2D>().enabled = !mStageInfo.IsLocked;
        StageInfo.Save(mStageInfo);
    }
    private void UpdateStageInfo()
    {
        transform.Find("Number").GetComponentInChildren<Text>().text = Number.ToString();
        transform.Find("Stars/Separated/Star1").gameObject.SetActive(mStageInfo.StarCount >= 1 ? true : false);
        transform.Find("Stars/Separated/Star2").gameObject.SetActive(mStageInfo.StarCount >= 2 ? true : false);
        transform.Find("Stars/Separated/Star3").gameObject.SetActive(mStageInfo.StarCount >= 3 ? true : false);
        transform.Find("Lock").gameObject.SetActive(mStageInfo.IsLocked);
        GetComponent<BoxCollider2D>().enabled = !mStageInfo.IsLocked;
        
    }
}
