using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Android;
using UnityEngine.SceneManagement;

public class MenuLogo : MonoBehaviour
{
    [SerializeField] Image LogoForeground = null;

    private void Awake()
    {
        StartCoroutine(CoStart());
    }

    IEnumerator CoStart()
    {
        float duration = 0.5f;
        float time = 0;
        while (time < duration)
        {
            LogoForeground.color = new Color(0, 0, 0, (duration - time) / duration);
            time += Time.deltaTime;
            yield return null;
        }
        LogoForeground.color = new Color(0, 0, 0, 0);

        yield return new WaitForSeconds(3f);

        time = 0;
        while (time < duration)
        {
            LogoForeground.color = new Color(0, 0, 0, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        LogoForeground.color = new Color(0, 0, 0, 1);
        SceneManager.LoadScene("InGame");
    }
}
