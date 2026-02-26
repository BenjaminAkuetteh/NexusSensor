using UnityEngine;
using UnityEngine.UI;

public class CameraPreviewFullScreen : MonoBehaviour
{
    [SerializeField] private CameraFrameProvider cameraProvider;
    [SerializeField] private RawImage preview;
    [SerializeField] private bool mirrorX = false;

    private void Update()
    {
        if (cameraProvider == null || preview == null) return;

        var tex = cameraProvider.WebCamTex;
        if (tex == null || !tex.isPlaying || tex.width <= 16) return; // wait until real frame size

        preview.texture = tex;
        preview.color = Color.white;

        preview.rectTransform.localEulerAngles = new Vector3(0, 0, -tex.videoRotationAngle);
        preview.rectTransform.localScale = new Vector3(mirrorX ? -1f : 1f, 1f, 1f);
    }
}