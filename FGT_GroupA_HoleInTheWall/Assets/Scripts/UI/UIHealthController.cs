using System.Linq;
using UnityEngine;

public class UIHealthController : MonoBehaviour
{
    [SerializeField] GameObject[] healthImages;
    private int currHealthImageIndex;
    public static UnityEventInt ChangeHealthUIEvent = new UnityEventInt();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        InitHealthImages();
        ChangeHealthUIEvent.AddListener(ChangeHealthImage);
    }
    private void InitHealthImages() => healthImages.ToList().ForEach(_img => _img.SetActive(false));
    private void ChangeHealthImage(int _health)
    {

        if (healthImages.Length <= 0)
            return;

        //Clamp so health index cant go over the imagelist
        int _newHealthImgIndex = Mathf.Clamp(_health, 0, healthImages.Length - 1);

        print("UI: " + _newHealthImgIndex);
        if (_newHealthImgIndex == currHealthImageIndex)
            return;

        healthImages[currHealthImageIndex].SetActive(false);
        healthImages[_newHealthImgIndex].SetActive(true);
        currHealthImageIndex = _newHealthImgIndex;
    }
}
