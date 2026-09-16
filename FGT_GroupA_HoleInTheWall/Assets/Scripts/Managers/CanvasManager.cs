using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

    public class CanvasManager : MonoBehaviour
    {
        [System.Serializable]
        public class UnityEventCanvasType : UnityEvent<CanvasType> { }
        public class UnityEventCanvasObject : UnityEvent<CanvasObject> { }

        public enum CanvasType
        {
            Base,
            Overlay,
            Modal
        }

        public static readonly UnityEventString ShowCanvas = new UnityEventString();
        public static readonly UnityEventCanvasType CloseCanvas = new UnityEventCanvasType();
        public static readonly UnityEvent ShowGameCanvas = new UnityEvent();
        public static readonly UnityEvent ReturnToLobby = new UnityEvent();

        [SerializeField] private CanvasObject[] canvasObjects = new CanvasObject[0];
        [SerializeField] private GameObject[] startEnabled = new GameObject[0];
        [SerializeField] private GameObject[] startDisabled = new GameObject[0];

        [SerializeField] private string lobbyCanvasName = string.Empty;
        [SerializeField] private string startCanvasName = string.Empty;
        [SerializeField] private string gameCanvasName = string.Empty;

        private CanvasObject currentBaseCanvas;
        private CanvasObject currentOverlayCanvas;
        private CanvasObject currentModalCanvas;

        private void Awake()
        {
            ShowCanvas.AddListener((targetCanvas) => 
            { 
                if (string.IsNullOrEmpty(targetCanvas))
                {
                    Debug.LogWarning("CanvasManager: ChangeCanvas: target canvas was null or empty");
                    return;
                }

                foreach (CanvasObject canvasObject in canvasObjects)
                    if (canvasObject.CanvasName == targetCanvas)
                    {
                        switch (canvasObject.CanvasType)
                        {
                            default:
                                throw new System.ArgumentException("Unknown canvas type", canvasObject.CanvasType.ToString());

                            case CanvasType.Base:
                                if (currentBaseCanvas != null)
                                    if (currentBaseCanvas.CanvasName == targetCanvas)
                                        return;

                                currentBaseCanvas?.gameObject.SetActive(false);
                                canvasObject.gameObject.SetActive(true);
                                currentBaseCanvas = canvasObject;
                                break;

                            case CanvasType.Modal:
                                if (currentModalCanvas != null)
                                    if (currentModalCanvas.CanvasName == targetCanvas)
                                        return;

                                currentModalCanvas?.gameObject.SetActive(false);
                                canvasObject.gameObject.SetActive(true);
                                currentModalCanvas = canvasObject;
                                break;

                            case CanvasType.Overlay:
                                if (currentOverlayCanvas != null)
                                    if (currentOverlayCanvas?.CanvasName == targetCanvas)
                                        return;

                                currentOverlayCanvas?.gameObject.SetActive(false);
                                canvasObject.gameObject.SetActive(true);
                                currentOverlayCanvas = canvasObject;
                                break;
                        }

                        return;
                    }

                Debug.LogWarning($"Could not find canvas object \"{targetCanvas}\"");
            });

            CloseCanvas.AddListener((canvasType) => 
            {
                switch (canvasType)
                {
                    default:
                        throw new System.ArgumentException("Unknown canvas type", canvasType.ToString());

                    case CanvasType.Base:
                        currentBaseCanvas?.gameObject.SetActive(false);
                        currentBaseCanvas = null;
                        break;

                    case CanvasType.Modal:
                        currentModalCanvas?.gameObject.SetActive(false);
                        currentModalCanvas = null;
                        break;

                    case CanvasType.Overlay:
                        currentOverlayCanvas?.gameObject.SetActive(false);
                        currentOverlayCanvas = null;
                        break;
                }
            });

            ShowGameCanvas.AddListener(() => 
            {
                ShowCanvas.Invoke(gameCanvasName);
            });

            ReturnToLobby.AddListener(() =>
            {
                ShowCanvas.Invoke(lobbyCanvasName);
            });
        }

        private void Start()
        {
            foreach (CanvasObject canvasObject in canvasObjects)
                canvasObject.gameObject.SetActive(true);

            foreach (GameObject gameObject in startEnabled)
                gameObject.SetActive(true);

            foreach (GameObject gameObject in startDisabled)
                gameObject.SetActive(false);

            ShowCanvas.Invoke(startCanvasName);
        }
    }