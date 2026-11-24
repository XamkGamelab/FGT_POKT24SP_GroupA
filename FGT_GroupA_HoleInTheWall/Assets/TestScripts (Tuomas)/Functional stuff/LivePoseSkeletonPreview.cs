
using System.Collections.Generic;
using UnityEngine;

public class LivePoseSkeletonPreview : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform wall;

    [Header("Skeleton Settings")]
    [SerializeField] private float overallScale = 1f;
    [SerializeField] private float limbWidth = 0.06f;
    [SerializeField] private float jointSize = 0.10f;

    //testing
    [Header("Simulation")]
    [SerializeField] private bool useSimulatedPose = true;

    [Header("Position Offset")]
    [SerializeField] private Vector3 positionOffset = Vector3.zero;

    [Header("Preview Depth")]
    [SerializeField] private float previewDepthOffset = 0.05f;

    [Header("Appearance")]
    [SerializeField] private Color skeletonColor = Color.green;

    private readonly Dictionary<HumanTrackingPoint, GameObject> joints = new();
    private readonly List<GameObject> limbObjects = new();

    private Material previewMaterial;

    private bool initialized;

    private void Start()
    {
        if (wall == null)
        {
            Debug.LogError("LivePoseSkeletonPreview: Wall is missing!");
            enabled = false;
            return;
        }

        CreateMaterial();
        CreateSkeletonObjects();

        initialized = true;
    }

    private void Update()
    {
        if (!initialized)
            return;

        Dictionary<HumanTrackingPoint, Vector3> points;

        // Use simulated pose when camera tracking is unavailable.
        if (useSimulatedPose)
        {
            points = GetSimulatedPose();
        }
        else
        {
            if (PoseRouter.Instance == null)
                return;

            if (PoseRouter.Instance.GetPoseLandmarks() == null)
                return;

            points = PoseRouter.Instance.GetTrackingPoints();
        }

        UpdateJointPositions(points);
        UpdateLimbPositions(points);
    }
    // for simulation purposes
    private Dictionary<HumanTrackingPoint, Vector3> GetSimulatedPose()
    {
        return new Dictionary<HumanTrackingPoint, Vector3>
    {
        // Head
        { HumanTrackingPoint.Head, new Vector3(0.50f, 0.92f, 0f) },

        // Shoulders
        { HumanTrackingPoint.LeftShoulder, new Vector3(0.40f, 0.75f, 0f) },
        { HumanTrackingPoint.RightShoulder, new Vector3(0.60f, 0.75f, 0f) },

        // Elbows
        { HumanTrackingPoint.LeftElbow, new Vector3(0.27f, 0.85f, 0f) },
        { HumanTrackingPoint.RightElbow, new Vector3(0.73f, 0.85f, 0f) },

        // Wrists
        { HumanTrackingPoint.LeftWrist, new Vector3(0.12f, 0.95f, 0f) },
        { HumanTrackingPoint.RightWrist, new Vector3(0.88f, 0.95f, 0f) },

        // Hips
        { HumanTrackingPoint.LeftHip, new Vector3(0.44f, 0.48f, 0f) },
        { HumanTrackingPoint.RightHip, new Vector3(0.56f, 0.48f, 0f) },

        // Knees
        { HumanTrackingPoint.LeftKnee, new Vector3(0.34f, 0.25f, 0f) },
        { HumanTrackingPoint.RightKnee, new Vector3(0.66f, 0.25f, 0f) },

        // Ankles
        { HumanTrackingPoint.LeftAnkle, new Vector3(0.23f, 0.05f, 0f) },
        { HumanTrackingPoint.RightAnkle, new Vector3(0.77f, 0.05f, 0f) }
    };
    }

    // =========================================================
    // CREATE MATERIAL
    // =========================================================

    private void CreateMaterial()
    {
        previewMaterial = new Material(
            Shader.Find("Universal Render Pipeline/Unlit")
        );

        if (previewMaterial.shader == null)
        {
            previewMaterial.shader = Shader.Find("Unlit/Color");
        }

        previewMaterial.color = skeletonColor;
    }

    // =========================================================
    // CREATE SKELETON OBJECTS
    // =========================================================

    private void CreateSkeletonObjects()
    {
        // Create joint spheres.
        foreach (HumanTrackingPoint point in
                 System.Enum.GetValues(typeof(HumanTrackingPoint)))
        {
            GameObject joint = GameObject.CreatePrimitive(
                PrimitiveType.Sphere
            );

            joint.name = "Joint_" + point;

            RemoveCollider(joint);

            joint.transform.SetParent(wall, false);

            joint.GetComponent<Renderer>().material = previewMaterial;

            joints.Add(point, joint);
        }

        // Create limb connections.
        CreateLimb(HumanTrackingPoint.LeftShoulder,
                  HumanTrackingPoint.RightShoulder);

        CreateLimb(HumanTrackingPoint.LeftShoulder,
                  HumanTrackingPoint.LeftElbow);

        CreateLimb(HumanTrackingPoint.LeftElbow,
                  HumanTrackingPoint.LeftWrist);

        CreateLimb(HumanTrackingPoint.RightShoulder,
                  HumanTrackingPoint.RightElbow);

        CreateLimb(HumanTrackingPoint.RightElbow,
                  HumanTrackingPoint.RightWrist);

        CreateLimb(HumanTrackingPoint.LeftShoulder,
                  HumanTrackingPoint.LeftHip);

        CreateLimb(HumanTrackingPoint.RightShoulder,
                  HumanTrackingPoint.RightHip);

        CreateLimb(HumanTrackingPoint.LeftHip,
                  HumanTrackingPoint.RightHip);

        CreateLimb(HumanTrackingPoint.LeftHip,
                  HumanTrackingPoint.LeftKnee);

        CreateLimb(HumanTrackingPoint.LeftKnee,
                  HumanTrackingPoint.LeftAnkle);

        CreateLimb(HumanTrackingPoint.RightHip,
                  HumanTrackingPoint.RightKnee);

        CreateLimb(HumanTrackingPoint.RightKnee,
                  HumanTrackingPoint.RightAnkle);
    }

    private void CreateLimb(
        HumanTrackingPoint start,
        HumanTrackingPoint end)
    {
        GameObject limb = GameObject.CreatePrimitive(
            PrimitiveType.Cylinder
        );

        limb.name = start + "_To_" + end;

        RemoveCollider(limb);

        limb.transform.SetParent(wall, false);

        limb.GetComponent<Renderer>().material = previewMaterial;

        limbObjects.Add(limb);
    }

    private void RemoveCollider(GameObject obj)
    {
        Collider collider = obj.GetComponent<Collider>();

        if (collider != null)
            Destroy(collider);
    }

    // =========================================================
    // UPDATE JOINTS
    // =========================================================

    private void UpdateJointPositions(
        Dictionary<HumanTrackingPoint, Vector3> points)
    {
        foreach (var pair in joints)
        {
            HumanTrackingPoint point = pair.Key;

            if (!points.ContainsKey(point))
                continue;

            Vector3 position = ConvertToWallPosition(points[point]);

            pair.Value.transform.localPosition = position;

            pair.Value.transform.localRotation = Quaternion.identity;

            pair.Value.transform.localScale =
                Vector3.one * jointSize * overallScale;
        }
    }

    // =========================================================
    // UPDATE LIMBS
    // =========================================================

    private void UpdateLimbPositions(
        Dictionary<HumanTrackingPoint, Vector3> points)
    {
        int index = 0;

        foreach (Connection connection in GetConnections())
        {
            HumanTrackingPoint startPoint = connection.start;
            HumanTrackingPoint endPoint = connection.end;

            if (!points.ContainsKey(startPoint) ||
                !points.ContainsKey(endPoint))
            {
                index++;
                continue;
            }

            Vector3 start =
                ConvertToWallPosition(points[startPoint]);

            Vector3 end =
                ConvertToWallPosition(points[endPoint]);

            Vector3 direction = end - start;

            float length = direction.magnitude;

            GameObject limb = limbObjects[index];

            if (length > 0.001f)
            {
                limb.transform.localPosition =
                    (start + end) * 0.5f;

                limb.transform.localRotation =
                    Quaternion.FromToRotation(
                        Vector3.up,
                        direction.normalized
                    );

                // Unity cylinders have a default radius of 0.5
                // and a default height of 2.
                limb.transform.localScale = new Vector3(
                    limbWidth * overallScale,
                    length * 0.5f,
                    limbWidth * overallScale
                );
            }

            index++;
        }
    }

    // =========================================================
    // TRACKING COORDINATES -> WALL COORDINATES
    // =========================================================

    private Vector3 ConvertToWallPosition(Vector3 trackingPosition)
    {
        MeshFilter meshFilter = wall.GetComponent<MeshFilter>();

        if (meshFilter == null || meshFilter.sharedMesh == null)
            return trackingPosition;

        Bounds bounds = meshFilter.sharedMesh.bounds;

        // Tracking X = wall's local Z (horizontal width)
        // Tracking Y = wall's local Y (vertical height)
        // Local X = wall thickness / preview depth

        float wallHorizontal = Mathf.Lerp(
            bounds.min.z,
            bounds.max.z,
            trackingPosition.x
        );

        float wallVertical = Mathf.Lerp(
            bounds.min.y,
            bounds.max.y,
            trackingPosition.y
        );

        Vector3 wallCenter = bounds.center;

        Vector3 mappedPosition = new Vector3(
            wallCenter.x + previewDepthOffset,
            wallVertical,
            wallHorizontal
        );

        // Scale the complete skeleton around the wall center.
        mappedPosition = wallCenter +
            (mappedPosition - wallCenter) * overallScale;

        mappedPosition += positionOffset;

        return mappedPosition;
    }

    // =========================================================
    // CONNECTION LIST
    // =========================================================

    private struct Connection
    {
        public HumanTrackingPoint start;
        public HumanTrackingPoint end;

        public Connection(
            HumanTrackingPoint start,
            HumanTrackingPoint end)
        {
            this.start = start;
            this.end = end;
        }
    }
    //to get points for different script
    public bool TryGetMappedPoints(
    out Dictionary<HumanTrackingPoint, Vector3> mappedPoints)
    {
        mappedPoints = new Dictionary<HumanTrackingPoint, Vector3>();

        if (!initialized)
            return false;

        Dictionary<HumanTrackingPoint, Vector3> points;

        if (useSimulatedPose)
        {
            points = GetSimulatedPose();
        }
        else
        {
            if (PoseRouter.Instance == null)
                return false;

            var landmarks = PoseRouter.Instance.GetPoseLandmarks();

            if (landmarks == null || landmarks.Count < 33)
                return false;

            points = PoseRouter.Instance.GetTrackingPoints();
        }

        foreach (var pair in points)
        {
            Vector3 position = ConvertToWallPosition(pair.Value);

            // Remove visual preview depth.
            // X is the thickness axis of our wall.
            position.x = 0f;

            mappedPoints.Add(pair.Key, position);
        }

        return mappedPoints.Count == 13;
    }

    public void SetPreviewVisible(bool visible)
    {
        foreach (GameObject joint in joints.Values)
        {
            if (joint != null)
                joint.SetActive(visible);
        }

        foreach (GameObject limb in limbObjects)
        {
            if (limb != null)
                limb.SetActive(visible);
        }
    }

    private IEnumerable<Connection> GetConnections()
    {
        yield return new Connection(
            HumanTrackingPoint.LeftShoulder,
            HumanTrackingPoint.RightShoulder
        );

        yield return new Connection(
            HumanTrackingPoint.LeftShoulder,
            HumanTrackingPoint.LeftElbow
        );

        yield return new Connection(
            HumanTrackingPoint.LeftElbow,
            HumanTrackingPoint.LeftWrist
        );

        yield return new Connection(
            HumanTrackingPoint.RightShoulder,
            HumanTrackingPoint.RightElbow
        );

        yield return new Connection(
            HumanTrackingPoint.RightElbow,
            HumanTrackingPoint.RightWrist
        );

        yield return new Connection(
            HumanTrackingPoint.LeftShoulder,
            HumanTrackingPoint.LeftHip
        );

        yield return new Connection(
            HumanTrackingPoint.RightShoulder,
            HumanTrackingPoint.RightHip
        );

        yield return new Connection(
            HumanTrackingPoint.LeftHip,
            HumanTrackingPoint.RightHip
        );

        yield return new Connection(
            HumanTrackingPoint.LeftHip,
            HumanTrackingPoint.LeftKnee
        );

        yield return new Connection(
            HumanTrackingPoint.LeftKnee,
            HumanTrackingPoint.LeftAnkle
        );

        yield return new Connection(
            HumanTrackingPoint.RightHip,
            HumanTrackingPoint.RightKnee
        );

        yield return new Connection(
            HumanTrackingPoint.RightKnee,
            HumanTrackingPoint.RightAnkle
        );
    }

    // =========================================================
    // CLEANUP
    // =========================================================

    private void OnDestroy()
    {
        foreach (GameObject joint in joints.Values)
        {
            if (joint != null)
                Destroy(joint);
        }

        foreach (GameObject limb in limbObjects)
        {
            if (limb != null)
                Destroy(limb);
        }

        if (previewMaterial != null)
            Destroy(previewMaterial);
    }
}
