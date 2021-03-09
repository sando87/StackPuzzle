using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TutorialEventCombo : TutorialEvent
{
    private int Step = 0;
    public GameObject BasePoint;
    public GameObject SwipeSubWindow;
    Vector3[] points = new Vector3[3]
    {
        new Vector3(1.8f, 4.3f, 0), //메시지 클릭 포인트
        new Vector3(-0.41f, -1.64f, 0), //스와이프 블럭
        new Vector3(1.23f, -2.46f, 0) //콤보 시작 클릭
    };

    protected override void Start()
    {
        base.Start();
        BasePoint.transform.localPosition = points[0];
    }

    protected override void OnClick(GameObject obj)
    {
        if (obj != gameObject)
            return;

        if (Step == 0)
        {
            SwipeSubWindow.gameObject.SetActive(true);
            BasePoint.transform.localPosition = points[1];
            Anim.SetTrigger("up");
            Step++;
        }
        else if (Step == 2)
        {
            EventUserAction?.Invoke(TutorialEventType.Click);
            LockSystemEvent(false);
            Destroy(ParentObject);
        }
    }
    protected override void OnSwipe(GameObject obj, SwipeDirection dir)
    {
        if (obj != gameObject)
            return;

        if (Step == 1 && dir == SwipeDirection.UP)
        {
            EventUserAction?.Invoke(TutorialEventType.Up);
            SwipeSubWindow.gameObject.SetActive(false);
            BasePoint.transform.localPosition = points[2];
            Anim.SetTrigger("click");
            Step++;
        }
    }
}
