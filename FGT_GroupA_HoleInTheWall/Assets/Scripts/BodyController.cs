using Mediapipe.Tasks.Vision.PoseLandmarker;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Full-body MediaPipe controller.
///
/// IMPORTANT HIERARCHY:
///
/// Pose Landmark Detection
/// ├── Main Camera
/// ├── ...
/// ├── Manager       <-- THIS SCRIPT IS HERE
/// ├── X Bot         <-- MODEL / AVATAR
/// └── Plane
///
/// The Manager receives MediaPipe data.
/// X Bot is the object that gets moved and rotated.
/// </summary>
public class BodyController : MonoBehaviour
{
    // ============================================================
    // REFERENCES
    // ============================================================

    [Header("References")]

    [SerializeField] private Player player;

    [Tooltip("Main Camera used for camera-relative movement.")]
    public Camera trackingCamera;


    // ============================================================
    // BONE TRACKING
    // ============================================================

    [Header("Bone Tracking")]

    public float smoothSpeed = 10f;

    [Tooltip("Mirror the horizontal MediaPipe movement.")]
    public bool mirrorMovement = true;

    [Range(-1.0f, 1.0f)]
    public float headOffset = 0f;


    // ============================================================
    // ENABLE / DISABLE BODY PARTS
    // ============================================================

    [Header("Enable Sections")]

    public bool enableArms = true;
    public bool enableLegs = true;
    public bool enableSpine = true;
    public bool enableHead = true;


    // ============================================================
    // ROOT MOVEMENT
    // ============================================================

    [Header("Whole Body Movement")]

    [Tooltip("Move the entire X Bot when you move.")]
    public bool enableRootMovement = true;

    [Tooltip("Left/right movement multiplier.")]
    public float movementScale = 3f;

    [Tooltip("Forward/back movement multiplier.")]
    public float depthMovementScale = 3f;

    [Tooltip("Vertical movement multiplier.")]
    public float verticalMovementScale = 0.5f;

    [Tooltip("Time that whole-body movement takes.")]
    public float rootMovementTime = .25f;

    [Tooltip("Ignore tiny movements caused by tracking noise.")]
    public float movementDeadzone = 0.01f;


    // ============================================================
    // ROOT ROTATION
    // ============================================================

    [Header("Whole Body Rotation")]

    [Tooltip("Rotate X Bot when you turn your body.")]
    public bool enableRootRotation = true;

    [Tooltip("Smoothness of whole-body rotation.")]
    public float rootRotationSmooth = 8f;

    [Tooltip("Rotation multiplier.")]
    public float rotationMultiplier = 1f;

    [Tooltip("Ignore very small body rotations.")]
    public float rotationDeadzone = 2f;


    // ============================================================
    // CALIBRATION
    // ============================================================

    [Header("Calibration")]

    [Tooltip("Automatically calibrate when the first pose is detected.")]
    public bool calibrateAutomatically = true;

    [Tooltip("Seconds to wait before automatic calibration.")]
    public float calibrationDelay = 1f;


    // ============================================================
    // DEBUG
    // ============================================================

    [Header("Debug")]

    public bool drawGizmos = false;

    public float gizmoScale = 5f;


    // ============================================================
    // INTERNAL BONE DATA
    // ============================================================

    private readonly Dictionary<HumanBodyBones, Transform> boneMap =
        new Dictionary<HumanBodyBones, Transform>();

    private readonly Dictionary<HumanBodyBones, Quaternion> bindRotations =
        new Dictionary<HumanBodyBones, Quaternion>();

    private readonly Dictionary<HumanBodyBones, Vector3> bindDirections =
        new Dictionary<HumanBodyBones, Vector3>();


    // ============================================================
    // MEDIAPIPE DATA
    // ============================================================

    private readonly object poseLock = new object();

    private List<Vector3> pendingLandmarks;

    private bool poseReady = false;

    private List<Vector3> currentLandmarks =
        new List<Vector3>();


    // ============================================================
    // CALIBRATION DATA
    // ============================================================

    private Vector3 initialHipPosition;

    private Vector3 initialBodyForward;

    private Vector3 initialRootPosition;

    private Quaternion initialRootRotation;

    private bool calibrated = false;

    public static UnityEvent<Player> AddPlayerToBodyControllerEvent = new UnityEvent<Player>();

