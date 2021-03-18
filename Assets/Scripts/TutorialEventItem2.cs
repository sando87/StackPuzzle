using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TutorialEventItem2 : TutorialEvent
{
    public GameObject MessageBox;
    public GameObject BasePoint;
    public GameObject Hand;
    public GameObject Circle;

    public GameObject Click1;
    public GameObject Swipe1;

    Vector3[] points = new Vector3[2]
    {
        new Vector3(1.8f, 4.3f, 0), //click1
        new Vector3(-0.41f, -1.64f, 0) //swipe1
    };

    protected override void Start()
    {
        base.Start();

        BasePoint.transform.localPosition = points[0];
        Click1.SetActive(true);
        Anim.SetTrigger("click");
    }

    protected override void OnClick(GameObject obj)
    {
        if (obj != gameObject)
            return;


        if (true)
        {
            EventUserAction?.Invoke(TutorialEventType.Click);

            ShowBasePoint(false);
            Click1.SetActive(false);
            MessageBox.SetActive(false);
            Anim.Play("TutorialHideAll", -1, 0);

            StartCoroutine(UnityUtils.CallAfterSeconds(1.0f, () =>
            {
                Anim.Play("tutorialDim", -1, 0);
                Swipe1.SetActive(true);
                StartCoroutine(UnityUtils.CallAfterSeconds(1.0f, () =>
                {
                    ShowBasePoint(true);
                    BasePoint.transform.localPosition = points[1];
                    Anim.SetTrigger("down");
                }));
            }));
        }
    }
    protected override void OnSwipe(GameObject obj, SwipeDirection dir)
    {
        if (obj != gameObject)
            return;

        if (dir == SwipeDirection.DOWN)
        {
            EventUserAction?.Invoke(TutorialEventType.Down);

            LockSystemEvent(false);
            Destroy(ParentObject);
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
