using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DragStageMap : MonoBehaviour
{
    public float MaxY = 1000;
    public float MinY = -10;

    private Vector3 mCameraDownPos;
    private Vector3 mMouseDownPos;

    private Vector3 defaultPosision = new Vector3(0, 0, -10);

    public float CameraMinY { get { return transform.position.y - Camera.main.orthographicSize; } }
    public float CameraMaxY { get { return transform.position.y + Camera.main.orthographicSize; } }

    public void Awake()
    {
        SetCameraWidth(7.68f);
    }

    public void Update()
    {
        if (MenuStages.Inst.gameObject.activeSelf)
        {
#if (UNITY_ANDROID || UNITY_IPHONE) && !UNITY_EDITOR
		    HandleTouchInput();
#else
            HandleMouseInput();
#endif
            LimitCamera();
        }
        else
        {
            transform.position = defaultPosision;
        }
    }

    public void SetCameraWidth(float worldWidth)
    {
        float aspect = (float)Screen.height / Screen.width;
        Camera.main.orthographicSize = aspect * worldWidth * 0.5f;
    }

    private void HandleTouchInput()
    {
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                mCameraDownPos = transform.position;
                mMouseDownPos = Input.mousePosition;
            }
            else if (touch.phase == TouchPhase.Moved)
            {
                float dy = Camera.main.ScreenToWorldPoint(mMouseDownPos).y - Camera.main.ScreenToWorldPoint(Input.mousePosition).y;
                transform.position = mCameraDownPos + new Vector3(0, dy, 0);
            }
            else if (touch.phase == TouchPhase.Ended)
            {
                mCameraDownPos = Vector3.zero;
                mMouseDownPos = Vector3.zero;
            }
        }
    }

    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            mCameraDownPos = transform.position;
            mMouseDownPos = Input.mousePosition;
        }
        else if (Input.GetMouseButton(0))
        {
            float dy = Camera.main.ScreenToWorldPoint(mMouseDownPos).y - Camera.main.ScreenToWorldPoint(Input.mousePosition).y;
            transform.position = mCameraDownPos + new Vector3(0, dy, 0);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            mCameraDownPos = Vector3.zero;
            mMouseDownPos = Vector3.zero;
        }
    }


    private void LimitCamera()
    {
        if(CameraMinY < MinY)
        {
            Vector3 tmpPos = transform.position;
            tmpPos.y = MinY + Camera.main.orthographicSize;
            transform.position = tmpPos;
        }
        else if(CameraMaxY > MaxY)
        {
            Vector3 tmpPos = transform.position;
            tmpPos.y = MaxY - Camera.main.orthographicSize;
            transform.position = tmpPos;
        }
    }
}
