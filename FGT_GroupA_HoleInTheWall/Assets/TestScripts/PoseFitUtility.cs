
using UnityEngine;

public static class PoseFitUtility
{
    public struct FitResult
    {
        public float scale;
        public Vector3 offset;
        public Bounds poseBounds;
    }

    public static bool CalculateFit(
        Pose2 pose,
        Bounds wallBounds,
        float wallMargin,
        out FitResult result)
    {
        result = new FitResult();

        if (pose == null || pose.bodyParts == null ||
            pose.bodyParts.Count == 0)
            return false;

        wallMargin = Mathf.Clamp(wallMargin, 0f, 0.25f);

        float minX = float.MaxValue;
        float maxX = float.MinValue;

        float minRestrictedY = float.MaxValue;
        float maxY = float.MinValue;

        bool hasRestrictedPart = false;

        Bounds combinedBounds = new Bounds();
        bool initialized = false;

        foreach (BodyPart part in pose.bodyParts)
        {
            if (part == null)
                continue;

            Vector3 half = part.size * 0.5f;

            Quaternion rotation =
                Quaternion.Euler(part.localRotation);

            Vector3 right = rotation * Vector3.right;
            Vector3 up = rotation * Vector3.up;
            Vector3 forward = rotation * Vector3.forward;

            // Calculate rotation-aware bounding box.
            Vector3 extent = new Vector3(
                Mathf.Abs(right.x) * half.x +
                Mathf.Abs(up.x) * half.y +
                Mathf.Abs(forward.x) * half.z,

                Mathf.Abs(right.y) * half.x +
                Mathf.Abs(up.y) * half.y +
                Mathf.Abs(forward.y) * half.z,

                Mathf.Abs(right.z) * half.x +
                Mathf.Abs(up.z) * half.y +
                Mathf.Abs(forward.z) * half.z
            );

            Bounds partBounds = new Bounds(
                part.localPosition,
                extent * 2f
            );

            if (!initialized)
            {
                combinedBounds = partBounds;
                initialized = true;
            }
            else
            {
                combinedBounds.Encapsulate(partBounds);
            }

            minX = Mathf.Min(
                minX,
                part.localPosition.x - extent.x
            );

            maxX = Mathf.Max(
                maxX,
                part.localPosition.x + extent.x
            );

            maxY = Mathf.Max(
                maxY,
                part.localPosition.y + extent.y
            );

            // Only restricted parts determine the bottom boundary.
            if (!part.allowBottomOverflow)
            {
                minRestrictedY = Mathf.Min(
                    minRestrictedY,
                    part.localPosition.y - extent.y
                );

                hasRestrictedPart = true;
            }
        }

        if (!initialized)
            return false;

        // Fallback if every part allows bottom overflow.
        if (!hasRestrictedPart)
            minRestrictedY = combinedBounds.min.y;

        float poseWidth = maxX - minX;
        float poseHeight = maxY - minRestrictedY;

        float availableWidth =
            wallBounds.size.x * (1f - wallMargin * 2f);

        float availableHeight =
            wallBounds.size.y * (1f - wallMargin * 2f);

        if (poseWidth <= 0f || poseHeight <= 0f ||
            availableWidth <= 0f || availableHeight <= 0f)
            return false;

        float fitScale = Mathf.Min(
            availableWidth / poseWidth,
            availableHeight / poseHeight
        );

        // Clamp overall scale so it cannot exceed wall fit.
        float finalScale =
            fitScale * Mathf.Clamp(pose.overallScale, 0.01f, 1f);

        Vector3 offset = new Vector3(
            wallBounds.center.x -
            (minX + maxX) * 0.5f * finalScale,

            wallBounds.min.y +
            wallBounds.size.y * wallMargin -
            minRestrictedY * finalScale,

            wallBounds.center.z
        );

        result.scale = finalScale;
        result.offset = offset;
        result.poseBounds = combinedBounds;

        return true;
    }
}
