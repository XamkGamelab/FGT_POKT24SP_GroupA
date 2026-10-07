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
    [SerializeField] private int maxHealth = 3;

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


    private Color ogColor = Color.white;
    private Rigidbody rb = null;

    public static Action OnPlayerDeath = delegate{};

    private void Awake()
    {
        health.Value = maxHealth;
        score.Value = 0;
        streak.Value = 0;

        animator = GetComponentInChildren<Animator>();
        modelRoot = transform;

        cols = GetComponentsInChildren<Collider>().ToList();
        rb = GetComponent<Rigidbody>();
        rend = GetComponentInChildren<SkinnedMeshRenderer>();

        health.Subscribe(_value =>
        {
            print($"Health: {_value}");
        }).AddTo(this);

        score.Subscribe(_value =>
        {
            print($"Score: {_value}");
        }).AddTo(this);

        streak.Subscribe(_value =>
        {
            print($"Streak: {_value}");
        }).AddTo(this);

        OnPlayerDeath += HandleDeath;

        ogColor = rend.sharedMaterial.color;
    }

    public void TakeDmg()
    {
        health.Value--;
        streak.Value = 0;


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
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.AddExplosionForce(1000, transform.position + new Vector3(UnityEngine.Random.Range(0, 1f), 0, UnityEngine.Random.Range(0, 1f)), UnityEngine.Random.Range(.2f, 1f));
    }

    public void AddScore(int _scoreAmount)
    {
        score.Value += Mathf.RoundToInt(_scoreAmount * (1 + streak.Value * .1f));
        streak.Value++;
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
}
