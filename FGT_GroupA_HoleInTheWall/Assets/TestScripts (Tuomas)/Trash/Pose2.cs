using System;
using System.Collections.Generic;
using UnityEngine;

public enum BodyPartShape
{
    Sphere,
    Capsule,
    Box
}

[Serializable]
public class BodyPart
{
    public string partName = "Body Part";

    public BodyPartShape shape = BodyPartShape.Capsule;

    [Header("Pose Position")]
    public Vector3 localPosition;

    [Header("Pose Rotation")]
    public Vector3 localRotation;

    [Header("Pose Dimensions")]
    public Vector3 size = Vector3.one;

    [Header("Wall Boundary Rules")]
    [Tooltip("Allows this body part to extend below the wall.")]
    public bool allowBottomOverflow = false;
}

[CreateAssetMenu(
    fileName = "HumanoidPose",
    menuName = "Hole In The Wall/Humanoid Pose")]
public class Pose2 : ScriptableObject
{
    public string poseName = "X-Pose";

    [Header("Scaling")]
    [Range(0.1f, 1f)]
    [Tooltip("1 = maximum size that fits inside the wall.")]
    public float overallScale = 1f;

    [Header("Body Parts")]
    public List<BodyPart> bodyParts = new();
}
