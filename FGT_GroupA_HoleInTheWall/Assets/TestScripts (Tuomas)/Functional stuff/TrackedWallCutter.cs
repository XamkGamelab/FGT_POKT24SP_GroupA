
using UnityEngine;
using ManifoldCSG;

public class TrackedWallCutter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform previewWall;

    [SerializeField] private GameObject uncutWallPrefab;

    [SerializeField]
    private HumanoidCutGenerator cutGenerator;

    private bool cutting;

    public bool IsCutting => cutting;

    [ContextMenu("Capture And Store Wall")]
    private void CutWallFromInspector()
    {
        CutWall();
    }

    // =========================================================
    // CAPTURE AND GENERATE WALL
    // =========================================================

    public bool CutWall()
    {
        if (!Application.isPlaying || cutting)
            return false;

        if (previewWall == null ||
            uncutWallPrefab == null ||
            cutGenerator == null ||
            GeneratedWallStorage.Instance == null)
        {
            Debug.LogError("Missing wall cutter references.");
            return false;
        }

        cutting = true;

        GameObject workingWall = null;

        try
        {
            // Capture the current pose.
            if (!cutGenerator.CapturePose())
            {
                Debug.LogWarning("Could not capture pose.");
                return false;
            }

            // Create a fresh, uncut working wall.
            workingWall = Instantiate(
                uncutWallPrefab,
                previewWall.position,
                previewWall.rotation
            );

            workingWall.name = "TemporaryWorkingWall";

            workingWall.transform.localScale =
                previewWall.lossyScale;

            CsgBody wall =
                workingWall.GetComponent<CsgBody>();

            MeshFilter wallFilter =
                workingWall.GetComponent<MeshFilter>();

            if (wall == null ||
                wallFilter == null ||
                wallFilter.sharedMesh == null)
            {
                Debug.LogError(
                    "Working wall requires CsgBody and MeshFilter."
                );

                return false;
            }

            // =================================================
            // BOOLEAN SUBTRACTION
            // =================================================

            foreach (GameObject cutter in cutGenerator.Cutters)
            {
                if (cutter == null)
                    return false;

                MeshFilter cutterFilter =
                    cutter.GetComponent<MeshFilter>();

                if (cutterFilter == null ||
                    cutterFilter.sharedMesh == null)
                {
                    Debug.LogError("Invalid cutter mesh.");
                    return false;
                }

                bool success = wall.Subtract(
                    cutterFilter.sharedMesh,
                    cutter.transform.localToWorldMatrix
                );

                if (!success)
                {
                    Debug.LogError(
                        $"Cut failed: {cutter.name} | " +
                        $"{wall.LastError}"
                    );

                    return false;
                }
            }

            // =================================================
            // STORE COMPLETED WALL
            // =================================================

            GameObject storedWall =
                GeneratedWallStorage.Instance.StoreWall(workingWall);

            if (storedWall == null)
            {
                Debug.LogError("Could not store generated wall.");
                return false;
            }

            Debug.Log(
                $"Pose recorded successfully! " +
                $"Stored walls: {GeneratedWallStorage.Instance.Count}"
            );

            return true;
        }
        finally
        {
            // Clean up even if a Boolean operation fails.
            cutGenerator.ClearCutters();

            if (workingWall != null)
            {
                workingWall.SetActive(false);
                Destroy(workingWall);
            }

            cutting = false;
        }
    }
}
