using UnityEngine;
using System.Collections.Generic;
using System;

    public enum BodyPartShape
    {
        Sphere,
        Capsule,
        Box
    }

    [Serializable]
    public class BodyPart
    {
        public string partName = "Body part";

        public BodyPartShape shape = BodyPartShape.Capsule;

        [Tooltip("Position relative to wall transform")]
        public Vector3 localPosition;

        [Tooltip("Rotation relation to wall")]
        public Vector3 localRotation;

        [Tooltip("SIze of primitive in local units")]
        public Vector3 size = Vector3.one; 
    }

    [CreateAssetMenu(fileName = "Pose", menuName = "Hole-in-the-wall/pose")]

    public class Pose : ScriptableObject
    {
        public string poseName = "Standing";

        [Min(0.01f)]
        public float overallScale = 1.0f;
        [Tooltip("Maker sure bodyparts overlap")]
        public List<BodyPart> bodyParts = new();
    }

