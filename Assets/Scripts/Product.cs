using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ProductColor { Blue, Green, Orange, Purple, Red, Yellow };

public class Product : MonoBehaviour
{
    private bool mLocked;
    private Frame mParent;
    private Frame mDropTarget;
    private Product mComboProduct;
    private AnimationClip mSwipeAnim;

    public GameObject mBeamUpEffect;
    public ProductColor mColor;
    public Animation mAnimation;

    public SpriteRenderer Renderer;
    public Sprite[] Images;
    public int ImageIndex;

    public bool IsLocked() { return mLocked; }

    // Start is called before the first frame update
    void Start()
    {
        mLocked = true;
        transform.localScale = Vector3.zero;
        mSwipeAnim = GetAnimation("swap");
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
        float distPerFrame = (InGameManager.GridSize / mSwipeAnim.length) * Time.deltaTime;
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
        transform.SetParent(target.transform);
        mParent = target;
        mLocked = false;
        StartCoroutine(DoMatch());
    }

    IEnumerator StartDestroy()
    {
        mLocked = true;
        yield return null;
        mAnimation.Play("destroy");
        mComboProduct = FindComboableProduct();
        if (mComboProduct != null)
            mComboProduct.StartDropAnimate(mParent);
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

        if (mComboProduct == null)
            InGameManager.Inst.CreateNewProduct(mParent);
    }

    void EndCreate()
    {
        mLocked = false;
        StartCoroutine(DoMatch());
    }

    void StartDropAnimate(Frame frame)
    {
        mLocked = true;
        //mAnimation.Play("drop");
        mDropTarget = frame;
        StartCoroutine(AnimateDrop());
    }
    void StartDropMove()
    {
        Vector3 midPos = (mParent.transform.position + mDropTarget.transform.position) * 0.5f;
        midPos.z = -2;
        GameObject.Instantiate(mBeamUpEffect, midPos, Quaternion.identity, transform);
        InGameManager.Inst.CreateNewProduct(mParent);
        transform.position = mDropTarget.transform.position;
        transform.SetParent(mDropTarget.transform);
        mParent = mDropTarget;
    }
    IEnumerator AnimateDrop()
    {
        yield return new WaitForSeconds(0.5f);
        Vector3 dest = mDropTarget.transform.position;
        dest.z = transform.position.z;
        float distPerFrame = InGameManager.GridSize * Time.deltaTime;
        while ((transform.position - dest).magnitude >= distPerFrame)
        {
            transform.position = Vector3.MoveTowards(transform.position, dest, distPerFrame);
            distPerFrame += 0.001f;
            yield return null;
        }
        transform.position = dest;

        InGameManager.Inst.CreateNewProduct(mParent);
        transform.SetParent(mDropTarget.transform);
        mParent = mDropTarget;
        mDropTarget = null;
        mLocked = false;
        StartCoroutine(DoMatch());
    }
    void EndDrop()
    {
        mLocked = false;
        mDropTarget = null;
        StartCoroutine(DoMatch());
    }
    #endregion

    #region Support Functions
    public void SetParentFrame(Frame parent)
    {
        mParent = parent;
    }
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
        }
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

        List<Product> matchList = new List<Product>();
        Product nearProduct = null;
        nearProduct = Left();
        if (nearProduct != null)
            nearProduct.SearchMatchedProducts(matchList, curPro.mColor);
        nearProduct = Right();
        if (nearProduct != null)
            nearProduct.SearchMatchedProducts(matchList, curPro.mColor);
        nearProduct = Down();
        if (nearProduct != null)
            nearProduct.SearchMatchedProducts(matchList, curPro.mColor);

        if (matchList.Count < InGameManager.MatchCount - 1)
            return null;

        return curPro;
    }
    public Product Left()
    {
        Frame nearFrame = mParent.Left();
        if (nearFrame == null)
            return null;

        Product pro = nearFrame.GetComponentInChildren<Product>();
        if (pro == null)
            return null;

        return pro;
    }
    public Product Right()
    {
        Frame nearFrame = mParent.Right();
        if (nearFrame == null)
            return null;

        Product pro = nearFrame.GetComponentInChildren<Product>();
        if (pro == null)
            return null;

        return pro;
    }
    public Product Up()
    {
        Frame nearFrame = mParent.Up();
        if (nearFrame == null)
            return null;

        Product pro = nearFrame.GetComponentInChildren<Product>();
        if (pro == null)
            return null;

        return pro;
    }
    public Product Down()
    {
        Frame nearFrame = mParent.Down();
        if (nearFrame == null)
            return null;

        Product pro = nearFrame.GetComponentInChildren<Product>();
        if (pro == null)
            return null;

        return pro;
    }
    #endregion
}
