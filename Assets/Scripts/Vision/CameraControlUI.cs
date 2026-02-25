using UnityEngine;
using UnityEngine.UI;

public class CameraControlUI : MonoBehaviour
{
    [SerializeField] private CameraFrameProvider cameraProvider;
    [SerializeField] private VisionPipeline visionPipeline;

    [Header("UI")]
    [SerializeField] private Button btnStart;
    [SerializeField] private Button btnStop;

    private void Awake()
    {
        if (btnStart) btnStart.onClick.AddListener(StartCamera);
        if (btnStop) btnStop.onClick.AddListener(StopCamera);

        RefreshButtons();
    }

    private void StartCamera()
    {
        if (cameraProvider == null) return;

        cameraProvider.StartCamera();

        // If you're still using test image in editor, turn it off so camera is used
        if (visionPipeline != null)
            visionPipeline.UseTestImageInEditor(false);

        RefreshButtons();
    }

    private void StopCamera()
    {
        if (cameraProvider == null) return;

        cameraProvider.StopCamera();
        RefreshButtons();
    }

    private void RefreshButtons()
    {
        bool running = (cameraProvider != null && cameraProvider.IsRunning);
        if (btnStart) btnStart.interactable = !running;
        if (btnStop) btnStop.interactable = running;
    }
}