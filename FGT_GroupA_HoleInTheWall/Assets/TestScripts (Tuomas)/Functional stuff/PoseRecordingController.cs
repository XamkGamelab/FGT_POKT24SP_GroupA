
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PoseRecordingController : MonoBehaviour
{
    private enum RecordingState
    {
        Idle,
        Recording,
        Countdown,
        Capturing
    }

    [Header("References")]
    [SerializeField]
    private LivePoseSkeletonPreview posePreview;

    [SerializeField]
    private HumanoidCutGenerator cutGenerator;

    [SerializeField]
    private TrackedWallCutter wallCutter;

    [SerializeField]
    private GeneratedWallStorage wallStorage;

    [Header("Recording Settings")]
    [Min(1)]
    [SerializeField] private int countdownDuration = 3;

    [Header("Debug UI")]
    [SerializeField] private bool showDebugUI = true;

    private RecordingState state = RecordingState.Idle;

    private int countdownRemaining;

    private string statusMessage = "";

    private void Start()
    {
        if (posePreview == null ||
            cutGenerator == null ||
            wallCutter == null ||
            wallStorage == null)
        {
            Debug.LogError(
                "PoseRecordingController is missing references."
            );

            enabled = false;
            return;
        }

        ReturnToIdle();
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            switch (state)
            {
                case RecordingState.Idle:
                    StartRecording();
                    break;

                case RecordingState.Recording:
                    StartCoroutine(CaptureCountdown());
                    break;
            }
        }

        // Optional: cancel the current recording.
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (state == RecordingState.Recording ||
                state == RecordingState.Countdown)
            {
                StopAllCoroutines();

                ReturnToIdle();

                statusMessage = "Recording cancelled.";
            }
        }
    }

    // =========================================================
    // START RECORDING
    // =========================================================

    private void StartRecording()
    {
        state = RecordingState.Recording;

        statusMessage = "Prepare your pose.";

        posePreview.SetPreviewVisible(true);

        cutGenerator.SetLivePreview(true);

        Debug.Log("Pose recording mode enabled.");
    }

    // =========================================================
    // COUNTDOWN
    // =========================================================

    private IEnumerator CaptureCountdown()
    {
        state = RecordingState.Countdown;

        statusMessage = "Hold your pose!";

        for (int i = countdownDuration; i > 0; i--)
        {
            countdownRemaining = i;

            yield return new WaitForSecondsRealtime(1f);
        }

        countdownRemaining = 0;

        state = RecordingState.Capturing;

        statusMessage = "Generating wall...";

        // Allow the UI to display the capturing state.
        yield return null;

        bool success = false;

        try
        {
            // Capture the current pose and generate the wall.
            success = wallCutter.CutWall();
        }
        finally
        {
            ReturnToIdle();
        }

        statusMessage = success
            ? "Pose saved successfully!"
            : "Pose recording failed.";
    }

    // =========================================================
    // RESET RECORDING MODE
    // =========================================================

    private void ReturnToIdle()
    {
        state = RecordingState.Idle;

        countdownRemaining = 0;

        posePreview.SetPreviewVisible(false);

        cutGenerator.SetLivePreview(false);
    }

    // =========================================================
    // TEMPORARY DEBUG UI
    // =========================================================

    private void OnGUI()
    {
        if (!showDebugUI)
            return;

        GUILayout.BeginArea(
            new Rect(15, 15, 300, 170),
            GUI.skin.box
        );

        GUILayout.Label(
            "POSE RECORDING SYSTEM",
            GUI.skin.box
        );

        GUILayout.Label("State: " + state);

        if (wallStorage != null)
        {
            GUILayout.Label(
                "Recorded walls: " + wallStorage.Count
            );
        }

        GUILayout.Space(10);

        switch (state)
        {
            case RecordingState.Idle:
                GUILayout.Label("Press R to start recording.");
                break;

            case RecordingState.Recording:
                GUILayout.Label("Press R to begin countdown.");
                GUILayout.Label("Press ESC to cancel.");
                break;

            case RecordingState.Countdown:
                GUILayout.Label(
                    "Capturing in: " + countdownRemaining
                );
                break;

            case RecordingState.Capturing:
                GUILayout.Label("Generating wall...");
                break;
        }

        GUILayout.Space(5);

        GUILayout.Label(statusMessage);

        GUILayout.EndArea();

        // Large countdown in the center of the screen.
        if (state == RecordingState.Countdown)
        {
            GUIStyle countdownStyle =
                new GUIStyle(GUI.skin.label);

            countdownStyle.fontSize = 80;

            countdownStyle.fontStyle = FontStyle.Bold;

            countdownStyle.alignment =
                TextAnchor.MiddleCenter;

            countdownStyle.normal.textColor = Color.white;

            GUI.Label(
                new Rect(
                    Screen.width / 2f - 100f,
                    Screen.height / 2f - 100f,
                    200f,
                    200f
                ),
                countdownRemaining.ToString(),
                countdownStyle
            );
        }
    }
}
