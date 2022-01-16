﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Product : MonoBehaviour
{
    public ProductColor Color;
    public Animation Animation;
    public SpriteRenderer Renderer;
    public Sprite[] Images;
    public Sprite[] Chocos;
    public Sprite[] CapImages;
    public Sprite[] IceBreakSprites;
    public Sprite ImgHorizontal;
    public Sprite ImgVertical;
    public Sprite ImgBomb;
    public Sprite ImgSameColor;
    public Sprite ImgHammer;
    public Sprite ImgCombo;
    public Sprite ImgClosed;
    public GameObject ComboNumPrefab;
    public GameObject CapObject;
    public GameObject WaterDropParticle;
    public GameObject CapBreakingEffectPrefab;
    public IceBlock IcedBlock;

    public Action EventUnWrapChoco;
    public Action EventUnWrapCap;

    public bool IsSkillable { get { return Skill != ProductSkill.Nothing && !SkillCasted; } }
    public int CapIndex { get; private set; }
    public bool IsCapped { get { return CapIndex > 0; } }
    public InGameManager Manager { get; set; }
    public Frame ParentFrame { get; private set; }
    public ProductSkill Skill { get; private set; }
    public float DropSpeed { get; set; } = 0;
    public float Weight { get; set; }
    public int Combo { get; set; }
    public int InstanceID { get; set; }
    public bool IsMerging { get; private set; }
    public bool IsDestroying { get; private set; }
    public bool IsMoving { get; private set; }
    public bool IsDropping { get; private set; }
    public bool SkillCasted { get; set; } = false;
    public bool IsLocked { get { return IsDestroying || IsMerging || IsMoving || IsDropping || SkillCasted; } }
    public bool IsChocoBlock { get { return IcedBlock.IsIced; } }
    public bool IsClosed { get { return false; } }
    public VerticalFrames VertFrames { get { return ParentFrame != null ? ParentFrame.VertFrames : transform.parent.GetComponent<VerticalFrames>(); } }

    public void AttachTo(Frame parentFrame)
    {
        parentFrame.ChildProduct = this;
        ParentFrame = parentFrame;
        transform.SetParent(parentFrame.transform);
    }
    public Frame Detach(Transform toTransform)
    {
        if (ParentFrame == null)
            return null;

        Frame frame = ParentFrame;
        frame.ChildProduct = null;
        transform.SetParent(toTransform);
        ParentFrame = null;
        return frame;
    }
    public void SkillMerge(Product targetProduct, Action EventMergeEnd)
    {
        StartCoroutine(SkillMergeEffect(targetProduct, EventMergeEnd));
    }
    private IEnumerator SkillMergeEffect(Product target, Action EventEnd)
    {
        IsMoving = true;
        target.IsMoving = true;
        float duration = 0.2f;
        Vector3 start = transform.position;
        Vector3 dest = new Vector3(target.transform.position.x, target.transform.position.y, start.z);
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

        Product sizingProduct = PickMainProduct(this, target);
        if(sizingProduct != null)
        {
            Vector3 refPos = sizingProduct.transform.position;
            refPos.z -= 0.1f;
            Vector3 randomOff = Vector3.zero;
            Vector3 sizeStep = new Vector3(0.1f, 0.1f, 0);
            time = 0;
            while (time < 0.4f)
            {
                randomOff.x = UnityEngine.Random.Range(-0.05f, 0.05f);
                randomOff.y = UnityEngine.Random.Range(-0.05f, 0.05f);
                sizingProduct.transform.localScale += sizeStep;
                sizingProduct.transform.position = refPos + randomOff;
                time += Time.deltaTime;
                yield return null;
            }
            yield return new WaitForSeconds(0.2f);
        }

        IsMoving = false;
        target.IsMoving = false;
        EventEnd?.Invoke();
    }
    Product PickMainProduct(Product main, Product sub)
    {
        if (main.Skill == ProductSkill.SameColor)
            return main;
        else if (sub.Skill == ProductSkill.SameColor)
            return sub;
        else
        {
            if (main.Skill == ProductSkill.Bomb)
                return sub;
            else if (sub.Skill == ProductSkill.Bomb)
                return main;
        }
        return null;
    }
    public void Swipe(Product targetProduct, Action EventSwipeEnd)
    {
        Frame myFrame = Detach(Manager.transform);
        Frame targetFrame = targetProduct.Detach(Manager.transform);
        myFrame.TouchBush();
        targetFrame.TouchBush();

        AttachTo(targetFrame);
        targetProduct.AttachTo(myFrame);

        Animation.Play("swap");
        targetProduct.Animation.Play("swap");

        targetProduct.StartCoroutine(targetProduct.AnimateMove(myFrame.transform.position, 0.3f, null));
        StartCoroutine(AnimateMove(targetFrame.transform.position, 0.3f, EventSwipeEnd));
    }
    public bool ReadyForMerge(int combo)
    {
        IsMerging = true;
        Combo = combo;
        ParentFrame.TouchBush();
        ParentFrame.CreateComboTextEffect(Combo, Color);

        Animation.Play("spitout");
        StartCoroutine(AnimateFlash(1.3f));
        return true;
    }
    public void MergeImImmediately(Product destProduct, ProductSkill skill)
    {
        if (destProduct == this)
        {
            ChangeProductImage(skill);
            IsMerging = false;
        }
        else
        {
            Frame parent = Detach(Manager.transform);
            Manager.ProductIDs.Remove(InstanceID);
            StartCoroutine(AnimateMoveTo(destProduct, 0.2f, () => {
                Destroy(gameObject);
            }));
        }
    }
    public bool IsObstacled()
    {
        if (IsCapped || IsChocoBlock)
            return true;

        return false;
    }
    public void BreakObstacle()
    {
        if (IsCapped)
        {
            BreakCap();
        }
        else if(IsChocoBlock)
        {
            BreakChocoBlock();
        }
    }
    public bool ReadyForDestroy(int combo)
    {
        if(IsObstacled() || IsLocked)
        {
            return false;
        }

        IsDestroying = true;
        Combo = combo;
        ParentFrame.TouchBush();
        Animation.Play("destroy");
        StartCoroutine(AnimateFlash(1.3f));
        return true;
    }
    public Frame DestroyImmediately()
    {
        if (ParentFrame == null)
            return null;
        
        IsDestroying = true;
        Animation.Stop();
        transform.localPosition = new Vector3(0, 0, -1);
        transform.localScale = new Vector3(0.6f, 0.6f, 1);
        ParentFrame.CreateComboTextEffect(Combo, Color);
        Frame parent = Detach(Manager.transform);
        WaterDropParticle.SetActive(true);
        Manager.ProductIDs.Remove(InstanceID);
        StartCoroutine(AnimateDestroy());

        return parent;
    }
    public void DetachFromField()
    {
        IsDestroying = true;
        Animation.Stop();
        transform.localPosition = new Vector3(0, 0, -1);
        transform.localScale = new Vector3(0.6f, 0.6f, 1);
        Detach(Manager.transform);
        Manager.ProductIDs.Remove(InstanceID);
        Destroy(gameObject);
    }
    public Product Dir(SwipeDirection dir)
    {
        switch (dir)
        {
            case SwipeDirection.LEFT: return Left();
            case SwipeDirection.RIGHT: return Right();
            case SwipeDirection.UP: return Up();
            case SwipeDirection.DOWN: return Down();
        }
        return null;
    }
    public void FlashProduct()
    {
        Animation.Play("spitout");
        StartCoroutine(AnimateFlash(1.3f));
    }
    public void Disappear()
    {
        IsDestroying = true;
        Animation.Stop();
        Detach(Manager.transform);
        Manager.ProductIDs.Remove(InstanceID);
        Destroy(gameObject);
    }

    private void FixedUpdate()
    {
        //if(IsDropping)
        //{
        //    Vector3 pos = transform.position;
        //    DropSpeed += UserSetting.ProductDropGravity * Time.deltaTime;
        //    pos.y += DropSpeed * Time.deltaTime;
        //    transform.position = pos;
        //}
    }
    public void Drop()
    {
        if (IsDropping)
            return;

        if(ParentFrame != null)
            Detach(ParentFrame.VertFrames.transform);

        DropSpeed = 0;
        IsDropping = true;
        GetComponent<BoxCollider2D>().isTrigger = true;
        //StartCoroutine("UpdateDropping");
    }
    private IEnumerator UpdateDropping()
    {
        float dropGravity = -50;
        Vector3 pos = transform.position;
        while(true)
        {
            DropSpeed += dropGravity * Time.deltaTime;
            pos.y += DropSpeed * Time.deltaTime;
            transform.position = pos;
            yield return new WaitForFixedUpdate();
        }
    }
    public void StartToDrop(Frame frame, float duration)
    {
        if (ParentFrame != null)
            Detach(ParentFrame.VertFrames.transform);

        IsDropping = true;
        AttachTo(frame);
        transform.DOKill();
        transform.DOLocalMoveY(0, duration).SetEase(Ease.InQuad).OnComplete(() =>
        {
            Animation.Play("drop");
        });
    }
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!IsDropping)
            return;


        if (collision.name == "ground" && collision.transform.parent == transform.parent)
        {
            EndDrop(VertFrames.BottomFrame);
            return;
        }

        Product target = collision.GetComponent<Product>();
        if (target == null || target.transform.position.y > transform.position.y || target.VertFrames != VertFrames)
            return;

        if (target.ParentFrame == null)
        {
            DropSpeed = 0;
            transform.position = target.transform.position + new Vector3(0, Manager.GridSize, 0);
        }
        else
        {
            Frame targetFrame = target.ParentFrame;
            if (targetFrame == VertFrames.TopFrame)
            {
                DropSpeed = 0;
                transform.position = target.transform.position + new Vector3(0, Manager.GridSize, 0);
            }
            else
            {
                EndDrop(targetFrame.Up());
            }
        }

    }
    private void EndDrop(Frame parentFrame)
    {
        Frame curFrame = parentFrame;
        while (curFrame.ChildProduct != null)
            curFrame = curFrame.Up();

        DropSpeed = 0;
        IsDropping = false;
        AttachTo(curFrame);
        StopCoroutine("UpdateDropping");
        GetComponent<BoxCollider2D>().isTrigger = false;
        transform.localPosition = new Vector3(0, 0, -1);
        DisableMasking();
    }
    public void DropEnd()
    {
        IsDropping = false;
        DropSpeed = 0;
        transform.localPosition = new Vector3(0, 0, -1);
        DisableMasking();
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
    IEnumerator AnimateFlash(float intensity)
    {
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
    

    #region Support Functions

    public void CreateComboTextEffect()
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
        if (Color != color || IsObstacled() || Skill != ProductSkill.Nothing || IsLocked)
            return;

        if(ParentFrame != null && ParentFrame.IsObstacled())
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
    public void ChangeProductImage(ProductSkill skill)
    {
        Animation.Play("swap");
        Skill = skill;
        switch (skill)
        {
            case ProductSkill.Horizontal:   Renderer.sprite = ImgHorizontal; break;
            case ProductSkill.Vertical:     Renderer.sprite = ImgVertical; break;
            case ProductSkill.Bomb:         Renderer.sprite = ImgBomb; break;
            case ProductSkill.SameColor:    Renderer.sprite = ImgSameColor; break;
            case ProductSkill.Hammer:       Renderer.sprite = ImgHammer; break;
            default: break;
        }
    }
    public void EnableMasking(int order)
    {
        SpriteRenderer[] renders = GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer render in renders)
        {
            render.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            render.sortingOrder = order;
        }
    }
    public void DisableMasking()
    {
        SpriteRenderer[] renders = GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer render in renders)
        {
            render.maskInteraction = SpriteMaskInteraction.None;
        }
    }
    public Sprite ToSkillImage(ProductSkill skill)
    {
        switch (skill)
        {
            case ProductSkill.Horizontal: return ImgHorizontal;
            case ProductSkill.Vertical: return ImgVertical;
            case ProductSkill.Bomb: return ImgBomb;
            case ProductSkill.SameColor: return ImgSameColor;
            default: break;
        }
        return null;
    }


    private bool BreakChocoBlock()
    {
        if (!IcedBlock.IsIced)
            return false;

        if(IcedBlock.BreakBlock())
        {
            EventUnWrapChoco?.Invoke();
            return true;
        }
        return false;
    }

    public void InitCap(int capIndex)
    {
        CancelInvoke("ChangeCapImage");
        CapIndex = capIndex;
        CapObject.GetComponent<Animator>().enabled = false;
        CapObject.GetComponent<SpriteRenderer>().sprite = CapImages[CapIndex];
        CapObject.SetActive(IsCapped);
    }
    private void BreakCap()
    {
        if (CapIndex <= 0)
            return;

        CapIndex--;
        ChangeCapImage();
        
        if (CapIndex <= 0)
            EventUnWrapCap?.Invoke();
    }
    private void ChangeCapImage()
    {
        Instantiate(CapBreakingEffectPrefab, CapObject.transform.position, Quaternion.identity, transform);
        CapObject.GetComponent<SpriteRenderer>().sprite = CapImages[CapIndex];
        CapObject.transform.DOKill();
        CapObject.transform.DOScale(new Vector3(2.5f, 2.5f, 1), 0.2f).From(new Vector3(1.6f, 1.6f, 1)).SetLoops(2, LoopType.Yoyo);
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectBreakCap);
    }

    #endregion
}
