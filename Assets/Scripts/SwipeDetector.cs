using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum SwipeDirection { UP, DOWN, LEFT, RIGHT };

public class SwipeDetector : MonoBehaviour
{
    public const float SwipeDetectRange = 0.1f;

    private GameObject mDownObject = null;
    private Vector3 mDownPosition;

    public Action<GameObject, SwipeDirection> EventSwipe;

    void Update()
    {
        CheckSwipe();
    }

    void CheckSwipe()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 worldPt = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Collider2D hit = Physics2D.OverlapPoint(worldPt);
            if (hit != null)
            {
                mDownObject = hit.gameObject;
                mDownPosition = worldPt;
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
            mDownObject = null;
            mDownPosition = Vector3.zero;
        }
    }

}
