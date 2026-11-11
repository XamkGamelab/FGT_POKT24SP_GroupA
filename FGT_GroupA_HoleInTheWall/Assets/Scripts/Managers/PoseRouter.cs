using Mediapipe.Tasks.Vision.PoseLandmarker;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PoseRouter : SingletonMono<PoseRouter>
{
    private readonly object poseLock = new object();

    private List<Vector3> pendingLandmarks;

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
        if (
            result.poseLandmarks == null ||
            result.poseLandmarks.Count == 0
        )
        {
            return;
        }


        var raw = result.poseLandmarks[0].landmarks;


        if (
            raw == null ||
            raw.Count < 33
        )
        {
            return;
        }


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

            poseReady = true;
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
