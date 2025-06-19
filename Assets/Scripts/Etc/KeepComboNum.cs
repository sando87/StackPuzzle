using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class KeepComboNum : MonoBehaviour
{
    public Sprite[] NumberImages;
    public Sprite[] NumberOutlineImages;

    public GameObject Num0_Parent;
    public SpriteRenderer Num0_First;
    public SpriteRenderer Num0_First_Outline;

    public GameObject Num00_Parent;
    public SpriteRenderer Num00_First;
    public SpriteRenderer Num00_First_Outline;
    public SpriteRenderer Num00_Second;
    public SpriteRenderer Num00_Second_Outline;

    public GameObject Num000_Parent;
    public SpriteRenderer Num000_First;
    public SpriteRenderer Num000_First_Outline;
    public SpriteRenderer Num000_Second;
    public SpriteRenderer Num000_Second_Outline;
    public SpriteRenderer Num000_Third;
    public SpriteRenderer Num000_Third_Outline;

    int mNumber = 0;

    public int GetNumber() { return mNumber; }
    public void SetNumber(int num)
    {
        if(mNumber == num)
            return;
            
        mNumber = num;

        Num0_Parent.gameObject.SetActive(num < 10);
        Num00_Parent.gameObject.SetActive(10 <= num && num < 100);
        Num000_Parent.gameObject.SetActive(100 <= num && num < 1000);

        if(num < 10)
        {
            Num0_First.sprite = NumberImages[num];
            Num0_First_Outline.sprite = NumberOutlineImages[num];
        }
        else if(num < 100)
        {
            Num00_First.sprite = NumberImages[num / 10];
            Num00_First_Outline.sprite = NumberOutlineImages[num / 10];
            Num00_Second.sprite = NumberImages[num % 10];
            Num00_Second_Outline.sprite = NumberOutlineImages[num % 10];
        }
        else
        {
            Num000_First.sprite = NumberImages[num / 100];
            Num000_First_Outline.sprite = NumberOutlineImages[num / 100];
            Num000_Second.sprite = NumberImages[(num % 100) / 10];
            Num000_Second_Outline.sprite = NumberOutlineImages[(num % 100) / 10];
            Num000_Third.sprite = NumberImages[num % 10];
            Num000_Third_Outline.sprite = NumberOutlineImages[num % 10];
        }
    }
}
