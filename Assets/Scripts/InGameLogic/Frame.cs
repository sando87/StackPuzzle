using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;


public class Frame : MonoBehaviour
{
    private int mCoverCount;
    private int mBushIndex;

    public Sprite[] Covers;
    public Sprite[] Bushes;
    public SpriteRenderer[] Borders;
    public SpriteRenderer CoverRenderer;
    public GameObject BreakStonesParticle;
    public GameObject BushObject;
    public GameObject BushEffectPrefab;
    public GameObject ComboNumPrefab;

    public VerticalFrames VertFrames { get { return transform.parent.GetComponent<VerticalFrames>(); } }
    public InGameManager GameManager { get; private set; }
    public bool Empty { get; private set; }
    public int IndexX { get; private set; }
    public int IndexY { get; private set; }
    public bool IsBottom { get { return IndexY == 0; } }
    public bool IsTop { get { return IndexY == GameManager.CountY - 1; } }
    public Product ChildProduct { get; set; }
    public bool IsCovered { get { return mCoverCount > 0; } }
    public bool IsBushed { get { return mBushIndex > 0; } }

    public Action<Frame> EventBreakCover;
    public Action<Frame> EventBreakBush;
    public Action<int> EventScoreText;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Initialize(InGameManager mgr, int idxX, int idxY, bool isDisabled, int coverCount, int bushIndex = 0)
    {
        GameManager = mgr;
        IndexX = idxX;
        IndexY = idxY;

        if (isDisabled)
        {
            Empty = true;
            mCoverCount = 0;
            CoverRenderer.sprite = Covers[0];
            gameObject.SetActive(false);
        }
        else
        {
            Empty = false;
            mCoverCount = coverCount;
            CoverRenderer.sprite = Covers[mCoverCount];
            float deg = 90 * (mCoverCount - 1);
            CoverRenderer.transform.rotation = Quaternion.Euler(0, 0, deg);
        }

        mBushIndex = bushIndex;
        UpdateBush();
    }

    public void ShowBorder(int pos)
    {
        //pos 0:Left, 1:Right, 2:Top, 3:Bottom
        Borders[pos].enabled = true;
    }


    public void BreakCover(int count = 1)
    {
        if (mCoverCount <= 0)
            return;

        mCoverCount = Mathf.Max(0, mCoverCount - count);
        CoverRenderer.sprite = Covers[mCoverCount];
        CoverRenderer.transform.DORotate(new Vector3(0, 0, 90 * (mCoverCount - 1)), 0.5f, RotateMode.FastBeyond360);
        CreateBreakStoneEffect();
        if(mCoverCount <= 0)
            EventBreakCover?.Invoke(this);
    }

