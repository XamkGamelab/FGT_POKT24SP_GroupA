using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleObject : MonoBehaviour
{
    [SerializeField] private AudioClip hitAudio = null;

    [SerializeField] private Vector3 moveDir = Vector3.back;


    private float deletePos = 0f;

    private float gameSpeed = 1;


    private Rigidbody rb;
    // Start is called before the first frame update
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        deletePos = Camera.main.transform.position.z;

        GameManager.OnUpdateGameSpeed += SetGameSpeed;
    }

    private void SetGameSpeed(float _speed) => gameSpeed = _speed;

    public void Init(float _gameSpeed)
    {
        SetGameSpeed(_gameSpeed);
    }
    // Update is called once per frame
    void FixedUpdate()
    {
        if (!GameManager.Instance.GameOn)
            return;

        rb.MovePosition(transform.position + moveDir * gameSpeed * Time.deltaTime);

        if (transform.position.z < deletePos)
            DestroyObject();
    }

    private void DestroyObject()
    {
        ObstacleManager.SpawnNewObstacleEvent.Invoke();
        GameManager.OnUpdateGameSpeed -= SetGameSpeed;
        Destroy(gameObject);
    }
    private void OnCollisionEnter(Collision collision)
    {
        print("osu");
        if (collision.gameObject.TryGetComponent(out Player p))
        {
            p.TakeDmg();
            print(p.name);
            DestroyObject();
            //SoundFXManager.Instance.PlayAudioClip(hitAudio, transform);
        }
            
    }
}
