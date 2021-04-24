using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ClipSound
{
    Match, Drop, Swipe, Merge1, Merge2, Merge3, Skill1, Skill2, Skill3, Star1, Star2, Star3, Coin1, Coin2
}

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
    public AudioSource PlayerBack;

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
    public AudioClip EffectFirework;
    public AudioClip EffectDropIce;
    public AudioClip EffectBreakIce;
    public AudioClip EffectLevelComplete;
    public AudioClip EffectBreakStone;
    public AudioClip EffectCashGold;
    public AudioClip EffectLaunchMissile;
    public AudioClip EffectEndGameReward;
    public AudioClip EffectStartBeam;
    public AudioClip EffectEndBeam;
    public AudioClip EffectLightBomb;

    public AudioClip[] MatchClip;
    public AudioClip[] DropClip;
    public AudioClip[] SwipeClip;
    public AudioClip[] MergeClip;
    public AudioClip[] SkillClip;
    public AudioClip[] StarClip;
    public AudioClip[] CoinClip;

    private Dictionary<AudioClip, int> mRequestedClips = new Dictionary<AudioClip, int>();

    private void Awake()
    {
        Player.volume = UserSetting.VolumeSFX;
        PlayerBack.volume = UserSetting.VolumeBackground;
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

    public void AdjustVolumeSFX(float volume)
    {
        Player.volume = volume;
    }
    public void AdjustVolumeBack(float volume)
    {
        PlayerBack.volume = volume;
    }

    public void PlayBackMusic(AudioClip bkMusic)
    {
        PlayerBack.clip = bkMusic;
        PlayerBack.loop = true;
        PlayerBack.Play();
    }


    public void PlaySoundEffect(AudioClip sound)
    {
        mRequestedClips[sound] = 1;
    }
    public void PlaySoundEffect(ClipSound sound)
    {
        mRequestedClips[GetAudioClip(sound)] = 1;
    }

    public AudioClip GetAudioClip(ClipSound sound)
    {
        AudioClip clip = null;
        switch(sound)
        {
            case ClipSound.Match: clip = MatchClip[Random.Range(0, MatchClip.Length)]; break;
            case ClipSound.Drop: clip = DropClip[Random.Range(0, DropClip.Length)]; break;
            case ClipSound.Swipe: clip = SwipeClip[0]; break;
            case ClipSound.Merge1: clip = MergeClip[0]; break;
            case ClipSound.Merge2: clip = MergeClip[1]; break;
            case ClipSound.Merge3: clip = MergeClip[2]; break;
            case ClipSound.Skill1: clip = SkillClip[0]; break;
            case ClipSound.Skill2: clip = SkillClip[1]; break;
            case ClipSound.Skill3: clip = SkillClip[2]; break;
            case ClipSound.Star1: clip = StarClip[0]; break;
            case ClipSound.Star2: clip = StarClip[1]; break;
            case ClipSound.Star3: clip = StarClip[2]; break;
            case ClipSound.Coin1: clip = CoinClip[0]; break;
            case ClipSound.Coin2: clip = CoinClip[1]; break;
        }
        return clip;
    }
}
