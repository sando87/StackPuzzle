using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Numbers : MonoBehaviour
{
    public Sprite[] NumberImages;
    public SpriteRenderer First;
    public SpriteRenderer Second;
    public SpriteRenderer Third;

    public int Number = 0;
    public bool Outline = false;

    private void Start()
    {
        float imgWorldWidth = 0.64f;
        if (Number >= 1000)
            Number = 999;

        if (Number < 10)
        {
            First.sprite = NumberImages[Number];
            First.transform.position = transform.position;
            Second.enabled = false;
            Third.enabled = false;
        }
        else if (Number < 100)
        {
            First.sprite = NumberImages[Number % 10];
            Second.sprite = NumberImages[Number / 10];

            Vector3 center = transform.position;
            center.x += (imgWorldWidth * 0.5f);
            First.transform.position = center;
            center.x -= imgWorldWidth;
            Second.transform.position = center;

            Third.enabled = false;
        }
        else if (Number < 1000)
        {
            First.sprite = NumberImages[Number % 10];
            Second.sprite = NumberImages[(Number / 10) % 10];
            Third.sprite = NumberImages[Number / 100];

            Vector3 center = transform.position;
            center.x += imgWorldWidth;
            First.transform.position = center;
            center.x -= imgWorldWidth;
            Second.transform.position = center;
            center.x -= imgWorldWidth;
            Third.transform.position = center;
        }

        First.transform.GetChild(0).gameObject.SetActive(Outline);
        Second.transform.GetChild(0).gameObject.SetActive(Outline);
        Third.transform.GetChild(0).gameObject.SetActive(Outline);
    }
}
