using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TutorialEventItem2 : TutorialEvent
{
    public GameObject BasePoint;
    public GameObject Hand;
    public GameObject Circle;

    public GameObject Swipe1;

    protected override void OnClick(GameObject obj)
    {
        if (obj != gameObject)
            return;


        if (true)
        {
            EventUserAction?.Invoke(TutorialEventType.Click);

            ShowBasePoint(false);
            Anim.Play("TutorialHideAll", -1, 0);

            StartCoroutine(UnityUtils.CallAfterSeconds(2.0f, () =>
            {
                Anim.Play("tutorialDim", -1, 0);
                Swipe1.SetActive(true);
                StartCoroutine(UnityUtils.CallAfterSeconds(1.0f, () =>
                {
                    ShowBasePoint(true);
                    Vector3 pos = InGameManager.InstStage.Frame(3, 3).transform.position;
                    pos.z = BasePoint.transform.position.z;
                    BasePoint.transform.position = pos;
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
