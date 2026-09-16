using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    [Header("Game")]
    [SerializeField] private float gameSpeed = 1;
    public float GameSpeed => gameSpeed;

    private float lastGameSpeedUpdate = 0;
    [SerializeField] private int scoreUpdateTreshold = 100;
    [SerializeField] private int maxGameSpeed = 5;

    [SerializeField] private int scoreMultiplier = 10;

    [SerializeField] private int gravitySwitchScore = 10;


    private float score = 0;
    private int lastUpdatedScore = 0;


    private int highscore = 0;
    private bool highscoreUpdated = false;

    [Header("Player")]
    [SerializeField] private GameObject playerPrefab  = null;
    [SerializeField] private Transform playerHolder  = null;
    private GameObject curPlayer = null;

    [Header("Music")]
    [SerializeField] private AudioSource menuMusicStart = null;
    [SerializeField] private AudioSource menuMusicLoop = null;
    [SerializeField] private AudioSource gameMusic = null;


    public static readonly UnityEvent StartGameEvent = new UnityEvent();
    public static readonly UnityEvent EndGameEvent = new UnityEvent();


    public static Action<float> OnUpdateGameSpeed = delegate { };

    public static readonly UnityEvent GravitySwitchEvent = new UnityEvent();

    //UI events
    public static Action<string> UpdateScoreEvent = delegate { };
    public static readonly UnityEventString UpdateHighscoreEvent = new UnityEventString();


    private bool gameOn = false;
    public bool GameOn => gameOn;

    public static GameManager Instance = null;


    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(this);

        //load highscore from playerprefs
        highscore = PlayerPrefs.GetInt("highscore");

        StartGameEvent.AddListener(() =>
        {
            gameOn = true;

            score = 0;
            lastUpdatedScore = 0;
            gameSpeed = 1;
            lastGameSpeedUpdate = 0;
            Time.timeScale = 1;


            UpdateScoreEvent(lastUpdatedScore.ToString());
            SpawnPlayer();
            PlayGameMusic();
            CanvasManager.ShowGameCanvas.Invoke();
        });

        EndGameEvent.AddListener(() =>
        {
            gameOn = false;
            DeletePlayer();
            CheckForHighScore();

            if (highscoreUpdated)
                PlayerPrefs.SetInt("highscore", highscore);

            UpdateHighscoreEvent.Invoke(highscore.ToString());

            CanvasManager.ShowCanvas.Invoke("MainMenu");

            PlayMenuMusic();
        });

        GravitySwitchEvent.AddListener(() =>
        {
            if (!gameOn)
                return;

            score += gravitySwitchScore;
        });
    }
    // Start is called before the first frame update
    private void Start()
    {
        highscore = PlayerPrefs.GetInt("highscore", highscore);

        print(highscore);
        UpdateHighscoreEvent.Invoke(highscore.ToString());
        PlayMenuMusic();
    }

    // Update is called once per frame
    void Update()
    {
        if(!gameOn)
            return;

        AddScore();
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

    private void SpawnPlayer()
    {
        if (curPlayer != null)
            return;

       curPlayer = Instantiate(playerPrefab, playerHolder);
    }

    private void DeletePlayer()
    {
        if (curPlayer == null)
            return;

        Destroy(curPlayer);
    }

    private void AddScore() 
    {
        score += Time.deltaTime * gameSpeed * scoreMultiplier;

        //Check if score is move on to the next int
        if (Mathf.FloorToInt(score) < lastUpdatedScore+1)
            return;

        lastUpdatedScore = Mathf.FloorToInt(score);
        CheckForGameSpeed();
        
        UpdateScoreEvent(lastUpdatedScore.ToString());
    }

    private void CheckForGameSpeed()
    {
        if(gameSpeed > (float)maxGameSpeed)
        {
            gameSpeed = (float)maxGameSpeed;
            return;
        }

        if (score < lastGameSpeedUpdate + (float)scoreUpdateTreshold * gameSpeed)
            return;

        lastGameSpeedUpdate += (float)scoreUpdateTreshold * gameSpeed;
        gameSpeed += .1f;

        OnUpdateGameSpeed(gameSpeed);
    }

    private void CheckForHighScore()
    {
        if (score < highscore)
            return;

        highscoreUpdated = true;
        //Update highscore if score is higher

        highscore = Mathf.FloorToInt(score);
    }
}
