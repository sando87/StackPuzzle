using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InGameManager : MonoBehaviour
{
    public static InGameManager Inst = null;

    public const int MatchCount = 3;
    public const float SwipeDetectRange = 0.1f;
    public const int GridSize = 1;
    public GameObject[] ProductPrefabs;
    public GameObject FramePrefab;

    private Frame[,] mFrames = null;
    private Product mDownProduct = null;
    private Vector3 mDownPosition;
    private StageInfo mStageInfo;

    public Text UIScore;
    public Image UIBar;
    public Text UILimit;
    public GameObject Star1;
    public GameObject Star2;
    public GameObject Star3;
    public GameObject GameField;

    void Awake()
    {
        Inst = this;
        enabled = false;
    }

    void Start()
    {

    }

    void Update()
    {
        if(IsRunning())
            CheckSwipe();
    }

    public Frame GetFrame(int x, int y)
    {
        return mFrames[x, y];
    }

    public void Init(StageInfo info)
    {
        mStageInfo = info;

        UIBar.fillAmount = 0;
        Star1.SetActive(false);
        Star2.SetActive(false);
        Star3.SetActive(false);

        UIScore.text = "0";
        UILimit.text = info.MoveLimit.ToString();
        transform.Find("Canvas/Panel/Panel/Target").GetComponent<Text>().text = info.GoalScore.ToString();
        transform.Find("Canvas/BottomPanel/Panel/Level").GetComponent<Text>().text = info.Num.ToString();

        mFrames = new Frame[info.RowCount, info.ColumnCount];
        for (int y = 0; y < info.ColumnCount; y++)
        {
            for (int x = 0; x < info.RowCount; x++)
            {
                GameObject frameObj = GameObject.Instantiate(FramePrefab, new Vector3(GridSize * x, GridSize * y, 0), Quaternion.identity, GameField.transform);
                mFrames[x, y] = frameObj.GetComponent<Frame>();
                mFrames[x, y].Initialize(x, y, this);
                CreateNewProduct(mFrames[x, y]);
            }
        }
    }
    public int XCount { get { return mStageInfo.RowCount; } }
    public int YCount { get { return mStageInfo.ColumnCount; } }
    public int Limit
    {
        get { return int.Parse(UILimit.text); }
        set { if(value >= 0) UILimit.text = value.ToString(); }
    }
    public int Score
    {
        get { return int.Parse(UIScore.text); }
        set
        {
            UIScore.text = value.ToString();
            UIBar.fillAmount = (float)value / (float)mStageInfo.GoalScore;
            Star1.SetActive(UIBar.fillAmount > 0.3f);
            Star2.SetActive(UIBar.fillAmount > 0.6f);
            Star3.SetActive(UIBar.fillAmount > 0.9f);
        }
    }
    public bool Show
    {
        get
        {
            return transform.Find("Canvas").gameObject.activeSelf
            && transform.Find("CanvasBack").gameObject.activeSelf
            && transform.Find("GameField").gameObject.activeSelf
            && enabled;
        }
        set
        {
            transform.Find("Canvas").gameObject.SetActive(value);
            transform.Find("CanvasBack").gameObject.SetActive(value);
            transform.Find("GameField").gameObject.SetActive(value);
            enabled = value;
        }
        
    }
    public void CreateNewProduct(Frame parent)
    {
        int typeIdx = Random.Range(0, ProductPrefabs.Length);
        GameObject obj = GameObject.Instantiate(ProductPrefabs[typeIdx], parent.transform, false);
        obj.GetComponent<Product>().SetParentFrame(parent);
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

                    if (target != null && !mDownProduct.IsLocked() && !target.IsLocked())
                    {
                        Limit--;
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
    int GetStarCount()
    {
        float rate = (float)Score / (float)mStageInfo.GoalScore;
        if (rate < 0.3f)
            return 0;
        else if (rate < 0.6f)
            return 1;
        else if (rate < 0.9f)
            return 2;
        return 3;
    }
    bool IsRunning()
    {
        if (Score >= mStageInfo.GoalScore)
        {
            mStageInfo.StarCount = GetStarCount();
            StageInfo.Save(mStageInfo);

            StageInfo info = StageInfo.Load(mStageInfo.Num + 1);
            info.IsLocked = false;
            StageInfo.Save(info);
            ResetField();
            MenuComplete.PopUp(mStageInfo.Num, mStageInfo.StarCount, Score);
            
            Show = false;
            return false;
        }
        else if(Limit <= 0)
        {
            mStageInfo.StarCount = GetStarCount();
            StageInfo.Save(mStageInfo);
            ResetField();
            MenuFailed.PopUp(mStageInfo.Num, mStageInfo.GoalScore, Score);
            Show = false;
            return false;
        }
        return true;
    }
    void ResetField()
    {
        int cnt = GameField.transform.childCount;
        for(int i = 0; i < cnt; ++i)
        {
            Destroy(GameField.transform.GetChild(i).gameObject);
        }
        mFrames = null;
    }

}
