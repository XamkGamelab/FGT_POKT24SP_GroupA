
using System.Collections.Generic;
using UnityEngine;

public class HumanoidCutGenerator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LivePoseSkeletonPreview posePreview;
    [SerializeField] private Transform wall;

    [Header("Body Dimensions")]
    [Min(0.01f)]
    [SerializeField] private float limbWidth = 0.24f;

    [Min(0.01f)]
    [SerializeField] private float neckWidth = 0.20f;

    [Min(0.01f)]
    [SerializeField] private float headWidth = 0.40f;

    [Min(0f)]
    [SerializeField] private float torsoPadding = 0.08f;

    [Header("Cutting Settings")]
    [Min(0f)]
    [SerializeField] private float clearance = 0.12f;

    [Min(0.01f)]
    [SerializeField] private float depthPadding = 0.05f;

    [Range(8, 32)]
    [SerializeField] private int roundness = 16;

    [Header("Ground Connection")]
    [SerializeField] private bool extendGroundedLegs = true;

    [Min(0f)]
    [SerializeField] private float groundedThreshold = 0.25f;

    [Header("Preview")]
    [SerializeField] private Material cutterMaterial;
    [SerializeField] private bool showCutterPreview = true;
    [SerializeField] private bool livePreview = false;

    private readonly List<GameObject> cutters = new();

    private Dictionary<HumanTrackingPoint, Vector2> points;

    private Bounds wallBounds;

    private float wallScaleX;
    private float wallScaleY;
    private float wallScaleZ;

    public IReadOnlyList<GameObject> Cutters => cutters;

    private void Update()
    {
        if (livePreview)
            GenerateCutters();
    }

    public void SetLivePreview(bool enabled)
    {
        livePreview = enabled;

        if (!enabled)
            ClearCutters();
    }

    // =========================================================
    // GENERATE COMPLETE HUMANOID
    // =========================================================

    [ContextMenu("Generate Humanoid Cutters")]
    public void GenerateCutters()
    {
        if (!Application.isPlaying)
            return;

        if (posePreview == null || wall == null)
            return;

        MeshFilter filter = wall.GetComponent<MeshFilter>();

        if (filter == null || filter.sharedMesh == null)
            return;

        if (!posePreview.TryGetMappedPoints(out var mapped))
            return;

        ClearCutters();

        wallBounds = filter.sharedMesh.bounds;

        // Physical size of one wall-local unit.
        wallScaleX = wall.TransformVector(Vector3.right).magnitude;
        wallScaleY = wall.TransformVector(Vector3.up).magnitude;
        wallScaleZ = wall.TransformVector(Vector3.forward).magnitude;

        if (wallScaleX < 0.0001f ||
            wallScaleY < 0.0001f ||
            wallScaleZ < 0.0001f)
            return;

        points = new Dictionary<HumanTrackingPoint, Vector2>();

        // Convert wall-local XY coordinates into physical
        // horizontal and vertical coordinates.
        foreach (var pair in mapped)
        {
            points.Add(
                pair.Key,
                new Vector2(
                    pair.Value.x * wallScaleX,
                    pair.Value.y * wallScaleY
                )
            );
        }

        GenerateTorso();
        GenerateHeadAndNeck();
        GenerateArms();
        GenerateLegs();

        if (extendGroundedLegs)
            GenerateGroundExtensions();
    }

    // =========================================================
    // TORSO
    // =========================================================

    private void GenerateTorso()
    {
        Vector2 leftShoulder =
            points[HumanTrackingPoint.LeftShoulder];

        Vector2 rightShoulder =
            points[HumanTrackingPoint.RightShoulder];

        Vector2 leftHip =
            points[HumanTrackingPoint.LeftHip];

        Vector2 rightHip =
            points[HumanTrackingPoint.RightHip];

        List<Vector2> outline = new();

        float padding = Mathf.Max(
            0.005f,
            torsoPadding + clearance
        );

        AddCirclePoints(outline, leftShoulder, padding);
        AddCirclePoints(outline, rightShoulder, padding);
        AddCirclePoints(outline, leftHip, padding);
        AddCirclePoints(outline, rightHip, padding);

        // Fill the complete area between shoulders and hips.
        List<Vector2> hull = CreateConvexHull(outline);

        CreateExtrudedCutter("Cutter_Torso", hull);
    }

    // =========================================================
    // HEAD AND NECK
    // =========================================================

    private void GenerateHeadAndNeck()
    {
        Vector2 head =
            points[HumanTrackingPoint.Head];

        Vector2 leftShoulder =
            points[HumanTrackingPoint.LeftShoulder];

        Vector2 rightShoulder =
            points[HumanTrackingPoint.RightShoulder];

        // Automatically calculated neck origin.
        Vector2 shoulderCenter =
            (leftShoulder + rightShoulder) * 0.5f;

        CreateRoundedSegment(
            "Cutter_Neck",
            shoulderCenter,
            head,
            neckWidth
        );

        List<Vector2> headOutline = new();

        AddCirclePoints(
            headOutline,
            head,
            headWidth * 0.5f + clearance
        );

        CreateExtrudedCutter(
            "Cutter_Head",
            CreateConvexHull(headOutline)
        );
    }

    // =========================================================
    // ARMS
    // =========================================================

    private void GenerateArms()
    {
        CreateLimb(
            "Cutter_LeftUpperArm",
            HumanTrackingPoint.LeftShoulder,
            HumanTrackingPoint.LeftElbow
        );

        CreateLimb(
            "Cutter_LeftLowerArm",
            HumanTrackingPoint.LeftElbow,
            HumanTrackingPoint.LeftWrist
        );

        CreateLimb(
            "Cutter_RightUpperArm",
            HumanTrackingPoint.RightShoulder,
            HumanTrackingPoint.RightElbow
        );

        CreateLimb(
            "Cutter_RightLowerArm",
            HumanTrackingPoint.RightElbow,
            HumanTrackingPoint.RightWrist
        );
    }

    // =========================================================
    // LEGS
    // =========================================================

    private void GenerateLegs()
    {
        CreateLimb(
            "Cutter_LeftUpperLeg",
            HumanTrackingPoint.LeftHip,
            HumanTrackingPoint.LeftKnee
        );

        CreateLimb(
            "Cutter_LeftLowerLeg",
            HumanTrackingPoint.LeftKnee,
            HumanTrackingPoint.LeftAnkle
        );

        CreateLimb(
            "Cutter_RightUpperLeg",
            HumanTrackingPoint.RightHip,
            HumanTrackingPoint.RightKnee
        );

        CreateLimb(
            "Cutter_RightLowerLeg",
            HumanTrackingPoint.RightKnee,
            HumanTrackingPoint.RightAnkle
        );
    }

    // =========================================================
    // GROUNDED FEET
    // =========================================================

    private void GenerateGroundExtensions()
    {
        float wallBottom =
            wallBounds.min.y * wallScaleY;

        ExtendAnkleToBottom(
            HumanTrackingPoint.LeftAnkle,
            wallBottom
        );

        ExtendAnkleToBottom(
            HumanTrackingPoint.RightAnkle,
            wallBottom
        );
    }

    private void ExtendAnkleToBottom(
        HumanTrackingPoint anklePoint,
        float wallBottom)
    {
        Vector2 ankle = points[anklePoint];

        float distanceFromBottom = ankle.y - wallBottom;

        // Do not extend feet that are clearly above the floor
        // or already below the wall.
        if (distanceFromBottom > groundedThreshold ||
            distanceFromBottom < 0f)
            return;

        Vector2 bottomPoint = new Vector2(
            ankle.x,
            wallBottom - limbWidth
        );

        CreateRoundedSegment(
            "Cutter_Ground_" + anklePoint,
            ankle,
            bottomPoint,
            limbWidth
        );
    }

    // =========================================================
    // LIMB GENERATION
    // =========================================================

    private void CreateLimb(
        string cutterName,
        HumanTrackingPoint start,
        HumanTrackingPoint end)
    {
        CreateRoundedSegment(
            cutterName,
            points[start],
            points[end],
            limbWidth
        );
    }

    private void CreateRoundedSegment(
        string cutterName,
        Vector2 start,
        Vector2 end,
        float width)
    {
        float radius = width * 0.5f + clearance;

        List<Vector2> outline = new();

        // Rounded ends overlap at shared tracking points.
        AddCirclePoints(outline, start, radius);
        AddCirclePoints(outline, end, radius);

        List<Vector2> hull = CreateConvexHull(outline);

        CreateExtrudedCutter(cutterName, hull);
    }

    private void AddCirclePoints(
        List<Vector2> outline,
        Vector2 center,
        float radius)
    {
        for (int i = 0; i < roundness; i++)
        {
            float angle =
                i * Mathf.PI * 2f / roundness;

            Vector2 point = center + new Vector2(
                Mathf.Cos(angle),
                Mathf.Sin(angle)
            ) * radius;

            outline.Add(point);
        }
    }

    // =========================================================
    // CONVEX HULL
    // =========================================================

    private List<Vector2> CreateConvexHull(
        List<Vector2> input)
    {
        input.Sort((a, b) =>
        {
            int comparison = a.x.CompareTo(b.x);

            return comparison != 0
                ? comparison
                : a.y.CompareTo(b.y);
        });

        List<Vector2> lower = new();
        List<Vector2> upper = new();

        foreach (Vector2 point in input)
        {
            while (lower.Count >= 2 &&
                   Cross(
                       lower[lower.Count - 2],
                       lower[lower.Count - 1],
                       point
                   ) <= 0f)
            {
                lower.RemoveAt(lower.Count - 1);
            }

            lower.Add(point);
        }

        for (int i = input.Count - 1; i >= 0; i--)
        {
            Vector2 point = input[i];

            while (upper.Count >= 2 &&
                   Cross(
                       upper[upper.Count - 2],
                       upper[upper.Count - 1],
                       point
                   ) <= 0f)
            {
                upper.RemoveAt(upper.Count - 1);
            }

            upper.Add(point);
        }

        lower.RemoveAt(lower.Count - 1);
        upper.RemoveAt(upper.Count - 1);

        lower.AddRange(upper);

        return lower;
    }

    private float Cross(
        Vector2 a,
        Vector2 b,
        Vector2 c)
    {
        Vector2 ab = b - a;
        Vector2 ac = c - a;

        return ab.x * ac.y - ab.y * ac.x;
    }

    private void EnsureCounterClockwise(List<Vector2> outline)
    {
        float signedArea = 0f;

        for (int i = 0; i < outline.Count; i++)
        {
            Vector2 a = outline[i];
            Vector2 b = outline[(i + 1) % outline.Count];
            signedArea += a.x * b.y - b.x * a.y;
        }

        if (signedArea < 0f)
            outline.Reverse();
    }

    // =========================================================
    // EXTRUDE 2D SHAPE THROUGH WALL
    // =========================================================

    private void CreateExtrudedCutter(
        string cutterName,
        List<Vector2> outline)
    {
        EnsureCounterClockwise(outline);

        int count = outline.Count;

        if (count < 3)
            return;

        float localPadding =
            depthPadding / wallScaleZ;

        float frontZ =
            wallBounds.max.z + localPadding;

        float backZ =
            wallBounds.min.z - localPadding;

        Vector3[] vertices = new Vector3[count * 2];

        List<int> triangles = new();

        // Create the XY profile twice, once on each side along Z.
        for (int i = 0; i < count; i++)
        {
            float x = outline[i].x / wallScaleX;
            float y = outline[i].y / wallScaleY;

            vertices[i] =
                new Vector3(x, y, frontZ);

            vertices[i + count] =
                new Vector3(x, y, backZ);
        }

        // Front and back XY faces.
        for (int i = 1; i < count - 1; i++)
        {
            // Front face points toward positive Z.
            triangles.Add(0);
            triangles.Add(i);
            triangles.Add(i + 1);

            // Back face points toward negative Z.
            triangles.Add(count);
            triangles.Add(count + i + 1);
            triangles.Add(count + i);
        }

        // Connect the front and back XY faces along Z.
        for (int i = 0; i < count; i++)
        {
            int next = (i + 1) % count;

            triangles.Add(i);
            triangles.Add(i + count);
            triangles.Add(next);

            triangles.Add(next);
            triangles.Add(i + count);
            triangles.Add(next + count);
        }

        Mesh mesh = new Mesh();

        mesh.name = cutterName + "_Mesh";
        mesh.vertices = vertices;
        mesh.triangles = triangles.ToArray();

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        // Create runtime cutter GameObject.
        GameObject cutter = new GameObject(cutterName);

        cutter.transform.SetParent(wall, false);

        MeshFilter meshFilter =
            cutter.AddComponent<MeshFilter>();

        meshFilter.sharedMesh = mesh;

        MeshRenderer renderer =
            cutter.AddComponent<MeshRenderer>();

        if (cutterMaterial != null)
            renderer.sharedMaterial = cutterMaterial;

        renderer.enabled = showCutterPreview;

        cutters.Add(cutter);
    }

    // =========================================================
    // CLEANUP
    // =========================================================

    [ContextMenu("Clear Humanoid Cutters")]
    public void ClearCutters()
    {
        foreach (GameObject cutter in cutters)
        {
            if (cutter == null)
                continue;

            cutter.SetActive(false);

            MeshFilter filter =
                cutter.GetComponent<MeshFilter>();

            if (filter != null &&
                filter.sharedMesh != null)
            {
                Destroy(filter.sharedMesh);
            }

            Destroy(cutter);
        }

        cutters.Clear();
    }

    //to capture poses.
    public bool CapturePose()
    {
        // Stop regenerating the geometry every frame.
        livePreview = false;

        // Remove any previously generated cutters.
        ClearCutters();

        // Generate cutters from the current pose.
        GenerateCutters();

        return cutters.Count >= 11;
    }

    private void OnDestroy()
    {
        ClearCutters();
    }
}
