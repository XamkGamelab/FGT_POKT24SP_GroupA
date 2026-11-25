using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class PauseBtn : MonoBehaviour
{
    [SerializeField] private bool _shouldPause = true;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake() => GetComponent<Button>().onClick.AddListener(() => GameManager.OnPauseGameEvent.Invoke(_shouldPause));
}
