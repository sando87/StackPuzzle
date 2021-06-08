using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.UI;

public class UnityUtils
{
    public static void DisableAllChilds(GameObject parent)
    {
        int cnt = parent.transform.childCount;
        for(int i = 0; i < cnt; ++i)
        {
            parent.transform.GetChild(i).gameObject.SetActive(false);
        }
    }
    public static IEnumerator CallAfterSeconds(float delay, Action func)
    {
        if (delay > 0)
            yield return new WaitForSeconds(delay);

        func.Invoke();

        yield return null;
    }

    //오목함수
    public static IEnumerator AnimateConcave(GameObject obj, Vector3 worldDest, float duration, Action action = null)
    {
        float time = 0;
        Vector3 startPos = obj.transform.position;
        Vector3 destPos = worldDest;
        Vector3 dir = destPos - startPos;
        Vector3 offset = Vector3.zero;
        Vector3 axisZ = new Vector3(0, 0, 1);
        float slopeY = dir.y / (duration * duration);
        float slopeX = -dir.x / (duration * duration);
        while (time < duration)
        {
            float nowT = time - duration;
            offset.x = slopeX * nowT * nowT + dir.x;
            offset.y = slopeY * time * time;
            obj.transform.position = startPos + offset;
            obj.transform.Rotate(axisZ, (offset - dir).magnitude);
            time += Time.deltaTime;
            yield return null;
        }

        action?.Invoke();
    }

    //볼록함수
    public static IEnumerator AnimateConvex(GameObject obj, Vector3 worldDest, float duration, Action action = null)
    {
        float time = 0;
        Vector3 startPos = obj.transform.position;
        Vector3 destPos = worldDest;
        Vector3 dir = destPos - startPos;
        Vector3 offset = Vector3.zero;
        Vector3 axisZ = new Vector3(0, 0, 1);
        float slopeY = -dir.y / (duration * duration);
        float slopeX = -dir.x / (duration * duration);
        while (time < duration)
        {
            float nowT = time - duration;
            offset.x = slopeX * nowT * nowT + dir.x;
            offset.y = slopeY * nowT * nowT + dir.y;
            obj.transform.position = startPos + offset;
            obj.transform.Rotate(axisZ, (offset - dir).magnitude);
            time += Time.deltaTime;
            yield return null;
        }

        action?.Invoke();
    }
    public static IEnumerator AnimateThrow(GameObject obj)
    {
        float dragCoeff = 0.05f;
        int ranDegree = UnityEngine.Random.Range(0, 360);
        int power = UnityEngine.Random.Range(10, 15);
        Vector3 startPos = obj.transform.position;
        Vector3 dir = new Vector3(Mathf.Cos(ranDegree * Mathf.Deg2Rad), Mathf.Sin(ranDegree * Mathf.Deg2Rad), 0);
        dir.Normalize();
        Vector3 speed = power * dir;
        while (true)
        {
            yield return null;
            if (obj == null)
                break;

            obj.transform.position += speed * Time.deltaTime;
            speed -= dragCoeff * speed.sqrMagnitude * dir;
            if (Vector3.Dot(dir, speed) < 0)
                break;
        }
    }
    public static IEnumerator AnimateStandOut(GameObject obj)
    {
        Vector3 backup = new Vector3(1, 1, 1); //obj.transform.localScale;
        Vector3 maxSize = backup * 1.2f;
        float time = 0;
        float duration = 0.2f;
        float durationH = duration * 0.5f;
        Vector3 coeff = ((backup - maxSize) / (durationH * durationH));
        while (time < duration)
        {
            Vector3 size = coeff * (time - durationH) * (time - durationH) + maxSize;
            obj.transform.localScale = size;
            time += Time.deltaTime;
            yield return null;
        }
        obj.transform.localScale = backup;
    }
    public static float AccelPlus(float time, float duration)
    {
        return time * time / (duration * duration);
    }
    public static float AccelMinus(float time, float duration)
    {
        return 1 - AccelPlus(time, duration);
    }
    public static float DecelPlus(float time, float duration)
    {
        return 1 - DecelMinus(time, duration);
    }
    public static float DecelMinus(float time, float duration)
    {
        return (time - duration) * (time - duration) / (duration * duration);
    }

