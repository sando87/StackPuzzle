using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ProductManager : MonoBehaviour
{
    public static ProductManager Inst = null;

    public const int MatchCount = 3;
    public const float SwipeDetectRange = 0.1f;
    public const int XCount = 5;
    public const int YCount = 5;
    public const int GridSize = 1;
    public GameObject[] ProductPrefabs;
    public GameObject FramePrefab;

    private Frame[,] mFrames = null;
    private Product mDownProduct = null;
    private Vector3 mDownPosition;

    void Awake()
    {
        Inst = this;
    }

    void Start()
    {
        mFrames = new Frame[XCount, YCount];
        for (int y = 0; y < YCount; y++)
        {
            for (int x = 0; x < XCount; x++)
            {
                GameObject frameObj = GameObject.Instantiate(FramePrefab, new Vector3(GridSize * x, GridSize * y, 0), Quaternion.identity, transform);
                mFrames[x, y] = frameObj.GetComponent<Frame>();
                mFrames[x, y].Initialize(x, y, this);
                CreateNewProduct(mFrames[x, y]);
            }
        }
    }

    void Update()
    {
        CheckSwipe();
    }

    public Frame GetFrame(int x, int y)
    {
        return mFrames[x, y];
    }

    public void CreateNewProduct(Frame parent)
    {
        int typeIdx = Random.Range(0, ProductPrefabs.Length);
        GameObject obj = GameObject.Instantiate(ProductPrefabs[typeIdx], parent.transform, false);
        obj.GetComponent<Product>().StartCreate(parent);
    }
    void CheckSwipe()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 worldPt = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Collider2D hit = Physics2D.OverlapPoint(worldPt);
            if (hit != null)
            {
                mDownProduct = hit.gameObject.GetComponent<Product>();
                mDownPosition = worldPt;
            }
        }
        else if (Input.GetMouseButton(0))
        {
            if(mDownProduct != null)
            {
                Vector3 curWorldPt = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                if ((curWorldPt - mDownPosition).magnitude >= SwipeDetectRange)
                {
                    Vector2 _currentSwipe = new Vector2(curWorldPt.x - mDownPosition.x, curWorldPt.y - mDownPosition.y);
                    _currentSwipe.Normalize();

                    Product target = null;
                    if (_currentSwipe.y > 0 && _currentSwipe.x > -0.5f && _currentSwipe.x < 0.5f)
                        target = mDownProduct.Up();
                    if (_currentSwipe.y < 0 && _currentSwipe.x > -0.5f && _currentSwipe.x < 0.5f)
                        target = mDownProduct.Down();
                    if (_currentSwipe.x < 0 && _currentSwipe.y > -0.5f && _currentSwipe.y < 0.5f)
                        target = mDownProduct.Left();
                    if (_currentSwipe.x > 0 && _currentSwipe.y > -0.5f && _currentSwipe.y < 0.5f)
                        target = mDownProduct.Right();

                    if (target != null && !mDownProduct.Locked && !target.Locked)
                    {
                        mDownProduct.StartSwipe(target.GetComponentInParent<Frame>());
                        target.StartSwipe(mDownProduct.GetComponentInParent<Frame>());
                    }

                    mDownProduct = null;
                }
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            mDownProduct = null;
        }
    }


}
