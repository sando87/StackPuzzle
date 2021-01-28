using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public enum TutorialEventType
{
    None, Left, Right, Up, Down, Click
}
public class TutorialEvent : MonoBehaviour, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    private Vector2 accDelta = Vector2.zero;
    private TutorialEventType type = TutorialEventType.None;
    private Action<TutorialEventType> EventUserAction = null;
    private GameObject ParentObject = null;

    static public void Start(int level, TutorialEventType type, Action<TutorialEventType> eventUserAction)
    {
        GameObject UICanvas = GameObject.Find("UISpace/CanvasPopup");
        string prefabName = "Tutorial" + level.ToString();
        GameObject prefab = Resources.Load<GameObject>("Prefabs/" + prefabName);
        GameObject obj = Instantiate(prefab, UICanvas.transform);
        TutorialEvent comp = obj.GetComponentInChildren<TutorialEvent>();
        comp.ParentObject = obj;
        comp.EventUserAction = eventUserAction;
        comp.type = type;
    }
    public void OnDrag(PointerEventData eventData)
    {
        if (type == TutorialEventType.Click)
            return;

        accDelta += eventData.delta;

        if (type == TutorialEventType.Left)
        {
            if (accDelta.x < -1)
            {
                EventUserAction?.Invoke(type);
                Destroy(ParentObject);
            }
        }
        else if (type == TutorialEventType.Right)
        {
            if (accDelta.x > 1)
            {
                EventUserAction?.Invoke(type);
                Destroy(ParentObject);
            }
        }
        else if (type == TutorialEventType.Up)
        {
            if (accDelta.y > 1)
            {
                EventUserAction?.Invoke(type);
                Destroy(ParentObject);
            }
        }
        else if (type == TutorialEventType.Down)
        {
            if (accDelta.y < -1)
            {
                EventUserAction?.Invoke(type);
                Destroy(ParentObject);
            }
        }
        
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        accDelta = Vector2.zero;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (type != TutorialEventType.Click)
            return;

        EventUserAction?.Invoke(type);
        Destroy(ParentObject);
    }
}