    //등속도로 목적지까지 움직인다.
    public static IEnumerator MoveLinear(GameObject obj, Vector2 dest, float duration, float rotDeg, Action EventEnd = null)
    {
        float time = 0;
        Vector2 startPos = obj.transform.position;
        Vector2 dir = dest - startPos;
        float distance = dir.magnitude;
        dir.Normalize();
        float speed = distance / duration;
        while (time < duration)
        {
            Vector2 nextPos = startPos + (dir * speed * time);
            obj.transform.SetPosition2D(nextPos);
            obj.transform.Rotate(Vector3.back, rotDeg);
            yield return null;
            time += Time.deltaTime;
        }
        obj.transform.SetPosition2D(dest);
        obj.transform.rotation = Quaternion.identity;
        EventEnd?.Invoke();
    }

    //부드럽게 목적지까지 움직인다.(Cos 0~180도 파형)
    public static IEnumerator MoveNatural(GameObject obj, Vector2 dest, float duration, Action EventEnd = null)
    {
        float time = 0;
        Vector2 startPos = obj.transform.position;
        Vector2 dir = dest - startPos;
        float distance = dir.magnitude;
        dir.Normalize();
        while (time < duration)
        {
            float rad = Mathf.PI * time / duration;
            float curDist = (1 - Mathf.Cos(rad)) * 0.5f * distance;
            Vector2 nextPos = startPos + (dir * curDist);
            obj.transform.SetPosition2D(nextPos);
            yield return null;
            time += Time.deltaTime;
        }
        obj.transform.SetPosition2D(dest);
        EventEnd?.Invoke();
    }

    //가속하며 목적지까지 움직인다.
    public static IEnumerator MoveAccelerate(GameObject obj, Vector2 dest, float duration, Action EventEnd = null)
    {
        float time = 0;
        Vector2 startPos = obj.transform.position;
        Vector2 dir = dest - startPos;
        float distance = dir.magnitude;
        dir.Normalize();
        while (time < duration)
        {
            float curDist = distance * time * time / (duration * duration);
            Vector2 nextPos = startPos + (dir * curDist);
            obj.transform.SetPosition2D(nextPos);
            yield return null;
            time += Time.deltaTime;
        }
        obj.transform.SetPosition2D(dest);
        EventEnd?.Invoke();
    }

    //감속하며 목적지까지 움직인다.
    public static IEnumerator MoveDecelerate(GameObject obj, Vector2 dest, float duration, Action EventEnd = null)
    {
        float time = 0;
        Vector2 startPos = obj.transform.position;
        Vector2 dir = dest - startPos;
        float distance = dir.magnitude;
        dir.Normalize();
        while (time < duration)
        {
            float curDist = distance * (1 - ((time - duration) * (time - duration) / (duration * duration)));
            Vector2 nextPos = startPos + (dir * curDist);
            obj.transform.SetPosition2D(nextPos);
            yield return null;
            time += Time.deltaTime;
        }
        obj.transform.SetPosition2D(dest);
        EventEnd?.Invoke();
    }

