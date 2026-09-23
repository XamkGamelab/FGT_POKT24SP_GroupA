using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleObject : MonoBehaviour
{
    [SerializeField] private AudioClip hitAudio = null;

    [SerializeField] private Vector3 moveDir = Vector3.back;
    [SerializeField] private float initVelocity = 1f;

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

    //This is called from ObstacleManager after a set amount of time
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

        rb.AddForce(CalculateVel(moveDir * initVelocity * gameSpeed), ForceMode.VelocityChange);

        if (transform.position.z < deletePos)
            DestroyObject();
    }

    private Vector3 CalculateVel(Vector3 _wantedVel)
    {
        Vector3 _curVel = rb.linearVelocity;
        float _mag = _curVel.magnitude;
        _curVel.y = 0;

        _curVel = _curVel.normalized * _mag;

        return _wantedVel - _curVel;
    }

    private void DestroyObject()
    {
        //tells the game that the wall has passed
        GameManager.OnObstaclePassEvent.Invoke();

        //Tells ObstacleManager that it can spawn a new obstacle
        ObstacleManager.SpawnNewObstacleEvent.Invoke();

        //UnSubcribe from OnUpdateGameSpeed
        GameManager.OnUpdateGameSpeed -= SetGameSpeed;
        Destroy(gameObject);
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.TryGetComponent(out Player p))
        {
            p.TakeDmg();
            //SoundFXManager.Instance.PlayAudioClip(hitAudio, transform);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.TryGetComponent(out Player p))
            p.AddScore(scoreAmount);
    }
}
