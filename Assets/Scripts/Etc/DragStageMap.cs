using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DragStageMap : MonoBehaviour
{
    public void Awake()
    {
        InitCameraView();
    }

    public void InitCameraView()
    {
        // 현재 해상도별 게임화면 정책
        // 16:9 ~ 20:9 해상도비율은 100% 동일하게 출력
        // 16:9 이하의 해상도비율은 화면 좌우에 검은 레터박스로 채우고
        // 20:9 이상의 해상도비율은 화면 위아래쪽에 검은 레터박스로 채운다
        float curScreenAspect = (float)Screen.height / Screen.width;
        float minAspect = 16.0f / 9.0f;
        float maxAspect = 20.0f / 9.0f;

        if (curScreenAspect < minAspect)
        {
            // 16:9 이하의 해상도비율은 화면 좌우에 검은 레터박스로 채우고
            float worldWidth = 8.0f;
            float worldHeight = worldWidth * minAspect;
            Camera.main.orthographicSize = worldHeight * 0.5f;

            Rect letterBoxRect = new Rect(0, 0, 1, 1);
            letterBoxRect.width = curScreenAspect / minAspect;
            letterBoxRect.x = (1 - letterBoxRect.width) * 0.5f;
            Camera.main.rect = letterBoxRect;
        }
        else if (maxAspect < curScreenAspect)
        {
            // 20:9 이상의 해상도비율은 화면 위아래쪽에 검은 레터박스로 채운다
            float worldWidth = 8.0f;
            float worldHeight = worldWidth * maxAspect;
            Camera.main.orthographicSize = worldHeight * 0.5f;

            Rect letterBoxRect = new Rect(0, 0, 1, 1);
            letterBoxRect.height = maxAspect / curScreenAspect;
            letterBoxRect.y = (1 - letterBoxRect.height) * 0.5f;
            Camera.main.rect = letterBoxRect;
        }
        else
        {
            // 16:9 ~ 20:9 해상도비율은 100% 동일하게 출력
            // worldWith값을 길이 8로 고정
            float worldWidth = 8.0f;
            float worldHeight = worldWidth * curScreenAspect;
            Camera.main.orthographicSize = worldHeight * 0.5f;

            Camera.main.rect = new Rect(0, 0, 1, 1);
        }
    }
}
