﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VerticalFrames : MonoBehaviour
{
    private List<Product> NewProducts = new List<Product>();
    private Frame[] Frames = null;
    public int MaskOrder { get; private set; } = 0;

    public void init(int maskOrder, float scale)
    {
        List<Frame> list = new List<Frame>();
        foreach (Transform child in transform)
        {
            Frame frame = child.GetComponent<Frame>();
            if(frame != null)
                list.Add(frame);
        }

        Frames = list.ToArray();
        NewProducts.Clear();

        MaskOrder = maskOrder;
        Vector3 centerPos = (Frames[0].transform.position + Frames[Frames.Length - 1].transform.position) * 0.5f;

        SpriteMask mask = new GameObject().AddComponent<SpriteMask>();
        mask.name = "Mask";
        mask.transform.SetParent(transform);
        mask.transform.position = centerPos;
        mask.transform.localScale = new Vector3(scale, list.Count * scale, 1);
        mask.sprite = Resources.Load<Sprite>("Images/spriteMask");
        mask.isCustomRangeActive = true;
        mask.frontSortingOrder = maskOrder + 1;
        mask.backSortingOrder = maskOrder;
    }

    public int FrameCount { get { return Frames.Length; } }
    public int Droppingcount { get { return transform.childCount - FrameCount - 2; } } //dummy ground 하나 더 빼줘야함.
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

    public void StartToDrop()
    {
        if (NewProducts.Count <= 0)
            return;

        Frame firstFrame = FindFirstEmptyFrame();
        if (firstFrame == null)
            return;

        bool isLocked = IsLockedProduct(firstFrame);
        if (isLocked)
            return;

        List<Product> targets = new List<Product>();
        Product[] pros = GetComponentsInChildren<Product>();
        foreach(Product pro in pros)
        {
            if (pro.transform.position.y > firstFrame.transform.position.y)
                targets.Add(pro);
        }
        targets.Sort((lsh, rsh) => { return lsh.transform.position.y < rsh.transform.position.y ? -1 : 1; });
        Vector3 topPosition = targets.Count > 0 ? targets[targets.Count - 1].transform.position : TopFrame.transform.position;
        Product[] newPros = RepositionNewProducts(topPosition);
        targets.AddRange(newPros);

        Frame curFrame = firstFrame;
        foreach(Product target in targets)
        {
            target.StartToDrop(curFrame);
            curFrame = curFrame.Up();
        }

        NewProducts.Clear();
    }
    private Frame FindFirstEmptyFrame()
    {
        foreach(Frame frame in Frames)
        {
            if (!frame.Empty && frame.ChildProduct == null)
                return frame;
        }
        return null;
    }
    private bool IsLockedProduct(Frame startFrame)
    {
        Frame curFrame = startFrame;
        while (curFrame != null)
        {
            Product pro = curFrame.ChildProduct;
            if(pro != null && pro.IsLocked)
                return true;

            curFrame = curFrame.Up();
        }
        return false;
    }
    private Product[] RepositionNewProducts(Vector3 topPosition)
    {
        List<Product> rets = new List<Product>();
        if (NewProducts.Count <= 0)
            return rets.ToArray();

        InGameManager mgr = transform.parent.GetComponent<InGameManager>();
        Vector3 startPos = topPosition;
        startPos.y = Mathf.Max(startPos.y, TopFrame.transform.position.y);
        Vector3 step = new Vector3(0, mgr.GridSize, 0);
        foreach (Product pro in NewProducts)
        {
            startPos += step;
            pro.gameObject.SetActive(true);
            pro.transform.SetParent(transform);
            pro.transform.position = startPos;
            rets.Add(pro);
        }
        return rets.ToArray();
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
