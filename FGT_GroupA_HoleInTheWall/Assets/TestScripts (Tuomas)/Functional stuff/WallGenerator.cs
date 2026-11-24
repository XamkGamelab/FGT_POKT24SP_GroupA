
using UnityEngine;

public class WallGenerator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GeneratedWallStorage wallStorage;
    [SerializeField] private GameObject gapObject;

    [Header("Lane Settings")]
    [SerializeField] private float laneSpacing = 4f;

    [SerializeField]
    private Vector3 laneDirection = Vector3.forward;

    [Header("Generation Settings")]
    [SerializeField] private bool generateOnStart = true;

    private const int LaneCount = 3;

    private bool hasGenerated;

    private void Start()
    {
        if (generateOnStart)
            GenerateWall();
    }

    [ContextMenu("Generate Wall")]
    public void GenerateWall()
    {
        if (!Application.isPlaying || hasGenerated)
            return;

        if (wallStorage == null || gapObject == null)
        {
            Debug.LogError("WallGenerator is missing references.");
            return;
        }

        if (wallStorage.Count == 0)
        {
            Debug.LogWarning("No recorded wall shapes available.");
            return;
        }

        GameObject poseWall = wallStorage.GetRandomWall();

        if (poseWall == null)
            return;

        // Randomly select one of the three lanes.
        int poseLane = Random.Range(0, LaneCount);

        Vector3 direction = transform.TransformDirection(
            laneDirection.normalized
        );

        for (int lane = 0; lane < LaneCount; lane++)
        {
            // Center the three lanes around this GameObject.
            Vector3 position = transform.position +
                direction * laneSpacing * (lane - 1);

            GameObject prefab =
                lane == poseLane ? poseWall : gapObject;

            GameObject spawnedWall = Instantiate(
                prefab,
                position,
                transform.rotation,
                transform
            );

            spawnedWall.SetActive(true);

            spawnedWall.name = lane == poseLane
                ? "PoseWall_Lane_" + lane
                : "SolidWall_Lane_" + lane;
        }

        hasGenerated = true;

        Debug.Log(
            $"Wall generated. Pose wall is in lane {poseLane + 1}."
        );
    }
}