    public static IEnumerator FadeOut(Image image, Action EventEnd = null)
    {
        float duration = 0.1f;
        float time = 0;
        Color oriColor = image.color;
        Color curColor = oriColor;
        image.gameObject.SetActive(true);
        while (time < duration)
        {
            curColor.a = oriColor.a * (1 - time / duration);
            image.color = curColor;
            yield return null;
            time += Time.deltaTime;
        }

        EventEnd?.Invoke();
        image.color = oriColor;
        image.gameObject.SetActive(false);
    }
    public static IEnumerator FadeIn(Image image, Action EventEnd = null)
    {
        float duration = 0.1f;
        float time = 0;
        Color oriColor = image.color;
        Color curColor = oriColor;
        image.gameObject.SetActive(true);
        while (time < duration)
        {
            curColor.a = oriColor.a * time / duration;
            image.color = curColor;
            yield return null;
            time += Time.deltaTime;
        }
        image.color = oriColor;
        EventEnd?.Invoke();
    }
    public static IEnumerator UpSizing(GameObject obj, float duration, Action EventEnd = null)
    {
        float time = 0;
        Vector2 oriSize = obj.transform.localScale;
        Vector2 curSize = oriSize;
        obj.SetActive(true);
        while (time < duration)
        {
            curSize = oriSize * time / duration;
            obj.transform.localScale = new Vector3(curSize.x, curSize.y, 1);
            yield return null;
            time += Time.deltaTime;
        }
        obj.transform.localScale = oriSize;
        EventEnd?.Invoke();
    }
    public static IEnumerator DownSizing(GameObject obj, float duration, Action EventEnd = null)
    {
        float time = 0;
        Vector2 oriSize = obj.transform.localScale;
        Vector2 curSize = oriSize;
        obj.SetActive(true);
        while (time < duration)
        {
            curSize = oriSize * (1 - time / duration);
            obj.transform.localScale = new Vector3(curSize.x, curSize.y, 1);
            yield return null;
            time += Time.deltaTime;
        }

        EventEnd?.Invoke();
        obj.transform.localScale = oriSize;
        obj.SetActive(false);
    }
    public static IEnumerator ReSizing(GameObject obj, float duration, Vector2 targetScale, Action EventEnd = null)
    {
        float time = 0;
        Vector2 oriScale = obj.transform.localScale;
        while (time < duration)
        {
            float rate = time / duration;
            Vector2 nextSize = oriScale * (1 - rate) + targetScale * rate;
            obj.transform.localScale = new Vector3(nextSize.x, nextSize.y, 1);
            time += Time.deltaTime;
            yield return null;
        }
        EventEnd?.Invoke();
    }

}

//c# 확장 메서드 방식
public static class MyExtensions
{
    private static Sprite SkillImageVert = null;
    private static Sprite SkillImageHori = null;
    private static Sprite SkillImageBomb = null;
    private static Sprite SkillImageSame = null;

    private static Sprite LeagueImageBronze = null;
    private static Sprite LeagueImageSilver = null;
    private static Sprite LeagueImageGold = null;
    private static Sprite LeagueImageMaster = null;

    static MyExtensions()
    {
        SkillImageVert = Resources.Load<Sprite>("Images/skillHori");
        SkillImageHori = Resources.Load<Sprite>("Images/skillVert");
        SkillImageBomb = Resources.Load<Sprite>("Images/skillBomb");
        SkillImageSame = Resources.Load<Sprite>("Images/skillSame");

        LeagueImageBronze = Resources.Load<Sprite>("Images/rune_bronze");
        LeagueImageSilver = Resources.Load<Sprite>("Images/rune_silver");
        LeagueImageGold = Resources.Load<Sprite>("Images/rune_gold");
        LeagueImageMaster = Resources.Load<Sprite>("Images/rune_master");
    }

    //transform.position = Vector2()를 하면 z값이 0으로 소실된다.
    //z값 변경없이 편하게 x,y값만 바꿀 수 있도록 하기 위해 구현
    public static void SetPosition2D(this Transform tr, Vector2 val)
    {
        tr.position = new Vector3(val.x, val.y, tr.position.z);
    }
    public static void SetLocalPosition2D(this Transform tr, Vector2 val)
    {
        tr.localPosition = new Vector3(val.x, val.y, tr.localPosition.z);
    }
    public static Sprite GetSprite(this ProductSkill skill)
    {
        switch(skill)
        {
            case ProductSkill.Horizontal: return SkillImageHori;
            case ProductSkill.Vertical: return SkillImageVert;
            case ProductSkill.Bomb: return SkillImageBomb;
            case ProductSkill.SameColor: return SkillImageSame;
            default: return null;
        }
    }
    public static Sprite GetSprite(this MatchingLevel level)
    {
        switch (level)
        {
            case MatchingLevel.Bronze: return LeagueImageBronze;
            case MatchingLevel.Silver: return LeagueImageSilver;
            case MatchingLevel.Gold: return LeagueImageGold;
            case MatchingLevel.Master: return LeagueImageMaster;
            default: return null;
        }
    }
    public static string GetText(this MatchingLevel level)
    {
        switch (level)
        {
            case MatchingLevel.Bronze: return "Bronze";
            case MatchingLevel.Silver: return "Silver";
            case MatchingLevel.Gold: return "Gold";
            case MatchingLevel.Master: return "Master";
            default: return "";
        }
    }
}
