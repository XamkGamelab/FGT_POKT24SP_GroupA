using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class ObstacleManager : MonoBehaviour
{
    [SerializeField] private ObstacleObject[] obstacles = null;
    private int gameState = 1;
    private float gameSpeed = 1f;

    [SerializeField] Transform obstacleSpawnPoint = null;
    [SerializeField] Transform obstacleHolder = null;

    ObstacleObject curObstacle = null;
    [SerializeField] int startBgCount = 0;
    public static readonly UnityEvent SpawnNewObstacleEvent = new UnityEvent();
    // Start is called before the first frame update

    private void Awake()
    {
        SpawnNewObstacleEvent.AddListener(() =>
        {
           SpawnObject(obstacles[0], obstacleSpawnPoint.position);
        });

        GameManager.OnUpdateGameSpeed += (_speed) =>
        {
            gameSpeed = _speed;

            if (Mathf.FloorToInt(_speed) < gameState + 1)
                return;

        };

        GameManager.StartGameEvent.AddListener(() =>
        {
            gameSpeed = 1f;
            SpawnObject(obstacles[0], obstacleSpawnPoint.position);
        });

        GameManager.EndGameEvent.AddListener(() =>
        {
            DestroyObstacles();
            gameSpeed = 1f;
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
}
