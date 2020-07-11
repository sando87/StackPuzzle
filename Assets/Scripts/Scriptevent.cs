using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Scriptevent : MonoBehaviour
{
    void OnClickNewGame()
    {
        Debug.Log("새 게임");
    }

    public void OnClickLoad()
    {
        Debug.Log("불러오기");
    }

    public void OnClickOption()
    {
        Debug.Log("옵션");
    }

    public void OnClickQuit()
    {
        Debug.Log("게임 끝내기");

        #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    public void OnBackButtonEvent()
    {
        Debug.Log("뒤로 가기 터치");
    }
}
