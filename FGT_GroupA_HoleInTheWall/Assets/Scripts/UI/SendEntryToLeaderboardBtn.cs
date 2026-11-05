using TMPro;
using UnityEngine;
using UnityEngine.UI;


[RequireComponent(typeof(Button))]
public class SendEntryToLeaderboardBtn : MonoBehaviour
{
    [SerializeField] TMP_InputField nameField = null;

    void Awake() => GetComponent<Button>().onClick.AddListener(()=>
    {
        if (!ValidateEntry())
            return;

        //LeaderboardManager.Send.Invoke(nameField);
    });

    private bool ValidateEntry()
    {
        if (nameField == null)
            return false;

        if(string.IsNullOrEmpty(nameField.text))
            return false;

        return true;
    }
}
