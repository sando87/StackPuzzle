using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VerticalFrames : MonoBehaviour
{
    private List<Product> NewProducts = new List<Product>();
    private Frame[] Frames = null;

    private void Start()
    {
        List<Frame> list = new List<Frame>();
        foreach (Transform child in transform)
            list.Add(child.GetComponent<Frame>());

        Frames = list.ToArray();
    }


    public int FrameCount { get { return Frames.Length; } }
    public int Droppingcount { get { return transform.childCount - FrameCount; } }
    public Frame TopFrame { get { return transform.GetChild(FrameCount - 1).GetComponent<Frame>(); } }
    public Frame BottomFrame { get { return transform.GetChild(0).GetComponent<Frame>(); } }
    public int StartToDrop()
    {
        if (NewProducts.Count <= 0)
            return 0;

        int newCount = NewProducts.Count;
        StartToDropFloatingProducts();
        Product topProduct = FindTopProduct();
        StartToDropNewProducts(topProduct);
        return newCount;
    }
    public void AddNewProduct(Product pro)
    {
        pro.gameObject.SetActive(false);
        NewProducts.Add(pro);
    }
    public Frame NextDropEndFrame()
    {
        foreach(Frame frame in Frames)
        {
            if (frame.ChildProduct == null)
                return frame;
        }
        return null;
    }


    private Product FindTopProduct()
    {
        Product[] pros = GetComponentsInChildren<Product>();
        List<Product> list = new List<Product>();
        list.AddRange(pros);
        list.Sort((lsh, rsh) => { return lsh.transform.position.y > rsh.transform.position.y ? -1 : 1; });
        return list[0];
    }
    private void StartToDropFloatingProducts()
    {
        bool isFloating = false;
        foreach (Frame frame in Frames)
        {
            if (isFloating)
            {
                if (frame.ChildProduct != null)
                    frame.ChildProduct.Drop();
            }
            else
            {
                if (frame.ChildProduct == null)
                    isFloating = true;
            }
        }
    }
    private void StartToDropNewProducts(Product topProduct)
    {
        InGameManager mgr = transform.parent.GetComponent<InGameManager>();
        Vector3 startPos = topProduct.transform.position;
        startPos.y = Mathf.Max(startPos.y, TopFrame.transform.position.y);
        Vector3 step = new Vector3(0, mgr.GridSize, 0);
        foreach (Product pro in NewProducts)
        {
            startPos += step;
            pro.gameObject.SetActive(true);
            pro.transform.SetParent(transform);
            pro.transform.position = startPos;
            pro.Drop();
        }
        NewProducts.Clear();
    }
}
