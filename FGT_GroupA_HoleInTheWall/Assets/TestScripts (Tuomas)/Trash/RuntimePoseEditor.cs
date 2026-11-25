
using System.Collections.Generic;
using UnityEngine;

public class RuntimePoseEditor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Pose2 sourcePose;
    [SerializeField] private WallCutter wallCutter;
    [SerializeField] private Transform wall;

    [Header("Preview")]
    [SerializeField] private Material previewMaterial;

    //!
    [SerializeField] private float previewSurfaceOffset = 0.05f;

    [SerializeField]
    private Color previewColor =
        new Color(0f, 1f, 0.4f, 0.5f);

    [Header("Wall Fit")]
    [Range(0f, 0.25f)]
    [SerializeField] private float wallMargin = 0.08f;

    [Header("Editing Ranges")]
    [SerializeField] private float positionRange = 3f;
    [SerializeField] private float sizeRange = 2f;

    private Pose2 runtimePose;

    private readonly List<BodyPart> editedParts = new();
    private readonly List<GameObject> previews = new();

    private int selectedPart;
    private bool editing = true;
    private bool fitValid;

    private PoseFitUtility.FitResult fit;

    private void Start()
    {
        if (sourcePose == null ||
            wall == null ||
            wallCutter == null)
        {
            Debug.LogError(
                "RuntimePoseEditor is missing references.",
                this);

            enabled = false;
            return;
        }

        CreateRuntimePose();

        if (!CalculateFit())
        {
            Debug.LogError(
                "Could not calculate initial pose fit.", this);

            enabled = false;
            return;
        }

        CreatePreviews();
        UpdatePreviews();
    }

    private void Update()
    {
        if (!editing || editedParts.Count == 0)
            return;

        // Recalculate whenever pose values change.
        CalculateFit();
        UpdatePreviews();
    }

    private void CreateRuntimePose()
    {
        runtimePose = Instantiate(sourcePose);

        runtimePose.bodyParts.Clear();

        foreach (BodyPart original in sourcePose.bodyParts)
        {
            BodyPart copy = new BodyPart
            {
                partName = original.partName,
                shape = original.shape,
                localPosition = original.localPosition,
                localRotation = original.localRotation,
                size = original.size,
                allowBottomOverflow =
                    original.allowBottomOverflow
            };

            editedParts.Add(copy);
            runtimePose.bodyParts.Add(copy);
        }
    }

    private bool CalculateFit()
    {
        MeshFilter filter = wall.GetComponent<MeshFilter>();

        if (filter == null || filter.sharedMesh == null)
        {
            fitValid = false;
            return false;
        }

        Bounds wallBounds = filter.sharedMesh.bounds;

        fitValid = PoseFitUtility.CalculateFit(
            runtimePose,
            wallBounds,
            wallMargin,
            out fit
        );

        return fitValid;
    }

    private void CreatePreviews()
    {
        foreach (BodyPart part in editedParts)
        {
            PrimitiveType type = part.shape switch
            {
                BodyPartShape.Sphere => PrimitiveType.Sphere,
                BodyPartShape.Capsule => PrimitiveType.Capsule,
                BodyPartShape.Box => PrimitiveType.Cube,
                _ => PrimitiveType.Capsule
            };

            GameObject preview =
                GameObject.CreatePrimitive(type);

            preview.name = "Preview_" + part.partName;

            Collider collider =
                preview.GetComponent<Collider>();

            if (collider != null)
                Destroy(collider);

            Renderer renderer =
                preview.GetComponent<Renderer>();

            if (previewMaterial != null)
                renderer.material = previewMaterial;

            renderer.material.color = previewColor;

            // Use wall-local coordinates for the preview.
            preview.transform.SetParent(wall, false);

            previews.Add(preview);
        }
    }

    private void UpdatePreviews()
    {
        if (!fitValid)
            return;

        for (int i = 0; i < editedParts.Count; i++)
        {
            BodyPart part = editedParts[i];

            Vector3 finalPosition =
                fit.offset + part.localPosition * fit.scale;

            Vector3 finalSize =
                part.size * fit.scale;

            Mesh mesh =
                previews[i].GetComponent<MeshFilter>().sharedMesh;

            Vector3 meshDimensions = mesh.bounds.size;

            Vector3 primitiveScale = new Vector3(
                finalSize.x / meshDimensions.x,
                finalSize.y / meshDimensions.y,
                finalSize.z / meshDimensions.z
            );

            //!
            // Move preview slightly towards the camera.
            Camera cam = Camera.main;

            if (cam != null)
            {
                float cameraSide = Mathf.Sign(
                    wall.InverseTransformPoint(cam.transform.position).z
                    - finalPosition.z
                );

                finalPosition += Vector3.forward *
                                 cameraSide *
                                 previewSurfaceOffset;
            }

            previews[i].transform.localPosition =
                finalPosition;

            previews[i].transform.localRotation =
                Quaternion.Euler(part.localRotation);

            previews[i].transform.localScale =
                primitiveScale;
        }

    }

    private void OnGUI()
    {
        if (!editing || editedParts.Count == 0)
            return;

        GUILayout.BeginArea(
            new Rect(15, 15, 340, Screen.height - 30),
            GUI.skin.box
        );

        GUILayout.Label(
            "HUMANOID POSE EDITOR",
            GUI.skin.box
        );

        GUILayout.Label("Select Body Part:");

        for (int i = 0; i < editedParts.Count; i++)
        {
            string label = editedParts[i].partName;

            if (selectedPart == i)
                label += " [SELECTED]";

            if (GUILayout.Button(label))
                selectedPart = i;
        }

        BodyPart selected = editedParts[selectedPart];

        GUILayout.Space(10);

        GUILayout.Label("POSITION");

        selected.localPosition = Vector3Slider(
            selected.localPosition,
            -positionRange,
            positionRange
        );

        GUILayout.Space(5);

        GUILayout.Label("ROTATION");

        selected.localRotation = Vector3Slider(
            selected.localRotation,
            -180f,
            180f
        );

        GUILayout.Space(5);

        GUILayout.Label("DIMENSIONS");

        selected.size = Vector3Slider(
            selected.size,
            0.01f,
            sizeRange
        );

        // Keep dimensions positive.
        selected.size = new Vector3(
            Mathf.Max(0.01f, selected.size.x),
            Mathf.Max(0.01f, selected.size.y),
            Mathf.Max(0.01f, selected.size.z)
        );

        GUILayout.Space(5);

        selected.allowBottomOverflow =
            GUILayout.Toggle(
                selected.allowBottomOverflow,
                "Allow bottom overflow"
            );

        GUILayout.Space(10);

        if (GUILayout.Button("Reset Selected Part"))
        {
            BodyPart original =
                sourcePose.bodyParts[selectedPart];

            selected.localPosition = original.localPosition;
            selected.localRotation = original.localRotation;
            selected.size = original.size;

            selected.allowBottomOverflow =
                original.allowBottomOverflow;
        }

        if (GUILayout.Button("Reset Entire Pose"))
        {
            for (int i = 0; i < editedParts.Count; i++)
            {
                BodyPart original = sourcePose.bodyParts[i];
                BodyPart current = editedParts[i];

                current.localPosition = original.localPosition;
                current.localRotation = original.localRotation;
                current.size = original.size;

                current.allowBottomOverflow =
                    original.allowBottomOverflow;
            }

            runtimePose.overallScale = sourcePose.overallScale;
        }

        GUILayout.Space(10);

        GUILayout.Label(
            fitValid
                ? $"Current Fit Scale: {fit.scale:F3}"
                : "Invalid pose dimensions!"
        );

        if (GUILayout.Button("CUT WALL"))
        {
            if (!CalculateFit())
            {
                Debug.LogError("Invalid pose. Cannot cut.");
            }
            else
            {
                DestroyPreviews();

                editing = false;

                bool success =
                    wallCutter.CutWall(runtimePose);

                if (!success)
                {
                    editing = true;
                    CreatePreviews();
                    UpdatePreviews();
                }
            }
        }

        if (GUILayout.Button("Cancel Editing"))
        {
            editing = false;
            DestroyPreviews();
        }

        GUILayout.EndArea();
    }

    private Vector3 Vector3Slider(
        Vector3 value,
        float min,
        float max)
    {
        value.x = DrawSlider("X", value.x, min, max);
        value.y = DrawSlider("Y", value.y, min, max);
        value.z = DrawSlider("Z", value.z, min, max);

        return value;
    }

    private float DrawSlider(
        string label,
        float value,
        float min,
        float max)
    {
        GUILayout.BeginHorizontal();

        GUILayout.Label(label, GUILayout.Width(20));

        value = GUILayout.HorizontalSlider(
            value, min, max
        );

        GUILayout.Label(
            value.ToString("F2"),
            GUILayout.Width(48)
        );

        GUILayout.EndHorizontal();

        return value;
    }

    private void DestroyPreviews()
    {
        foreach (GameObject preview in previews)
        {
            if (preview != null)
                Destroy(preview);
        }

        previews.Clear();
    }

    private void OnDestroy()
    {
        DestroyPreviews();

        if (runtimePose != null)
            Destroy(runtimePose);
    }
}
