using UnityEngine;
using ManifoldCSG;

public class WallCutter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Pose pose;
    [Tooltip("Wall object should have CsgBody component in it")]
    [SerializeField] private CsgBody wall;

    [Header("Cut settings")]
    [Tooltip("Extra depth on each side of the wall")]
    [SerializeField, Min(0.01f)]
    private float depthPadding = 0.1f;

    private bool hasCut;

    [ContextMenu("Cut Wall")]
    public void CutWall()
    {
        if (hasCut)
        {
            Debug.LogWarning("Wall already cut");
            return;
        }

        if (pose == null || wall == null )
        {
            Debug.LogError("Assign a Pose and CsgBody");
            return;
        }

        if (pose.bodyParts.Count == 0)
        {
            Debug.LogError("No bodyparts");
            return;
        }

        //primitives must pass through the wall to be cut.
        //pose's part sizes determine its shape and depth.

        foreach(BodyPart part in pose.bodyParts)
        {
            Mesh cutterMesh = GetPrimitiveMesh(part.shape);
        }
    }

    private static Mesh GetPrimitiveMesh(BodyPartShape shape)
    {
        PrimitiveType primitiveType = shape switch;
        {
            BodyPartShape.Sphere => PrimitiveType.Sphere;
            BodyPartShape.Capsule => PrimitiveType.Capsule;

        }
    }

}
