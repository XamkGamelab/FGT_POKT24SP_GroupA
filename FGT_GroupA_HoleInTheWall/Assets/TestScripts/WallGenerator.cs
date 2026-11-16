using UnityEngine;

public class WallGenerator : MonoBehaviour
{
    public float x_Start, y_Start;

    public int columnLength, rowLength; //IMPORTANT: keep columnLength at 3 and rows a 1 for the intended wall generation. This was just for scalability

    public float x_Space, y_Space; //for spacing the wall objects

    //The wall obj used to fill the wall
    public GameObject gapObject;

    //different shape objects that can appear in the wall
    public GameObject[] shapeObjects;


    void Start()
    {
        GenerateWall();
        //put GenerateWall() here when it is attached to the wall instantiation script. OnGUI() elements are just for debugging and shii
    }

    void GenerateWall()
    {
        //randomly choose which column gets the shape. For 3 columns this will be 0, 1 or 2
        int shapeColumn = Random.Range(0, columnLength);

        //pick a random shape from the shapeObjects array
        int randomShape = Random.Range(0, shapeObjects.Length);

        for (int i = 0; i < columnLength * rowLength; i++)
        {
            //calculate the column and row of this object
            int column = i % columnLength;
            int row = i / columnLength;

            //calculate the position
            Vector3 position = new Vector3(transform.position.x + (x_Space * column), transform.position.y + (-y_Space * row), transform.position.z);

            //if this is the randomly selected column, spawn the shape instead of the gap
            if (column == shapeColumn)
            {
                //Testing, disabled collider 
                Instantiate(shapeObjects[randomShape], position, Quaternion.identity, transform).GetComponent<Collider>().enabled = false;
            }
            else
            {
                //otherwise spawn the normal gap object.
                Instantiate(gapObject, position, Quaternion.identity, transform);
            }
        }
    }
}