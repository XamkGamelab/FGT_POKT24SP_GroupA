using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundFXManager : SingletonMono<SoundFXManager>
{
    [SerializeField] private AudioSource soundFxObj;

    public void PlayAudioClipForButton(AudioClip _clip) => PlayAudioClip(_clip, transform);
    public void PlayAudioClip(AudioClip _clip, Transform _pos, bool _randomizePitch = true)
    {
        AudioSource _as = Instantiate(soundFxObj, _pos.position, Quaternion.identity);

        _as.clip = _clip;

        if(_randomizePitch)
        {
            float randomPitch = Random.Range(0.95f, 1.05f);
            _as.pitch = randomPitch;
        }

        _as.Play();

        float _clipLength = _as.clip.length;

        Destroy(_as.gameObject, _clipLength);
    }

    public void PlayRandomAudioClip(AudioClip[] _clips, Transform _pos)
    {
        int _rnd = Random.Range(0, _clips.Length);

        AudioSource _as = Instantiate(soundFxObj, _pos.position, Quaternion.identity);

        _as.clip = _clips[_rnd];

        _as.Play();

        float _clipLength = _as.clip.length;

        Destroy(_as.gameObject, _clipLength);
    }
}
