using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ProductColor { Blue, Green, Orange, Purple, Red, Yellow };

public class Product : MonoBehaviour
{
    private bool mLocked = false;
    private Frame mParentFrame = null;

    public GameObject mBeamUpEffect;
    public ProductColor mColor;
    public Animation mAnimation;

    public SpriteRenderer Renderer;
    public Sprite[] Images;
    public int ImageIndex;

    public bool IsLocked() { return mLocked; }
    public Frame ParentFrame
    {
        get
        {
            return mParentFrame;
        }
        set
        {
            transform.SetParent(value.transform);
            mParentFrame = value;
        }
    }


    #region MatchCycle
    public void StartSwipe(Frame target)
    {
        if (mLocked)
            return;

        mLocked = true;
        mAnimation.Play("swap");
        StartCoroutine(AnimateSwipe(target));
    }
    IEnumerator AnimateSwipe(Frame target)
    {
        Vector3 dest = target.transform.position;
        dest.z = transform.position.z;
        float distPerFrame = (InGameManager.GridSize * 3) * Time.deltaTime;
        while ((transform.position - dest).magnitude >= distPerFrame)
        {
            transform.position = Vector3.MoveTowards(transform.position, dest, distPerFrame);
            yield return null;
        }
        transform.position = dest;
        EndSwipe(target);
    }
    void EndSwipe(Frame target)
    {
        ParentFrame = target;
        mLocked = false;
        StartCoroutine(DoMatch());
    }
    IEnumerator DoMatch()
    {
        yield return null;
        List<Product> matchList = new List<Product>();
        SearchMatchedProducts(matchList, mColor);
        if (matchList.Count >= InGameManager.MatchCount)
        {
            InGameManager.Inst.AddScore(1 * matchList.Count, mColor);
            SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectMatched);
            foreach (Product pro in matchList)
            {
                StartCoroutine(pro.StartDestroy());
            }
            StartCoroutine(StartFlashing(matchList));
        }
    }

    IEnumerator StartDestroy()
    {
        mLocked = true;
        yield return null;
        mAnimation.Play("destroy");
    }
    void StartSpriteAnim()
    {
        StartCoroutine(AnimateDestroySprite());
    }
    IEnumerator AnimateDestroySprite()
    {
        while(true) //this Coroutine is stopped when object destroyed.
        {
            Renderer.sprite = Images[ImageIndex];
            yield return null;
        }
    }
    void EndDestroy()
    {
        mLocked = false;
        Destroy(gameObject);
    }

    public void StartToDrop()
    {
        List<Frame> empties = GetEmptyDownFrames();
        List<Frame> idles = GetIdleUpFrames();
        if (idles[0].IsDummy)
        {
            Frame curFrame = empties[empties.Count - 1];
            int idx = 0;
            while (true)
            {
                if (idx < idles.Count)
                {
                    Product pro = idles[idx].ChildProduct;
                    pro.StartDropAnimate(curFrame, empties.Count, idx == 0);
                }
                else
                {
                    Product pro = InGameManager.Inst.CreateNewProduct(curFrame);
                    pro.StartDropAnimate(curFrame, empties.Count, false);
                }
                idx++;

                if (curFrame.IsDummy)
                    break;
                else if(curFrame.Up() == null)
                    curFrame = curFrame.UpDummy();
                else
                    curFrame = curFrame.Up();
            }

        }
        else if (idles[idles.Count - 1].Up() == null)
        {
            idles.Add(idles[idles.Count - 1].UpDummy());
            Frame curFrame = empties[empties.Count - 1];
            int idx = 0;
            while(true)
            {
                if (idx < idles.Count)
                {
                    Product pro = idles[idx].ChildProduct;
                    pro.StartDropAnimate(curFrame, empties.Count, idx == 0);
                }
                else
                {
                    Product pro = InGameManager.Inst.CreateNewProduct(curFrame);
                    pro.StartDropAnimate(curFrame, empties.Count, false);
                }
                idx++;

                if (curFrame.IsDummy)
                    break;
                else if (curFrame.Up() == null)
                    curFrame = curFrame.UpDummy();
                else
                    curFrame = curFrame.Up();
            }
        }
        else
        {
            Frame curFrame = empties[empties.Count - 1];
            for (int i = 0; i < idles.Count; ++i)
            {
                Product pro = idles[i].ChildProduct;
                pro.StartDropAnimate(curFrame, empties.Count, i == 0);
                curFrame = curFrame.Up();
            }
        }
    }
    void StartDropAnimate(Frame parent, int emptyCount, bool isComboable)
    {
        mLocked = true;
        ParentFrame = parent;
        float height = InGameManager.GridSize * emptyCount;
        transform.localPosition = new Vector3(0, height, -1);
        StartCoroutine(AnimateDrop(isComboable, emptyCount));
    }
    IEnumerator AnimateDrop(bool isComboable, int emptyCount)
    {
        int[] delayTable = { 55, 40, 25, 15, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        Vector3 dest = ParentFrame.transform.position;
        dest.z = transform.position.z;
        float distPerFrame = InGameManager.GridSize * Time.deltaTime;
        int dropAnimCnt = 0;
        while (dropAnimCnt < 90)
        {
            if(dropAnimCnt >= delayTable[emptyCount - 1])
            {
                Vector3 nextPos = Vector3.MoveTowards(transform.position, dest, distPerFrame);
                nextPos.y = nextPos.y < dest.y ? dest.y : nextPos.y;
                transform.position = nextPos;
                distPerFrame += 0.001f;
            }
            dropAnimCnt++;
            yield return null;
        }
        transform.position = dest;


        mLocked = false;

        if(isComboable)
            StartCoroutine(DoMatch());
    }

    IEnumerator StartFlashing(List<Product> matchedPros)
    {
        yield return null;
        foreach (Product pro in matchedPros)
        {
            float dist = (pro.transform.position - transform.position).magnitude;
            float delay_sec = dist / 10.0f; //0 ~ 1.0f ~;
            float intensity = 1 - (dist / 5.0f); //1.0f ~ 0 ~;
            StartCoroutine(pro.FlashProduct(delay_sec, intensity));
        }
    }
    IEnumerator FlashProduct(float delay_sec, float intensity)
    {
        yield return new WaitForSeconds(delay_sec);

        float halfTime = 0.12f;
        float k = -intensity / (halfTime * halfTime);
        float t = 0;
        while (t < halfTime * 2)
        {
            float light = k * (t - halfTime) * (t - halfTime) + intensity;
            light = light < 0 ? 0 : light;
            Renderer.material.SetColor("_Color", new Color(light, light, light, 0));
            t += Time.deltaTime;
            yield return null;
        }
        Renderer.material.color = new Color(0, 0, 0, 0);
    }
    #endregion

    #region Support Functions
    void SearchMatchedProducts(List<Product> products, ProductColor color)
    {
        if (mLocked || mColor != color)
            return;

        if (products.Contains(this))
            return;

        products.Add(this);

        Product nearProduct = null;
        nearProduct = Left();
        if (nearProduct != null)
            nearProduct.SearchMatchedProducts(products, color);
        nearProduct = Right();
        if (nearProduct != null)
            nearProduct.SearchMatchedProducts(products, color);
        nearProduct = Up();
        if (nearProduct != null)
            nearProduct.SearchMatchedProducts(products, color);
        nearProduct = Down();
        if (nearProduct != null)
            nearProduct.SearchMatchedProducts(products, color);
    }
    public Product Left()
    {
        Frame nearFrame = ParentFrame.Left();
        if (nearFrame == null)
            return null;

        Product pro = nearFrame.GetComponentInChildren<Product>();
        if (pro == null)
            return null;

        return pro;
    }
    public Product Right()
    {
        Frame nearFrame = ParentFrame.Right();
        if (nearFrame == null)
            return null;

        Product pro = nearFrame.GetComponentInChildren<Product>();
        if (pro == null)
            return null;

        return pro;
    }
    public Product Up()
    {
        Frame nearFrame = ParentFrame.Up();
        if (nearFrame == null)
            return null;

        Product pro = nearFrame.GetComponentInChildren<Product>();
        if (pro == null)
            return null;

        return pro;
    }
    public Product Down()
    {
        Frame nearFrame = ParentFrame.Down();
        if (nearFrame == null)
            return null;

        Product pro = nearFrame.GetComponentInChildren<Product>();
        if (pro == null)
            return null;

        return pro;
    }
    List<Frame> GetEmptyDownFrames()
    {
        List<Frame> emptyFrames = new List<Frame>();
        Frame curFrame = ParentFrame.Down();
        while (curFrame != null && curFrame.ChildProduct == null)
        {
            emptyFrames.Add(curFrame);
            curFrame = curFrame.Down();
        }
        return emptyFrames;
    }
    List<Frame> GetIdleUpFrames()
    {
        List<Frame> idleFrames = new List<Frame>();
        idleFrames.Add(ParentFrame);
        Frame curFrame = ParentFrame.Up();
        while (curFrame != null && curFrame.ChildProduct != null && !curFrame.ChildProduct.IsLocked())
        {
            idleFrames.Add(curFrame);
            curFrame = curFrame.Up();
        }
        return idleFrames;
    }
    #endregion
}
