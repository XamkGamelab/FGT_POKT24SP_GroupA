using UnityEngine;

public class WallGenerator : MonoBehaviour
{
    public float x_Start, y_Start;
    public int columnLength, rowLength;
    public float x_Space, y_Space;
    public GameObject cubePrefab;
    
    public GameObject[] armShape;

    
    void Start()
    {
        
    }
    void OnGUI()
    {
        if (GUI.Button(new Rect(10, 10, 150, 100), "Generate wall"))
        {
            for (int i = 0; i < columnLength * rowLength; i++)
            {
                Instantiate(cubePrefab, new Vector3(x_Start + (x_Space * (i % columnLength)), y_Start + (-y_Space * (i / columnLength))), Quaternion.identity, this.transform);
            }
        }
        else if (GUI.Button(new Rect(10, 200, 150, 100), "Destory wall"))
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
        }

    }

}