    private void Awake()
    {
        AddPlayerToBodyControllerEvent.AddListener((_player) =>
        {
            player = _player;
            print(player);
        });

        GameManager.InitBodyControllerEvent.AddListener(StartGame);
    }
    // ============================================================
    // START
    // ============================================================

    private void StartGame()
    {
        // -----------------------------
        // Check Animator
        // -----------------------------

        if (player.Animator == null)
        {
            Debug.LogError(
                "[BodyController] Animator is not assigned!"
            );

            return;
        }


        // -----------------------------
        // Check Model Root
        // -----------------------------

        if (player.modelRoot == null)
        {
            Debug.LogError(
                "[BodyController] Model Root is not assigned!"
            );

            return;
        }


        // -----------------------------
        // Camera
        // -----------------------------

        if (trackingCamera == null)
        {
            trackingCamera = Camera.main;

            if (trackingCamera == null)
            {
                Debug.LogWarning(
                    "[BodyController] Tracking Camera not assigned and " +
                    "Camera.main could not be found."
                );
            }
        }


        // -----------------------------
        // Cache bones
        // -----------------------------

        CacheBones();


        // -----------------------------
        // Capture T-pose
        // -----------------------------

        CaptureBindPose();


        // -----------------------------
        // Save X Bot starting transform
        // -----------------------------

        initialRootPosition = player.modelRoot.position;

        initialRootRotation = player.modelRoot.rotation;


        // -----------------------------
        // Automatic calibration
        // -----------------------------

        if (calibrateAutomatically)
        {
            StartCoroutine(CalibrateAfterDelay());
        }


        Debug.Log(
            "[BodyController] Initialized successfully."
        );
    }


    // ============================================================
    // AUTOMATIC CALIBRATION
    // ============================================================

    private IEnumerator CalibrateAfterDelay()
    {
        yield return new WaitForSeconds(calibrationDelay);


        while (currentLandmarks == null ||
               currentLandmarks.Count < 33)
        {
            yield return null;
        }


        Calibrate();
    }


    // ============================================================
    // CALIBRATE
    // ============================================================

    public void Calibrate()
    {
        //Make this return Bool so that GameManager can get info if cannot calibrate => dont start game

        if (currentLandmarks == null ||
            currentLandmarks.Count < 33)
        {
            Debug.LogWarning(
                "[BodyController] Cannot calibrate. " +
                "No pose detected."
            );

            return;
        }


        // Current hip position
        initialHipPosition = Mid(23, 24);


        // Current body facing direction
        initialBodyForward =
            CalculateBodyForwardWorld();


        // Save X Bot position
        initialRootPosition =
            player.modelRoot.position;


        // Save X Bot rotation
        initialRootRotation =
            player.modelRoot.rotation;


        calibrated = true;


        Debug.Log(
            "[BodyController] Calibration complete."
        );
    }


    // ============================================================
    // CACHE HUMANOID BONES
    // ============================================================

    private void CacheBones()
    {
        HumanBodyBones[] bones =
        {
            HumanBodyBones.Hips,

            HumanBodyBones.Spine,

            HumanBodyBones.Chest,

            HumanBodyBones.Neck,

            HumanBodyBones.Head,

            HumanBodyBones.LeftUpperArm,

            HumanBodyBones.LeftLowerArm,

            HumanBodyBones.LeftHand,

            HumanBodyBones.RightUpperArm,

            HumanBodyBones.RightLowerArm,

            HumanBodyBones.RightHand,

            HumanBodyBones.LeftUpperLeg,

            HumanBodyBones.LeftLowerLeg,

            HumanBodyBones.LeftFoot,

            HumanBodyBones.RightUpperLeg,

            HumanBodyBones.RightLowerLeg,

            HumanBodyBones.RightFoot
        };


        foreach (HumanBodyBones bone in bones)
        {
            Transform t =
                player.Animator.GetBoneTransform(bone);


            if (t != null)
            {
                boneMap[bone] = t;
            }
            else
            {
                Debug.LogWarning(
                    "[BodyController] Bone not found: " +
                    bone
                );
            }
        }
    }


    // ============================================================
    // CAPTURE T-POSE
    // ============================================================

