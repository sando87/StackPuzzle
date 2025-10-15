using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

namespace JoyPop
{
    public class Product : MonoBehaviour
    {
        public ProductColor Color;
        public Animation Animation;
        public SpriteRenderer Renderer;
        public SpriteRenderer SkillRenderer;
        public Sprite[] ColorImages;
        public GameObject[] WaterDropPrefabs;
        public Sprite ImgHorizontal;
        public Sprite ImgVertical;
        public Sprite ImgBomb;
        public Sprite ImgSameColor;
        public Sprite ImgHammer;
        public Sprite ImgKeepCombo;
        public IceBlock IcedBlock;

        public Action EventUnWrapIce;
        public Action EventDestroy;

        public bool IsSkillable { get { return Skill != ProductSkill.Nothing && !SkillCasted; } }
        public InGameManager Manager { get; set; }
        public Frame ParentFrame { get; private set; }
        public ProductSkill Skill { get; private set; }
        public float DropSpeed { get; set; } = 0;
        public int InstanceID { get; set; }
        public bool IsMerging { get; private set; }
        public bool IsDestroying { get; private set; }
        public bool IsMoving { get; private set; }
        public bool IsDropping { get; private set; }
        private bool mSkillCasted = false;
        public bool SkillCasted { get { return mSkillCasted; } set { if (!IsObstacled()) mSkillCasted = value; } }
        public bool IsLocked { get { return IsDestroying || IsMerging || IsMoving || IsDropping || SkillCasted; } }
        public bool IsIceBlock { get { return IcedBlock.IsIced; } }
        public bool IsClosed { get { return false; } }
        public SwipChain Chain { get; set; } = null;
        public int KeepComboNum { get; set; } = 0;

        private BoxCollider2D mCollider = null;

        public void ResetProductColor(ProductColor color, InGameManager manager, Transform parent = null)
        {
            if (parent != null)
                transform.SetParent(parent);

            IsMerging = false;
            IsDestroying = false;
            IsMoving = false;
            IsDropping = false;
            Color = color;
            Renderer.enabled = true;
            Renderer.sprite = ColorImages[ColorToIndex(color)];
            Renderer.material.SetColor("_Color", new Color(0, 0, 0, 0));
            SkillRenderer.gameObject.SetActive(false);
            transform.localScale = new Vector3(0.6f, 0.6f, 1);
            transform.localPosition = new Vector3(0, 0, -1);
            Skill = ProductSkill.Nothing;
            DropSpeed = 0;
            mSkillCasted = false;
            IcedBlock.SetDepth(0);
            EventUnWrapIce = null;
            EventDestroy = null;
            KeepComboNum = 0;
            gameObject.SetActive(true);
            if (mCollider == null)
                mCollider = GetComponent<BoxCollider2D>();
            mCollider.enabled = true;
            Manager = manager;
        }
        public int ColorToIndex(ProductColor color)
        {
            return (int)color - 1;
        }
        public void ReturnToPool()
        {
            gameObject.SetActive(false);
            Detach(Manager.transform);
            Destroy(gameObject);
        }

        void Awake()
        {
            mCollider = GetComponent<BoxCollider2D>();
            IcedBlock.EventBreakIce += () =>
            {
                EventUnWrapIce?.Invoke();
            };
        }

