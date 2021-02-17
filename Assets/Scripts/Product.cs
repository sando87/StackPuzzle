using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Product : MonoBehaviour
{
    public ProductColor Color;
    public GameObject ChocoBlock;
    public Animation Animation;
    public SpriteRenderer Renderer;
    public Sprite[] Images;
    public Sprite[] Chocos;
    public Sprite ImgHorizontal;
    public Sprite ImgVertical;
    public Sprite ImgBomb;
    public Sprite ImgSameColor;
    public Sprite ImgCombo;
    public GameObject ComboNumPrefab;

    public Action EventUnWrapChoco;

    public Frame ParentFrame { get; private set; }
    public ProductSkill Skill { get; private set; }
    public float DropSpeed { get; set; }
    public float Weight { get; set; }
    public int Combo { get; private set; }
    public bool IsMerging { get; private set; }
    public bool IsDestroying { get; private set; }
    public bool IsMoving { get; private set; }
    public bool IsDropping { get; set; }
    public bool SkillCasted { get; set; } = false;
    public bool IsLocked { get { return IsDestroying || IsMerging || IsMoving || IsDropping; } }
    public bool IsChocoBlock { get { return ChocoBlock.tag == "on"; } }

    public void AttachTo(Frame parentFrame)
    {
        parentFrame.ChildProduct = this;
        ParentFrame = parentFrame;
        transform.SetParent(parentFrame.transform);
    }
    public Frame Detach()
    {
        if (ParentFrame == null)
            return null;

        Frame frame = ParentFrame;
        frame.ChildProduct = null;
        InGameManager mgr = frame.GameManager;
        transform.SetParent(mgr.transform);
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
        Frame myFrame = Detach();
        Frame targetFrame = targetProduct.Detach();

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
        CreateComboTextEffect();

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
            Detach();
            StartCoroutine(AnimateMoveTo(destProduct, 0.2f, () => {
                Destroy(gameObject);
            }));
        }
    }
    public bool ReadyForDestroy(int combo)
    {
        if (IsDestroying)
            return false;

        IsDestroying = true;
        Combo = combo;
        CreateComboTextEffect();
        StartCoroutine(AnimateFlash(1.3f));

        return true;
    }
    public Frame DestroyImmediately()
    {
        if (ParentFrame == null)
            return null;
        
        IsDestroying = true;
        Frame parent = Detach();
        UnWrapChocoBlocksAroundFrame(parent, Combo);
        parent.BreakCover(Combo);
        StartCoroutine(AnimateDestroy());

        return parent;
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
        Animation.Play("spitout");
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
        if (Color != color || IsChocoBlock || Skill != ProductSkill.Nothing || IsLocked)
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
        Animation.Play("swap");
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
    public void UnWrapChocoBlocksAroundFrame(Frame frame, int combo)
    {
        Product[] around = GetAroundProducts(frame);
        foreach(Product pro in around)
            pro.BreakChocoBlock(combo);
    }

    #endregion
}
