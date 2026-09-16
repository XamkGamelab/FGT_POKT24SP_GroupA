using UnityEngine;
using UnityEngine.UI;

namespace Coder
{
    [RequireComponent(typeof(Button))]
    public class ChangeCanvasButton : MonoBehaviour
    {
        [SerializeField] private string canvasName = string.Empty;

        private void Awake() =>
            GetComponent<Button>().onClick.AddListener(() => 
            {
                CanvasManager.ShowCanvas.Invoke(canvasName);
            });
    }
}