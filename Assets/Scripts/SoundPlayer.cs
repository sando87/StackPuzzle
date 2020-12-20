using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundPlayer : MonoBehaviour
{
    static public SoundPlayer Inst = null;
    public AudioSource Player;

    public AudioClip BackMusicMap;
    public AudioClip BackMusicInGame;

    public AudioClip EffectButton1;
    public AudioClip EffectButton2;
    public AudioClip EffectSuccess;
    public AudioClip EffectGameOver;
    public AudioClip EffectAlarm;
    public AudioClip EffectCountDown;
    public AudioClip EffectMatched;
    public AudioClip EffectWrongMatched;
    public AudioClip EffectGoodEffect;
    public AudioClip EffectBadEffect;
    public AudioClip EffectCooltime;

    private void Awake()
    {
        Inst = this;
        Player.mute = UserSetting.Mute;
    }

    private void Start()
    {
        //StartCoroutine(FPSCounter());
    }

    IEnumerator FPSCounter()
    {
        float time = 0;
        int count = 0;
        while(true)
        {
            count++;
            time += Time.deltaTime;
            if(time > 1)
            {
                Debug.Log(count);
                count = 0;
                time = 0;
            }
            yield return null;
        }
    }

    public bool OnOff()
    {
        Player.mute = !Player.mute;
        return Player.mute;
    }

    public void PlayBackMusic(AudioClip bkMusic)
    {
        Player.clip = bkMusic;
        Player.loop = true;
        Player.Play();
    }


    public void PlaySoundEffect(AudioClip sound)
    {
        Player.PlayOneShot(sound);
    }
}
