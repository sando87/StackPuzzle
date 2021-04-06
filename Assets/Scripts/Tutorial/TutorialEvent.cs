using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum TutorialEventType
{
    None, Left, Right, Up, Down, Click, Click2
}
public class TutorialEvent : MonoBehaviour
{
    [SerializeField] protected Animator Anim = null;
    protected GraphicRaycaster UIEvnets = null;
    protected DragStageMap WorldEvnets = null;
    protected TutorialEventType type = TutorialEventType.None;
    protected Action<TutorialEventType> EventUserAction = null;
    protected GameObject ParentObject = null;

    static public void Start(int level, TutorialEventType type, Vector2 worldPos, Action<TutorialEventType> eventUserAction)
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
        comp.UIEvnets = GameObject.Find("UISpace/CanvasPopup").GetComponent<GraphicRaycaster>();
        comp.WorldEvnets = GameObject.Find("Main Camera").GetComponent<DragStageMap>();

        Transform baseTr = obj.transform.Find("Point");
        Vector3 basePoint = baseTr.position;
        basePoint.x = worldPos.x;
        basePoint.y = worldPos.y;
        baseTr.position = basePoint;
    }
    protected virtual void Start()
    {
        GetComponent<SwipeDetector>().EventClick = OnClick;
        GetComponent<SwipeDetector>().EventSwipe = OnSwipe;
        LockSystemEvent(true);
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
    protected virtual void OnClick(GameObject obj)
    {
        if (obj != gameObject || type != TutorialEventType.Click)
            return;

        EventUserAction?.Invoke(TutorialEventType.Click);
        LockSystemEvent(false);
        Destroy(ParentObject);
    }
    protected virtual void OnSwipe(GameObject obj, SwipeDirection dir)
    {
        if (obj != gameObject || type == TutorialEventType.Click)
            return;

        if ((type == TutorialEventType.Left && dir == SwipeDirection.LEFT)
            || (type == TutorialEventType.Right && dir == SwipeDirection.RIGHT)
            || (type == TutorialEventType.Up && dir == SwipeDirection.UP)
            || (type == TutorialEventType.Down && dir == SwipeDirection.DOWN))
        {
            EventUserAction?.Invoke(type);
            LockSystemEvent(false);
            Destroy(ParentObject);
        }
    }
    protected void LockSystemEvent(bool isLock)
    {
        UIEvnets.enabled = !isLock;
        WorldEvnets.enabled = !isLock;
        InGameManager.InstStage.GetComponent<SwipeDetector>().enabled = !isLock;
    }
}
