using System.Collections;
using TMPro;
using UnityEngine;

public class IceBlock : MonoBehaviour
{
    // [SerializeField]
    // private TextMeshPro ComboText = null;
    [SerializeField]
    private Sprite[] IceBlockImages = null;

    public bool IsIced { get { return BreakDepth > 0; } }
    public int BreakDepth { get; set; } = 0;
    public Frame ParentFrame { get { return transform.GetComponentInParent<Frame>(); } }

    public bool BreakBlock(int count)
    {
        if (!IsIced)
        {
            return false;
        }

        StartCoroutine(AnimShake());
        BreakAction(count);

        return true;
    }
    private void BreakAction(int count)
    {
        SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectBreakIce);

        IceBlock obj = Instantiate(this, transform.position, Quaternion.identity, ParentFrame.transform);
        obj.SetDepth(BreakDepth);
        obj.GetComponent<SpriteRenderer>().sortingLayerName = "UIParticle";
        obj.GetComponent<SpriteRenderer>().sortingOrder = 1;
        obj.transform.localScale = new Vector3(0.6f, 0.6f, 1);
        ParentFrame.StartCoroutine(AnimatePickedUp(obj.gameObject));

        SetDepth(Mathf.Max(0, BreakDepth - count));
    }
    public void SetDepth(int depth)
    {
        CancelInvoke("BreakAction");
        BreakDepth = depth;
        gameObject.SetActive(IsIced);
        GetComponent<SpriteRenderer>().sprite = IceBlockImages[BreakDepth];
        //ComboText.text = BreakDepth.ToString();
        transform.localScale = Vector3.one;
        transform.localPosition = new Vector3(0, 0, -0.5f);
    }

    private IEnumerator AnimatePickedUp(GameObject obj)
    {
        float time = 0;
        float duration = 3.0f;
        Vector3 acc = new Vector3(0, -60.0f, 0);
        float x = UnityEngine.Random.Range(-3f, 3f);
        float y = UnityEngine.Random.Range(20.0f, 25.0f);
        float rot = y * 0.5f;
        Vector3 startVel = new Vector3(x, y, 0);
        while (time < duration)
        {
            obj.transform.position += (startVel * Time.deltaTime);
            obj.transform.Rotate(x < 0 ? Vector3.forward : Vector3.back, rot);
            startVel += (acc * Time.deltaTime);
            time += Time.deltaTime;
            yield return null;
        }
        Destroy(obj);
    }
    IEnumerator AnimShake()
    {
        float dist = 0.1f;
        Vector3 startPos = transform.localPosition;
        Vector3 dir = new Vector3(0, -1, 0);
        dir.Normalize();
        while (dist > 0.01f)
        {
            transform.localPosition = startPos + (dist * dir);
            dist *= 0.7f;
            dir *= -1;
            yield return new WaitForSeconds(0.1f);
        }
        transform.localPosition = startPos;
    }
    IEnumerator AnimateTwinkle()
    {
        float t = 0;
        SpriteRenderer ren = GetComponent<SpriteRenderer>();
        while (t < 0.4f)
        {
            int light = (int)(t * 10) % 2;
            ren.material.SetColor("_Color", new Color(1 - light, 1 - light, 1 - light, 0));
            t += Time.deltaTime;
            yield return null;
        }
        ren.material.color = new Color(0, 0, 0, 0);
    }
}
