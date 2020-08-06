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
            if (mParentFrame == null)
                mParentFrame = transform.parent.GetComponent<Frame>();
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
            foreach (Product pro in matchList)
            {
                StartCoroutine(pro.StartDestroy());
            }
            yield return null;
            ReadyToDropProducts(matchList);
        }
    }
    void ReadyToDropProducts(List<Product> matches)
    {
        Dictionary<int, List<Product>> verties = new Dictionary<int, List<Product>>();
        foreach (Product pro in matches)
        {
            Product upPro = pro.Up();
            if (upPro == null)
            {
                pro.ParentFrame.UpDummy().GetProduct().ReadyToDropAnimate();
            }
            else if (upPro.IsLocked())
                continue;
            else
                upPro.ReadyToDropAnimate();
        }
    }

    IEnumerator StartDestroy()
    {
        mLocked = true;
        yield return null;
        ParentFrame.EnableMask(true);
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
        InGameManager.Inst.CurrentScore += 10;
    }

    void ReadyToDropAnimate()
    {
        mLocked = true;
        StartCoroutine(WaitDropAnimate());
    }
    IEnumerator WaitDropAnimate()
    {
        float duration = 1;
        float t = 0;
        while (t < duration)
        {
            int bright = 1 - ((int)(t * 4.0f) % 2);
            Renderer.material.SetColor("_Color", new Color(bright, bright, bright, 0));
            t += Time.deltaTime;
            yield return null;
        }
        Renderer.material.color = new Color(0, 0, 0, 0);
        mLocked = false;

        List<Frame> emptyFrames = new List<Frame>();
        emptyFrames.Add(ParentFrame);
        Frame curFrame = ParentFrame.Down();
        while (curFrame != null && curFrame.GetProduct() == null)
        {
            emptyFrames.Add(curFrame);
            curFrame = curFrame.Down();
        }

        if (emptyFrames.Count >= 2)
        {
            float height = InGameManager.GridSize * (emptyFrames.Count - 1);
            for (int i = 0; i < emptyFrames.Count - 1; ++i)
            {
                Product pro = InGameManager.Inst.CreateNewProduct(emptyFrames[i]);
                pro.StartDropAnimate(emptyFrames[i], height, false);
            }
            StartDropAnimate(emptyFrames[emptyFrames.Count - 1], height, true);
        }
    }
    void StartDropAnimate(Frame parent, float height, bool isComboable, float delay = 0)
    {
        mLocked = true;
        ParentFrame = parent;
        transform.localPosition = new Vector3(0, height, -1);
        if(!ParentFrame.IsDummy)
            ParentFrame.EnableMask(true);
        Renderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        StartCoroutine(AnimateDrop(isComboable, delay));
    }
    IEnumerator AnimateDrop(bool isComboable, float delay)
    {
        if(delay > 0)
            yield return new WaitForSeconds(delay);
        else
            yield return null;

        Vector3 dest = ParentFrame.transform.position;
        dest.z = transform.position.z;
        float distPerFrame = InGameManager.GridSize * Time.deltaTime;
        while ((transform.position - dest).magnitude >= distPerFrame)
        {
            transform.position = Vector3.MoveTowards(transform.position, dest, distPerFrame);
            distPerFrame += 0.001f;
            yield return null;
        }
        transform.position = dest;

        mLocked = false;
        ParentFrame.EnableMask(false);
        Renderer.maskInteraction = ParentFrame.IsDummy ? SpriteMaskInteraction.VisibleInsideMask : SpriteMaskInteraction.None;

        if(isComboable)
        {
            StartCoroutine(StartFlashing());

            //yield return new WaitForSeconds(0.5f);

            StartCoroutine(DoMatch());
        }
    }
    IEnumerator StartFlashing()
    {
        yield return null;
        List<Product> matchList = new List<Product>();
        SearchMatchedProducts(matchList, mColor);
        if (matchList.Count >= InGameManager.MatchCount)
        {
            foreach (Product pro in matchList)
            {
                float dist = (pro.transform.position - transform.position).magnitude;
                float delay_sec = dist / 10.0f; //0 ~ 1.0f ~;
                float intensity = 1 - (dist / 5.0f); //1.0f ~ 0 ~;
                StartCoroutine(pro.FlashProduct(delay_sec, intensity));
            }
        }
        else
        {
            StartCoroutine(FlashProduct(0, 0));
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
    AnimationClip GetAnimation(string name)
    {
        foreach (AnimationState c in mAnimation)
            if (c.name == name)
                return c.clip;
        return null;
    }
    Product FindComboableProduct()
    {
        Product downPro = Down();
        if (downPro != null && downPro.IsLocked())
            return null;

        Product curPro = Up();
        while (curPro != null && curPro.IsLocked())
            curPro = curPro.Up();

        if (curPro == null)
            return curPro;

        //List<Product> matchList = new List<Product>();
        //Product nearProduct = null;
        //nearProduct = Left();
        //if (nearProduct != null)
        //    nearProduct.SearchMatchedProducts(matchList, curPro.mColor);
        //nearProduct = Right();
        //if (nearProduct != null)
        //    nearProduct.SearchMatchedProducts(matchList, curPro.mColor);
        //nearProduct = Down();
        //if (nearProduct != null)
        //    nearProduct.SearchMatchedProducts(matchList, curPro.mColor);
        //
        //if (matchList.Count < InGameManager.MatchCount - 1)
        //    return null;

        return curPro;
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
    #endregion
}
