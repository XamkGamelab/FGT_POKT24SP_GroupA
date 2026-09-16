using UnityEngine;
using UnityEngine.UI;

namespace Coder
{
    using CanvasType = CanvasManager.CanvasType;

    [RequireComponent(typeof(Button))]
    public class CloseCanvasButton : MonoBehaviour
    {
        [SerializeField] private CanvasType canvasType = CanvasType.Base;

        private void Awake() =>
            GetComponent<Button>().onClick.AddListener(() => 
            {
                CanvasManager.CloseCanvas.Invoke(canvasType);
            });
    }
}