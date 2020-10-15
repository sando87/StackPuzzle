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
    public AudioClip EffectMatched;

    private void Awake()
    {
        Inst = this;
        Player.mute = UserSetting.Mute;
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
