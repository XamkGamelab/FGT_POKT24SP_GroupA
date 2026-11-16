using System.Collections;
using UnityEditor.Build.Content;
using UnityEngine;
using UniRx;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System;

public class Player : MonoBehaviour
{

    private ReactiveProperty<int> health = new();
    [SerializeField] private int maxHealth = 4;

    [Header("REFS")]
    [Tooltip("Animator component on X Bot.")]
    private Animator animator;
    public Animator Animator => animator;

    [Tooltip("Root Transform of X Bot. Drag X Bot here.")]
    public Transform modelRoot;

    private ReactiveProperty<int> score = new();

    private ReactiveProperty<int> streak = new();


    private List<Collider> cols = null;
    private SkinnedMeshRenderer rend = null;

    private ParticleSystem passEffect = null;

    private Color ogColor = Color.white;
    private Rigidbody rb = null;

    private bool hitCurWall = false;
    public bool HitCurWall => hitCurWall;
    private bool scoreAddedCurWall = false;
    public bool ScoreAddedCurWall => scoreAddedCurWall;
    private bool isDead = false;
    public static event Action OnPlayerDeath = delegate{};

    //These events are for spawning text or something on action
    public static event Action<Player> OnObstacleHit = delegate{};
    public static event Action<Player> OnObstaclePass = delegate{};

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        modelRoot = transform;

        passEffect = GetComponentInChildren<ParticleSystem>();

        cols = GetComponentsInChildren<Collider>().ToList();
        rb = GetComponent<Rigidbody>();
        rend = GetComponentInChildren<SkinnedMeshRenderer>();

        health.Subscribe(_value =>
        {
            print($"Health: {_value}");
            UIHealthController.ChangeHealthUIEvent.Invoke(_value);
        }).AddTo(this);

        score.Subscribe(_value =>
        {
            print($"Score: {_value}");
            UIScoreController.ChangeScoreUIEvent.Invoke(_value);
        }).AddTo(this);

        streak.Subscribe(_value =>
        {
            print($"streak: {_value}");
            UIScoreController.ChangeStreakUIEvent.Invoke(_value);
        }).AddTo(this);

        OnPlayerDeath += HandleDeath;

        ObstacleManager.SpawnNewObstacleEvent.AddListener(ResetHit);

        ogColor = rend.sharedMaterial.color;

        health.Value = maxHealth;
        score.Value = 0;
        streak.Value = 0;
    }
    private void ResetHit()
    {
        scoreAddedCurWall = false;
        hitCurWall = false;
    }
    public void TakeDmg()
    {
        //OnObstacleHit(this);

        health.Value--;
        health.Value = Math.Clamp(health.Value, 0, maxHealth);
        streak.Value = 0;

        hitCurWall = true;

        if (health.Value <= 0)
            Die();
        else
            StartCoroutine(HandleWallHit());
    }

    void Die()
    {
        OnPlayerDeath();
        //GameManager.EndGameEvent.Invoke();
    }

    void HandleDeath()
    {
        //Enable Ragdoll
        isDead = true;
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.AddExplosionForce(1000, transform.position + new Vector3(UnityEngine.Random.Range(0, 1f), 0, UnityEngine.Random.Range(0, 1f)), UnityEngine.Random.Range(.2f, 1f));
    }

    public void AddScore(int _scoreAmount)
    {
        if (isDead)
            return;

        if (hitCurWall)
            return;

        if (scoreAddedCurWall)
            return;

        score.Value += Mathf.RoundToInt(_scoreAmount * (1 + streak.Value * .1f));
        streak.Value++;

        scoreAddedCurWall = true;

        passEffect.Play();

        
        //OnObstaclePass(this);
    }

    private IEnumerator HandleWallHit()
    {
        cols.ForEach((_col) => _col.enabled = false);
        //QuickFix
       // rb.useGravity = false;
        yield return FlashRed();
        //rb.useGravity = true;
        cols.ForEach((_col) => _col.enabled = true);
    }

    private IEnumerator FlashRed()
    {
        rend.material.color = Color.red;
        print("moi");
        yield return new WaitForSeconds(1f);
        rend.material.color = ogColor;
    }


    private void OnDestroy()
    {
        ObstacleManager.SpawnNewObstacleEvent.RemoveListener(ResetHit);
    }
}
