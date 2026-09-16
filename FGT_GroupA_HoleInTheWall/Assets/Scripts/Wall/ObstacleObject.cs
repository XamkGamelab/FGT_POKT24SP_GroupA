using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleObject : MonoBehaviour
{
    [SerializeField] private AudioClip hitAudio = null;

    [SerializeField] private Vector3 moveDir = Vector3.back;


    //  private float deletePos = 0f;

    private float gameSpeed = 1;


    private SpriteRenderer spriteRenderer;
    // Start is called before the first frame update
    void Awake()
    {
        //deletePos = -Camera.main.transform.position * Camera.main.aspect - boundX;

        GameManager.OnUpdateGameSpeed += SetGameSpeed;
    }

    private void SetGameSpeed(float _speed) => gameSpeed = _speed;

    public void Init(float _gameSpeed)
    {
        SetGameSpeed(_gameSpeed);
    }
    // Update is called once per frame
    void Update()
    {
        if (!GameManager.Instance.GameOn)
            return;

        transform.Translate(moveDir * gameSpeed * Time.deltaTime * 2f);

        // if (transform.position.x < deletePos)
        DestroyObject();
    }

    private void DestroyObject()
    {
        ObstacleManager.SpawnNewObstacleEvent.Invoke();
        GameManager.OnUpdateGameSpeed -= SetGameSpeed;
        Destroy(gameObject);
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.TryGetComponent(out Player p))
        {
            p.Die();
            //SoundFXManager.Instance.PlayAudioClip(hitAudio, transform);
        }
            
    }
}
