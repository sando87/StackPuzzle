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
                    Frame frame = StageGameField.Frame(2, 1);
                    TutorialEvent.Start(curNum, TutorialEventType.Click, frame.transform.position, OnEvnetHandler);
                    break;
                }
            }
            else if (curNum == 2)
            {
                Frame frame = StageGameField.Frame(4, 2);
                TutorialEvent.Start(curNum, TutorialEventType.Left, frame.transform.position, OnEvnetHandler);
                break;
            }
            else if (curNum == 3)
            {
                Frame frame = StageGameField.Frame(3, 2);
                TutorialEvent.Start(curNum, TutorialEventType.Click, frame.transform.position, OnEvnetHandler);
                break;
            }
            else if (curNum == 4)
            {
                if (StageGameField.gameObject.activeInHierarchy && StageGameField.StageNum == 2)
                {
                    TutorialEvent.Start(curNum, TutorialEventType.Click, Vector3.zero, OnEvnetHandler);
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
            Frame frame = StageGameField.Frame(2, 1);
            StageGameField.OnClick(frame.ChildProduct.gameObject);
            UserSetting.TutorialNumber = curNum + 1;
            StartCoroutine(TutorialStarter());
        }
        else if (curNum == 2)
        {
            Frame frame = StageGameField.Frame(4, 2);
            StageGameField.OnSwipe(frame.ChildProduct.gameObject, SwipeDirection.LEFT);
            UserSetting.TutorialNumber = curNum + 1;
            StartCoroutine(TutorialStarter());
        }
        else if (curNum == 3)
        {
            Frame frame = StageGameField.Frame(3, 2);
            StageGameField.OnClick(frame.ChildProduct.gameObject);
            UserSetting.TutorialNumber = curNum + 1;
            StartCoroutine(TutorialStarter());
        }
        else if (curNum == 4)
        {
            if (type == TutorialEventType.Up)
            {
                Frame frame = StageGameField.Frame(2, 1);
                StageGameField.OnSwipe(frame.ChildProduct.gameObject, SwipeDirection.UP);
            }
            else if (type == TutorialEventType.Click)
            {
                Frame frame = StageGameField.Frame(4, 0);
                StageGameField.OnClick(frame.ChildProduct.gameObject);
                UserSetting.TutorialNumber = curNum + 1;
                StartCoroutine(TutorialStarter());
            }
        }
    }
}
