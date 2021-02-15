using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class Tutorials : MonoBehaviour
{
    private InGameManager StageGameField = null;

    private void Start()
    {
        StageGameField = InGameManager.InstStage;
        StartCoroutine(TutorialStarter());
    }

    IEnumerator TutorialStarter()
    {
        yield return null;
        int curNum = UserSetting.TutorialNumber;
        while (true)
        {
            if(curNum == 1)
            {
                if(StageGameField.gameObject.activeInHierarchy && StageGameField.StageNum == 1)
                {
                    Frame frame = StageGameField.Frame(1, 1);
                    TutorialEvent.Start(curNum, TutorialEventType.Click, frame.transform.position, OnEvnetHandler);
                    break;
                }
            }
            else if (curNum == 2)
            {
                if (StageGameField.gameObject.activeInHierarchy && StageGameField.StageNum == 2)
                {
                    Frame frame = StageGameField.Frame(4, 2);
                    TutorialEvent.Start(curNum, TutorialEventType.Right, frame.transform.position, OnEvnetHandler);
                    break;
                }
            }
            yield return null;
        }
    }

    private void OnEvnetHandler(TutorialEventType type)
    {
        int curNum = UserSetting.TutorialNumber;
        if (curNum == 1)
        {
            Frame frame = StageGameField.Frame(1, 1);
            StageGameField.OnClick(frame.ChildProduct.gameObject);
            UserSetting.TutorialNumber = curNum + 1;
            StartCoroutine(TutorialStarter());
        }
        else if (curNum == 2)
        {
            Frame frame = StageGameField.Frame(4, 2);
            StageGameField.OnSwipe(frame.ChildProduct.gameObject, SwipeDirection.RIGHT);
            UserSetting.TutorialNumber = curNum + 1;
            StartCoroutine(TutorialStarter());
        }
    }
}
