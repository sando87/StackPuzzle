using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackPoints : MonoBehaviour
{
    private const float mGridSize = 1;
    private int mAttackPoint = 0;
    private bool mIsReady = false;
    private List<GameObject> mChilds = new List<GameObject>();

    public GameObject[] Images = new GameObject[4];

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
                float speed = mGridSize * i;
                child.transform.position = Vector3.MoveTowards(child.transform.position, Vector3.zero, speed * Time.deltaTime);
            }

            if (mChilds[mChilds.Count - 1].transform.position.x <= 0.02f)
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
            mChilds.Add(Instantiate(Images[0]));
        for (int i = 0; i < cntB; ++i)
            mChilds.Add(Instantiate(Images[1]));
        for (int i = 0; i < cntC; ++i)
            mChilds.Add(Instantiate(Images[2]));
        for (int i = 0; i < cntD; ++i)
            mChilds.Add(Instantiate(Images[3]));

        StartCoroutine("AnimateUnFold");
    }

    IEnumerator AnimateUnFold()
    {
        float time = 0;
        while (time < 1)
        {
            for (int i = 0; i < mChilds.Count; ++i)
            {
                GameObject child = mChilds[i];
                float speed = mGridSize * i;
                Vector3 dest = new Vector3(mGridSize * i, 0, 0);
                child.transform.position = Vector3.MoveTowards(child.transform.position, dest, speed * Time.deltaTime);
            }
            time += Time.deltaTime;
            yield return null;
        }
    }
}
