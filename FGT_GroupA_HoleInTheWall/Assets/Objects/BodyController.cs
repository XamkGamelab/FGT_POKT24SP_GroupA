/// -----------------------------------------------------------------------------
/// Credits: Prabalabs
/// YouTube: www.youtube.com/@PrabaLabs
/// -----------------------------------------------------------------------------

using Mediapipe.Tasks.Vision.PoseLandmarker;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Drives a Unity Humanoid Animator to imitate the pose detected by PoseLandmarkerRunner.
///
/// How it works:
///   1. On Start, the character must be in T-pose. We record each bone's world rotation
///      and the direction it "points" toward its child bone (bind pose capture).
///   2. MediaPipe results arrive on a background thread. We only store them there (thread-safe).
///   3. In Update (main thread), we compute the rotation delta from the bind pose to the
///      current landmark direction, then Slerp each bone toward the target.
/// </summary>
public class BodyController : MonoBehaviour
{
    [Header("References")]
    public Animator animator;

    [Header("Settings")]
    public float smoothSpeed = 10f;
    public bool mirrorMovement = true;

    [Header("Enable Sections")]
    public bool enableArms = true;
    public bool enableLegs = true;
    public bool enableSpine = true;

    [Header("Debug")]
    public bool drawGizmos = false;
    public float gizmoScale = 5f;

    // Bone transforms and their bind-pose data
    private readonly Dictionary<HumanBodyBones, Transform> boneMap = new();
    private readonly Dictionary<HumanBodyBones, Quaternion> bindRotations = new();
    private readonly Dictionary<HumanBodyBones, Vector3> bindDirections = new();

    // Thread-safe landmark buffer
    private readonly object poseLock = new();
    private List<Vector3> pendingLandmarks;
    private bool poseReady = false;

    // Current frame landmarks (main thread only)
    private List<Vector3> currentLandmarks = new();

    private void Start()
    {
        if (animator == null)
        {
            Debug.LogError("[BodyController] Animator not assigned!");
            return;
        }

        CacheBones();
        CaptureBindPose();
    }

    private void CacheBones()
    {
        HumanBodyBones[] bones =
        {
            HumanBodyBones.Hips,
            HumanBodyBones.Spine,
            HumanBodyBones.Chest,
            HumanBodyBones.Head,
            HumanBodyBones.LeftUpperArm,  HumanBodyBones.LeftLowerArm,  HumanBodyBones.LeftHand,
            HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand,
            HumanBodyBones.LeftUpperLeg,  HumanBodyBones.LeftLowerLeg,  HumanBodyBones.LeftFoot,
            HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot,
        };

        foreach (var bone in bones)
        {
            Transform t = animator.GetBoneTransform(bone);
            if (t != null)
                boneMap[bone] = t;
            else
                Debug.LogWarning($"[BodyController] Bone not found in rig: {bone}");
        }
    }

    /// <summary>
    /// Records each bone's world rotation and the direction it naturally points
    /// toward its child bone while the character is in T-pose (called at Start).
    /// This is used later to compute rotation deltas at runtime.
    /// </summary>
    private void CaptureBindPose()
    {
        // Each pair: (this bone, child bone) — defines the bone's "pointing" direction.
        // The order matters: A must be the parent, B the child in the skeleton.
        (HumanBodyBones A, HumanBodyBones B)[] pairs =
        {
            (HumanBodyBones.Hips,         HumanBodyBones.Spine),
            (HumanBodyBones.Spine,        HumanBodyBones.Chest),
            (HumanBodyBones.Chest,        HumanBodyBones.Head),
            (HumanBodyBones.LeftUpperArm,  HumanBodyBones.LeftLowerArm),
            (HumanBodyBones.LeftLowerArm,  HumanBodyBones.LeftHand),
            (HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm),
            (HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand),
            (HumanBodyBones.LeftUpperLeg,  HumanBodyBones.LeftLowerLeg),
            (HumanBodyBones.LeftLowerLeg,  HumanBodyBones.LeftFoot),
            (HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg),
            (HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot),
        };

        foreach (var (A, B) in pairs)
        {
            if (!boneMap.TryGetValue(A, out Transform tA)) continue;
            if (!boneMap.TryGetValue(B, out Transform tB)) continue;

            Vector3 dir = (tB.position - tA.position).normalized;
            if (dir == Vector3.zero) continue;

            bindDirections[A] = dir;
            bindRotations[A] = tA.rotation;
        }
    }

    private void OnEnable()
    {
        StartCoroutine(WaitForRunner());
    }

    private IEnumerator WaitForRunner()
    {
        while (PoseLandmarkerRunner.Instance == null)
            yield return null;

        PoseLandmarkerRunner.Instance.OnResult += OnPoseResult;
        Debug.Log("[BodyController] Subscribed to PoseLandmarkerRunner.");
    }

    private void OnDisable()
    {
        if (PoseLandmarkerRunner.Instance != null)
            PoseLandmarkerRunner.Instance.OnResult -= OnPoseResult;
    }

