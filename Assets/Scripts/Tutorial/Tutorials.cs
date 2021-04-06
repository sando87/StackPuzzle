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
            else if (curNum == 4) //콤보 튜토리얼
            {
                if (StageGameField.gameObject.activeInHierarchy && StageGameField.StageNum == 2)
                {
                    TutorialEvent.Start(curNum, TutorialEventType.Click, Vector3.zero, OnEvnetHandler);
                    break;
                }
            }
            else if (curNum == 5) //1개 아이템 스킬 튜토리얼
            {
            }
            else if (curNum == 6) //2개 아이템 조합 스킬 튜토리얼
            {
                if (StageGameField.gameObject.activeInHierarchy && StageGameField.StageNum == 3)
                {
                    Frame frame = StageGameField.Frame(3, 5);
                    TutorialEvent.Start(curNum, TutorialEventType.Click, frame.transform.position, OnEvnetHandler);
                    break;
                }
            }
            else if (curNum == 7) //얼음 블럭 깨는 튜토리얼
            {
                if (StageGameField.gameObject.activeInHierarchy && StageGameField.StageNum == 4)
                {
                    Frame frame = StageGameField.Frame(3, 2);
                    TutorialEvent.Start(curNum, TutorialEventType.Click, frame.transform.position, OnEvnetHandler);
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
            TutorialEventCombo tutorial4 = FindObjectOfType<TutorialEventCombo>();
            if(tutorial4 != null)
            {
                if (type == TutorialEventType.Up)
                {
                    Frame frame = StageGameField.Frame(2, 1);
                    StageGameField.OnSwipe(frame.ChildProduct.gameObject, SwipeDirection.UP);
                }
                else if (type == TutorialEventType.Click)
                {
                    Frame frame = StageGameField.Frame(5, 0);
                    StageGameField.OnClick(frame.ChildProduct.gameObject);
                }
                else if (type == TutorialEventType.Click2)
                {
                    curNum++;
                    Frame frame = StageGameField.Frame(2, 5);
                    TutorialEvent.Start(curNum, TutorialEventType.Down, frame.transform.position, OnEvnetHandler);
                }
            }
            else
            {
                if (type == TutorialEventType.Down)
                {
                    Frame frame = StageGameField.Frame(2, 5);
                    StageGameField.OnSwipe(frame.ChildProduct.gameObject, SwipeDirection.DOWN);
                }
                else if (type == TutorialEventType.Left)
                {
                    Frame frame = StageGameField.Frame(4, 3);
                    StageGameField.OnSwipe(frame.ChildProduct.gameObject, SwipeDirection.LEFT);
                }
                else if (type == TutorialEventType.Click)
                {
                    Frame frame = StageGameField.Frame(3, 3);
                    StageGameField.OnClick(frame.ChildProduct.gameObject);
                }
                else if (type == TutorialEventType.Click2)
                {
                    Frame frame = StageGameField.Frame(3, 3);
                    StageGameField.OnClick(frame.ChildProduct.gameObject);
                    UserSetting.TutorialNumber = UserSetting.TutorialNumber + 2;
                    StartCoroutine(TutorialStarter());
                }
            }
        }
        else if (curNum == 5)
        {
        }
        else if (curNum == 6)
        {
            if (type == TutorialEventType.Down)
            {
                Frame frame = StageGameField.Frame(3, 3);
                StageGameField.OnSwipe(frame.ChildProduct.gameObject, SwipeDirection.DOWN);

                UserSetting.TutorialNumber = curNum + 1;
                StartCoroutine(TutorialStarter());
            }
            else if (type == TutorialEventType.Click)
            {
                Frame frame = StageGameField.Frame(3, 5);
                StageGameField.OnClick(frame.ChildProduct.gameObject);
            }
        }
        else if (curNum == 7)
        {
            if (type == TutorialEventType.Click)
            {
                Frame frame = StageGameField.Frame(3, 2);
                StageGameField.OnClick(frame.ChildProduct.gameObject);
                UserSetting.TutorialNumber = curNum + 1;
            }
        }
    }
}
