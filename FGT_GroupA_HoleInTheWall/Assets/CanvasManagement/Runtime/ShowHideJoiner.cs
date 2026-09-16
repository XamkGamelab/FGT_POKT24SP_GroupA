using UnityEngine;

public class ShowHideJoiner : MonoBehaviour
{
    [SerializeField] private GameObject[] joinedObjects = new GameObject[0];

    private void OnEnable()
    {
        foreach (GameObject gameObject in joinedObjects)
            gameObject.SetActive(true);
    }

    private void OnDisable()
    {
        foreach (GameObject gameObject in joinedObjects)
            gameObject.SetActive(false);
    }
}
