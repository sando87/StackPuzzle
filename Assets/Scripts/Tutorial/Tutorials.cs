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
        StartCoroutine(TutorialStarter());
    }

    IEnumerator TutorialStarter()
    {
        yield return null;
        StageGameField = InGameManager.InstStage;
        int curNum = UserSetting.TutorialNumber;
        while (true)
        {
            if (curNum == 1)
            {
                if (StageGameField.gameObject.activeInHierarchy && StageGameField.StageNum == 1)
                {
                    Frame frame = StageGameField.Frame(4, 2);
                    TutorialEvent.Start(curNum, TutorialEventType.Click, frame.transform.position, OnEvnetHandler);
                    break;
                }
            }
            else if (curNum == 2)
            {
                Frame frame = StageGameField.Frame(1, 2);
                TutorialEvent.Start(curNum, TutorialEventType.Right, frame.transform.position, OnEvnetHandler);
                break;
            }
            else if (curNum == 3)
            {
                Frame frame = StageGameField.Frame(2, 1);
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
            else if (curNum == 5) //Battle(PVP)모드 개방
            {
                if (MenuStages.Inst.gameObject.activeInHierarchy && UserSetting.GetHighestStageNumber() >= UserSetting.BattleModeUnlockStage)
                {
                    TutorialEvent.Start(curNum, TutorialEventType.Click, MenuStages.Inst.BattleButton.transform.position, OnEvnetHandler);
                    break;
                }
            }
            else
                break;
            yield return null;
        }
    }

    private void OnEvnetHandler(TutorialEventType type)
    {
        int curNum = UserSetting.TutorialNumber;
        if (curNum == 1)
        {
            Frame frame = StageGameField.Frame(4, 2);
            StageGameField.OnClick(frame.ChildProduct.gameObject);
            UserSetting.TutorialNumber = curNum + 1;
            StartCoroutine(TutorialStarter());
        }
        else if (curNum == 2)
        {
            Frame frame = StageGameField.Frame(1, 2);
            StageGameField.OnSwipe(frame.ChildProduct.gameObject, SwipeDirection.RIGHT);
            UserSetting.TutorialNumber = curNum + 1;
            StartCoroutine(TutorialStarter());
        }
        else if (curNum == 3)
        {
            Frame frame = StageGameField.Frame(2, 1);
            StageGameField.OnClick(frame.ChildProduct.gameObject);
            UserSetting.TutorialNumber = curNum + 1;
            StartCoroutine(TutorialStarter());
        }
        else if (curNum == 4)
        {
            TutorialEventCombo tutorial4 = FindObjectOfType<TutorialEventCombo>();
            if (type == TutorialEventType.Down)
            {
                Frame frame = StageGameField.Frame(0, 1);
                StageGameField.OnSwipe(frame.ChildProduct.gameObject, SwipeDirection.DOWN);
            }
            else if (type == TutorialEventType.Click)
            {
                Frame frame = StageGameField.Frame(0, 0);
                StageGameField.OnClick(frame.ChildProduct.gameObject);
            }
            else if (type == TutorialEventType.Click2)
            {
                UserSetting.TutorialNumber = curNum + 1;
                StartCoroutine(TutorialStarter());
            }
        }
        else if(curNum == 5)
        {
            UserSetting.TutorialNumber = curNum + 1;
            StartCoroutine(TutorialStarter());
        }
    }
}
