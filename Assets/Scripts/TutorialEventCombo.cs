using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TutorialEventCombo : TutorialEvent
{
    private int Step = 0;
    public GameObject MessageBox;
    public GameObject BasePoint;
    public GameObject Hand;
    public GameObject Circle;
    public GameObject SwipeSubWindow;
    public GameObject ComboSet1;
    public GameObject ComboSet2;
    public GameObject ComboSet3;

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
            Step++;
            EventUserAction?.Invoke(TutorialEventType.Up);
            SwipeSubWindow.SetActive(false);
            ShowBasePoint(false);
            MessageBox.SetActive(false);
            Anim.Play("TutorialHideAll", -1, 0);

            StartCoroutine(UnityUtils.CallAfterSeconds(1.0f, () =>
            {
                Anim.Play("tutorialDim", -1, 0);
                StartCoroutine(UnityUtils.CallAfterSeconds(1.0f, () =>
                {
                    EnLightCombo();
                }));
            }));
            
        }
    }
    private void EnLightCombo()
    {
        ComboSet1.SetActive(true);
        StartCoroutine(FadeOut(ComboSet1, () =>
        {
            ComboSet2.SetActive(true);
            StartCoroutine(FadeOut(ComboSet2, () =>
            {
                ComboSet3.SetActive(true);
                StartCoroutine(FadeOut(ComboSet3, () =>
                {
                    ShowBasePoint(true);
                    BasePoint.transform.localPosition = points[2];
                    Anim.SetTrigger("click");
                }));
            }));
        }));
    }
    private void ShowBasePoint(bool show)
    {
        Hand.SetActive(show);
        Circle.SetActive(show);
        GetComponent<SpriteMask>().enabled = show;
        GetComponent<BoxCollider2D>().enabled = show;
    }
    private IEnumerator FadeIn(GameObject obj, Action EventEnd)
    {
        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        float targetAlpha = 160.0f / 255.0f;
        float currentAlpha = 0;
        while(currentAlpha < targetAlpha)
        {
            sr.color = new Color(0, 0, 0, currentAlpha);

            currentAlpha += 0.02f;
            yield return null;
        }

        sr.color = new Color(0, 0, 0, targetAlpha);

        EventEnd?.Invoke();
    }
    private IEnumerator FadeOut(GameObject obj, Action EventEnd)
    {
        List<SpriteRenderer> srs = new List<SpriteRenderer>();
        srs.Add(obj.GetComponent<SpriteRenderer>());
        srs.AddRange(obj.GetComponentsInChildren<SpriteRenderer>());
        float targetAlpha = 0;
        float currentAlpha = srs[0].color.a;
        while (currentAlpha > targetAlpha)
        {
            foreach (SpriteRenderer sr in srs)
                sr.color = new Color(0, 0, 0, currentAlpha);

            currentAlpha -= 0.02f;
            yield return null;
        }

        foreach (SpriteRenderer sr in srs)
            sr.color = new Color(0, 0, 0, 0);

        EventEnd?.Invoke();
    }
}
