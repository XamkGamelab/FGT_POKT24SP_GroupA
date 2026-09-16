using UnityEngine;
using UnityEngine.UI;

public class WebcamDisplay : MonoBehaviour
{
    public RawImage rawImage; // Or use Renderer for 3D objects
    private WebCamTexture camTexture;

    void Start()
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices.Length > 0)
        {
            camTexture = new WebCamTexture(devices[0].name);
            rawImage.texture = camTexture;
            camTexture.Play();
        }
        else
        {
            Debug.Log("No webcam found.");
        }
    }
}
