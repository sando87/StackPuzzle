using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackPoints : MonoBehaviour
{
    private const float mDist = 0.6f;
    private int mChocoCount = 0;
    private int mAttackPoint = 0;
    private bool mIsReady = false;
    private AttackPoints OppAttackPoints = null;
    private List<GameObject> mChilds = new List<GameObject>();

    public GameObject BaseSprite;
    public GameObject Projectile;
    public Sprite[] Images = new Sprite[4];

    public int Count { get { return mAttackPoint; } }
    public bool IsReady { get { return mIsReady && mChocoCount > 0; } }
    public void Add(int point, Vector3 fromPos)
    {
        if (OppAttackPoints == null)
            OppAttackPoints = transform.parent.GetComponent<BattleFieldManager>().Opponent.AttackPoints;

        mAttackPoint += point;
        if(mAttackPoint < 0)
        {
            OppAttackPoints.mAttackPoint = Mathf.Abs(mAttackPoint);
            mAttackPoint = 0;
        }

        fromPos.z -= 1;
        GameObject obj = GameObject.Instantiate(Projectile, fromPos, Quaternion.identity, transform.parent);
        int imgIndex = Mathf.Abs(point) >= 12 ? 3 : (Mathf.Abs(point) / 3);
        obj.GetComponent<SpriteRenderer>().sprite = Images[imgIndex];
        StartCoroutine(Utils.AnimateConcave(obj, transform.position, 1.0f, () =>
        {
            AddChoco(point);
            Destroy(obj);
        }));
    }
    public int Pop(int point)
    {
        if (mChocoCount < point)
            point = mChocoCount;

        mAttackPoint -= point;
        AddChoco(-point);

        return point;
    }

    void AddChoco(int count)
    {
        mChocoCount += count;

        mIsReady = false;
        StopCoroutine("WaitForReady");
        StartCoroutine("WaitForReady");

        StopCoroutine("AnimateFold");
        StopCoroutine("AnimateUnFold");

        if (mChocoCount < 0)
        {
            OppAttackPoints.AddChoco(Mathf.Abs(mChocoCount));
            mChocoCount = 0;
            StartCoroutine("AnimateFold", mChocoCount);
        }
        else
        {
            StartCoroutine("AnimateFold", mChocoCount);
        }
    }
    IEnumerator WaitForReady()
    {
        yield return new WaitForSeconds(UserSetting.ChocoFlushInterval);
        mIsReady = true;
    }

    IEnumerator AnimateFold(int count)
    {

        if (mChilds.Count > 0)
        {

            float time = 0;
            while (time < 0.2f)
            {
                for (int i = 0; i < mChilds.Count; ++i)
                {
                    GameObject child = mChilds[i];
                    float speed = mDist * (i + 1) * 5;
                    child.transform.localPosition = Vector3.MoveTowards(child.transform.localPosition, Vector3.zero, speed * Time.deltaTime);
                }
                time += Time.deltaTime;
                yield return null;
            }
        }

        ClearChocos();
        if(count > 0)
            CreateNewChild(count);

    }

    private void ClearChocos()
    {
        for (int i = 0; i < mChilds.Count; ++i)
            Destroy(mChilds[i]);
        mChilds.Clear();
    }

    private void CreateNewChild(int count)
    {
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

        StartCoroutine("AnimateUnFold");
    }

    IEnumerator AnimateUnFold()
    {
        float time = 0;
        while (time < 0.2f)
        {
            for (int i = 0; i < mChilds.Count; ++i)
            {
                GameObject child = mChilds[i];
                float speed = mDist * (i + 1) * 5;
                Vector3 dest = new Vector3(mDist * (i + 1), 0, 0);
                child.transform.localPosition = Vector3.MoveTowards(child.transform.localPosition, dest, speed * Time.deltaTime);
            }
            time += Time.deltaTime;
            yield return null;
        }
    }
}
