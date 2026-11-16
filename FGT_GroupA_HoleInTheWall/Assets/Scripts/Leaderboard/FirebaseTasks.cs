using Firebase;
using Firebase.Auth;
using Firebase.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using UnityEngine.SocialPlatforms;

public class FirebaseTasks : Singleton<FirebaseTasks>
{
    async public Task<AuthResult> SignUp(string _email, string _password)
    {
        Task<AuthResult> _createNewUserTask = FirebaseAuth.DefaultInstance.CreateUserWithEmailAndPasswordAsync(_email, _password);
        await _createNewUserTask;
        return _createNewUserTask.Result;
    }
    
    async public Task<AuthResult> SignIn(string _email, string _password)
    {
        Task<AuthResult> _createNewUserTask = FirebaseAuth.DefaultInstance.SignInWithEmailAndPasswordAsync(_email, _password);
        await _createNewUserTask;
        return _createNewUserTask.Result;
    }
    public void SignOut()
    {
        FirebaseAuth.DefaultInstance?.SignOut();
    }

    async public Task WriteUserDetails(string _uid, UserDetails _userDetails, bool _updateScore)
    {
        Task<UserDetails> readUserDetails = ReadUserDetails(_uid);
        await readUserDetails;

        try
        {

            if(readUserDetails.Result == null)
            {
                await FirebaseDatabase.DefaultInstance.RootReference.Child("users").Child(_uid).SetRawJsonValueAsync(JsonUtility.ToJson(_userDetails));
                Debug.Log("Saved a new save");
            }
            else
            {
                await FirebaseDatabase.DefaultInstance.RootReference.Child("users").Child(_uid).Child("playTime").SetValueAsync(_userDetails.playTime);

                await FirebaseDatabase.DefaultInstance.RootReference.Child("users").Child(_uid).Child("totalInAppTime").SetValueAsync(_userDetails.totalInAppTime);
                
                if(_updateScore)
                    await FirebaseDatabase.DefaultInstance.RootReference.Child("users").Child(_uid).Child("highScore").SetValueAsync(_userDetails.highScore);

                Debug.Log("Saved to an existing one");
            }

        }
        catch (AggregateException ae)
        {
            foreach (var ex in ae.InnerExceptions)
            {
                Debug.LogWarning(ex.Message);
            }
        }
    }

    async public Task<UserDetails> ReadUserDetails(string _uid)
    {
        Task<DataSnapshot> task = FirebaseDatabase.DefaultInstance.GetReference($"users/{_uid}/").GetValueAsync();
        await task;

        try
        {
            DataSnapshot snapshot = task.Result;

            if (!snapshot.Exists)
            {
                await FirebaseDatabase.DefaultInstance.RootReference.Child("users").Child(_uid).SetRawJsonValueAsync(JsonUtility.ToJson(new UserDetails(_uid, 0, 0, 0)));
                return new UserDetails(_uid, 0, 0, 0);
            }


            return JsonUtility.FromJson<UserDetails>(snapshot.GetRawJsonValue());

        }
        catch (AggregateException ae)
        {
            foreach (var ex in ae.InnerExceptions)
            {
                Debug.LogWarning(ex.Message);
            }
        }

        return new UserDetails(_uid, 0, 0, 0);
    }
    async public Task<DataSnapshot> AddScoreToLeaders(string email, long score, int maxScores)
    {
        Task<DataSnapshot> updateLeaderboards = FirebaseDatabase.DefaultInstance.GetReference("leaderboards").RunTransaction(mutableData => {
            List<object> leaders = mutableData.Value as List<object>;

            if (leaders == null)
            {
                leaders = new List<object>();
            }
            else if (mutableData.ChildrenCount >= maxScores)
            {
                object entryWithLowestScore = leaders.OrderBy(entry => (long)((Dictionary<string, object>)entry)["score"]).FirstOrDefault();
                long minScore = (long)((Dictionary<string, object>)entryWithLowestScore)["score"];
                if (minScore > score)
                    // The new score is lower than the existing 5 scores, abort.
                    return TransactionResult.Abort();
                else
                    // Remove the lowest score.
                    leaders.Remove(entryWithLowestScore);
            }

            // Add the new high score.
            Dictionary<string, object> newScoreMap = new Dictionary<string, object>();
            newScoreMap["score"] = score;
            newScoreMap["email"] = email;
            leaders.Add(newScoreMap);

            leaders = leaders.OrderByDescending(entry => (long)((Dictionary<string, object>)entry)["score"]).ToList();

            mutableData.Value = leaders;
            return TransactionResult.Success(mutableData);
        });

        try
        {
            await updateLeaderboards;
            return updateLeaderboards.Result;
        }
        catch(DatabaseException de)
        {
            Debug.LogWarning(de.Message);
            return null;
        }
        catch (AggregateException ae)
        {
            Debug.LogWarning(ae.InnerException.Message);
            return null;
        }

    }
}
