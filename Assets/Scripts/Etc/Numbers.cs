﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Numbers : MonoBehaviour
{
    public Sprite[] NumberImages;
    public Sprite[] NumberOutlineImages;
    public SpriteRenderer First;
    public SpriteRenderer FirstOutline;
    public SpriteRenderer Second;
    public SpriteRenderer SecondOutline;
    public SpriteRenderer Third;
    public SpriteRenderer ThirdOutline;

    public Color NumberColor = Color.white;
    public int Number = 321;
    public bool Outline = true;
    public float gap = 0.3f;

    private void Start()
    {
        float imgWorldWidth = 0.64f * gap;
        if (Number >= 1000)
            Number = 999;
        if (Number < 0)
            Number = 0;

        if (Number < 10)
        {
            First.sprite = NumberImages[Number];
            First.color = NumberColor;
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
            First.color = NumberColor;
            Second.sprite = NumberImages[Number / 10];
            Second.color = NumberColor;

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
            First.color = NumberColor;
            Second.sprite = NumberImages[(Number / 10) % 10];
            Second.color = NumberColor;
            Third.sprite = NumberImages[Number / 100];
            Third.color = NumberColor;

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
    }
}
