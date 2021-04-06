using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackPoints : MonoBehaviour
{
    [SerializeField] private Sprite[] Images = new Sprite[4];
    [SerializeField] private GameObject BaseSprite = null;

    private const float mAnimateSpeed = 0.6f;
    private List<GameObject> mChilds = new List<GameObject>();

    public int Points { get; private set; }
    public float TouchedTime { get; private set; }

    //private int mChocoCount = 0;
    //private int mAttackPoint = 0;
    //private float InitSize = 1.0f;
    //private float mScaleForEffect = 1.0f;
    //private AttackPoints OppAttackPoints = null;
    //private InGameManager ParentManager { get { return transform.parent.GetComponent<InGameManager>(); } }
    //public GameObject Projectile;
    //public int Count { get { return mAttackPoint; } }


    private void StartAnimate()
    {
        StopCoroutine("AnimateFold");
        StopCoroutine("AnimateUnFold");

        StartCoroutine("AnimateFold", Points);
    }
    private IEnumerator AnimateFold(int count)
    {
        if (mChilds.Count > 0)
        {

            float time = 0;
            while (time < 0.2f)
            {
                for (int i = 0; i < mChilds.Count; ++i)
                {
                    GameObject child = mChilds[i];
                    float speed = mAnimateSpeed * (i + 1) * 5;
                    child.transform.localPosition = Vector3.MoveTowards(child.transform.localPosition, Vector3.zero, speed * Time.deltaTime);
                }
                time += Time.deltaTime;
                yield return null;
            }
        }

        ClearChocos();
        if(count != 0)
            CreateNewChild(count);

    }
    private void ClearChocos()
    {
        for (int i = 0; i < mChilds.Count; ++i)
            Destroy(mChilds[i]);
        mChilds.Clear();
    }
    private void CreateNewChild(int points)
    {
        int count = Math.Abs(points);
        int cntA = count / 27;
        int cntB = (count % 27) / 9;
        int cntC = (count % 9) / 3;
        int cntD = count % 3;

        for (int i = 0; i < cntA; ++i)
        {
            GameObject obj = Instantiate(BaseSprite, transform);
            obj.transform.localScale = new Vector3(0.5f, 0.5f, 1.0f);
            obj.GetComponent<SpriteRenderer>().sprite = Images[3];
            mChilds.Add(obj);
        }
        for (int i = 0; i < cntB; ++i)
        {
            GameObject obj = Instantiate(BaseSprite, transform);
            obj.transform.localScale = new Vector3(0.5f, 0.5f, 1.0f);
            obj.GetComponent<SpriteRenderer>().sprite = Images[2];
            mChilds.Add(obj);
        }
        for (int i = 0; i < cntC; ++i)
        {
            GameObject obj = Instantiate(BaseSprite, transform);
            obj.transform.localScale = new Vector3(0.5f, 0.5f, 1.0f);
            obj.GetComponent<SpriteRenderer>().sprite = Images[1];
            mChilds.Add(obj);
        }
        for (int i = 0; i < cntD; ++i)
        {
            GameObject obj = Instantiate(BaseSprite, transform);
            obj.transform.localScale = new Vector3(0.5f, 0.5f, 1.0f);
            obj.GetComponent<SpriteRenderer>().sprite = Images[0];
            mChilds.Add(obj);
        }

        StartCoroutine("AnimateUnFold", points > 0 ? true : false);
    }
    private IEnumerator AnimateUnFold(bool isLeft)
    {
        float time = 0;
        while (time < 0.2f)
        {
            for (int i = 0; i < mChilds.Count; ++i)
            {
                GameObject child = mChilds[i];
                float speed = mAnimateSpeed * (i + 1) * 5;
                Vector3 dest = new Vector3(mAnimateSpeed * (i + 1), 0, 0);
                if (isLeft)
                    dest.x *= -1;
                child.transform.localPosition = Vector3.MoveTowards(child.transform.localPosition, dest, speed * Time.deltaTime);
            }
            time += Time.deltaTime;
            yield return null;
        }
    }

    public void ResetPoints()
    {
        TouchedTime = 0;
        Points = 0;
        ClearChocos();
    }
    public void AddPoints(int point)
    {
        TouchedTime = Time.realtimeSinceStartup;
        Points += point;
        StartAnimate();
    }
    public int Flush(int reqPoint)
    {
        TouchedTime = Time.realtimeSinceStartup;

        if (Points > 0)
        {
            int flushPoint = Points >= reqPoint ? reqPoint : Points;
            Points -= flushPoint;
            StartAnimate();
            return flushPoint;
        }
        else if(Points < 0)
        {
            int flushPoint = -Points >= reqPoint ? reqPoint : -Points;
            Points += flushPoint;
            StartAnimate();
            return flushPoint;
        }

        return 0;
    }

    /*
    public void Add(int point, Vector3 fromPos)
    {
        if (OppAttackPoints == null)
        {
            ParentManager = transform.parent.GetComponent<InGameManager>();
            OppAttackPoints = ParentManager.Opponent.AttackPoints;
            InitSize = Projectile.transform.localScale.x;
            mScaleForEffect = ParentManager.FieldType == GameFieldType.pvpPlayer ? UserSetting.BattleOppResize : 1 / UserSetting.BattleOppResize;
            mScaleForEffect *= InitSize;
        }


        mAttackPoint += point;
        if (mAttackPoint < 0)
        {
            OppAttackPoints.mAttackPoint = Mathf.Abs(mAttackPoint);
            mAttackPoint = 0;
        }

        fromPos.z -= 1;
        GameObject obj = GameObject.Instantiate(Projectile, fromPos, Quaternion.identity, transform.parent);
        obj.transform.localScale = new Vector3(mScaleForEffect, mScaleForEffect, 1);
        int imgIndex = Mathf.Abs(point) >= 12 ? 3 : (Mathf.Abs(point) / 3);
        obj.GetComponent<SpriteRenderer>().sprite = Images[imgIndex];

        if (ParentManager.FieldType != GameFieldType.pvpPlayer && point > 0)
        {
            StartCoroutine(AnimateThrowSide(obj, transform.position, 1.0f, () =>
            {
                AddChoco(point);
                Destroy(obj);
            }));
        }
        else
        {
            StartCoroutine(AnimateThrowOver(obj, transform.position, 1.0f, () =>
            {
                AddChoco(point);
                Destroy(obj);
            }));
        }
    }
    IEnumerator AnimateThrowSide(GameObject obj, Vector3 worldDest, float duration, Action action = null)
    {
        float time = 0;
        Vector3 startPos = obj.transform.position;
        Vector3 destPos = worldDest;
        Vector3 dir = destPos - startPos;
        Vector3 offset = Vector3.zero;
        Vector3 axisZ = new Vector3(0, 0, 1);
        Vector3 resize = new Vector3(1, 1, 1);
        float slopeY = dir.y / (duration * duration);
        float slopeX = -dir.x / (duration * duration);
        float slopeSize = (InitSize - mScaleForEffect) / duration;
        while (time < duration)
        {
            float nowT = time - duration;
            offset.x = slopeX * nowT * nowT + dir.x;
            offset.y = slopeY * time * time;
            obj.transform.position = startPos + offset;
            obj.transform.Rotate(axisZ, (offset - dir).magnitude);

            resize.x = slopeSize * time + mScaleForEffect;
            resize.y = slopeSize * time + mScaleForEffect;
            obj.transform.localScale = resize;
            time += Time.deltaTime;
            yield return null;
        }

        action?.Invoke();
    }
    IEnumerator AnimateThrowOver(GameObject obj, Vector3 worldDest, float duration, Action action = null)
    {
        float time = 0;
        Vector3 startPos = obj.transform.position;
        Vector3 destPos = worldDest;
        Vector3 dir = destPos - startPos;
        Vector3 offset = Vector3.zero;
        Vector3 axisZ = new Vector3(0, 0, 1);
        Vector3 resize = new Vector3(1, 1, 1);
        float slopeY = -dir.y / (duration * duration);
        float slopeX = -dir.x / (duration * duration);
        float slopeSize = (InitSize - mScaleForEffect) / duration;
        while (time < duration)
        {
            float nowT = time - duration;
            offset.x = slopeX * nowT * nowT + dir.x;
            offset.y = slopeY * nowT * nowT + dir.y;
            obj.transform.position = startPos + offset;
            obj.transform.Rotate(axisZ, (offset - dir).magnitude);

            resize.x = slopeSize * time + mScaleForEffect;
            resize.y = slopeSize * time + mScaleForEffect;
            obj.transform.localScale = resize;
            time += Time.deltaTime;
            yield return null;
        }

        action?.Invoke();
    }
    */

}
