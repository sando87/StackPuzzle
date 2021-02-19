using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundPlayer : MonoBehaviour
{
    static private SoundPlayer mInst = null;
    static public SoundPlayer Inst
    {
        get
        {
            if(mInst == null)
                mInst = GameObject.Find("SoundPlayer").GetComponent<SoundPlayer>();
            return mInst;
        }
    }
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

    private Dictionary<AudioClip, int> mRequestedClips = new Dictionary<AudioClip, int>();

    private void Awake()
    {
        Player.mute = UserSetting.Mute;
    }

    private void Update()
    {
        if(mRequestedClips.Count > 0)
        {
            foreach(var item in mRequestedClips)
            {
                AudioClip clip = item.Key;
                Player.PlayOneShot(clip);
            }
            mRequestedClips.Clear();
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
        mRequestedClips[sound] = 1;
    }
}
