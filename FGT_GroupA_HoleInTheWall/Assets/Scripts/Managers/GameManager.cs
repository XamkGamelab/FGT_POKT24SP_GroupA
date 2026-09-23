using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UniRx;


public class GameManager : MonoBehaviour
{
    [Header("Game")]
    private ReactiveProperty<float> gameSpeed = new();
    [SerializeField] float gameSpeedIncrease = .1f;

    [SerializeField] private int maxGameSpeed = 5;
    [SerializeField] private float wallWaitTime = 1f;
    public float WallWaitTime => wallWaitTime;

    [Header("Player")]
    [SerializeField] private GameObject playerPrefab  = null;
    [SerializeField] private Transform playerHolder  = null;
    private HashSet<Player> players = new HashSet<Player>();

    [Header("Music")]
    [SerializeField] private AudioSource menuMusicStart = null;
    [SerializeField] private AudioSource menuMusicLoop = null;
    [SerializeField] private AudioSource gameMusic = null;


    public static readonly UnityEvent StartGameEvent = new UnityEvent();
    public static readonly UnityEvent EndGameEvent = new UnityEvent();


    public static Action<float> OnUpdateGameSpeed = delegate { };

    //UI events
    public static Action<string> UpdateScoreEvent = delegate { };
    public static readonly UnityEvent OnObstaclePassEvent = new UnityEvent();


    private bool gameOn = false;
    public bool GameOn => gameOn;

    public static GameManager Instance = null;


    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(this);

        gameSpeed.Subscribe(OnUpdateGameSpeed).AddTo(this);

        StartGameEvent.AddListener(() =>
        {
            gameOn = true;

            gameSpeed.Value = 1;
            Time.timeScale = 1;

            SpawnPlayers();
           // PlayGameMusic();
            CanvasManager.ShowGameCanvas.Invoke();
            ObstacleManager.SpawnNewObstacleEvent.Invoke();
        });

        EndGameEvent.AddListener(() =>
        {
            gameOn = false;
            DeletePlayers();

            CanvasManager.ShowCanvas.Invoke("MainMenu");

            //PlayMenuMusic();
        });

        OnObstaclePassEvent.AddListener(CheckForGameSpeed);
    }
    // Start is called before the first frame update
    private void Start()
    {
        StartGameEvent.Invoke();
        //PlayMenuMusic();
    }

    private void PlayMenuMusic()
    {
        gameMusic.Stop();
        menuMusicStart.Play();
        menuMusicLoop.PlayDelayed(menuMusicStart.clip.length);
    }

    private void PlayGameMusic()
    {
        menuMusicStart.Stop();
        menuMusicLoop.Stop();
        gameMusic.Play();

        print(menuMusicLoop.isPlaying);
    }

    private void SpawnPlayers()
    {
        if (playerPrefab == null)
            return;

       players.Add(Instantiate(playerPrefab, playerHolder).GetComponent<Player>());
    }

    private void DeletePlayers()
    {
        if (players.Count <= 0)
            return;

        foreach (Player item in players)
        {
            Destroy(item.gameObject);
        }
        players.Clear();
    }

    private void CheckForGameSpeed()
    {
        gameSpeed.Value = Mathf.Clamp(gameSpeed.Value + gameSpeedIncrease, 1, maxGameSpeed);
        print(gameSpeed.Value);
    }
}
