﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum TutorialEventType
{
    None, Left, Right, Up, Down, Click
}
public class TutorialEvent : MonoBehaviour
{
    [SerializeField] private Animator Anim = null;
    private GraphicRaycaster UIEvnets = null;
    private DragStageMap WorldEvnets = null;
    private TutorialEventType type = TutorialEventType.None;
    private Action<TutorialEventType> EventUserAction = null;
    private GameObject ParentObject = null;

    static public void Start(int level, TutorialEventType type, Vector2 screenWorldPos, Action<TutorialEventType> eventUserAction)
    {
        string prefabName = "Tutorial" + level.ToString();
        GameObject prefab = Resources.Load<GameObject>("Prefabs/" + prefabName);
        GameObject obj = Instantiate(prefab);
        Vector3 camPos = Camera.main.transform.position;
        camPos.z = -5;
        obj.transform.position = camPos;
        TutorialEvent comp = obj.GetComponentInChildren<TutorialEvent>();
        comp.ParentObject = obj;
        comp.EventUserAction = eventUserAction;
        comp.type = type;
        Vector3 pos = comp.transform.position;
        pos.x = screenWorldPos.x;
        pos.y = screenWorldPos.y;
        comp.transform.position = pos;
        comp.UIEvnets = GameObject.Find("UISpace/CanvasPopup").GetComponent<GraphicRaycaster>();
        comp.WorldEvnets = GameObject.Find("Main Camera").GetComponent<DragStageMap>();
    }
    void Start()
    {
        GetComponent<SwipeDetector>().EventClick = OnClick;
        GetComponent<SwipeDetector>().EventSwipe = OnSwipe;
        UIEvnets.enabled = false;
        WorldEvnets.enabled = false;
        InGameManager.InstStage.GetComponent<SwipeDetector>().enabled = false;
        StartCoroutine(PlayAnim());
    }
    private IEnumerator PlayAnim()
    {
        Anim.StopPlayback();
        yield return new WaitForSeconds(2);
        Anim.Play("tutorialDim", -1, 0);
        yield return new WaitForSeconds(1);
        switch(type)
        {
            case TutorialEventType.Click: Anim.SetTrigger("click"); break;
            case TutorialEventType.Left: Anim.SetTrigger("left"); break;
            case TutorialEventType.Right: Anim.SetTrigger("right"); break;
            case TutorialEventType.Up: Anim.SetTrigger("up"); break;
            case TutorialEventType.Down: Anim.SetTrigger("down"); break;
        }
    }
    void OnClick(GameObject obj)
    {
        if (obj != gameObject || type != TutorialEventType.Click)
            return;

        EventUserAction?.Invoke(TutorialEventType.Click);
        UIEvnets.enabled = true;
        WorldEvnets.enabled = true;
        InGameManager.InstStage.GetComponent<SwipeDetector>().enabled = true;
        Destroy(ParentObject);
    }
    void OnSwipe(GameObject obj, SwipeDirection dir)
    {
        if (obj != gameObject || type == TutorialEventType.Click)
            return;

        if ((type == TutorialEventType.Left && dir == SwipeDirection.LEFT)
            || (type == TutorialEventType.Right && dir == SwipeDirection.RIGHT)
            || (type == TutorialEventType.Up && dir == SwipeDirection.UP)
            || (type == TutorialEventType.Down && dir == SwipeDirection.DOWN))
        {
            EventUserAction?.Invoke(type);
            UIEvnets.enabled = true;
            WorldEvnets.enabled = true;
            InGameManager.InstStage.GetComponent<SwipeDetector>().enabled = true;
            Destroy(ParentObject);
        }
    }
}
