using UnityEngine;
using ManifoldCSG;

public class WallCutter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Pose pose;

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

}
