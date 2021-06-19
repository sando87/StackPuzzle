using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DragStageMap : MonoBehaviour
{
    public void Awake()
    {
        SetCameraWidth(7.68f);
    }

    public void SetCameraWidth(float worldWidth)
    {
        float aspect = (float)Screen.height / Screen.width;
        Camera.main.orthographicSize = aspect * worldWidth * 0.5f;
    }
}
