using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UniRx;
using UnityEngine.SceneManagement;


public class GameManager : MonoBehaviour
{
    [Header("Game")]
    private ReactiveProperty<float> gameSpeed = new();
    [SerializeField] float gameSpeedDecrease = .1f;

    [SerializeField] private float startGameSpeed = 5;
    [SerializeField] private float minGameSpeed = 5;
    [SerializeField] private float wallWaitTime = 1f;
    public float WallWaitTime => wallWaitTime;

    [Header("Player")]
    [SerializeField] private GameObject playerPrefab  = null;
    [SerializeField] private Transform playerHolder  = null;
    private List<Player> players = new List<Player>();

    [Header("Music")]
    [SerializeField] private AudioSource menuMusicStart = null;
    [SerializeField] private AudioSource menuMusicLoop = null;
    [SerializeField] private AudioSource gameMusic = null;


    public static readonly UnityEvent StartGameEvent = new UnityEvent();
    public static readonly UnityEvent InitBodyControllerEvent = new UnityEvent();
    public static readonly UnityEvent EndGameEvent = new UnityEvent();


    public static Action<float> OnUpdateGameSpeed = delegate { };

    //UI events
    public static Action<string> UpdateScoreEvent = delegate { };
    public static readonly UnityEvent OnObstaclePassEvent = new UnityEvent();


    private bool gameOn = false;
    public bool GameOn => gameOn;

    private float gameTime = 0f;

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

            gameSpeed.Value = startGameSpeed;
            Time.timeScale = 1;

            SpawnPlayers();

            //Change this to get Bool and not start if cannot Init
            InitBodyControllerEvent.Invoke();

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

        SceneManager.LoadScene(1, LoadSceneMode.Additive);
    }
    // Start is called before the first frame update
    private void Start()
    {
        StartGameEvent.Invoke();
        //PlayMenuMusic();
    }

    private void Update()
    {
        if (!gameOn)
            return;

        AddToGameTime();
    }

    TimeSpan time;
    void AddToGameTime()
    {
        gameTime += Time.deltaTime;

        time = TimeSpan.FromSeconds(gameTime);

        // Muotoillaan merkkijonoksi: mm (minuutit), ss (sekunnit), fff (millisekunnit)
        string _formattedTime = string.Format("{0:D2}:{1:D2}:{2:D3}",
            time.Minutes,
            time.Seconds,
            time.Milliseconds);

        UIScoreController.ChangeGameTimeUIEvent.Invoke(_formattedTime);
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

        Player _playerInstance = Instantiate(playerPrefab, playerHolder).GetComponent<Player>();

        BodyController.AddPlayerToBodyControllerEvent.Invoke(_playerInstance);
        players.Add(_playerInstance);
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
        gameSpeed.Value = Mathf.Clamp(gameSpeed.Value - gameSpeedDecrease, minGameSpeed, startGameSpeed);
        print(gameSpeed.Value);
    }
}
