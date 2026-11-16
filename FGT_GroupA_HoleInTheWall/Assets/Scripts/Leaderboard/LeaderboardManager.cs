using Firebase.Database;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UI;

public class LeaderboardManager : SingletonMono<LeaderboardManager>
{
    private List<LeaderboardEntry> leaderboardEntries = new List<LeaderboardEntry>();

    [SerializeField] private int maxScores = 10;
    [SerializeField] private LeaderboardEntry leaderBoardEntry = null;
    [SerializeField] private Transform leaderBoardContainer = null;

    public static UnityEvent updateLeaderboardsEvent = new UnityEvent();
    private void Awake()
    {
        updateLeaderboardsEvent.AddListener(() =>
        {
            CleanEntries();
            CreateEntries();
        });
    }

    private void CleanEntries()
    {
        foreach(LeaderboardEntry entry in leaderboardEntries)
        {
            Destroy(entry.gameObject);
        }

        leaderboardEntries.Clear();
    }
    private void CreateEntries()
    {
        //Handle DataSnapshot


        FirebaseTasks.Instance.AddScoreToLeaders(
            UserAccountDetails.Instance.User.Email,
            UserAccountDetails.Instance.UserDetails.highScore,
            maxScores).
            ToObservable().ObserveOnMainThread().Subscribe(
            onSuccess =>
            {
                if (onSuccess == null)
                {
                    Debug.LogWarning("Something went wrong");
                    return;
                }
                    

                DataSnapshot leaderboardData = onSuccess;

                string JSON = "{\"leaderboardList\":"+leaderboardData.GetRawJsonValue()+"}";
                print(JSON);
                Leaderboard parseData = JsonUtility.FromJson<Leaderboard>(JSON);

                print(parseData.leaderboardList.Count);
                parseData.leaderboardList.ForEach(_listEntry =>
                {
                    LeaderboardEntry entry = Instantiate(leaderBoardEntry, leaderBoardContainer);
                    entry.SetEntry(_listEntry.email, _listEntry.score);

                    leaderboardEntries.Add(entry);
                });
            });
    }

    public class Leaderboard
    {
        public List<LeaderboardEntryJSON> leaderboardList;
    }

    [Serializable]
    public class LeaderboardEntryJSON
    {
        public string email;
        public int score;
        public LeaderboardEntryJSON(string email, int score)
        {
            this.email = email;
            this.score = score;
        }
    }
}
