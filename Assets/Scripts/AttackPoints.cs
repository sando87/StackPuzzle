using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackPoints : MonoBehaviour
{
    private const float mGridSize = 1;
    private int mAttackPoint = 0;
    private bool mIsReady = false;
    private List<GameObject> mChilds = new List<GameObject>();

    public GameObject BaseSprite;
    public Sprite[] Images = new Sprite[4];

    public bool IsEmpty { get { return mChilds.Count == 0; } }
    public bool IsReady { get { return mIsReady; } }
    public int Add(int point)
    {
        mAttackPoint += point;
        if (mAttackPoint < 0)
        {
            point = mAttackPoint - point;
            mAttackPoint = 0;
        }

        mIsReady = false;
        StopCoroutine("WaitForReady");
        StartCoroutine("WaitForReady");

        StopCoroutine("AnimateFold");
        StopCoroutine("AnimateUnFold");

        StartCoroutine("AnimateFold");
        return point;
    }
    public int Pop(int point)
    {
        mAttackPoint -= point;
        if (mAttackPoint < 0)
        {
            point += mAttackPoint;
            mAttackPoint = 0;
        }

        StopCoroutine("AnimateFold");
        StopCoroutine("AnimateUnFold");

        StartCoroutine("AnimateFold");
        return point;
    }

    IEnumerator WaitForReady()
    {
        yield return new WaitForSeconds(2);
        mIsReady = true;
    }

    IEnumerator AnimateFold()
    {
        while (mChilds.Count > 0)
        {
            for(int i = 0; i < mChilds.Count; ++i)
            {
                GameObject child = mChilds[i];
                float speed = mGridSize * i * 5;
                child.transform.localPosition = Vector3.MoveTowards(child.transform.localPosition, Vector3.zero, speed * Time.deltaTime);
            }

            if (mChilds[mChilds.Count - 1].transform.localPosition.x <= 0.02f)
                break;

            yield return null;
        }

        CreateNewChild();

    }

    private void CreateNewChild()
    {
        for (int i = 0; i < mChilds.Count; ++i)
            Destroy(mChilds[i]);
        mChilds.Clear();

        int cntA = mAttackPoint / 100;
        int cntB = (mAttackPoint % 100) / 25;
        int cntC = (mAttackPoint % 25) / 5;
        int cntD = mAttackPoint % 5;

        for (int i = 0; i < cntA; ++i)
        {
            GameObject obj = Instantiate(BaseSprite, transform);
            obj.GetComponent<SpriteRenderer>().sprite = Images[3];
            mChilds.Add(obj);
        }
        for (int i = 0; i < cntB; ++i)
        {
            GameObject obj = Instantiate(BaseSprite, transform);
            obj.GetComponent<SpriteRenderer>().sprite = Images[2];
            mChilds.Add(obj);
        }
        for (int i = 0; i < cntC; ++i)
        {
            GameObject obj = Instantiate(BaseSprite, transform);
            obj.GetComponent<SpriteRenderer>().sprite = Images[1];
            mChilds.Add(obj);
        }
        for (int i = 0; i < cntD; ++i)
        {
            GameObject obj = Instantiate(BaseSprite, transform);
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
                float speed = mGridSize * i * 5;
                Vector3 dest = new Vector3(mGridSize * i, 0, 0);
                child.transform.localPosition = Vector3.MoveTowards(child.transform.localPosition, dest, speed * Time.deltaTime);
            }
            time += Time.deltaTime;
            yield return null;
        }
    }
}
