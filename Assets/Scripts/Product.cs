using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ProductColor { Blue, Green, Orange, Purple, Red, Yellow };

public class Product : MonoBehaviour
{
    private bool mLocked;
    private Frame mParent;
    public ProductColor mColor;
    public Animator mAnimator;

    public bool IsLocked() { return mLocked; }

    // Start is called before the first frame update
    void Start()
    {
        mLocked = true;
        transform.localScale = Vector3.zero;
        mAnimator.SetTrigger("create");
    }

    // Update is called once per frame
    void Update()
    {
    }


    #region MatchCycle
    public void StartSwipe(Frame target)
    {
        if (mLocked)
            return;

        mLocked = true;
        //mAnimator.SetTrigger("swipe");
        StartCoroutine(AnimateSwipe(target));
    }
    IEnumerator AnimateSwipe(Frame target)
    {
        Vector3 dest = target.transform.position;
        while ((transform.position - dest).magnitude > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, dest, InGameManager.GridSize * Time.deltaTime);
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
    void StartDestroy()
    {
        mLocked = true;
        mAnimator.SetTrigger("destroy");
    }
    void EndDestroy()
    {
        mLocked = false;
        InGameManager.Inst.Score += 10;
        InGameManager.Inst.CreateNewProduct(mParent);
        Destroy(this);
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
                pro.StartDestroy();
            }
        }
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
