using Mediapipe.Tasks.Vision.PoseLandmarker;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public enum HumanTrackingPoint
{
    Head = 0,
    LeftShoulder = 11,
    RightShoulder = 12,
    LeftElbow = 13,
    RightElbow = 14,
    LeftWrist = 15,
    RightWrist = 16,
    LeftHip = 23,
    RightHip = 24,
    LeftKnee = 25,
    RightKnee = 26,
    LeftAnkle = 27,
    RightAnkle = 28
}

public class PoseRouter : SingletonMono<PoseRouter>
{
    [Tooltip("Mirror the horizontal MediaPipe movement.")]
    [SerializeField] private bool mirrorMovement = true;
    private readonly object poseLock = new object();

    private List<Vector3> pendingLandmarks;
    private readonly Dictionary<HumanTrackingPoint, Vector3> trackingPoints = new Dictionary<HumanTrackingPoint, Vector3>();

    private bool poseReady = false;
    private int poseVersion;
    private float capturedHipX;

    BodyController bodyController;
    public static UnityEvent<Player> AddPlayerToBodyControllerEvent = new UnityEvent<Player>();

    [Header("Play Area Calibration")]
    [SerializeField] private float calibrationStandTime = 1f;
    [SerializeField] private float calibrationPositionThreshold = 0.02f;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        AddPlayerToBodyControllerEvent.AddListener((_player) =>
        {
            bodyController = _player.GetComponent<BodyController>();
        });
    }

    private void OnEnable()
    {
        StartCoroutine(WaitForRunner());
    }

    public IEnumerator HandleBodyCalibration()
    {
        Debug.Log("[PoseRouter] Waiting for the body controller to initialize.");
        yield return new WaitUntil(() => bodyController != null);

        bodyController.calibrateAutomatically = false;
        yield return bodyController.HandleStartCalibrationCoroutine();

        Debug.Log($"[PoseRouter] Move to the left edge of the play area and hold still for {calibrationStandTime:F1} seconds.");
        // CanvasManager.ShowCanvas.Invoke("MoveLeft");
        yield return CaptureStableHipX();
        float leftX = capturedHipX;
        Debug.Log($"[PoseRouter] Left position captured at tracking X={leftX:F3}.");

        Debug.Log($"[PoseRouter] Move to the right edge of the play area and hold still for {calibrationStandTime:F1} seconds.");
        // CanvasManager.ShowCanvas.Invoke("MoveRight");
        yield return CaptureStableHipX();
        float rightX = capturedHipX;
        Debug.Log($"[PoseRouter] Right position captured at tracking X={rightX:F3}.");

        if (bodyController.CalibratePlayArea(leftX, rightX))
        {
            Debug.Log("[PoseRouter] Play area calibration succeeded.");
        }
        else
        {
            Debug.LogError("[PoseRouter] Play area calibration failed.");
        }
    }

    private IEnumerator CaptureStableHipX()
    {
        float stablePosition = 0f;
        float positionSum = 0f;
        float stableTime = 0f;
        int sampleCount = 0;
        int lastPoseVersion = -1;
        bool hasStablePosition = false;

        while (!hasStablePosition || stableTime < calibrationStandTime)
        {
            float currentHipX;
            int currentPoseVersion;
            if (TryGetHipX(out currentHipX, out currentPoseVersion) && currentPoseVersion != lastPoseVersion)
            {
                if (!hasStablePosition || Mathf.Abs(currentHipX - stablePosition) > calibrationPositionThreshold)
                {
                    stablePosition = currentHipX;
                    stableTime = 0f;
                    positionSum = currentHipX;
                    sampleCount = 1;
                    hasStablePosition = true;
                }
                else
                {
                    stableTime += Time.deltaTime;
                    positionSum += currentHipX;
                    sampleCount++;
                }

                lastPoseVersion = currentPoseVersion;
            }

            yield return null;
        }

        capturedHipX = positionSum / sampleCount;
    }

    private bool TryGetHipX(out float hipX, out int currentPoseVersion)
    {
        lock (poseLock)
        {
            int leftHipIndex = (int)HumanTrackingPoint.LeftHip;
            int rightHipIndex = (int)HumanTrackingPoint.RightHip;
            if (pendingLandmarks == null || pendingLandmarks.Count <= rightHipIndex)
            {
                hipX = 0f;
                currentPoseVersion = poseVersion;
                return false;
            }

            hipX = (pendingLandmarks[leftHipIndex].x + pendingLandmarks[rightHipIndex].x) * 0.5f;
            currentPoseVersion = poseVersion;
            return true;
        }
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
            "[PoseRouter] Connected to PoseLandmarkerRunner."
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

    private void OnPoseResult(PoseLandmarkerResult result)
    {
        if (result.poseLandmarks == null || result.poseLandmarks.Count == 0)
            return;


        var raw = result.poseLandmarks[0].landmarks;


        if (raw == null || raw.Count < 33)
            return;


        List<Vector3> mapped = new List<Vector3>(raw.Count);


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

            mapped.Add(new Vector3(mirrorMovement ? 1f - lm.x : lm.x, 1f - lm.y, -lm.z));
        }


        lock (poseLock)
        {
            pendingLandmarks = mapped;
            poseVersion++;

            poseReady = true;
        }
    }

    public List<Vector3> GetPoseLandmarks()
    {
        lock (poseLock)
        {
            return pendingLandmarks;
        }
    }

    public Dictionary<HumanTrackingPoint, Vector3> GetTrackingPoints()
    {
        lock (poseLock)
        {
            foreach (HumanTrackingPoint _trackingPoint in System.Enum.GetValues(typeof(HumanTrackingPoint)))
            {
                Vector3 _landmark = pendingLandmarks[(int)_trackingPoint];

                if (!trackingPoints.TryGetValue(_trackingPoint, out Vector3 _currentLandmark) || _currentLandmark != _landmark)
                {
                    trackingPoints[_trackingPoint] = _landmark;
                }
            }

            return trackingPoints;
        }
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if(!poseReady)
        {
            return;
        }

        bodyController.UpdateLandmarks(pendingLandmarks);
    }
}
