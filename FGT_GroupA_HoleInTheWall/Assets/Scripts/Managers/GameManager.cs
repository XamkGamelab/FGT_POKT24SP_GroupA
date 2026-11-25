using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UniRx;
using UnityEngine.SceneManagement;
using Mediapipe.Unity.Sample.UI;


public class GameManager : MonoBehaviour
{
    public enum GameState
    {
        Menu,
        GameOn,
        Paused
    }

    [Header("Game")]
    private float gameSpeed = 0f;
    public float GameSpeed => gameSpeed;

    [SerializeField] float gameSpeedDecrease = .1f;

    [SerializeField] private float startGameSpeed = 5f;
    [SerializeField] private float minGameSpeed = 5f;
    [SerializeField] private float wallWaitTime = 1f;
    [SerializeField] private float pauseToGameCountdown = 3f;

    private bool pauseCountdownActive;
    private Coroutine pauseCountdownCoroutine;
    private GameState previousGameState = GameState.Menu;
    public float WallWaitTime => wallWaitTime;

    [Header("Player")]
    [SerializeField] private GameObject playerPrefab  = null;
    [SerializeField] private Transform playerHolder  = null;
    private List<Player> players = new List<Player>();

    [Header("Music")]
    [SerializeField] private AudioSource menuMusicStart = null;
    [SerializeField] private AudioSource menuMusicLoop = null;
    [SerializeField] private AudioSource gameMusic = null;


    public static readonly UnityEvent ChekcForStartGameEvent = new UnityEvent();
    public static Action OnInitBodyController = delegate { };

    public static readonly UnityEvent EndGameEvent = new UnityEvent();
    //public static readonly Action OnUnpauseGame = delegate { };

    public static readonly UnityEvent OnObstaclePassEvent = new UnityEvent();


    private ReactiveProperty<GameState> curGameState = new ReactiveProperty<GameState>(GameState.Menu);
    public GameState CurGameState => curGameState.Value;

    private float gameTime = 0f;

    public static GameManager Instance = null;


    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(this);

        ChekcForStartGameEvent.AddListener(() =>
        {
            curGameState.Value = GameState.Paused;

            SpawnPlayers().ToObservable()
                .SelectMany(PoseRouter.Instance.HandleBodyCalibration())
                .Subscribe(
                onSuccess =>
                {
                    CanvasManager.CloseCanvas.Invoke(CanvasManager.CanvasType.Overlay);
                    StartGame();
                }, onError =>
                {
                    //Return to mainmenu if cannot start
                    EndGameEvent.Invoke();
                    CanvasManager.ShowCanvas.Invoke("MainMenu");
                });
        });

        EndGameEvent.AddListener(() =>
        {
            curGameState.Value = GameState.Menu;
            DeletePlayers();

            //CanvasManager.ShowCanvas.Invoke("AddEntryToLeaderboard");

            //PlayMenuMusic();
        });

        curGameState.Subscribe(state =>
        {
            if (previousGameState == GameState.Paused && state == GameState.GameOn)
            {
                if (pauseCountdownCoroutine != null)
                    StopCoroutine(pauseCountdownCoroutine);

                pauseCountdownCoroutine = StartCoroutine(StartFromPauseTimer());
            }

            previousGameState = state;
        });

        OnObstaclePassEvent.AddListener(CheckForGameSpeed);

        SceneManager.LoadScene(1, LoadSceneMode.Additive);
        //SceneManager.LoadScene(2, LoadSceneMode.Additive);
        SceneManager.LoadScene(3, LoadSceneMode.Additive);
    }
    // Start is called before the first frame update
    private void Start()
    {
        ChekcForStartGameEvent.Invoke();
        //PlayMenuMusic();
    }

    private IEnumerator StartFromPauseTimer()
    {
        pauseCountdownActive = true;
        float remainingTime = Mathf.Max(0f, pauseToGameCountdown);

        while (remainingTime > 0f)
        {
            //UIScoreController.ChangeGameTimeUIEvent.Invoke($"Starting in {Mathf.CeilToInt(remainingTime)}");
            remainingTime -= Time.deltaTime;
            yield return null;
        }

        pauseCountdownActive = false;
        pauseCountdownCoroutine = null;
       // OnUnpauseGame.Invoke();
    }

    private void StartGame()
    {
        gameSpeed = startGameSpeed;

        // PlayGameMusic();
        curGameState.Value = GameState.GameOn;
        CanvasManager.ShowGameCanvas.Invoke();
        ObstacleManager.SpawnNewObstacleEvent.Invoke();
    }
    private void Update()
    {
        if (CurGameState != GameState.GameOn || pauseCountdownActive)
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
        gameSpeed = Mathf.Clamp(gameSpeed - gameSpeedDecrease, minGameSpeed, startGameSpeed);
    }

    private IEnumerator SpawnPlayers()
    {
        if (playerPrefab == null)
            yield return null;

        print("Spawniing");
        Player _playerInstance = Instantiate(playerPrefab, playerHolder).GetComponent<Player>();

        PoseRouter.AddPlayerToBodyControllerEvent.Invoke(_playerInstance);
        players.Add(_playerInstance);
    }

}
