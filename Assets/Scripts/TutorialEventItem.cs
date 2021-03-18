using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TutorialEventItem : TutorialEvent
{
    private int Step = 0;
    public GameObject MessageBox;
    public GameObject BasePoint;
    public GameObject Hand;
    public GameObject Circle;

    public GameObject SwipeBlock1;
    public GameObject SwipeBlock2;
    public GameObject ClickBlock;
    public GameObject ClickItem;

    Vector3[] points = new Vector3[4]
    {
        new Vector3(1.8f, 4.3f, 0), //swipe1
        new Vector3(-0.41f, -1.64f, 0), //swipe2
        new Vector3(1.23f, -2.46f, 0), //click
        new Vector3(2.6f, 6.2f, 0) //click item
    };

    protected override void Start()
    {
        base.Start();
        Step = 0;

        BasePoint.transform.localPosition = points[0];
        SwipeBlock1.SetActive(true);
        Anim.SetTrigger("down");
    }

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
            MessageBox.SetActive(false);
            Anim.Play("TutorialHideAll", -1, 0);

            StartCoroutine(UnityUtils.CallAfterSeconds(1.0f, () =>
            {
                Anim.Play("tutorialDim", -1, 0);
                ClickItem.SetActive(true);
                StartCoroutine(UnityUtils.CallAfterSeconds(1.0f, () =>
                {
                    ShowBasePoint(true);
                    BasePoint.transform.localPosition = points[3];
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

            BasePoint.transform.localPosition = points[1];
            SwipeBlock1.SetActive(false);
            SwipeBlock2.SetActive(true);
            Anim.SetTrigger("left");
        }
        else if(Step == 1 && dir == SwipeDirection.LEFT)
        {
            Step++;
            EventUserAction?.Invoke(TutorialEventType.Left);

            BasePoint.transform.localPosition = points[2];
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