    private void CaptureBindPose()
    {
        (
            HumanBodyBones A,
            HumanBodyBones B
        )[] pairs =
        {
            (
                HumanBodyBones.Hips,
                HumanBodyBones.Spine
            ),

            (
                HumanBodyBones.Spine,
                HumanBodyBones.Chest
            ),

            (
                HumanBodyBones.Chest,
                HumanBodyBones.Neck
            ),

            (
                HumanBodyBones.Neck,
                HumanBodyBones.Head
            ),

            (
                HumanBodyBones.LeftUpperArm,
                HumanBodyBones.LeftLowerArm
            ),

            (
                HumanBodyBones.LeftLowerArm,
                HumanBodyBones.LeftHand
            ),

            (
                HumanBodyBones.RightUpperArm,
                HumanBodyBones.RightLowerArm
            ),

            (
                HumanBodyBones.RightLowerArm,
                HumanBodyBones.RightHand
            ),

            (
                HumanBodyBones.LeftUpperLeg,
                HumanBodyBones.LeftLowerLeg
            ),

            (
                HumanBodyBones.LeftLowerLeg,
                HumanBodyBones.LeftFoot
            ),

            (
                HumanBodyBones.RightUpperLeg,
                HumanBodyBones.RightLowerLeg
            ),

            (
                HumanBodyBones.RightLowerLeg,
                HumanBodyBones.RightFoot
            )
        };


        foreach (var pair in pairs)
        {
            if (!boneMap.TryGetValue(
                    pair.A,
                    out Transform tA))
            {
                continue;
            }


            if (!boneMap.TryGetValue(
                    pair.B,
                    out Transform tB))
            {
                continue;
            }


            Vector3 direction =
                (tB.position - tA.position).normalized;


            if (direction == Vector3.zero)
            {
                continue;
            }


            bindDirections[pair.A] =
                direction;

            bindRotations[pair.A] =
                tA.rotation;
        }
    }


    // ============================================================
    // ENABLE
    // ============================================================

    private void OnEnable()
    {
        StartCoroutine(WaitForRunner());
    }


    // ============================================================
    // WAIT FOR MEDIAPIPE
    // ============================================================

    private IEnumerator WaitForRunner()
    {
        while (
            PoseLandmarkerRunner.Instance == null
        )
        {
            yield return null;
        }


        PoseLandmarkerRunner.Instance.OnResult +=
            OnPoseResult;


        Debug.Log(
            "[BodyController] Connected to PoseLandmarkerRunner."
        );
    }


    // ============================================================
    // DISABLE
    // ============================================================

    private void OnDisable()
    {
        if (
            PoseLandmarkerRunner.Instance != null
        )
        {
            PoseLandmarkerRunner.Instance.OnResult -=
                OnPoseResult;
        }
    }


    // ============================================================
    // MEDIA PIPE RESULT
    // ============================================================

    private void OnPoseResult(
        PoseLandmarkerResult result)
    {
        if (
            result.poseLandmarks == null ||
            result.poseLandmarks.Count == 0
        )
        {
            return;
        }


        var raw =
            result.poseLandmarks[0].landmarks;


        if (
            raw == null ||
            raw.Count < 33
        )
        {
            return;
        }


        List<Vector3> mapped =
            new List<Vector3>(raw.Count);


        foreach (var lm in raw)
        {
            /*
             * Convert MediaPipe image coordinates
             * to our tracking coordinate system.
             *
             * X:
             * left/right
             *
             * Y:
             * bottom/top
             *
             * Z:
             * depth
             */

            mapped.Add(
                new Vector3(
                    mirrorMovement
                        ? 1f - lm.x
                        : lm.x,

                    1f - lm.y,

                    -lm.z
                )
            );
        }


        lock (poseLock)
        {
            pendingLandmarks =
                mapped;

            poseReady =
                true;
        }
    }


    // ============================================================
    // UPDATE
    // ============================================================

