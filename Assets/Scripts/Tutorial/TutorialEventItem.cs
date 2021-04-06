using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TutorialEventItem : TutorialEvent
{
    private int Step = 0;
    public TextMeshPro MessageBox;
    public GameObject BasePoint;
    public GameObject Hand;
    public GameObject Circle;

    public GameObject SwipeBlock1;
    public GameObject SwipeBlock2;
    public GameObject ClickBlock;
    public GameObject ClickItem;

    protected override void OnClick(GameObject obj)
    {
        if (obj != gameObject)
            return;


        if (Step == 2)
        {
            Step++;
            EventUserAction?.Invoke(TutorialEventType.Click);

            ShowBasePoint(false);
            SwipeBlock2.SetActive(false);
            MessageBox.transform.parent.gameObject.SetActive(false);
            Anim.Play("TutorialHideAll", -1, 0);

            StartCoroutine(UnityUtils.CallAfterSeconds(1.0f, () =>
            {
                Anim.Play("tutorialDim", -1, 0);
                ClickBlock.SetActive(false);
                ClickItem.SetActive(true);
                MessageBox.transform.parent.gameObject.SetActive(true);
                MessageBox.text = "Click to use special block.";
                StartCoroutine(UnityUtils.CallAfterSeconds(1.0f, () =>
                {
                    ShowBasePoint(true);
                    Vector3 pos = InGameManager.InstStage.Frame(3, 3).transform.position;
                    pos.z = BasePoint.transform.position.z;
                    BasePoint.transform.position = pos;
                    Anim.SetTrigger("click");
                }));
            }));
        }
        else if (Step == 3)
        {
            EventUserAction?.Invoke(TutorialEventType.Click2);

            LockSystemEvent(false);
            Destroy(ParentObject);
        }
    }
    protected override void OnSwipe(GameObject obj, SwipeDirection dir)
    {
        if (obj != gameObject)
            return;

        if (Step == 0 && dir == SwipeDirection.DOWN)
        {
            Step++;
            EventUserAction?.Invoke(TutorialEventType.Down);

            Vector3 pos = InGameManager.InstStage.Frame(4, 3).transform.position;
            pos.z = BasePoint.transform.position.z;
            BasePoint.transform.position = pos;
            SwipeBlock1.SetActive(false);
            SwipeBlock2.SetActive(true);
            Anim.SetTrigger("left");
        }
        else if(Step == 1 && dir == SwipeDirection.LEFT)
        {
            Step++;
            EventUserAction?.Invoke(TutorialEventType.Left);

            Vector3 pos = InGameManager.InstStage.Frame(3, 3).transform.position;
            pos.z = BasePoint.transform.position.z;
            BasePoint.transform.position = pos;
            SwipeBlock2.SetActive(false);
            ClickBlock.SetActive(true);
            Anim.SetTrigger("click");
        }
    }
    private void ShowBasePoint(bool show)
    {
        Hand.SetActive(show);
        Circle.SetActive(show);
        GetComponent<SpriteMask>().enabled = show;
        GetComponent<BoxCollider2D>().enabled = show;
    }
}
