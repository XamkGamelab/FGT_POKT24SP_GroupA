using UnityEditor.Build.Content;
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] int health;


    public void TakeDmg()
    {
        health--;

        print(health);
        if(health <= 0)
            GameManager.EndGameEvent.Invoke();
    }
}