    private void Update()
    {
        // -----------------------------
        // Get newest MediaPipe pose
        // -----------------------------

        lock (poseLock)
        {
            if (!poseReady)
            {
                return;
            }


            currentLandmarks =
                pendingLandmarks;


            poseReady =
                false;
        }


        if (
            currentLandmarks == null ||
            currentLandmarks.Count < 33
        )
        {
            return;
        }


        // -----------------------------
        // Auto calibration
        // -----------------------------

        if (
            !calibrated &&
            calibrateAutomatically
        )
        {
            Calibrate();
        }


        // -----------------------------
        // Move X Bot
        // -----------------------------

        if (
            enableRootMovement &&
            calibrated
        )
        {
            ApplyRootMovement();
        }


        // -----------------------------
        // Rotate X Bot
        // -----------------------------

        if (
            enableRootRotation &&
            calibrated
        )
        {
            ApplyRootRotation();
        }


        // -----------------------------
        // Move individual bones
        // -----------------------------

        ApplyPose();
    }


    // ============================================================
    // ROOT MOVEMENT
    // ============================================================

    Coroutine moveCoroutine;
    private void ApplyRootMovement()
    {
        Vector3 currentHip =
            Mid(23, 24);


        /*
         * Difference between the position
         * where we calibrated and where
         * the user is now.
         */

        Vector3 movement =
            currentHip - initialHipPosition;


        // -----------------------------
        // Deadzone
        // -----------------------------

        if (
            Mathf.Abs(movement.x) <
            movementDeadzone
        )
        {
            movement.x = 0f;
        }


        if (
            Mathf.Abs(movement.y) <
            movementDeadzone
        )
        {
            movement.y = 0f;
        }


        if (
            Mathf.Abs(movement.z) <
            movementDeadzone
        )
        {
            movement.z = 0f;
        }


        /*
         * MediaPipe X:
         *
         * horizontal camera movement
         *
         * MediaPipe Z:
         *
         * distance from camera
         */


        Vector3 worldMovement =
            ConvertTrackingMovementToWorld(
                movement
            );


        // Apply individual scales

        worldMovement.x *= movementScale;

        worldMovement.z *= depthMovementScale;

        worldMovement.y *= verticalMovementScale;


        // Target X Bot position

        Vector3 targetPosition =
            initialRootPosition +
            worldMovement;


        // Smooth movement
        if(moveCoroutine != null && targetPosition != Vector3.zero)
            StopCoroutine( moveCoroutine );
       
        moveCoroutine = StartCoroutine(SmoothMove(player.modelRoot.position, targetPosition, rootMovementTime));

        //player.modelRoot.position =
        //    Vector3.Lerp(
        //        player.modelRoot.position,
        //        targetPosition,
        //        Time.deltaTime *
        //        rootMovementSmooth
        //    );
    }

    private IEnumerator SmoothMove(Vector3 _curPos, Vector3 _targetPos, float _duration)
    {
        float _timePassed = 0f;
        while(_timePassed < _duration)
        {
            _timePassed += Time.deltaTime;

            float _percent = _timePassed / _duration;
            player.modelRoot.position = Vector3.Lerp(_curPos, _targetPos, _percent);
            yield return null;
        }
        player.modelRoot.position = _targetPos;

    }


    // ============================================================
    // CAMERA RELATIVE MOVEMENT
    // ============================================================

    private Vector3 ConvertTrackingMovementToWorld(
        Vector3 movement)
    {
        /*
         * If there is no camera,
         * use normal Unity axes.
         */

        if (trackingCamera == null)
        {
            return new Vector3(
                movement.x,
                movement.y,
                -movement.z
            );
        }


        /*
         * Camera right.
         *
         * Positive X means movement toward
         * the right side of the camera.
         */

        Vector3 cameraRight =
            trackingCamera.transform.right;


        /*
         * Camera forward.
         *
         * Our mapped MediaPipe Z is positive
         * when the user moves toward the camera.
         *
         * Therefore we use NEGATIVE camera.forward.
         */

        Vector3 towardCamera =
            -trackingCamera.transform.forward;


        /*
         * Keep horizontal movement horizontal.
         */

        cameraRight.y = 0f;

        towardCamera.y = 0f;


        cameraRight.Normalize();

        towardCamera.Normalize();


        Vector3 worldMovement =
            cameraRight * movement.x;


        worldMovement +=
            towardCamera * movement.z;


        /*
         * Vertical movement remains world vertical.
         */

        worldMovement.y =
            movement.y;


        return worldMovement;
    }


    // ============================================================
    // ROOT ROTATION
    // ============================================================

