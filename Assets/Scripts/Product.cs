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
    public ProductColor mColor;
    public ProductSkill mSkill;
    public Animation mAnimation;

    public SpriteRenderer Renderer;
    public Sprite[] Images;
    public Sprite ImgOneMore;
    public Sprite ImgSameColor;
    public Sprite ImgKeepCombo;
    public Sprite ImgReduceColor;
    public int ImageIndex;

    public int Combo { get; set; }
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


    #region MatchCycle
    public void StartSwipe(Frame target)
    {
        if (mLocked)
            return;

        mLocked = true;
        Combo = 0;
        mAnimation.Play("swap");
        StartCoroutine(AnimateSwipe(target));
    }
    IEnumerator AnimateSwipe(Frame target)
    {
        Vector3 dest = target.transform.position;
        dest.z = transform.position.z;
        while ((transform.position - dest).magnitude >= 0.02f)
        {
            transform.position = Vector3.MoveTowards(transform.position, dest, InGameManager.GridSize * 3 * Time.deltaTime);
            yield return null;
        }
        transform.position = dest;
        EndSwipe(target);
    }
    void EndSwipe(Frame target)
    {
        ParentFrame = target;
        mLocked = false;

        if(InGameManager.Inst.MatchLock == false)
            StartCoroutine(DoMatch());
    }
    IEnumerator DoMatch()
    {
        yield return null;
        List<Product> matchList = new List<Product>();
        SearchMatchedProducts(matchList, mColor);
        if (matchList.Count >= InGameManager.MatchCount)
        {
            MakeProductEffect(matchList.Count);

            Combo++;
            Product SameColorSkillProduct = null;
            Product ReduceColorSkillProduct = null;
            foreach (Product pro in matchList)
            {
                if (pro.mSkill == ProductSkill.MatchOneMore)
                    Combo++;
                else if (pro.mSkill == ProductSkill.BreakSameColor)
                    SameColorSkillProduct = pro;
                else if (pro.mSkill == ProductSkill.ReduceColor)
                    ReduceColorSkillProduct = pro;
            }

            if (ReduceColorSkillProduct != null)
            {
                SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectMatched);
                Product[] pros = InGameManager.Inst.GetSameProducts(ReduceColorSkillProduct.mColor);
                foreach (Product pro in pros)
                {
                    if (pro.IsLocked())
                        continue;

                    pro.Combo = ReduceColorSkillProduct.Combo;
                    StartCoroutine(pro.StartDestroy());
                }
            }
            else if (SameColorSkillProduct != null)
            {
                SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectMatched);
                Product[] pros = InGameManager.Inst.GetSameProducts(SameColorSkillProduct.mColor);
                foreach (Product pro in pros)
                {
                    if (pro.IsLocked())
                        continue;

                    pro.Combo = SameColorSkillProduct.Combo;
                    StartCoroutine(pro.StartDestroy());
                }
            }
            else
            {
                SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectMatched);
                foreach (Product pro in matchList)
                {
                    pro.Combo = Combo;
                    StartCoroutine(pro.StartDestroy());
                }
            }
            
            StartCoroutine(StartFlashing(matchList));
        }
    }

    IEnumerator StartDestroy()
    {
        mLocked = true;
        yield return null;
        if (mSkill == ProductSkill.KeepCombo)
            InGameManager.Inst.KeepCombo(Combo);
        InGameManager.Inst.AddScore(this);
        mAnimation.Play("destroy");
        KeepComboToUpperProduct();
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
        Destroy(gameObject);
    }

    public void StartToDrop()
    {
        List<Frame> empties = GetEmptyDownFrames();
        List<Frame> idles = GetIdleUpFrames();
        if (idles[0].IsDummy)
        {
            Frame curFrame = empties[empties.Count - 1];
            int idx = 0;
            while (true)
            {
                if (idx < idles.Count)
                {
                    Product pro = idles[idx].ChildProduct;
                    pro.StartDropAnimate(curFrame, empties.Count, idx == 0);
                }
                else
                {
                    Product pro = InGameManager.Inst.CreateNewProduct(curFrame);
                    pro.StartDropAnimate(curFrame, empties.Count, false);
                }
                idx++;

                if (curFrame.IsDummy)
                    break;
                else if(curFrame.Up() == null)
                    curFrame = curFrame.UpDummy();
                else
                    curFrame = curFrame.Up();
            }

        }
        else if (idles[idles.Count - 1].Up() == null)
        {
            idles.Add(idles[idles.Count - 1].UpDummy());
            Frame curFrame = empties[empties.Count - 1];
            int idx = 0;
            while(true)
            {
                if (idx < idles.Count)
                {
                    Product pro = idles[idx].ChildProduct;
                    pro.StartDropAnimate(curFrame, empties.Count, idx == 0);
                }
                else
                {
                    Product pro = InGameManager.Inst.CreateNewProduct(curFrame);
                    pro.StartDropAnimate(curFrame, empties.Count, false);
                }
                idx++;

                if (curFrame.IsDummy)
                    break;
                else if (curFrame.Up() == null)
                    curFrame = curFrame.UpDummy();
                else
                    curFrame = curFrame.Up();
            }
        }
        else
        {
            Frame curFrame = empties[empties.Count - 1];
            for (int i = 0; i < idles.Count; ++i)
            {
                Product pro = idles[i].ChildProduct;
                pro.StartDropAnimate(curFrame, empties.Count, i == 0);
                curFrame = curFrame.Up();
            }
        }
    }
    void StartDropAnimate(Frame parent, int emptyCount, bool isComboable)
    {
        mLocked = true;
        ParentFrame = parent;
        float height = InGameManager.GridSize * emptyCount;
        transform.localPosition = new Vector3(0, height, -1);
        StartCoroutine(AnimateDrop(isComboable));
    }
    IEnumerator AnimateDrop(bool isComboable)
    {
        //블럭이 떨어져야 하는 높이에 따라 다르게 delay를 줘서 떨어져야 한다.
        //채공시간은 0.6초, 착지순간은 모두 동일하게 수학적으로 계산한다.
        float totalTime = 0.6f;
        float totalHeight = InGameManager.GridSize * 8; //totalTime 동안 떨어지는 블럭 높이(즉 속도조절값)
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

        if (isComboable)
            StartCoroutine(DoMatch());
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
    #endregion

    #region Support Functions
    void SearchMatchedProducts(List<Product> products, ProductColor color)
    {
        if (mLocked || mColor != color)
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
    public bool IsTop()
    {
        return ParentFrame.IndexY == InGameManager.Inst.YCount - 1;
    }
    public Product DummyProduct()
    {
        int idxX = ParentFrame.IndexX;
        int idxY = InGameManager.Inst.YCount;
        return InGameManager.Inst.GetFrame(idxX, idxY).ChildProduct;
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
    void KeepComboToUpperProduct()
    {
        if (IsTop())
        {
            ParentFrame.UpDummy().ChildProduct.Combo = Combo;
            return;
        }
        Product upProduct = Up();
        if (upProduct == null)
            return;
        if (upProduct.IsLocked())
            return;

        upProduct.Combo = Combo;
    }
    void MakeProductEffect(int matchCount)
    {
        if (matchCount <= InGameManager.MatchCount)
            return;

        ProductSkill skill = ProductSkill.Nothing;
        Sprite image = null;
        switch(matchCount)
        {
            case 4:
                skill = ProductSkill.MatchOneMore;
                image = ImgOneMore;
                break;
            case 5:
                skill = ProductSkill.BreakSameColor;
                image = ImgSameColor;
                break;
            case 6:
                skill = ProductSkill.KeepCombo;
                image = ImgKeepCombo;
                break;
            default:
                skill = ProductSkill.ReduceColor;
                image = ImgReduceColor;
                break;
        }
        Product dummy = DummyProduct();
        dummy.mSkill = skill;
        dummy.GetComponent<SpriteRenderer>().sprite = image;
    }
    #endregion
}
