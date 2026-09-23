using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class ObstacleManager : MonoBehaviour
{
    [SerializeField] private ObstacleObject[] obstacles = null;
    private float gameSpeed = 1f;

    [SerializeField] Transform obstacleSpawnPoint = null;
    [SerializeField] Transform obstacleHolder = null;

    ObstacleObject curObstacle = null;
    public static readonly UnityEvent SpawnNewObstacleEvent = new UnityEvent();
    // Start is called before the first frame update

    private void Awake()
    {
        SpawnNewObstacleEvent.AddListener(() =>
        {
           SpawnObject(obstacles[0], obstacleSpawnPoint.position);

            StartCoroutine(PlayWallMoveAnimations());
        });

        GameManager.OnUpdateGameSpeed += (_speed) =>
        {
            gameSpeed = _speed;
        };

        GameManager.EndGameEvent.AddListener(() =>
        {
            DestroyObstacles();
        });
    }

    private void DestroyObstacles()
    {
        for (int i = obstacleHolder.childCount; i > 0; i--)
            Destroy(obstacleHolder.GetChild(i-1).gameObject);
    }

    private void SpawnObject(ObstacleObject _go, Vector3 _pos)
    {
        curObstacle = Instantiate(_go, _pos, Quaternion.identity, obstacleHolder);
        curObstacle.Init(gameSpeed);
    }

    private IEnumerator PlayWallMoveAnimations()
    {
        //Start animations etc.
        //Get the time of them and wait for that long
        //for now a variable in GmaeManager
        yield return new WaitForSeconds(GameManager.Instance.WallWaitTime);
        curObstacle.StartMove();

    }
}