    private void ApplyRootRotation()
    {
        Vector3 currentForward =
            CalculateBodyForwardWorld();


        if (
            currentForward.sqrMagnitude <
            0.0001f
        )
        {
            return;
        }


        currentForward.y = 0f;

        currentForward.Normalize();


        Vector3 startingForward =
            initialBodyForward;


        startingForward.y = 0f;

        startingForward.Normalize();


        /*
         * Calculate how many degrees
         * the user turned.
         */

        float angle =
            Vector3.SignedAngle(
                startingForward,
                currentForward,
                Vector3.up
            );


        /*
         * Ignore tiny tracking noise.
         */

        if (
            Mathf.Abs(angle) <
            rotationDeadzone
        )
        {
            angle = 0f;
        }


        angle *=
            rotationMultiplier;


        Quaternion targetRotation =
            initialRootRotation *
            Quaternion.Euler(
                0f,
                angle,
                0f
            );


        player.modelRoot.rotation =
            Quaternion.Slerp(
                player.modelRoot.rotation,
                targetRotation,
                Time.deltaTime *
                rootRotationSmooth
            );
    }


    // ============================================================
    // CALCULATE BODY FORWARD
    // ============================================================

    private Vector3 CalculateBodyForwardWorld()
    {
        /*
         * Get shoulders.
         */

        Vector3 leftShoulder =
            Lm(11);

        Vector3 rightShoulder =
            Lm(12);


        /*
         * Get hips.
         */

        Vector3 leftHip =
            Lm(23);

        Vector3 rightHip =
            Lm(24);


        /*
         * Body right direction.
         */

        Vector3 shoulderRight =
            rightShoulder -
            leftShoulder;


        Vector3 hipRight =
            rightHip -
            leftHip;


        Vector3 bodyRight =
            (
                shoulderRight +
                hipRight
            ) * 0.5f;


        bodyRight.Normalize();


        /*
         * Body up direction.
         */

        Vector3 hipCenter =
            (
                leftHip +
                rightHip
            ) * 0.5f;


        Vector3 shoulderCenter =
            (
                leftShoulder +
                rightShoulder
            ) * 0.5f;


        Vector3 bodyUp =
            (
                shoulderCenter -
                hipCenter
            ).normalized;


        /*
         * Calculate forward.
         */

        Vector3 trackingForward =
            Vector3.Cross(
                bodyRight,
                bodyUp
            ).normalized;


        /*
         * Convert tracking coordinates
         * to Unity world coordinates.
         */

        Vector3 worldForward;


        if (trackingCamera != null)
        {
            Vector3 cameraRight =
                trackingCamera.transform.right;

            Vector3 cameraUp =
                trackingCamera.transform.up;

            /*
             * Our tracking Z is opposite the
             * camera's normal forward direction.
             */

            Vector3 cameraDepth =
                -trackingCamera.transform.forward;


            worldForward =
                cameraRight *
                trackingForward.x;

            worldForward +=
                cameraUp *
                trackingForward.y;

            worldForward +=
                cameraDepth *
                trackingForward.z;
        }
        else
        {
            worldForward =
                new Vector3(
                    trackingForward.x,
                    trackingForward.y,
                    -trackingForward.z
                );
        }


        worldForward.y = 0f;


        if (
            worldForward.sqrMagnitude <
            0.0001f
        )
        {
            return Vector3.forward;
        }


        worldForward.Normalize();


        return worldForward;
    }


    // ============================================================
    // BODY BONE POSE
    // ============================================================

