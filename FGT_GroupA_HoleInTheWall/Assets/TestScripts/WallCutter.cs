using UnityEngine;
using ManifoldCSG;
using System.Collections.Generic;
public class WallCutter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Pose2 pose;
    [SerializeField] private CsgBody wall;

    [Header("Wall Fit")]
    [Range(0f, 0.25f)]
    [SerializeField] private float wallMargin = 0.08f;

    [Header("Cut Settings")]
    [Min(0.001f)]
    [SerializeField] private float depthPadding = 0.05f;

    private bool hasCut;

    private static readonly Dictionary<BodyPartShape, Mesh>
        primitiveMeshes = new();

    [ContextMenu("Cut Wall Using Default Pose")]
    private void CutWallFromInspector()
    {
        CutWall(pose);
    }

    public bool CutWall(Pose2 overridePose)
    {
        if (hasCut)
        {
            Debug.LogWarning(
                "This wall has already been cut.", this);

            return false;
        }

        if (overridePose == null || wall == null)
        {
            Debug.LogError(
                "Missing pose or wall reference.", this);

            return false;
        }

        MeshFilter filter = wall.GetComponent<MeshFilter>();

        if (filter == null || filter.sharedMesh == null)
        {
            Debug.LogError(
                "Wall requires a valid MeshFilter.", this);

            return false;
        }

        if (overridePose.bodyParts == null ||
            overridePose.bodyParts.Count == 0)
        {
            Debug.LogError(
                "Pose contains no body parts.", this);

            return false;
        }

        Bounds wallBounds = filter.sharedMesh.bounds;

        if (!PoseFitUtility.CalculateFit(
            overridePose,
            wallBounds,
            wallMargin,
            out PoseFitUtility.FitResult fit))
        {
            Debug.LogError(
                "Could not calculate pose fitting.", this);

            return false;
        }

        Debug.Log(
            $"Cutting pose: {overridePose.poseName} | " +
            $"Scale: {fit.scale:F3}",
            this
        );

        // Prepare all primitive meshes before starting.
        List<Mesh> cutters = new();

        foreach (BodyPart part in overridePose.bodyParts)
        {
            Mesh mesh = GetPrimitiveMesh(part.shape);

            if (mesh == null)
            {
                Debug.LogError(
                    $"Could not create mesh for {part.partName}.",
                    this);

                return false;
            }

            cutters.Add(mesh);
        }

        // Perform Boolean subtraction.
        for (int i = 0; i < overridePose.bodyParts.Count; i++)
        {
            BodyPart part = overridePose.bodyParts[i];

            Vector3 finalPosition =
                fit.offset + part.localPosition * fit.scale;

            Vector3 finalSize =
                part.size * fit.scale;

            // Always extend through the entire wall thickness.
            finalSize.z =
                wallBounds.size.z + depthPadding * 2f;

            Mesh cutterMesh = cutters[i];

            Vector3 meshDimensions = cutterMesh.bounds.size;

            Vector3 meshScale = new Vector3(
                finalSize.x / meshDimensions.x,
                finalSize.y / meshDimensions.y,
                finalSize.z / meshDimensions.z
            );

            Quaternion rotation =
                Quaternion.Euler(part.localRotation);

            Matrix4x4 partToWall = Matrix4x4.TRS(
                finalPosition,
                rotation,
                meshScale
            );

            Matrix4x4 partToWorld =
                wall.transform.localToWorldMatrix * partToWall;

            bool success = wall.Subtract(
                cutterMesh,
                partToWorld
            );

            if (!success)
            {
                Debug.LogError(
                    $"Boolean subtraction failed for " +
                    $"{part.partName}: {wall.LastError}",
                    this
                );

                return false;
            }
        }

        hasCut = true;

        Debug.Log(
            //$"Successfully cut wall using {overridePose.poseName}!",
            this
        );

        return true;
    }

    private static Mesh GetPrimitiveMesh(BodyPartShape shape)
    {
        if (primitiveMeshes.TryGetValue(shape, out Mesh cached))
            return cached;

        PrimitiveType primitiveType = shape switch
        {
            BodyPartShape.Sphere => PrimitiveType.Sphere,
            BodyPartShape.Capsule => PrimitiveType.Capsule,
            BodyPartShape.Box => PrimitiveType.Cube,
            _ => PrimitiveType.Capsule
        };

        GameObject temp =
            GameObject.CreatePrimitive(primitiveType);

        Mesh mesh =
            temp.GetComponent<MeshFilter>().sharedMesh;

        Destroy(temp);

        if (mesh != null)
            primitiveMeshes.Add(shape, mesh);

        return mesh;
    }

    /*
    [Header("References")]
    [SerializeField] private Pose2 pose;

    [Tooltip("The wall object that has a CsgBody component.")]
    [SerializeField] private CsgBody wall;

    [Header("Cut Settings")]
    [Tooltip("Extra depth on each side of the wall.")]
    [SerializeField, Min(0.01f)]
    private float depthPadding = 0.1f;

    [SerializeField, Min(0.01f)]
    private float wallThickness = 0.2f;

    private bool hasCut;

    [ContextMenu("Cut Wall")]
    public void CutWall()
    {
        if (hasCut)
        {
            Debug.LogWarning("This wall has already been cut.", this);
            return;
        }

        if (pose == null || wall == null)
        {
            Debug.LogError(
                "Assign a HumanoidPose and a CsgBody.", this);
            return;
        }

        if (pose.bodyParts.Count == 0)
        {
            Debug.LogError("The pose contains no body parts.", this);
            return;
        }

        // Every primitive must extend through the wall.
        // The pose's part sizes determine its shape and depth.
        foreach (BodyPart part in pose.bodyParts)
        {
            Mesh cutterMesh = GetPrimitiveMesh(part.shape);

            if (cutterMesh == null)
            {
                Debug.LogError(
                    $"Could not create mesh for {part.partName}.", this);
                return;
            }

            Vector3 size = part.size * pose.overallScale;

            // Ensure the cutter passes completely through the wall.
            // Assumes the wall's local Z axis is its thickness axis.
            size.z = Mathf.Max(size.z, wallThickness + depthPadding * 2f);

            Matrix4x4 partToWall = Matrix4x4.TRS(
                part.localPosition,
                Quaternion.Euler(part.localRotation),
                size
            );

            Matrix4x4 partToWorld =
                wall.transform.localToWorldMatrix * partToWall;

            // Subtract this body part from the wall.
            bool success = wall.Subtract(cutterMesh, partToWorld);

            if (!success)
            {
                Debug.LogError(
                    $"Boolean subtraction failed for {part.partName}. " +
                    $"Reason: {wall.LastError}", this);
                return;
            }
        }

        hasCut = true;
        //Debug.Log($"Generated hole for pose: {pose.poseName}", this);
    }

    private static Mesh GetPrimitiveMesh(BodyPartShape shape)
    {
        PrimitiveType primitiveType = shape switch
        {
            BodyPartShape.Sphere => PrimitiveType.Sphere,
            BodyPartShape.Capsule => PrimitiveType.Capsule,
            BodyPartShape.Box => PrimitiveType.Cube,
            _ => PrimitiveType.Capsule
        };

        // Create a temporary primitive to obtain Unity's closed mesh.
        GameObject temp = GameObject.CreatePrimitive(primitiveType);
        MeshFilter filter = temp.GetComponent<MeshFilter>();
        Mesh mesh = filter != null ? filter.sharedMesh : null;

        // Remove the temporary object and its collider.
        Destroy(temp);

        return mesh;
    }
    */
}