        public void AttachTo(Frame parentFrame)
        {
            parentFrame.ChildProduct = this;
            ParentFrame = parentFrame;
            transform.SetParent(parentFrame.transform);
        }
        public Frame Detach(Transform toTransform)
        {
            if (ParentFrame != null)
            {
                ParentFrame.ChildProduct = null;
            }

            Frame frame = ParentFrame;
            transform.SetParent(toTransform);
            ParentFrame = null;
            return frame;
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
            mCollider.enabled = false;
            IsMerging = true;
            ParentFrame.TouchBush();
            ParentFrame.CreateComboTextEffect(combo, Color);

            SoundPlayer.Inst.PlaySoundEffect(ClipSound.Match, Manager.SFXVolume);

            Animation.Play("spitout");
            StartCoroutine(AnimateFlash(1.3f));
            return true;
        }
        public void MergeImImmediately(Product destProduct, ProductSkill skill)
        {
            if (destProduct == this)
            {
                mCollider.enabled = true;
                IsMerging = false;
                
                if (skill == ProductSkill.SameColor)
                    SoundPlayer.Inst.PlaySoundEffect(ClipSound.Merge3, Manager.SFXVolume);
                else if (skill == ProductSkill.Bomb)
                    SoundPlayer.Inst.PlaySoundEffect(ClipSound.Merge2, Manager.SFXVolume);
                else
                    SoundPlayer.Inst.PlaySoundEffect(ClipSound.Merge1, Manager.SFXVolume);

                ChangeProductImage(skill);

                Renderer.enabled = true;
                SkillRenderer.transform.DOScale(Vector3.one, 0.3f).From(Vector3.zero).OnComplete(() => Renderer.enabled = false);

                SkillRenderer.transform.DORotate(new Vector3(0, 0, 360), 0.3f, RotateMode.FastBeyond360);
            }
            else
            {
                mCollider.enabled = false;
                Frame parent = Detach(Manager.transform);
                if (Chain != null)
                    Chain.DestroyChain();

                StartCoroutine(AnimateMoveTo(destProduct.transform.position, 0.2f, () =>
                {
                    ReturnToPool();
                }));
            }
        }
        public bool IsObstacled()
        {
            if (IsIceBlock)
                return true;

            return false;
        }
        public void BreakObstacle(int count = 1)
        {
            if (IsIceBlock)
            {
                BreakIceBlock(count);
            }
        }
        public bool ReadyForDestroy(int combo)
        {
            if (IsObstacled() || IsLocked)
            {
                return false;
            }

            mCollider.enabled = false;
            IsDestroying = true;
            ParentFrame.TouchBush();
            Animation.Play("destroy");
            StartCoroutine(AnimateFlash(1.3f));
            return true;
        }
        public void DestroyImmediately(int combo)
        {
            if (ParentFrame == null)
                return;

            mCollider.enabled = false;
            SoundPlayer.Inst.PlaySoundEffect(ClipSound.Match, Manager.SFXVolume);

            IsDestroying = true;
            Animation.Stop();
            transform.localPosition = new Vector3(0, 0, -1);
            transform.localScale = new Vector3(0.6f, 0.6f, 1);

            ParentFrame.CreateComboTextEffect(combo, Color);

            int index = ColorToIndex(Color);
            GameObject vfx = ObjectPooling.Instance.Instantiate(WaterDropPrefabs[index], transform.position, Quaternion.identity, Manager.transform);
            vfx.transform.localScale = new Vector3(0.6f, 0.6f, 1);
            vfx.ReturnAfter(2);
            
            EventDestroy?.Invoke();

            ReturnToPool();
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
            if (Chain != null)
                Chain.DestroyChain();
            Manager.ProductIDs.Remove(InstanceID);
            Destroy(gameObject);
        }

        public void StartToDrop(Frame frame, float duration)
        {
            if (ParentFrame != null)
                Detach(ParentFrame.VertFrames.transform);

            if (Chain != null)
                Chain.DestroyChain();

            IsDropping = true;
            mCollider.enabled = false;
            AttachTo(frame);
            transform.DOKill();
            transform.DOLocalMoveY(0, duration).SetEase(Ease.InQuad).OnComplete(() =>
            {
                Animation.Play("drop");
            });
        }
        public void DropEnd()
        {
            IsDropping = false;
            DropSpeed = 0;
            transform.localPosition = new Vector3(0, 0, -1);
            mCollider.enabled = true;
            DisableMasking();
        }


        IEnumerator AnimateMoveTo(Vector3 destPos, float duration, Action EventMoveEnd)
        {
            IsMoving = true;
            float vel = (transform.position - destPos).magnitude / duration;
            float time = 0;
            while (time < duration)
            {
                Vector3 dir = destPos - transform.position;
                dir.z = 0;
                dir.Normalize();
                transform.position += dir * vel * Time.deltaTime;
                time += Time.deltaTime;
                yield return null;
            }
            transform.position = destPos;
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
                if (SkillRenderer.gameObject.activeSelf)
                    SkillRenderer.material.SetColor("_Color", new Color(light, light, light, 0));
                t += Time.deltaTime;
                yield return null;
            }
            
            Renderer.material.color = new Color(0, 0, 0, 0);
            if (SkillRenderer.gameObject.activeSelf)
                SkillRenderer.material.color = new Color(0, 0, 0, 0);
        }


        #region Support Functions

        public void SearchMatchedProducts(List<Product> products, ProductColor color)
        {
            if (Color != color || IsObstacled() || Skill != ProductSkill.Nothing || IsLocked)
                return;

            if (ParentFrame == null)
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
            foreach (Frame iter in frames)
            {
                Product child = iter.ChildProduct;
                if (child != null)
                    products.Add(child);
            }
            return products.ToArray();
        }
        public void ChangeProductImage(ProductSkill skill)
        {
            // Animation.Play("swap");
            Skill = skill;
            SkillRenderer.gameObject.SetActive(true);
            Renderer.enabled = false;
            switch (skill)
            {
                case ProductSkill.Horizontal: SkillRenderer.sprite = ImgHorizontal; break;
                case ProductSkill.Vertical: SkillRenderer.sprite = ImgVertical; break;
                case ProductSkill.Bomb: SkillRenderer.sprite = ImgBomb; break;
                case ProductSkill.SameColor: SkillRenderer.sprite = ImgSameColor; break;
                case ProductSkill.Hammer: SkillRenderer.sprite = ImgHammer; break;
                case ProductSkill.KeepCombo: SkillRenderer.sprite = ImgKeepCombo; break;
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


        private void BreakIceBlock(int count = 1)
        {
            if (IcedBlock.IsIced)
            {
                IcedBlock.BreakIce(count, Manager);
            }
        }

        public void ShakeIceBlock()
        {
            if (IcedBlock.IsIced)
            {
                IcedBlock.ShakeIce();
            }
        }

        #endregion
    }
}