    private void ApplyPose()
    {
        if (
            currentLandmarks == null ||
            currentLandmarks.Count < 33
        )
        {
            return;
        }


        Vector3 midHip =
            Mid(23, 24);


        Vector3 midShoulder =
            Mid(11, 12);


        Vector3 midHead =
            Lm(0) +
            new Vector3(
                0f,
                0f,
                headOffset
            );


        // ========================================================
        // SPINE
        // ========================================================

        if (enableSpine)
        {
            RotateBone(
                HumanBodyBones.Hips,
                midHip,
                midShoulder
            );


            RotateBone(
                HumanBodyBones.Spine,
                midHip,
                midShoulder
            );
        }


        // ========================================================
        // ARMS
        // ========================================================

        if (enableArms)
        {
            // Left upper arm

            RotateBone(
                HumanBodyBones.LeftUpperArm,
                Lm(11),
                Lm(13)
            );


            // Left forearm

            RotateBone(
                HumanBodyBones.LeftLowerArm,
                Lm(13),
                Lm(15)
            );


            // Right upper arm

            RotateBone(
                HumanBodyBones.RightUpperArm,
                Lm(12),
                Lm(14)
            );


            // Right forearm

            RotateBone(
                HumanBodyBones.RightLowerArm,
                Lm(14),
                Lm(16)
            );
        }


        // ========================================================
        // LEGS
        // ========================================================

        if (enableLegs)
        {
            // Left upper leg

            RotateBone(
                HumanBodyBones.LeftUpperLeg,
                Lm(23),
                Lm(25)
            );


            // Left lower leg

            RotateBone(
                HumanBodyBones.LeftLowerLeg,
                Lm(25),
                Lm(27)
            );


            // Right upper leg

            RotateBone(
                HumanBodyBones.RightUpperLeg,
                Lm(24),
                Lm(26)
            );


            // Right lower leg

            RotateBone(
                HumanBodyBones.RightLowerLeg,
                Lm(26),
                Lm(28)
            );
        }


        // ========================================================
        // HEAD / CHEST
        // ========================================================

        if (enableHead)
        {
            RotateBone(
                HumanBodyBones.Chest,
                midShoulder,
                midHead
            );
        }
    }


    // ============================================================
    // ROTATE BONE
    // ============================================================

    private void RotateBone(
        HumanBodyBones bone,
        Vector3 from,
        Vector3 to)
    {
        if (
            !boneMap.TryGetValue(
                bone,
                out Transform boneTransform)
        )
        {
            return;
        }


        if (
            !bindDirections.TryGetValue(
                bone,
                out Vector3 bindDirection)
        )
        {
            return;
        }


        if (
            !bindRotations.TryGetValue(
                bone,
                out Quaternion bindRotation)
        )
        {
            return;
        }


        Vector3 targetDirection =
            (
                to -
                from
            ).normalized;


        if (
            targetDirection == Vector3.zero
        )
        {
            return;
        }


        /*
         * Find the rotation from the
         * T-pose direction to the
         * detected direction.
         */

        Quaternion delta =
            Quaternion.FromToRotation(
                bindDirection,
                targetDirection
            );


        Quaternion targetRotation =
            delta *
            bindRotation;


        /*
         * Smoothly rotate the bone.
         */

        boneTransform.rotation =
            Quaternion.Slerp(
                boneTransform.rotation,
                targetRotation,
                Time.deltaTime *
                smoothSpeed
            );
    }


    // ============================================================
    // LANDMARK HELPERS
    // ============================================================

    private Vector3 Lm(int index)
    {
        return currentLandmarks[index];
    }


    private Vector3 Mid(
        int a,
        int b)
    {
        return (
            Lm(a) +
            Lm(b)
        ) * 0.5f;
    }


    // ============================================================
    // DEBUG GIZMOS
    // ============================================================

    private void OnDrawGizmos()
    {
        if (
            !drawGizmos ||
            currentLandmarks == null ||
            currentLandmarks.Count < 33
        )
        {
            return;
        }


        Vector3 hip =
            Mid(23, 24);


        Vector3 shoulders =
            Mid(11, 12);


        Vector3 head =
            Mid(7, 8);


        // Hip

        Gizmos.color =
            Color.white;

        Gizmos.DrawSphere(
            transform.position +
            hip * gizmoScale,
            0.05f
        );


        // Shoulders

        Gizmos.color =
            Color.yellow;

        Gizmos.DrawSphere(
            transform.position +
            shoulders * gizmoScale,
            0.05f
        );


        // Head

        Gizmos.color =
            Color.red;

        Gizmos.DrawSphere(
            transform.position +
            head * gizmoScale,
            0.05f
        );


        // Body forward

        if (calibrated)
        {
            Vector3 forward =
                CalculateBodyForwardWorld();


            Gizmos.color =
                Color.green;


            Gizmos.DrawLine(
                player.modelRoot != null
                    ? player.modelRoot.position
                    : transform.position,

                (player.modelRoot != null
                    ? player.modelRoot.position
                    : transform.position)
                + forward
            );
        }
    }
}