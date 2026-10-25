using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class StartGameBtn : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake() => GetComponent<Button>().onClick.AddListener(GameManager.StartGameEvent.Invoke);
}
