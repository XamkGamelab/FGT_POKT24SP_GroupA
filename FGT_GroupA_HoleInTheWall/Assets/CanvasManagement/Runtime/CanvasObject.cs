using UnityEngine;
using CanvasType = CanvasManager.CanvasType;

    public class CanvasObject : MonoBehaviour
    {
        [SerializeField] private string canvasName = string.Empty;
        public string CanvasName => canvasName;

        [SerializeField] private CanvasType canvasType = CanvasType.Base;
        public CanvasType CanvasType => canvasType;
    }
