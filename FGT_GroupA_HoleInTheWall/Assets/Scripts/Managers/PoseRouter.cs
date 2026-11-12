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
    private readonly object poseLock = new object();

    private List<Vector3> pendingLandmarks;
    private readonly Dictionary<HumanTrackingPoint, Vector3> trackingPoints = new Dictionary<HumanTrackingPoint, Vector3>();

    private bool poseReady = false;

    BodyController bodyController;
    public static UnityEvent<Player> AddPlayerToBodyControllerEvent = new UnityEvent<Player>();



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
        yield return new WaitUntil(() => bodyController != null);
        yield return bodyController.HandleStartCalibrationCoroutine();
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

            mapped.Add(new Vector3( 1f - lm.x, 1f - lm.y, -lm.z));
        }


        lock (poseLock)
        {
            pendingLandmarks = mapped;

            foreach (HumanTrackingPoint trackingPoint in System.Enum.GetValues(typeof(HumanTrackingPoint)))
            {
                Vector3 landmark = mapped[(int)trackingPoint];

                if (!trackingPoints.TryGetValue(trackingPoint, out Vector3 currentLandmark) || currentLandmark != landmark)
                {
                    trackingPoints[trackingPoint] = landmark;
                }
            }

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
        Dictionary<HumanTrackingPoint, Vector3> trackingPoints = GetTrackingPoints();
        Vector3 headLandmark = trackingPoints[HumanTrackingPoint.Head];

        lock (poseLock)
        {
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
