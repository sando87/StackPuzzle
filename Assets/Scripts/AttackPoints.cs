using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackPoints : MonoBehaviour
{
    private const float mDist = 0.6f;
    private int mAttackPoint = 0;
    private bool mIsReady = false;
    private List<GameObject> mChilds = new List<GameObject>();

    public GameObject BaseSprite;
    public GameObject Projectile;
    public Sprite[] Images = new Sprite[4];

    public int Count { get { return mAttackPoint; } }
    public bool IsReady { get { return mIsReady; } }
    public void Add(int point, Vector3 fromPos)
    {
        fromPos.z = -4;
        GameObject proj = Instantiate(Projectile, fromPos, Quaternion.identity);
        Vector3 dest = transform.position;
        dest.z = fromPos.z;
        proj.transform.LookAt(dest);
        //proj.GetComponent<Rigidbody>().AddForce(proj.transform.forward * 500);
        StartCoroutine(MovingProjectile(proj, point));
    }
    public int Pop(int point)
    {
        if (mAttackPoint < point)
        {
            point = mAttackPoint;
            mAttackPoint = 0;
        }
        else
            mAttackPoint -= point;

        StopCoroutine("AnimateFold");
        StopCoroutine("AnimateUnFold");

        StartCoroutine("AnimateFold");
        return point;
    }

    IEnumerator MovingProjectile(GameObject projectile, int point)
    {
        float duration = 1;
        Vector3 diff = transform.position - projectile.transform.position;
        diff.z = 0;
        float dist = diff.magnitude;
        float a = -1 * dist / (duration * duration);
        float time = 0;
        diff.Normalize();
        Vector3 pos = projectile.transform.position;
        while (time < duration)
        {
            float k = a * (time - duration) * (time - duration) + dist;
            projectile.transform.position = (pos + diff * k);
            time += Time.deltaTime;
            yield return null;
        }
        projectile.GetComponent<SciFiProjectileScript>().DeadEffect();

        mAttackPoint += point;
        if (mAttackPoint < 0)
            mAttackPoint = 0;

        mIsReady = false;
        StopCoroutine("WaitForReady");
        StartCoroutine("WaitForReady");

        StopCoroutine("AnimateFold");
        StopCoroutine("AnimateUnFold");

        StartCoroutine("AnimateFold");

    }
    IEnumerator WaitForReady()
    {
        yield return new WaitForSeconds(UserSetting.ChocoFlushInterval);
        mIsReady = true;
    }

    IEnumerator AnimateFold()
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

        CreateNewChild();

    }

    private void CreateNewChild()
    {
        for (int i = 0; i < mChilds.Count; ++i)
            Destroy(mChilds[i]);
        mChilds.Clear();

        int cntA = mAttackPoint / 27;
        int cntB = (mAttackPoint % 27) / 9;
        int cntC = (mAttackPoint % 9) / 3;
        int cntD = mAttackPoint % 3;

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
