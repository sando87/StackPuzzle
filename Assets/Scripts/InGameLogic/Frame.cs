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
        InitBush();
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
        if (!IsBushed)
            return;

        int limitCombo = (mBushIndex - 1) * 3;
        if (combo < limitCombo)
            return;

        mBushIndex = 0;
        BushObject.GetComponent<Animator>().enabled = false;
        BushObject.GetComponent<SpriteRenderer>().sprite = BushImages[mBushIndex];
        BushObject.transform.GetChild(0).GetComponent<ParticleSystem>().gameObject.SetActive(true);
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectBreakBush);
        Invoke("InitBush", 2.0f);
        EventBreakBush?.Invoke(this);
    }
    public void TouchBush()
    {
        if (IsBushed)
        {
            SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectTouchBush);
            BushObject.GetComponent<Animator>().enabled = true;
            BushObject.GetComponent<Animator>().Play("bush", -1, 0);
        }
    }
    private void InitBush()
    {
        BushObject.GetComponent<Animator>().enabled = false;
        BushObject.GetComponent<SpriteRenderer>().sprite = BushImages[mBushIndex];
        BushObject.transform.GetChild(0).GetComponent<ParticleSystem>().gameObject.SetActive(false);
        BushObject.SetActive(IsBushed);
    }

    public void CreateComboTextEffect(int combo, ProductColor color)
    {
        Color textColor = Color.white;
        switch(color)
        {
            case ProductColor.Blue: textColor = Color.blue; break;
            case ProductColor.Green: textColor = Color.green; break;
            case ProductColor.Orange: textColor = new Color(1, 0.5f, 0, 1); break;
            case ProductColor.Purple: textColor = Color.magenta; break;
            case ProductColor.Red: textColor = Color.red; break;
            case ProductColor.Yellow: textColor = Color.yellow; break;
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

        if (GameManager.FieldType == GameFieldType.Stage)
        {
            StartCoroutine(UnityUtils.MoveNatural(obj, GameManager.ScoreTextDest.transform.position, 0.5f, () => {
                EventScoreText?.Invoke(combo);
                Destroy(obj);
            }));
        }
        else
        {
            StartCoroutine(DisappearGoingUP(obj, 0.5f));
        }
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

}
