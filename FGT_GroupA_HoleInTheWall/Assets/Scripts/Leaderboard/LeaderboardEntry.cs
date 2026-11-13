using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[Serializable]
public class LeaderboardEntry : MonoBehaviour
{
    public string Email;
    public int Score;

    [SerializeField] TMP_Text EmailText;
    [SerializeField] TMP_Text ScoreText;
    public void SetEntry(string email, int score)
    {
        Email = email;
        Score = score;

        EmailText.text = Email;
        ScoreText.text = Score.ToString();
    }
}
