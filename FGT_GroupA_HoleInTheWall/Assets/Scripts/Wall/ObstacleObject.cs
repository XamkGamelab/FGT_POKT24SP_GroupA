using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleObject : MonoBehaviour
{
    [SerializeField] private AudioClip hitAudio = null;

    private Vector3 startPos = Vector3.zero;
    private Vector3 deletePos = Vector3.zero;

    [SerializeField] private int scoreAmount = 10;

    // Start is called before the first frame update
    void Awake()
    {
        deletePos = Camera.main.transform.position;
        deletePos.y = transform.position.y;
        deletePos.x = transform.position.x;
        startPos = transform.position;
    }

    public void StartMove(float _dur)
    {
        if (GameManager.Instance.CurGameState != GameManager.GameState.GameOn)
            return;

        StartCoroutine(StartMoveLerp(_dur));
    }

    IEnumerator StartMoveLerp(float _dur)
    {
        float _timeElapsed = 0;
        float _percent = 0;
        while(_timeElapsed <= _dur)
        {

            if (GameManager.Instance.CurGameState != GameManager.GameState.GameOn)
                yield return null;

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
        Destroy(gameObject);
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.GetComponentInParent<Player>() != null)
        {
            Player _p = collision.gameObject.GetComponentInParent<Player>();
            _p.TakeDmg();
            //SoundFXManager.Instance.PlayAudioClip(hitAudio, transform);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.GetComponentInParent<Player>() != null)
        {
            Player _p = other.gameObject.GetComponentInParent<Player>();
            _p.AddScore(scoreAmount);
            //SoundFXManager.Instance.PlayAudioClip(hitAudio, transform);
        }
        //if (other.gameObject.TryGetComponent(out Player p))
        //    p.AddScore(scoreAmount);
    }
}
