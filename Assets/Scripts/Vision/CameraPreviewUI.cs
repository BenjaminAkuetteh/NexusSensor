using UnityEngine;
using UnityEngine.UI;

public class CameraPreviewUI : MonoBehaviour
{
    [SerializeField] private CameraFrameProvider cameraProvider;
    [SerializeField] private RawImage preview;

    [Header("Optional rotation/flip")]
    [SerializeField] private bool mirrorX = false;

    private void Update()
    {
        if (cameraProvider == null || preview == null) return;

        var tex = cameraProvider.WebCamTex;
        if (tex == null) return;

        preview.texture = tex;

        // Correct aspect ratio
        var fitter = preview.GetComponent<AspectRatioFitter>();
        if (fitter != null && tex.width > 16 && tex.height > 16)
            fitter.aspectRatio = (float)tex.width / tex.height;

        // Correct rotation (phone cameras often report videoRotationAngle)
        preview.rectTransform.localEulerAngles = new Vector3(0, 0, -tex.videoRotationAngle);

        // Optional mirror
        preview.rectTransform.localScale = new Vector3(mirrorX ? -1f : 1f, 1f, 1f);
    }
}