    /// <summary>
    /// Called on the MediaPipe background thread.
    /// IMPORTANT: Never access or modify Unity transforms here — only buffer the data.
    /// </summary>
    private void OnPoseResult(PoseLandmarkerResult result)
    {
        if (result.poseLandmarks == null || result.poseLandmarks.Count == 0) return;
        var raw = result.poseLandmarks[0].landmarks;
        if (raw == null || raw.Count < 33) return;

        var mapped = new List<Vector3>(raw.Count);
        foreach (var lm in raw)
        {
            // Convert from MediaPipe image space to Unity-friendly space:
            //   x: 0=left → 1=right (mirror optional)
            //   y: 0=bottom → 1=top  (flip from image coords)
            //   z: negate so depth is positive into the screen
            mapped.Add(new Vector3(
                mirrorMovement ? 1f - lm.x : lm.x,
                1f - lm.y,
                -lm.z
            ));
        }

        lock (poseLock)
        {
            pendingLandmarks = mapped;
            poseReady = true;
        }
    }

    /// <summary>
    /// Runs on the main thread — safe to read and write Unity transforms.
    /// </summary>
    private void Update()
    {
        lock (poseLock)
        {
            if (!poseReady) return;
            currentLandmarks = pendingLandmarks;
            poseReady = false;
        }

        ApplyPose();
    }

    // Shorthand helpers
    private Vector3 Lm(int i) => currentLandmarks[i];
    private Vector3 Mid(int a, int b) => (Lm(a) + Lm(b)) * 0.5f;

    /// <summary>
    /// MediaPipe Pose landmark indices used below:
    ///   0  = nose       7/8  = ears
    ///   11 = L shoulder 12   = R shoulder
    ///   13 = L elbow    14   = R elbow
    ///   15 = L wrist    16   = R wrist
    ///   23 = L hip      24   = R hip
    ///   25 = L knee     26   = R knee
    ///   27 = L ankle    28   = R ankle
    /// </summary>
    private void ApplyPose()
    {
        if (currentLandmarks.Count < 33) return;

        Vector3 midHip = Mid(23, 24);
        Vector3 midShoulder = Mid(11, 12);

        if (enableSpine)
        {
            // Spine/Hips: direction from hips up toward shoulders
            RotateBone(HumanBodyBones.Hips, midHip, midShoulder);
            RotateBone(HumanBodyBones.Spine, midHip, midShoulder);
            // Chest: direction from shoulders toward head (nose used as head proxy)
            RotateBone(HumanBodyBones.Chest, midShoulder, Lm(0));
        }

        if (enableArms)
        {
            RotateBone(HumanBodyBones.LeftUpperArm, Lm(11), Lm(13));
            RotateBone(HumanBodyBones.LeftLowerArm, Lm(13), Lm(15));
            RotateBone(HumanBodyBones.RightUpperArm, Lm(12), Lm(14));
            RotateBone(HumanBodyBones.RightLowerArm, Lm(14), Lm(16));
        }

        if (enableLegs)
        {
            RotateBone(HumanBodyBones.LeftUpperLeg, Lm(23), Lm(25));
            RotateBone(HumanBodyBones.LeftLowerLeg, Lm(25), Lm(27));
            RotateBone(HumanBodyBones.RightUpperLeg, Lm(24), Lm(26));
            RotateBone(HumanBodyBones.RightLowerLeg, Lm(26), Lm(28));
        }
    }

    /// <summary>
    /// Rotates a bone so its "pointing" direction aligns with (to - from),
    /// relative to its recorded T-pose orientation (bind pose).
    ///
    /// Formula:
    ///   delta = FromToRotation(bindDirection, targetDirection)
    ///   targetRotation = delta * bindRotation
    /// </summary>
    private void RotateBone(HumanBodyBones bone, Vector3 from, Vector3 to)
    {
        if (!boneMap.TryGetValue(bone, out Transform t)) return;
        if (!bindDirections.TryGetValue(bone, out Vector3 bindDir)) return;
        if (!bindRotations.TryGetValue(bone, out Quaternion bindRot)) return;

        Vector3 targetDir = (to - from).normalized;
        if (targetDir == Vector3.zero) return;

        // Compute how much the target direction has rotated from the bind direction,
        // then apply that same rotation on top of the T-pose bone rotation.
        Quaternion delta = Quaternion.FromToRotation(bindDir, targetDir);
        Quaternion targetRot = delta * bindRot;

        t.rotation = Quaternion.Slerp(t.rotation, targetRot, Time.deltaTime * smoothSpeed);
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos || currentLandmarks == null || currentLandmarks.Count == 0) return;

        Gizmos.color = Color.green;
        foreach (var lm in currentLandmarks)
        {
            Vector3 world = new Vector3(
                (lm.x - 0.5f) * gizmoScale,
                 lm.y * gizmoScale,
                 lm.z * gizmoScale
            );
            Gizmos.DrawSphere(world, 0.05f);
        }
    }
}