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
    public ProductColor mColor;
    public Animator mAnimator;
    public Product mComboProduct;
    public GameObject mBeamUpEffect;

    public SpriteRenderer Renderer;
    public Sprite[] Images;
    public int ImageIndex;

    public bool IsLocked() { return mLocked; }

    // Start is called before the first frame update
    void Start()
    {
        mLocked = true;
        transform.localScale = Vector3.zero;
    }

    // Update is called once per frame
    void Update()
    {
        Renderer.sprite = Images[ImageIndex];
    }


    #region MatchCycle
    public void StartSwipe(Frame target)
    {
        if (mLocked)
            return;

        mLocked = true;
        mAnimator.SetTrigger("swap");
        StartCoroutine(AnimateSwipe(target));
    }
    IEnumerator AnimateSwipe(Frame target)
    {
        int frameCount = 20;
        Vector3 dest = target.transform.position;
        dest.z = transform.position.z;
        float distPerFrame = (float)InGameManager.GridSize / ((float)frameCount * Time.deltaTime);
        while (frameCount > 0)
        {
            transform.position = Vector3.MoveTowards(transform.position, dest, distPerFrame * Time.deltaTime);
            frameCount--;
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
        mAnimator.SetTrigger("destroy");
        mComboProduct = FindComboableProduct();
        if (mComboProduct != null)
            mComboProduct.StartDrop(mParent);
    }
    void EndDestroy()
    {
        mLocked = false;
        Destroy(gameObject);
        InGameManager.Inst.CurrentScore += 10;

        if(mComboProduct == null)
            InGameManager.Inst.CreateNewProduct(mParent);
        
    }
    public void SetParentFrame(Frame parent)
    {
        mParent = parent;
    }
    void EndCreate()
    {
        mLocked = false;
        StartCoroutine(DoMatch());
    }
    void StartDrop(Frame frame)
    {
        mLocked = true;
        mAnimator.SetTrigger("drop");
        mDropTarget = frame;
    }
    void MidDrop()
    {
        Vector3 midPos = (mParent.transform.position + mDropTarget.transform.position) * 0.5f;
        midPos.z = -2;
        GameObject.Instantiate(mBeamUpEffect, midPos, Quaternion.identity, transform);
        InGameManager.Inst.CreateNewProduct(mParent);
        transform.position = mDropTarget.transform.position;
        transform.SetParent(mDropTarget.transform);
        mParent = mDropTarget;
    }
    void EndDrop()
    {
        mLocked = false;
        mDropTarget = null;
        StartCoroutine(DoMatch());
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
