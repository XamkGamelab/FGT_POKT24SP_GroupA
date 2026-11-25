using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class ObstacleManager : MonoBehaviour
{
    public enum TurnType
    {
        Wall,
        Pose
    }
    [SerializeField] private ObstacleObject[] obstacles = null;

    [SerializeField] Transform obstacleSpawnPoint = null;
    [SerializeField] Transform obstacleHolder = null;

    ObstacleObject curObstacle = null;
    public static readonly UnityEvent NextTurnEvent = new UnityEvent();
    // Start is called before the first frame update

    private void Awake()
    {
        NextTurnEvent.AddListener(() =>
        {
            //if(GetTurnType() == TurnType.Pose)
            //{

            //}
            //else
            //{
                SpawnObject(obstacles[0], obstacleSpawnPoint.position);

                StartCoroutine(PlayWallMoveAnimations());
            //}
        });

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

    private TurnType GetTurnType()
    {
        return UnityEngine.Random.Range(0, 11) < 3 ? TurnType.Pose : TurnType.Wall;
    }

    private void SpawnObject(ObstacleObject _go, Vector3 _pos)
    {
        curObstacle = Instantiate(_go, _pos, Quaternion.identity, obstacleHolder);
    }

    private IEnumerator PlayWallMoveAnimations()
    {
        //Start animations etc.
        //Get the time of them and wait for that long
        //for now a variable in GmaeManager

        yield return new WaitUntil(() => GameManager.Instance.CurGameState == GameManager.GameState.GameOn);
        yield return new WaitForSeconds(GameManager.Instance.WallWaitTime);

        curObstacle.StartMove(GameManager.Instance.GameSpeed);

    }
}
