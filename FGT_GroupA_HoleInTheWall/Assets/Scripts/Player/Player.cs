using System.Collections;
using UnityEditor.Build.Content;
using UnityEngine;
using UniRx;

public class Player : MonoBehaviour
{
    private ReactiveProperty<int> health = new();
    [SerializeField] private int maxHealth = 3;

    private ReactiveProperty<int> score = new();

    private ReactiveProperty<int> streak = new();


    private Collider col = null;
    private MeshRenderer rend = null;


    private Color ogColor = Color.white;

    private void Awake()
    {
        health.Value = maxHealth;
        score.Value = 0;
        streak.Value = 0;

        col = GetComponent<Collider>();
        rend = GetComponent<MeshRenderer>();

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

        ogColor = rend.sharedMaterial.color;
    }

    public void TakeDmg()
    {
        health.Value--;
        streak.Value = 0;
        StartCoroutine(HandleWallHit());

        if(health.Value <= 0)
            GameManager.EndGameEvent.Invoke();
    }

    public void AddScore(int _scoreAmount)
    {
        score.Value += Mathf.RoundToInt(_scoreAmount * (1 + streak.Value * .1f));
        streak.Value++;
    }

    private IEnumerator HandleWallHit()
    {
        col.enabled = false;
        yield return FlashRed();
        col.enabled = true;
    }

    private IEnumerator FlashRed()
    {
        rend.material.color = Color.red;
        print("moi");
        yield return new WaitForSeconds(1f);
        rend.material.color = ogColor;
    }
}