    public Frame Left(int offX = 1)
    {
        int nextIdxX = IndexX - offX;
        return nextIdxX >= 0 ? GameManager.Frame(nextIdxX, IndexY) : null;
    }
    public Frame MostLeft()
    {
        return GameManager.Frame(0, IndexY);
    }
    public Frame Right(int offX = 1)
    {
        int nextIdxX = IndexX + offX;
        return nextIdxX < GameManager.CountX ? GameManager.Frame(nextIdxX, IndexY) : null;
    }
    public Frame MostRight()
    {
        return GameManager.Frame(GameManager.CountX - 1, IndexY);
    }
    public Frame Down(int offY = 1)
    {
        int nextIdxY = IndexY - offY;
        return nextIdxY >= 0 ? GameManager.Frame(IndexX, nextIdxY) : null;
    }
    public Frame MostDown()
    {
        return GameManager.Frame(IndexX, 0);
    }
    public Frame Up(int offY = 1)
    {
        int nextIdxY = IndexY + offY;
        return nextIdxY < GameManager.CountY ? GameManager.Frame(IndexX, nextIdxY) : null;
    }
    public Frame MostUp()
    {
        return GameManager.Frame(IndexX, GameManager.CountY - 1);
    }
    public Frame[] GetAroundFrames()
    {
        Frame frame = null;
        List<Frame> frames = new List<Frame>();
        frame = Left(); if (frame != null && !frame.Empty) frames.Add(frame);
        frame = Right(); if (frame != null && !frame.Empty) frames.Add(frame);
        frame = Up(); if (frame != null && !frame.Empty) frames.Add(frame);
        frame = Down(); if (frame != null && !frame.Empty) frames.Add(frame);
        return frames.ToArray();
    }
    private void CreateBreakStoneEffect()
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectBreakStone);
        Vector3 start = new Vector3(transform.position.x, transform.position.y, -4.0f);
        GameObject obj = Instantiate(BreakStonesParticle, transform);
        obj.transform.position = start;
        Destroy(obj, 1.0f);
    }

    public void BreakBush(int count = 1)
    {
        if (!IsBushed)
            return;

        mBushIndex = Mathf.Max(0, mBushIndex - count);
        Instantiate(BushEffectPrefab, transform.position, Quaternion.identity, transform);
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectBreakBush);
        TouchBush();
        UpdateBush();

        if(mBushIndex <= 0)
            EventBreakBush?.Invoke(this);
    }
    public void TouchBush()
    {
        if (IsBushed)
        {
            SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectTouchBush);
            BushObject.GetComponentInChildren<Animator>().enabled = true;
            BushObject.GetComponentInChildren<Animator>().Play("bush", -1, 0);
        }
    }
    private void UpdateBush()
    {
        BushObject.SetActive(IsBushed);
        BushObject.transform.GetChild(0).GetComponent<SpriteRenderer>().sprite = Bushes[mBushIndex];
    }

    public void CreateComboTextEffect(int combo, ProductColor color)
    {
        Color textColor = Color.white;
        switch(color)
        {
            case ProductColor.Blue: textColor = new Color(0, 0.4f, 1, 1); break;
            case ProductColor.Green: textColor = new Color(0.17f, 0.7f, 0, 1); break;
            case ProductColor.Orange: textColor = new Color(1, 0.41f, 0, 1); break;
            case ProductColor.Purple: textColor = new Color(0.61f, 0, 0.84f, 1); break;
            case ProductColor.Red: textColor = new Color(0.98f, 0.1f, 0, 1); break;
            case ProductColor.Yellow: textColor = new Color(1, 0.82f, 0, 1); break;
        }
        StartCoroutine(_CreateComboTextEffect(combo, textColor));
    }
    IEnumerator _CreateComboTextEffect(int combo, Color textColor)
    {
        //Vector3 startPos = transform.position + new Vector3(0, UserSetting.GridSize * 0.2f, -2.0f);
        Vector3 startPos = transform.position + new Vector3(0, 0, -2.0f);
        GameObject obj = GameObject.Instantiate(ComboNumPrefab, startPos, Quaternion.identity, transform);
        Numbers numComp = obj.GetComponent<Numbers>();
        numComp.Number = combo;
        numComp.NumberColor = textColor;
        numComp.layerName = "UI";

        float time = 0;
        float duration = 0.3f;
        Vector3 acc = new Vector3(0, -60.0f, 0);
        Vector3 startVel = new Vector3(2, 10, 0);
        while (time < duration)
        {
            obj.transform.position += (startVel * Time.deltaTime);
            startVel += (acc * Time.deltaTime);
            time += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(0.3f);

        StartCoroutine(UnityUtils.MoveNatural(obj, GameManager.ScoreTextDest.transform.position, 0.5f, () => {
            EventScoreText?.Invoke(combo);
            Destroy(obj);
        }));
    }
    IEnumerator DisappearGoingUP(GameObject obj, float duration)
    {
        float time = 0;
        //SpriteRenderer renderer = obj.GetComponent<SpriteRenderer>();
        //Color color = renderer.color;
        float startSpeed = 5;
        while (time < duration)
        {
            float rate = time / duration;
            //color.a = 1 - rate;
            //renderer.color = color;
            startSpeed -= 10 * Time.deltaTime;
            startSpeed = Mathf.Max(0, startSpeed);
            obj.transform.position += new Vector3(0, startSpeed * Time.deltaTime, 0);
            time += Time.deltaTime;
            yield return null;
        }
        Destroy(obj);
    }
    public bool IsObstacled()
    {
        if (IsBushed || IsCovered)
            return true;

        return false;
    }
    public void BreakObstacle(int count = 1)
    {
        if (IsBushed)
        {
            BreakBush(count);
        }
        else if (IsCovered)
        {
            BreakCover(count);
        }
    }

}
