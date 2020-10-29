using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ProductColor { None, Blue, Green, Orange, Purple, Red, Yellow };
public enum ProductSkill { Nothing, MatchOneMore, BreakSameColor, KeepCombo, ReduceColor };

public class Product : MonoBehaviour
{
    private bool mLocked = false;
    private Frame mParentFrame = null;

    public GameObject mBeamUpEffect;
    public GameObject ChocoBlock;
    public ProductColor mColor;
    public ProductSkill mSkill;
    public Animation mAnimation;

    public SpriteRenderer Renderer;
    public Sprite[] Images;
    public Sprite[] Chocos;
    public Sprite ImgOneMore;
    public Sprite ImgSameColor;
    public Sprite ImgKeepCombo;
    public Sprite ImgReduceColor;
    public Sprite ImgCombo;
    public int ImageIndex;

    public float Weight { get; set; }
    public int Combo { get; set; }
    public bool IsSwipe { get; set; }
    public bool IsLocked() { return mLocked; }
    public Frame ParentFrame
    {
        get
        {
            return mParentFrame;
        }
        set
        {
            transform.SetParent(value.transform);
            mParentFrame = value;
        }
    }

    public Action<List<Product>> EventMatched;
    public Action<Product> EventDestroyed;
    public Action EventUnWrapChoco;

    #region MatchCycle
    public void StartSwipe(Frame target)
    {
        if (mLocked)
            return;

        mLocked = true;
        IsSwipe = true;
        mAnimation.Play("swap");
        StartCoroutine(AnimateSwipe(target));
    }
    IEnumerator AnimateSwipe(Frame target)
    {
        float duration = 0.3f;
        Vector3 start = transform.position;
        Vector3 dest = target.transform.position;
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
        EndSwipe(target);
    }
    void EndSwipe(Frame target)
    {
        ParentFrame = target;
        mLocked = false;
        StartCoroutine(DoMatch());
    }
    IEnumerator DoMatch()
    {
        yield return null;
        List<Product> matchList = new List<Product>();
        SearchMatchedProducts(matchList, mColor);
        EventMatched?.Invoke(matchList);
        IsSwipe = false;
    }

    public void StartDestroy()
    {
        mLocked = true;
        mAnimation.Play("destroy");
        UnWrapChocoBlocksAroundMe(Combo);
        ParentFrame.BreakCover(Combo);
    }
    void StartSpriteAnim()
    {
        StartCoroutine(AnimateDestroySprite());
    }
    IEnumerator AnimateDestroySprite()
    {
        while(true) //this Coroutine is stopped when object destroyed.
        {
            Renderer.sprite = Images[ImageIndex];
            yield return null;
        }
    }
    void EndDestroy()
    {
        mLocked = false;
        EventDestroyed?.Invoke(this);
        Destroy(gameObject);
    }

    public void StartDropAnimate(Frame parent, int emptyCount, bool isComboable)
    {
        mLocked = true;
        ParentFrame = parent;
        float height = UserSetting.GridSize * emptyCount;
        transform.localPosition = new Vector3(0, height, -1);
        StartCoroutine(AnimateDrop(isComboable));
        if(isComboable)
            StartCoroutine(StartHighLight());
    }
    IEnumerator AnimateDrop(bool isComboable)
    {
        Renderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        Renderer.sortingOrder = ParentFrame.MaskLayerOrder;
        
        //블럭이 떨어져야 하는 높이에 따라 다르게 delay를 줘서 떨어져야 한다.
        //채공시간은 0.6초, 착지순간은 모두 동일하게 수학적으로 계산한다.
        float totalTime = 0.6f;
        float totalHeight = UserSetting.GridSize * 8; //totalTime 동안 떨어지는 블럭 높이(즉 속도조절값)
        float a = totalHeight / (totalTime * totalTime);
        float delay = totalTime - Mathf.Sqrt(transform.localPosition.y / a);

        Vector3 startPos = transform.position;
        Vector3 endPos = ParentFrame.transform.position;
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

        Renderer.maskInteraction = SpriteMaskInteraction.None;

        if (isComboable)
        {
            StartCoroutine(DoMatch());
        }
            
    }

    public void StartFlash(List<Product> matchedPros)
    {
        StartCoroutine(StartFlashing(matchedPros));
    }
    IEnumerator StartFlashing(List<Product> matchedPros)
    {
        yield return null;
        foreach (Product pro in matchedPros)
        {
            float dist = (pro.transform.position - transform.position).magnitude;
            float delay_sec = dist / 10.0f; //0 ~ 1.0f ~;
            float intensity = 1 - (dist / 5.0f); //1.0f ~ 0 ~;
            StartCoroutine(pro.FlashProduct(delay_sec, intensity));
        }
    }
    IEnumerator FlashProduct(float delay_sec, float intensity)
    {
        yield return new WaitForSeconds(delay_sec);

        float halfTime = 0.12f;
        float k = -intensity / (halfTime * halfTime);
        float t = 0;
        while (t < halfTime * 2)
        {
            float light = k * (t - halfTime) * (t - halfTime) + intensity;
            light = light < 0 ? 0 : light;
            Renderer.material.SetColor("_Color", new Color(light, light, light, 0));
            t += Time.deltaTime;
            yield return null;
        }
        Renderer.material.color = new Color(0, 0, 0, 0);
    }
    IEnumerator StartHighLight()
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
    #endregion

