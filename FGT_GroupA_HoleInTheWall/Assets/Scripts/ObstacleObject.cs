using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleObject : MonoBehaviour
{
    [SerializeField] private AudioClip hitAudio = null;
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.TryGetComponent(out Player p))
        {
            p.Die();
            SoundFXManager.Instance.PlayAudioClip(hitAudio, transform);
        }
            
    }
}
