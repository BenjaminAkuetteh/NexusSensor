using UnityEngine;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class CameraFrameProvider : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private bool useFrontCamera = false;
    [SerializeField] private int requestedWidth = 1280;
    [SerializeField] private int requestedHeight = 720;
    [SerializeField] private int requestedFPS = 30;

    public WebCamTexture WebCamTex { get; private set; }
    public bool IsRunning => WebCamTex != null && WebCamTex.isPlaying;
    public Texture Texture => WebCamTex;

    public bool HasFrame
    {
        get
        {
            if (WebCamTex == null) return false;
            return WebCamTex.width > 16 && WebCamTex.height > 16;
        }
    }

    private void OnEnable()
    {
        StartCamera();
    }

    private void OnDisable()
    {
        StopCamera();
    }

    public void StartCamera()
    {
#if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            Permission.RequestUserPermission(Permission.Camera);
            // We’ll attempt again next frame(s).
        }
#endif
        if (IsRunning) return;

        var devices = WebCamTexture.devices;
        if (devices == null || devices.Length == 0)
        {
            Debug.LogError("No camera devices found.");
            return;
        }

        int chosen = 0;
        for (int i = 0; i < devices.Length; i++)
        {
            if (devices[i].isFrontFacing == useFrontCamera)
            {
                chosen = i;
                break;
            }
        }

        WebCamTex = new WebCamTexture(devices[chosen].name, requestedWidth, requestedHeight, requestedFPS);
        WebCamTex.Play();
    }

    public void StopCamera()
    {
        if (WebCamTex == null) return;
        if (WebCamTex.isPlaying) WebCamTex.Stop();
        Destroy(WebCamTex);
        WebCamTex = null;
    }
}