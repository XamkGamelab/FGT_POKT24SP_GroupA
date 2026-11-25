
using System.Collections.Generic;
using UnityEngine;

public class GeneratedWallStorage : SingletonMono<GeneratedWallStorage>
{
    [Header("Stored Wall Shapes")]
    [SerializeField]
    private List<GameObject> generatedWalls = new();

    public IReadOnlyList<GameObject> GeneratedWalls =>
        generatedWalls;

    public int Count => generatedWalls.Count;

    // =========================================================
    // STORE GENERATED WALL
    // =========================================================

    public GameObject StoreWall(GameObject sourceWall)
    {
        if (sourceWall == null)
            return null;

        MeshFilter sourceFilter =
            sourceWall.GetComponent<MeshFilter>();

        MeshRenderer sourceRenderer =
            sourceWall.GetComponent<MeshRenderer>();

        if (sourceFilter == null ||
            sourceFilter.sharedMesh == null ||
            sourceRenderer == null)
        {
            Debug.LogError("Cannot store invalid wall.");
            return null;
        }

        // Create an independent copy of the generated mesh.
        Mesh storedMesh = Instantiate(
            sourceFilter.sharedMesh
        );

        storedMesh.name =
            "GeneratedWallMesh_" + generatedWalls.Count;

        // Create an empty runtime wall template.
        GameObject storedWall = new GameObject(
            "GeneratedWall_" + generatedWalls.Count
        );

        storedWall.SetActive(false);

        storedWall.transform.SetParent(transform, false);

        storedWall.transform.localPosition = Vector3.zero;
        storedWall.transform.localRotation = Quaternion.identity;

        storedWall.transform.localScale =
            sourceWall.transform.lossyScale;

        storedWall.layer = sourceWall.layer;
        storedWall.tag = sourceWall.tag;

        // MeshFilter
        MeshFilter filter =
            storedWall.AddComponent<MeshFilter>();

        filter.sharedMesh = storedMesh;

        // MeshRenderer
        MeshRenderer renderer =
            storedWall.AddComponent<MeshRenderer>();

        renderer.sharedMaterials =
            sourceRenderer.sharedMaterials;

        // MeshCollider
        MeshCollider collider =
            storedWall.AddComponent<MeshCollider>();

        collider.convex = false;
        collider.sharedMesh = storedMesh;

        generatedWalls.Add(storedWall);

        Debug.Log(
            $"Wall stored successfully. " +
            $"Total generated walls: {generatedWalls.Count}"
        );

        return storedWall;
    }

    // =========================================================
    // GET STORED WALL
    // =========================================================

    public GameObject GetWall(int index)
    {
        if (index < 0 || index >= generatedWalls.Count)
            return null;

        return generatedWalls[index];
    }

    public GameObject GetRandomWall()
    {
        if (generatedWalls.Count == 0)
            return null;

        int index = Random.Range(
            0,
            generatedWalls.Count
        );

        return generatedWalls[index];
    }

    // =========================================================
    // SPAWN STORED WALL
    // =========================================================

    public GameObject SpawnWall(
        int index,
        Vector3 position,
        Quaternion rotation)
    {
        GameObject template = GetWall(index);

        if (template == null)
            return null;

        GameObject instance = Instantiate(
            template,
            position,
            rotation
        );

        instance.SetActive(true);

        return instance;
    }

    public GameObject SpawnRandomWall(
        Vector3 position,
        Quaternion rotation)
    {
        if (generatedWalls.Count == 0)
            return null;

        int index = Random.Range(
            0,
            generatedWalls.Count
        );

        return SpawnWall(index, position, rotation);
    }
}
