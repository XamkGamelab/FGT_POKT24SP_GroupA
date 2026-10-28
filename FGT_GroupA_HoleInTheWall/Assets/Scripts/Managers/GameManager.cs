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
    public static readonly Action OnUnpauseGame = delegate { };

    //UI events
    public static readonly UnityEvent OnObstaclePassEvent = new UnityEvent();


    private GameState curGameState = GameState.Menu;
    public GameState CurGameState => curGameState;

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
            curGameState = GameState.Paused;

            SpawnPlayers().ToObservable()
                .SelectMany(BodyController.Instance.HandleStartCalibrationCoroutine())
                .Subscribe(
                onSuccess =>
                {
                    CanvasManager.CloseCanvas.Invoke(CanvasManager.CanvasType.Overlay);
                    StartGame();
                }, onError =>
                {
                    //Return to mainmenu if cannot start
                   // CanvasManager.ShowCanvas.Invoke("MainMenu");
                });
        });

        EndGameEvent.AddListener(() =>
        {
            curGameState = GameState.Menu;
            DeletePlayers();

            //CanvasManager.ShowCanvas.Invoke("AddEntryToLeaderboard");

            //PlayMenuMusic();
        });

        OnObstaclePassEvent.AddListener(CheckForGameSpeed);

        SceneManager.LoadScene(1, LoadSceneMode.Additive);
        SceneManager.LoadScene(2, LoadSceneMode.Additive);
    }
    // Start is called before the first frame update
    private void Start()
    {
        ChekcForStartGameEvent.Invoke();
        //PlayMenuMusic();
    }


    private void StartGame()
    {
        gameSpeed = startGameSpeed;
        Time.timeScale = 1;

        // PlayGameMusic();
        curGameState = GameState.GameOn;
        CanvasManager.ShowGameCanvas.Invoke();
        ObstacleManager.SpawnNewObstacleEvent.Invoke();
    }
    private void Update()
    {
        if (CurGameState != GameState.GameOn)
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

        BodyController.AddPlayerToBodyControllerEvent.Invoke(_playerInstance);
        players.Add(_playerInstance);
    }

}
