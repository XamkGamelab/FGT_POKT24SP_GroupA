using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class ObstacleManager : MonoBehaviour
{
    [SerializeField] private ObstacleObject[] backgrounds = null;
    private int gameState = 1;
    private float gameSpeed = 1f;

    [SerializeField] Transform obstacleSpawnPoint = null;
    [SerializeField] Transform obstacleHolder = null;

    [SerializeField] int startBgCount = 0;
    public static readonly UnityEvent SpawnNewBgEvent = new UnityEvent();
    // Start is called before the first frame update

    private void Awake()
    {
        SpawnNewBgEvent.AddListener(() =>
        {
            int _state = gameState >= backgrounds.Length ? backgrounds.Length : gameState;

           //SpawnObject(backgrounds[_state - 1], obstacleSpawnPoint);
        });

        GameManager.OnUpdateGameSpeed += (_speed) =>
        {
            gameSpeed = _speed;

            if (Mathf.FloorToInt(_speed) < gameState + 1)
                return;

            gameState = Mathf.FloorToInt(_speed);

            int _state = gameState >= backgrounds.Length ? backgrounds.Length : gameState;

            //SpawnObject(backgrounds[_state - 1], lastBG.SpawnPoint, true);
        };

        GameManager.StartGameEvent.AddListener(() =>
        {
            gameSpeed = 1f;
            gameState = 1;
        });

        GameManager.EndGameEvent.AddListener(() =>
        {
            DestroyBgs();
            gameState = 1;
            gameSpeed = 1f;
        });
    }

    private void DestroyBgs()
    {
        for (int i = obstacleHolder.childCount; i > 0; i--)
            Destroy(obstacleHolder.GetChild(i-1).gameObject);
    }

    private void SpawnObject(ObstacleObject _go, Vector3 _pos, bool _isOnStart = false)
    {
       // lastBG = Instantiate(_go, _pos, Quaternion.identity, obstacleHolder);
        //lastBG.Init(_isOnStart, gameSpeed);
    }
}
