using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class UIScoreController : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreTxt = null;
    [SerializeField] private TMP_Text streakTxt = null;
    [SerializeField] private TMP_Text gameTimeTxt = null;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public static UnityEventInt ChangeScoreUIEvent = new UnityEventInt();
    public static UnityEventInt ChangeStreakUIEvent = new UnityEventInt();
    public static UnityEventString ChangeGameTimeUIEvent = new();
    void Awake()
    {
        ChangeScoreUIEvent.AddListener(_value => HandleTxtChange(scoreTxt,_value.ToString()));
        ChangeStreakUIEvent.AddListener(_value => HandleTxtChange(streakTxt,$"{_value.ToString()}x"));
        ChangeGameTimeUIEvent.AddListener(_str => HandleTxtChange(gameTimeTxt,_str));
    }

    private void HandleTxtChange(TMP_Text _text, string _scoreValueStr)
    {
        _text.text = _scoreValueStr;
    }
}
