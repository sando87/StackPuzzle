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
        {
            Frame frame = child.GetComponent<Frame>();
            if(frame != null)
                list.Add(frame);
        }

        Frames = list.ToArray();
    }


    public int FrameCount { get { return Frames.Length; } }
    public int Droppingcount { get { return transform.childCount - FrameCount - 1; } } //dummy ground 하나 더 빼줘야함.
    public Frame TopFrame { get { return Frames[Frames.Length - 1]; } }
    public Frame BottomFrame { get { return Frames[0]; } }
    public Product[] StartToDropNewProducts()
    {
        List<Product> droppedPros = new List<Product>();
        if (NewProducts.Count <= 0)
            return droppedPros.ToArray();

        InGameManager mgr = transform.parent.GetComponent<InGameManager>();
        Vector3 startPos = FindTopPosition();
        startPos.y = Mathf.Max(startPos.y, TopFrame.transform.position.y);
        Vector3 step = new Vector3(0, mgr.GridSize, 0);
        foreach (Product pro in NewProducts)
        {
            startPos += step;
            pro.gameObject.SetActive(true);
            pro.transform.SetParent(transform);
            pro.transform.position = startPos;
            pro.Drop();
            droppedPros.Add(pro);
        }
        NewProducts.Clear();
        return droppedPros.ToArray();
    }
    public void AddNewProduct(Product pro)
    {
        pro.gameObject.SetActive(false);
        NewProducts.Add(pro);
    }
    public void AddNDropNewProduct(Product pro)
    {
        InGameManager mgr = transform.parent.GetComponent<InGameManager>();
        Vector3 startPos = FindTopPosition();
        startPos.y = Mathf.Max(startPos.y, TopFrame.transform.position.y);
        Vector3 step = new Vector3(0, mgr.GridSize, 0);
        startPos += step;
        pro.gameObject.SetActive(true);
        pro.transform.SetParent(transform);
        pro.transform.position = startPos;
        pro.Drop();
    }
    public Product[] StartToDropFloatingProducts()
    {
        List<Product> droppedPros = new List<Product>();
        bool isFloating = false;
        foreach (Frame frame in Frames)
        {
            if (isFloating)
            {
                Product pro = frame.ChildProduct;
                if (pro != null)
                {
                    if (pro.IsLocked)
                        break;
                    else
                    {
                        pro.Drop();
                        droppedPros.Add(pro);
                    }
                }
            }
            else
            {
                if (frame.ChildProduct == null)
                    isFloating = true;
            }
        }
        return droppedPros.ToArray();
    }


    private Vector3 FindTopPosition()
    {
        Product[] pros = GetComponentsInChildren<Product>();
        if (pros.Length <= 0)
            return TopFrame.transform.position;

        List<Product> list = new List<Product>();
        list.AddRange(pros);
        list.Sort((lsh, rsh) => { return lsh.transform.position.y > rsh.transform.position.y ? -1 : 1; });
        return list[0].transform.position;
    }
}
