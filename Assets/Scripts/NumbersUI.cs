using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NumbersUI : MonoBehaviour
{
    public Animation Anim;
    public Sprite[] NumberImages;
    public Sprite[] NumberOutlineImages;
    public Image First;
    public Image FirstOutline;
    public Image Second;
    public Image SecondOutline;
    public Image Third;
    public Image ThirdOutline;
    public Image ComboText;
    public Image ComboTextOutline;

    private bool Outline = true;
    private float gap = 0.5f;
    private int Number = 0;

    public int GetNumber() { return Number; }
    public void SetNumber(int num)
    {
        if (Number == num)
            return;

        if (num >= 1000)
            Number = 999;
        else if (num < 0)
            Number = 0;
        else
            Number = num;

        ResetRenderState();

        float imgWorldWidth = 0.64f * gap;
        if (Number < 10)
        {
            First.sprite = NumberImages[Number];
            FirstOutline.sprite = NumberOutlineImages[Number];
            First.transform.position = transform.position;

            First.gameObject.SetActive(true);
            Second.gameObject.SetActive(false);
            Third.gameObject.SetActive(false);
            FirstOutline.gameObject.SetActive(Outline);
            SecondOutline.gameObject.SetActive(false);
            ThirdOutline.gameObject.SetActive(false);
        }
        else if (Number < 100)
        {
            First.sprite = NumberImages[Number % 10];
            Second.sprite = NumberImages[Number / 10];

            FirstOutline.sprite = NumberOutlineImages[Number % 10];
            SecondOutline.sprite = NumberOutlineImages[Number / 10];

            Vector3 center = transform.position;
            center.x += (imgWorldWidth * 0.5f);
            First.transform.position = center;
            center.x -= imgWorldWidth;
            Second.transform.position = center;

            First.gameObject.SetActive(true);
            Second.gameObject.SetActive(true);
            Third.gameObject.SetActive(false);
            FirstOutline.gameObject.SetActive(Outline);
            SecondOutline.gameObject.SetActive(Outline);
            ThirdOutline.gameObject.SetActive(false);
        }
        else if (Number < 1000)
        {
            First.sprite = NumberImages[Number % 10];
            Second.sprite = NumberImages[(Number / 10) % 10];
            Third.sprite = NumberImages[Number / 100];

            FirstOutline.sprite = NumberOutlineImages[Number % 10];
            SecondOutline.sprite = NumberOutlineImages[(Number / 10) % 10];
            ThirdOutline.sprite = NumberOutlineImages[Number / 100];

            Vector3 center = transform.position;
            center.x += imgWorldWidth;
            First.transform.position = center;
            center.x -= imgWorldWidth;
            Second.transform.position = center;
            center.x -= imgWorldWidth;
            Third.transform.position = center;

            First.gameObject.SetActive(true);
            Second.gameObject.SetActive(true);
            Third.gameObject.SetActive(true);
            FirstOutline.gameObject.SetActive(Outline);
            SecondOutline.gameObject.SetActive(Outline);
            ThirdOutline.gameObject.SetActive(Outline);
        }


        gameObject.SetActive(true);
        Anim.Play("comboUI");
    }

    public void BreakCombo()
    {
        Number = 0;
        if (gameObject.activeSelf)
        {
            StopCoroutine("Disappear");
            StartCoroutine("Disappear");
        }
    }

    IEnumerator Disappear()
    {
        float time = 0;
        Color white = Color.white;
        Vector3 offset = new Vector3(0, 0.01f, 0);
        while (time < 1)
        {
            white.a = 1 - time;
            First.color = white;
            FirstOutline.color = white;
            Second.color = white;
            SecondOutline.color = white;
            Third.color = white;
            ThirdOutline.color = white;
            ComboText.color = white;
            ComboTextOutline.color = white;
            transform.position += offset;
            time += Time.deltaTime;
            yield return null;
        }

        gameObject.SetActive(false);
    }

    private void ResetRenderState()
    {
        StopCoroutine("Disappear");
        First.color = Color.white;
        FirstOutline.color = Color.white;
        Second.color = Color.white;
        SecondOutline.color = Color.white;
        Third.color = Color.white;
        ThirdOutline.color = Color.white;
        ComboText.color = Color.white;
        ComboTextOutline.color = Color.white;
        GetComponent<RectTransform>().localPosition = new Vector3(0, -130.0f);
    }

    public void Clear()
    {
        Number = 0;
        gameObject.SetActive(false);
    }
}
