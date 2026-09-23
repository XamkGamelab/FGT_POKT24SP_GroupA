using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleObject : MonoBehaviour
{
    [SerializeField] private AudioClip hitAudio = null;

    [SerializeField] private Vector3 moveDir = Vector3.back;


    private float deletePos = 0f;

    private float gameSpeed = 1;

    [SerializeField] private int scoreAmount = 10;

    private bool canMove =false;

    private Rigidbody rb;
    // Start is called before the first frame update
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        deletePos = Camera.main.transform.position.z;
    }

    private void SetGameSpeed(float _speed) => gameSpeed = _speed;

    public void Init(float _gameSpeed)
    {
        SetGameSpeed(_gameSpeed);
        canMove = false;
    }

    public void StartMove() => canMove = true;
    // Update is called once per frame
    void FixedUpdate()
    {
        if (!GameManager.Instance.GameOn)
            return;
        if (!canMove)
            return;

        rb.MovePosition(transform.position + moveDir * gameSpeed * Time.deltaTime);

        if (transform.position.z < deletePos)
            DestroyObject();
    }

    private void DestroyObject()
    {
        GameManager.OnObstaclePassEvent.Invoke();
        ObstacleManager.SpawnNewObstacleEvent.Invoke();

        GameManager.OnUpdateGameSpeed -= SetGameSpeed;
        Destroy(gameObject);
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.TryGetComponent(out Player p))
        {
            p.TakeDmg();
            DestroyObject();
            //SoundFXManager.Instance.PlayAudioClip(hitAudio, transform);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.TryGetComponent(out Player p))
            p.AddScore(scoreAmount);
    }
}
