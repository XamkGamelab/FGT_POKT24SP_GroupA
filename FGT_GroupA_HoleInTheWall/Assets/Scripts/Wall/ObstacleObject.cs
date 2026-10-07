using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleObject : MonoBehaviour
{
    [SerializeField] private AudioClip hitAudio = null;

    private Vector3 startPos = Vector3.zero;
    private Vector3 deletePos = Vector3.zero;

    private float gameSpeed = 1f;

    [SerializeField] private int scoreAmount = 10;

    // Start is called before the first frame update
    void Awake()
    {
        deletePos = Camera.main.transform.position;
        deletePos.y = transform.position.y;
        startPos = transform.position;
    }

    //This is called from ObstacleManager after a set amount of time
    private void SetGameSpeed(float _speed) => gameSpeed = _speed;

    public void Init(float _gameSpeed)
    {
        SetGameSpeed(_gameSpeed);
    }

    public void StartMove()
    {
        if (!GameManager.Instance.GameOn)
            return;

        StartCoroutine(StartMoveLerp(gameSpeed));
    }

    IEnumerator StartMoveLerp(float _dur)
    {
        float _timeElapsed = 0;
        float _percent = 0;
        while(_timeElapsed <= _dur)
        {
            _timeElapsed += Time.deltaTime;
            _percent = _timeElapsed / _dur;
            transform.position = Vector3.Lerp(startPos, deletePos, _percent);
            yield return null;
        }

        transform.position = deletePos;
        DestroyObject();
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
