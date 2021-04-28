using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Frame : MonoBehaviour
{
    private int mCoverCount;
    private int mBushIndex;

    public Sprite[] Covers;
    public Sprite[] BushImages;
    public SpriteRenderer[] Borders;
    public SpriteRenderer CoverRenderer;
    public GameObject BreakStonesParticle;
    public GameObject BushObject;

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

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Initialize(InGameManager mgr, int idxX, int idxY, int coverCount, int bushIndex = 0)
    {
        GameManager = mgr;
        IndexX = idxX;
        IndexY = idxY;

        if (coverCount < 0)
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
        }

        mBushIndex = bushIndex;
        BushObject.SetActive(IsBushed);
    }

    public void ShowBorder(int pos)
    {
        //pos 0:Left, 1:Right, 2:Top, 3:Bottom
        Borders[pos].enabled = true;
    }


    public void BreakCover()
    {
        if (mCoverCount <= 0)
            return;

        mCoverCount--;
        CoverRenderer.sprite = Covers[mCoverCount];
        CreateBreakStoneEffect();
        if(mCoverCount <= 0)
            EventBreakCover?.Invoke(this);
    }

    public Frame Left()
    {
        return IndexX > 0 ? GameManager.Frame(IndexX - 1, IndexY) : null;
    }
    public Frame Right()
    {
        return IndexX < GameManager.CountX - 1 ? GameManager.Frame(IndexX + 1, IndexY) : null;
    }
    public Frame Down()
    {
        return IndexY > 0 ? GameManager.Frame(IndexX, IndexY - 1) : null;
    }
    public Frame Up()
    {
        return IndexY < GameManager.CountY - 1 ? GameManager.Frame(IndexX, IndexY + 1) : null;
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

    public void BreakBush(int combo)
    {
        int limitCombo = (mBushIndex - 1) * 3;
        if (combo < limitCombo)
        {
            TouchBush();
            return;
        }
            

        mBushIndex = 0;
        BushObject.GetComponent<SpriteRenderer>().sprite = BushImages[mBushIndex];
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectBreakBush);
        //create particle
    }
    public void TouchBush()
    {
        if (IsBushed)
        {
            SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectTouchBush);
            BushObject.GetComponent<Animator>().StartPlayback();
        }
    }

}
