using System.Collections;
using UnityEngine;

public class IceBlock : MonoBehaviour
{
    public bool IsIced { get { return ThresholdCombo > 0; } }
    public int ThresholdCombo { get; set; } = 0;
    public Frame ParentFrame { get { return transform.GetComponentInParent<Frame>(); } }

    public bool BreakChocoBlock(int combo)
    {
        if (combo < ThresholdCombo)
        {
            StartCoroutine(AnimateTwinkle());
            return false;
        }

        ThresholdCombo = 0;
        StartCoroutine(AnimShake());
        StartCoroutine(UnityUtils.CallAfterSeconds(UserSetting.MatchReadyInterval, () =>
        {
            SoundPlayer.Inst.PlaySoundEffect(SoundPlayer.Inst.EffectBreakIce);
            GetComponent<SpriteRenderer>().enabled = false;
            IceBlock obj = Instantiate(this, transform.position, Quaternion.identity, ParentFrame.transform);
            obj.transform.localScale = new Vector3(0.6f, 0.6f, 1);
            ParentFrame.StartCoroutine(AnimatePickedUp(obj.gameObject));
        }));
        return true;
    }
    public void SetChocoBlock(int thresholdCombo)
    {
        if (thresholdCombo <= 0)
            return;

        ThresholdCombo = thresholdCombo;
        GetComponent<SpriteRenderer>().enabled = true;
        //GetComponent<SpriteRenderer>().sprite = level <= Chocos.Length ? Chocos[level - 1] : ImgClosed;
        transform.localScale = Vector3.one;
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
        float dist = 0.05f;
        Vector3 startPos = transform.position;
        Vector3 dir = new Vector3(0, -1, 0);
        dir.Normalize();
        while (dist > 0.01f)
        {
            transform.position = startPos + (dist * dir);
            dist *= 0.7f;
            dir *= -1;
            yield return new WaitForSeconds(0.1f);
        }
        transform.position = startPos;
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
