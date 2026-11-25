using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioMixerManager : SingletonMono<AudioMixerManager>
{
    [SerializeField] private AudioMixer audioMixer;

    public void SetMasterVolume(float _level) => audioMixer.SetFloat("MasterVolume", Mathf.Log10(_level) * 20f);
    public void SetMusicVolume(float _level) => audioMixer.SetFloat("MusicVolume", Mathf.Log10(_level) * 20f);
    public void SetSoundFXVolume(float _level) => audioMixer.SetFloat("SoundFXVolume", Mathf.Log10(_level) * 20f);
}
