using UnityEngine;
using UnityEngine.UI;

public class VisionDebugOverlay : MonoBehaviour
{
    [SerializeField] private RawImage rawCamera;
    [SerializeField] private CameraFrameProvider cameraProvider;
    [SerializeField] private RawImage rawResized;
    [SerializeField] private YoloPreprocessor preprocessor;

    private void Update()
    {
        if (rawCamera && cameraProvider != null && cameraProvider.Texture != null)
            rawCamera.texture = cameraProvider.Texture;

        if (rawResized && preprocessor != null && preprocessor.ResizedRT != null)
            rawResized.texture = preprocessor.ResizedRT;
    }
}