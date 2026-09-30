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
        for (int i = 0; i < columnLength * rowLength; i++)
        {
            Instantiate(cubePrefab, new Vector3(x_Start + (x_Space * (i % columnLength)), y_Start + (-y_Space * (i / columnLength))), Quaternion.identity, this.transform);
        }
    }

}
