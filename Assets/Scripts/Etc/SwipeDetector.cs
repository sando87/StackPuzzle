using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class SwipeDetector : MonoBehaviour
{
    public const float SwipeDetectRange = 0.1f;

    private GameObject mDownObject = null;
    private Vector3 mDownPosition;

    public Action<GameObject, SwipeDirection> EventSwipe;
    public Action<GameObject> EventClick;

    void Update()
    {
        if (IsPointerOverUI())
            return;

        CheckSwipe();
    }

    bool IsPointerOverUI()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        return EventSystem.current.IsPointerOverGameObject();
#elif UNITY_ANDROID || UNITY_IOS
        if (Input.touchCount > 0)
            return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
        else
            return false;
#endif
    }

    void CheckSwipe()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 worldPt = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Collider2D hit = Physics2D.OverlapPoint(worldPt);
            if (hit != null)
            {
                if(hit.gameObject.GetComponentInParent<SwipeDetector>() != null)
                {
                    mDownObject = hit.gameObject;
                    mDownPosition = worldPt;
                }
            }
        }
        else if (Input.GetMouseButton(0))
        {
            if(mDownObject != null)
            {
                Vector3 curWorldPt = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                if ((curWorldPt - mDownPosition).magnitude >= SwipeDetectRange)
                {
                    Vector2 _currentSwipe = new Vector2(curWorldPt.x - mDownPosition.x, curWorldPt.y - mDownPosition.y);
                    _currentSwipe.Normalize();

                    if (_currentSwipe.y > 0 && _currentSwipe.x > -0.5f && _currentSwipe.x < 0.5f)
                        EventSwipe?.Invoke(mDownObject, SwipeDirection.UP);
                    else if (_currentSwipe.y < 0 && _currentSwipe.x > -0.5f && _currentSwipe.x < 0.5f)
                        EventSwipe?.Invoke(mDownObject, SwipeDirection.DOWN);
                    else if (_currentSwipe.x < 0 && _currentSwipe.y > -0.5f && _currentSwipe.y < 0.5f)
                        EventSwipe?.Invoke(mDownObject, SwipeDirection.LEFT);
                    else if (_currentSwipe.x > 0 && _currentSwipe.y > -0.5f && _currentSwipe.y < 0.5f)
                        EventSwipe?.Invoke(mDownObject, SwipeDirection.RIGHT);

                    mDownObject = null;
                    mDownPosition = Vector3.zero;
                }
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            Vector3 worldPt = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Collider2D hit = Physics2D.OverlapPoint(worldPt);
            if (hit != null && hit.gameObject == mDownObject)
                EventClick?.Invoke(mDownObject);

            mDownObject = null;
            mDownPosition = Vector3.zero;
        }
    }

}
