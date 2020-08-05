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
            CreateNextProducts(matchList);
            foreach (Product pro in matchList)
            {
                StartCoroutine(pro.StartDestroy());
            }
        }
    }
    void CreateNextProducts(List<Product> matches)
    {
        Dictionary<int, List<Product>> verties = new Dictionary<int, List<Product>>();
        foreach (Product pro in matches)
        {
            if(!verties.ContainsKey(pro.ParentFrame.IndexX))
                verties[pro.ParentFrame.IndexX] = new List<Product>();

            verties[pro.ParentFrame.IndexX].Add(pro);
        }

        foreach (var line in verties)
        {
            line.Value.Sort((lsh, rhs) => lsh.ParentFrame.IndexY.CompareTo(rhs.ParentFrame.IndexY));

            Product top = line.Value[line.Value.Count - 1];
            Product bottom = line.Value[0];
            int diffCount = top.ParentFrame.IndexY - bottom.ParentFrame.IndexY + 1;
            Frame frame = top.ParentFrame.Up();
            if (frame == null)
            {
                frame = top.ParentFrame;
                while (true)
                {
                    Product pro = InGameManager.Inst.CreateNewProduct(frame);
                    pro.GetComponent<SpriteRenderer>().enabled = false;
                    pro.StartDropAnimate(frame, InGameManager.GridSize * diffCount);
                    
                    if (frame == bottom.ParentFrame)
                        break;
                    else
                        frame = frame.Down();
                }
            }
            else
            {
                while(frame != bottom.ParentFrame)
                {
                    Product pro = InGameManager.Inst.CreateNewProduct(frame);
                    pro.GetComponent<SpriteRenderer>().enabled = false;
                    pro.StartDropAnimate(frame, InGameManager.GridSize * diffCount);
                    frame = frame.Down();
                }
                top.Up().StartDropAnimate(bottom.ParentFrame, InGameManager.GridSize * diffCount);
            }
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

    void StartDropAnimate(Frame parent, float height)
    {
        mLocked = true;
        ParentFrame = parent;
        transform.localPosition = new Vector3(0, height, -1);
        ParentFrame.EnableMask(true);
        GetComponent<SpriteRenderer>().maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        StartCoroutine(AnimateDrop());
    }
    IEnumerator AnimateDrop()
    {
        yield return null;
        SpriteRenderer render = GetComponent<SpriteRenderer>();
        bool isComboable = render.enabled;

        Color color = Color.white;
        Color step = new Color(0.01f, 0.01f, 0.01f, 0);
        float time = 0;
        while (time < 1)
        {
            if(isComboable)
            {
                //render.material.SetColor("_Color", color);
                //render.material.color = color;
                color -= step;
            }
            
            time += Time.deltaTime;
            yield return null;
        }

        render.enabled = true;
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
        GetComponent<SpriteRenderer>().maskInteraction = SpriteMaskInteraction.None;

        if(isComboable)
        {
            StartCoroutine(StartFlashing());

            while (time > 0)
            {
                //render.material.SetColor("_Color", color);
                //render.material.color = color;
                //color += step;

                time -= Time.deltaTime;
                yield return null;
            }

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
                float delay_sec = dist / 5.0f; //0 ~ 1.0f ~;
                float intensity = dist / 5.0f; //0 ~ 1.0f ~;
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

        SpriteRenderer render = GetComponent<SpriteRenderer>();
        Color color = new Color(intensity, intensity, intensity, 1);
        Color step = new Color(0.01f, 0.01f, 0.01f, 0);
        while (color.r < 0.9f)
        {
            //render.material.SetColor("_Color", color);
            //render.material.color = color;
            color += step;
            yield return null;
        }
        render.material.color = Color.white;
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
