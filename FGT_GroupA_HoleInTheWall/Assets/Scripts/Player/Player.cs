using UnityEditor.Build.Content;
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] int health;
    // Update is called once per frame
    void Update()
    {
        
    }

    public void Die()
    {
        GameManager.EndGameEvent.Invoke();
    }
}