    #region Support Functions
    public void SearchMatchedProducts(List<Product> products, ProductColor color)
    {
        if (mLocked || mColor != color || IsChocoBlock())
            return;

        if (products.Contains(this))
            return;

        products.Add(this);

        Product nearProduct = null;
        nearProduct = Left();
        if (nearProduct != null)
            nearProduct.SearchMatchedProducts(products, color);
        nearProduct = Right();
        if (nearProduct != null)
            nearProduct.SearchMatchedProducts(products, color);
        nearProduct = Up();
        if (nearProduct != null)
            nearProduct.SearchMatchedProducts(products, color);
        nearProduct = Down();
        if (nearProduct != null)
            nearProduct.SearchMatchedProducts(products, color);
    }
    public void SearchMatchedProductsAround(List<Product> products, ProductColor color, SwipeDirection skipDir)
    {
        Product nearProduct = null;
        nearProduct = Left();
        if (nearProduct != null && skipDir != SwipeDirection.LEFT)
            nearProduct.SearchMatchedProducts(products, color);
        nearProduct = Right();
        if (nearProduct != null && skipDir != SwipeDirection.RIGHT)
            nearProduct.SearchMatchedProducts(products, color);
        nearProduct = Up();
        if (nearProduct != null && skipDir != SwipeDirection.UP)
            nearProduct.SearchMatchedProducts(products, color);
        nearProduct = Down();
        if (nearProduct != null && skipDir != SwipeDirection.DOWN)
            nearProduct.SearchMatchedProducts(products, color);
    }
    public Product Left()
    {
        Frame nearFrame = ParentFrame.Left();
        if (nearFrame == null)
            return null;

        Product pro = nearFrame.GetComponentInChildren<Product>();
        if (pro == null)
            return null;

        return pro;
    }
    public Product Right()
    {
        Frame nearFrame = ParentFrame.Right();
        if (nearFrame == null)
            return null;

        Product pro = nearFrame.GetComponentInChildren<Product>();
        if (pro == null)
            return null;

        return pro;
    }
    public Product Up()
    {
        Frame nearFrame = ParentFrame.Up();
        if (nearFrame == null)
            return null;

        Product pro = nearFrame.GetComponentInChildren<Product>();
        if (pro == null)
            return null;

        return pro;
    }
    public Product Down()
    {
        Frame nearFrame = ParentFrame.Down();
        if (nearFrame == null)
            return null;

        Product pro = nearFrame.GetComponentInChildren<Product>();
        if (pro == null)
            return null;

        return pro;
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
    public void BackupSkillToFrame(int matchCount, bool enabledReduceColor)
    {
        if (matchCount <= UserSetting.MatchCount)
            return;

        switch(matchCount)
        {
            case 4:
                ParentFrame.SkillBackupSpace = ProductSkill.MatchOneMore;
                break;
            case 5:
                ParentFrame.SkillBackupSpace = ProductSkill.KeepCombo;
                break;
            case 6:
                ParentFrame.SkillBackupSpace = ProductSkill.BreakSameColor;
                break;
            case 7:
                if (!enabledReduceColor)
                    ParentFrame.SkillBackupSpace = ProductSkill.ReduceColor;
                break;
            default:
                break;
        }
    }
    public void ChangeSkilledProduct(ProductSkill skill)
    {
        mSkill = skill;
        switch (skill)
        {
            case ProductSkill.MatchOneMore:     GetComponent<SpriteRenderer>().sprite = ImgOneMore; break;
            case ProductSkill.KeepCombo:        GetComponent<SpriteRenderer>().sprite = ImgKeepCombo; break;
            case ProductSkill.BreakSameColor:   GetComponent<SpriteRenderer>().sprite = ImgSameColor; break;
            case ProductSkill.ReduceColor:      GetComponent<SpriteRenderer>().sprite = ImgReduceColor; break;
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
    public void UnWrapChocoBlocksAroundMe(int combo)
    {
        Product target = Up();
        if (target != null)
            target.BreakChocoBlock(combo);
        target = Down();
        if (target != null)
            target.BreakChocoBlock(combo);
        target = Right();
        if (target != null)
            target.BreakChocoBlock(combo);
        target = Left();
        if (target != null)
            target.BreakChocoBlock(combo);
    }
    #endregion
}
