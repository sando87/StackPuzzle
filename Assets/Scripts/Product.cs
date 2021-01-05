using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Product : MonoBehaviour
{
    private bool mLocked = false;
    private Frame mParentFrame = null;

    public GameObject mBeamUpEffect;
    public GameObject ChocoBlock;
    public ProductColor mColor;
    public ProductSkill mSkill;
    public Animation mAnimation;

    public GameObject IceCover;
    public GameObject SkillObject;

    public SpriteRenderer Renderer;
    public Sprite[] Images;
    public Sprite[] Chocos;
    public Sprite ImgHorizontal;
    public Sprite ImgVertical;
    public Sprite ImgBomb;
    public Sprite ImgSameColor;
    public Sprite ImgCombo;
    public GameObject ComboNumPrefab;

    public Product Dir(SwipeDirection dir)
    {
        switch(dir)
        {
            case SwipeDirection.LEFT: return Left();
            case SwipeDirection.RIGHT: return Right();
            case SwipeDirection.UP: return Up();
            case SwipeDirection.DOWN: return Down();
        }
        return null;
    }
    public Action EventUnWrapChoco;

    public float DropSpeed { get; set; }
    public float Weight { get; set; }
    public int Combo { get; set; }
    public bool IsMerging { get; private set; }
    public bool IsDestroying { get; private set; }
    public bool IsMoving { get; private set; }
    public bool IsDropping { get; set; }
    public bool IsLocked() { return mLocked || IsDestroying || IsMerging || IsMoving || IsDropping; }
    public Frame ParentFrame { get { return mParentFrame; } }
    public void AttachTo(Frame parentFrame)
    {
        parentFrame.ChildProduct = this;
        mParentFrame = parentFrame;
        transform.SetParent(parentFrame.transform);
    }
    public Frame Detach()
    {
        if (mParentFrame == null)
            return null;

        Frame frame = mParentFrame;
        frame.ChildProduct = null;
        InGameManager mgr = frame.GameManager;
        transform.SetParent(mgr.transform);
        mParentFrame = null;
        return frame;
    }
    public void SkillMerge(Product targetProduct, Action EventMergeEnd)
    {
        Vector3 center = (transform.position + targetProduct.transform.position) *0.5f;

        targetProduct.StartCoroutine(targetProduct.AnimateMove(center, 0.2f, null));
        StartCoroutine(AnimateMove(center, 0.2f, EventMergeEnd));
    }
    public void Swipe(Product targetProduct, Action EventSwipeEnd)
    {
        Frame myFrame = Detach();
        Frame targetFrame = targetProduct.Detach();

        AttachTo(targetFrame);
        targetProduct.AttachTo(myFrame);

        mAnimation.Play("swap");
        targetProduct.mAnimation.Play("swap");

        targetProduct.StartCoroutine(targetProduct.AnimateMove(myFrame.transform.position, 0.3f, null));
        StartCoroutine(AnimateMove(targetFrame.transform.position, 0.3f, EventSwipeEnd));
    }
    public void Drop(Action EventEndDrop)
    {
        if (IsDropping)
            return;

        Detach();
        StartCoroutine(AnimateDrop(EventEndDrop));
    }
    public void Drop(Product underProduct, Action EventEndDrop)
    {
        if (IsDropping)
            return;

        Detach();
        Vector3 newPos = underProduct.transform.position + new Vector3(0, InGameManager.InstCurrent.GridSize, 0);
        transform.position = newPos;
        StartCoroutine(AnimateDrop(underProduct, EventEndDrop));
    }
    private IEnumerator AnimateDrop(Action EventEndDrop)
    {
        IsDropping = true;
        float time = 0;
        Vector3 vel = new Vector3(0, 1.5f, 0);
        Vector3 nextPos = Vector3.zero;
        InGameManager mgr = InGameManager.InstCurrent;

        while (true)
        {
            Vector3 delta = vel * time;
            delta.y = Math.Min(delta.y, mgr.GridSize * 0.5f);
            nextPos = transform.position - delta;

            Frame footFrame = mgr.GetFrame(nextPos.x, nextPos.y - mgr.GridSize * 0.5f - 0.01f);

            if (footFrame == null) //In case of crashed with bottom
            {
                Frame frame = mgr.GetFrame(nextPos.x, nextPos.y);
                AttachTo(frame);
                transform.localPosition = new Vector3(0, 0, -1);
                break;
            }
            else if (footFrame.ChildProduct != null) //In case of crashed with under product
            {
                Frame frame = footFrame.Up();
                if (frame == null) //on the top product
                {
                    yield return null;
                }
                else
                {
                    AttachTo(frame);
                    transform.localPosition = new Vector3(0, 0, -1);
                    break;
                }
            }
            else //keep dropping..
            {
                transform.position = nextPos;
                time += Time.deltaTime;
                yield return null;
            }
        }

        IsDropping = false;
        EventEndDrop?.Invoke();
    }
    private IEnumerator AnimateDrop(Product underProduct, Action EventEndDrop)
    {
        IsDropping = true;
        float time = 0;
        Vector3 vel = new Vector3(0, 1.5f, 0);
        Vector3 nextPos = Vector3.zero;
        InGameManager mgr = InGameManager.InstCurrent;

        while (true)
        {
            Vector3 delta = vel * time;
            delta.y = Math.Min(delta.y, mgr.GridSize * 0.5f);
            nextPos = transform.position - delta;
            
            float myMin = nextPos.y - mgr.GridSize * 0.5f;
            float underMax = underProduct.transform.position.y + mgr.GridSize * 0.5f;

            if (myMin < underMax) //대상 블록과 충돌시
            {
                if (underProduct.ParentFrame == null) //다음 블록과 충돌을 했지만 다음 블록이 아직 멈추지 않은 상태(속도가 위에것이 더 빨리 떨어지는 경우)
                {
                    yield return null;
                }
                else
                {
                    Frame frame = underProduct.ParentFrame.Up();
                    if (frame == null)
                    {
                        yield return null;
                    }
                    else
                    {
                        AttachTo(frame);
                        transform.localPosition = new Vector3(0, 0, -1);
                        break;
                    }
                }
            }
            else //대상 블록과 비충돌시
            {
                transform.position = nextPos;
                time += Time.deltaTime;
                yield return null;
            }
        }

        IsDropping = false;
        EventEndDrop?.Invoke();
    }
    public void StartMergeTo(Product destProduct, float duration)
    {
        IsMerging = true;
        CreateComboTextEffect();

        StartCoroutine(AnimateFlash(1.3f));

        StartCoroutine(UnityUtils.CallAfterSeconds(duration, () => {
            Detach();
            StartCoroutine(AnimateMoveTo(destProduct, 0.2f, () => {
                Destroy(gameObject);
            }));

        }));
    }
    public void StartToChangeSkilledProduct(float duration, ProductSkill skill)
    {
        mSkill = skill;

        CreateComboTextEffect();
        StartCoroutine(AnimateFlash(1.3f));

        StartCoroutine(UnityUtils.CallAfterSeconds(duration, () => {
            ChangeProductImage(skill);
        }));
    }
    public Frame StartDestroy(float flashIntesity, float delay, int combo)
    {
        if (IsDestroying)
            return null;

        Combo = combo;
        IsDestroying = true;
        CreateComboTextEffect();

        if(flashIntesity > 0)
            StartCoroutine(AnimateFlash(flashIntesity));

        StartCoroutine(UnityUtils.CallAfterSeconds(delay, () => {

            Frame prvFrame = Detach();
            UnWrapChocoBlocksAroundFrame(prvFrame, Combo);
            prvFrame.BreakCover(Combo);

            StartCoroutine(AnimateDestroy());
        }));
        return ParentFrame;
    }
    public void StartFlash(float flashIntesity)
    {
        StartCoroutine(AnimateFlash(flashIntesity));
    }
    IEnumerator AnimateMoveTo(Product destProduct, float duration, Action EventMoveEnd)
    {
        IsMoving = true;
        float vel = (transform.position - destProduct.transform.position).magnitude / duration;
        float time = 0;
        while (time < duration)
        {
            Vector3 dir = destProduct.transform.position - transform.position;
            dir.z = 0;
            dir.Normalize();
            transform.position += dir * vel * Time.deltaTime;
            time += Time.deltaTime;
            yield return null;
        }
        transform.position = destProduct.transform.position;
        IsMoving = false;
        EventMoveEnd?.Invoke();
    }
    IEnumerator AnimateMove(Vector2 destPos, float duration, Action EventMoveEnd)
    {
        IsMoving = true;
        Vector3 start = transform.position;
        Vector3 dest = new Vector3(destPos.x, destPos.y, start.z);
        Vector3 vel = (dest - start) / duration;
        Vector3 offset = Vector3.zero;
        float time = 0;
        while (time < duration)
        {
            transform.position = start + (vel * time);
            time += Time.deltaTime;
            yield return null;
        }
        transform.position = dest;
        IsMoving = false;
        EventMoveEnd?.Invoke();
    }
    IEnumerator AnimateDestroy()
    {
        int idx = 0;
        while(true)
        {
            int imgIndex = idx / 2;
            if (imgIndex >= Images.Length)
                break;

            Renderer.sprite = Images[imgIndex];
            idx++;
            yield return null;
        }
        Destroy(gameObject);
    }



    IEnumerator AnimateDrop(float duration)
    {
        Renderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        Renderer.sortingOrder = ParentFrame.MaskLayerOrder;
        
        //블럭이 떨어져야 하는 높이에 따라 다르게 delay를 줘서 떨어져야 한다.
        //채공시간은 0.6초, 착지순간은 모두 동일하게 수학적으로 계산한다.
        float totalTime = duration;
        float totalHeight = UserSetting.GridSize * 8; //totalTime 동안 떨어지는 블럭 높이(즉 속도조절값)
        float a = totalHeight / (totalTime * totalTime);
        Vector3 startPos = transform.position;
        Vector3 endPos = ParentFrame.transform.position;
        float delay = totalTime - Mathf.Sqrt((startPos.y - endPos.y) / a);
        endPos.z = startPos.z;
        float time = 0;
        while (time < totalTime)
        {
            float x = time - delay;
            x = x < 0 ? 0 : x;
            float y = a * x * x;
            Vector3 curPos = startPos;
            curPos.y -= y;
            transform.position = curPos;
            time += Time.deltaTime;
            yield return null;
        }
        transform.position = endPos;

        mLocked = false;

        Renderer.sortingLayerName = "Default";
        Renderer.maskInteraction = SpriteMaskInteraction.None;
    }
    IEnumerator AnimateFlash(float intensity)
    {
        mAnimation.Play("spitout");
        float halfTime = 0.12f;
        float k = -intensity / (halfTime * halfTime);
        float t = 0;
        while (t < halfTime * 2)
        {
            float light = k * (t - halfTime) * (t - halfTime) + intensity;
            light = light < 0 ? 0 : light;
            light = light > 1 ? 1 : light;
            Renderer.material.SetColor("_Color", new Color(light, light, light, 0));
            t += Time.deltaTime;
            yield return null;
        }
        Renderer.material.color = new Color(0, 0, 0, 0);
    }
    IEnumerator AnimateTwinkle()
    {
        Vector3 scale = transform.localScale;
        Vector3 pos = transform.localPosition;
        float t = 0;
        while (t < 0.2f)
        {
            int light = (int)(t * 10) % 2;
            float posY = -10.0f * (t - 0.1f) * (t - 0.1f) + 0.1f;
            Renderer.material.SetColor("_Color", new Color(1 - light, 1 - light, 1 - light, 0));
            transform.localScale += new Vector3(light * 2 - 1, 1 - light * 2, 0) * 0.01f;
            transform.localPosition += new Vector3(0, posY, 0);
            t += Time.deltaTime;
            yield return null;
        }
        Renderer.material.color = new Color(0, 0, 0, 0);
        transform.localScale = scale;
        transform.localPosition = pos;
        transform.localRotation = Quaternion.identity;
    }

    #region Support Functions

    void CreateComboTextEffect()
    {
        if (Combo <= 0)
            return;

        Vector3 startPos = transform.position + new Vector3(0, UserSetting.GridSize * 0.2f, -1);
        GameObject obj = GameObject.Instantiate(ComboNumPrefab, startPos, Quaternion.identity, ParentFrame.gameObject.transform);
        obj.GetComponent<Numbers>().Number = Combo;

        Vector3 destPos = startPos + new Vector3(0, UserSetting.GridSize * 0.5f, 0);
        ParentFrame.StartCoroutine(UnityUtils.AnimateConvex(obj, destPos, 0.7f, () => {
            Destroy(obj);
        }));
    }

    public void SearchMatchedProducts(List<Product> products, ProductColor color)
    {
        if (mLocked || mColor != color || IsChocoBlock() || mSkill != ProductSkill.Nothing || IsIced)
            return;

        if (products.Contains(this))
            return;

        products.Add(this);


        Product[] around = GetAroundProducts(ParentFrame);
        foreach (Product pro in around)
            pro.SearchMatchedProducts(products, color);
    }
    public Product Left()
    {
        Frame nearFrame = ParentFrame.Left();
        if (nearFrame == null || nearFrame.Empty)
            return null;

        Product pro = nearFrame.GetComponentInChildren<Product>();
        if (pro == null)
            return null;

        return pro;
    }
    public Product Right()
    {
        Frame nearFrame = ParentFrame.Right();
        if (nearFrame == null || nearFrame.Empty)
            return null;

        Product pro = nearFrame.GetComponentInChildren<Product>();
        if (pro == null)
            return null;

        return pro;
    }
    public Product Up()
    {
        Frame nearFrame = ParentFrame.Up();
        if (nearFrame == null || nearFrame.Empty)
            return null;

        Product pro = nearFrame.GetComponentInChildren<Product>();
        if (pro == null)
            return null;

        return pro;
    }
    public Product Down()
    {
        Frame nearFrame = ParentFrame.Down();
        if (nearFrame == null || nearFrame.Empty)
            return null;

        Product pro = nearFrame.GetComponentInChildren<Product>();
        if (pro == null)
            return null;

        return pro;
    }
    public Product[] GetAroundProducts(Frame frame)
    {
        Frame[] frames = frame.GetAroundFrames();
        List<Product> products = new List<Product>();
        foreach(Frame iter in frames)
        {
            Product child = iter.ChildProduct;
            if (child != null)
                products.Add(child);
        }
        return products.ToArray();
    }
    List<Frame> GetEmptyDownFrames()
    {
        List<Frame> emptyFrames = new List<Frame>();
        Frame curFrame = ParentFrame.Down();
        while (curFrame != null && curFrame.ChildProduct == null)
        {
            emptyFrames.Add(curFrame);
            curFrame = curFrame.Down();
        }
        return emptyFrames;
    }
    List<Frame> GetIdleUpFrames()
    {
        List<Frame> idleFrames = new List<Frame>();
        idleFrames.Add(ParentFrame);
        Frame curFrame = ParentFrame.Up();
        while (curFrame != null && curFrame.ChildProduct != null && !curFrame.ChildProduct.IsLocked())
        {
            idleFrames.Add(curFrame);
            curFrame = curFrame.Up();
        }
        return idleFrames;
    }
    public void ChangeProductImage(ProductSkill skill)
    {
        mAnimation.Play("swap");
        mSkill = skill;
        switch (skill)
        {
            case ProductSkill.Horizontal:   Renderer.sprite = ImgHorizontal; break;
            case ProductSkill.Vertical:     Renderer.sprite = ImgVertical; break;
            case ProductSkill.Bomb:         Renderer.sprite = ImgBomb; break;
            case ProductSkill.SameColor:    Renderer.sprite = ImgSameColor; break;
            default: break;
        }
    }
    public bool BreakChocoBlock(int combo)
    {
        if (ChocoBlock.tag == "off")
            return false;

        int chocoLevel = int.Parse(ChocoBlock.name);
        if (combo < (chocoLevel - 1) * 3)
            return false;

        StartCoroutine(AnimBreakChoco());
        Renderer.enabled = true;
        ChocoBlock.tag = "off";
        ChocoBlock.name = "0";
        ChocoBlock.GetComponent<Animator>().enabled = true;
        ChocoBlock.GetComponent<Animator>().SetTrigger("hide");
        EventUnWrapChoco?.Invoke();
        return true;
    }
    IEnumerator AnimBreakChoco()
    {
        yield return new WaitForSeconds(0.2f);
        mAnimation.Play("swap");
    }
    public void ChangeColor(ProductColor color)
    {
        mColor = color;
        mAnimation.Play("swap");
    }
    public void SetChocoBlock(int level, bool anim = false)
    {
        if (level <= 0)
            return;

        Renderer.enabled = false;
        ChocoBlock.tag = "on";
        ChocoBlock.name = level.ToString();
        ChocoBlock.GetComponent<SpriteRenderer>().sprite = Chocos[level - 1];
        if (anim && level == 1)
        {
            ChocoBlock.GetComponent<Animator>().enabled = true;
            ChocoBlock.GetComponent<Animator>().SetTrigger("show");
        }
        else
        {
            ChocoBlock.GetComponent<Animator>().enabled = false;
            ChocoBlock.transform.localScale = new Vector3(1, 1, 1);
        }
            
    }
    public bool IsChocoBlock()
    {
        return ChocoBlock.tag == "on";
    }
    public void UnWrapChocoBlocksAroundFrame(Frame frame, int combo)
    {
        Product[] around = GetAroundProducts(frame);
        foreach(Product pro in around)
            pro.BreakChocoBlock(combo);
    }
    public bool IsIced { get { return IceCover.activeSelf; } }
    public void SetIce(bool ice)
    {
        IceCover.SetActive(ice);
    }
    #endregion
}
