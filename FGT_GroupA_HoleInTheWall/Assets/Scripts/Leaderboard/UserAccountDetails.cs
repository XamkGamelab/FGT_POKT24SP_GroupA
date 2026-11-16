using Firebase.Auth;
using Firebase.Database;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.Events;

public class UserAccountDetails : SingletonMono<UserAccountDetails>
{
    FirebaseAuth auth;
    FirebaseUser user;
    public FirebaseUser User => user;

    private bool userScoreUpdated = false;
    private bool signedIn = false;
    public bool SignedIn => signedIn;


    private float totalInAppTime = 0;

    UserDetails userDetails;
    public UserDetails UserDetails => userDetails;

    public static UnityEventString getUserDetailsEvent = new UnityEventString();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        totalInAppTime = 0;
        InitializeFirebase();

        getUserDetailsEvent.AddListener(_uid => GetUserDetails(_uid));
    }

    private void Update()
    {
        if (!signedIn)
            return;

        totalInAppTime += Time.deltaTime;
    }

    void InitializeFirebase()
    {
        auth = FirebaseAuth.DefaultInstance;
        auth.StateChanged += AuthStateChanged;
    }

    void AuthStateChanged(object sender, System.EventArgs eventArgs)
    {
        if (auth.CurrentUser != user)
        {
            signedIn = user != auth.CurrentUser && auth.CurrentUser != null
                && auth.CurrentUser.IsValid();
            if (!signedIn && user != null)
            {
                Debug.Log("Signed out " + user.UserId);
            }
            user = auth.CurrentUser;
            if (signedIn)
            {
                Debug.Log("Signed in " + user.UserId);
            }
        }
    }

    public void ChangeUserDetails(float _playTime, int score)
    {
        userDetails.playTime += _playTime;

        if(score > userDetails.highScore)
        {
            userDetails.highScore = score;
            userScoreUpdated = true;
        }   
    }
    private void GetUserDetails(string _uid)
    {
        FirebaseTasks.Instance.ReadUserDetails(_uid).ToObservable().ObserveOnMainThread().Subscribe(
        onSuccess =>
        {
            userDetails = onSuccess;
            userScoreUpdated = false;
            print($"GET: {userDetails.userName} : {userDetails.playTime} : {userDetails.highScore}");
        },
        onError =>
        {
            Debug.LogException(onError);
        });
    }

    public void SaveUserDetails()
    {
        userDetails.totalInAppTime += totalInAppTime;
        LeaderboardManager.updateLeaderboardsEvent.Invoke();
        FirebaseTasks.Instance.WriteUserDetails(user.UserId, userDetails, userScoreUpdated).ToObservable().ObserveOnMainThread().Subscribe(
        onSuccess =>
        {
            Debug.Log($"Saved");
            auth.SignOut();
        },
        onError =>
        {
            Debug.LogException(onError);
        });
    }
    protected override void OnDestroy()
    {
        SaveUserDetails();
        auth.StateChanged -= AuthStateChanged;
        auth = null;

        base.OnDestroy();
    }
}

public class UserDetails
{
    public string userName;
    public float playTime;
    public float totalInAppTime;
    public int highScore;

    public UserDetails(string userName, float playTime, float totalInAppTime, int highScore)
    {
        this.userName = userName;
        this.playTime = playTime;
        this.totalInAppTime = totalInAppTime;
        this.highScore = highScore;
    }
}
