using System.Collections;
using System.Collections.Generic;
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

    [Header("Behavior")]
    [SerializeField] private bool autoStartOnEnable = false;

    public WebCamTexture WebCamTex { get; private set; }
    public bool IsRunning => WebCamTex != null && WebCamTex.isPlaying;
    public Texture Texture => WebCamTex;

    public bool HasFrame => WebCamTex != null && WebCamTex.isPlaying && WebCamTex.width > 16 && WebCamTex.height > 16;

    private Coroutine _startRoutine;
    private bool _isStarting;

    // Track all providers so we can force-stop on scene changes / quit
    private static readonly HashSet<CameraFrameProvider> Instances = new();

    private void Awake()
    {
        Instances.Add(this);
    }

    private void OnDestroy()
    {
        Instances.Remove(this);
        StopCamera();
    }

    private void OnEnable()
    {
        if (autoStartOnEnable)
            StartCamera();
    }

    private void OnDisable()
    {
        StopCamera();
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause) StopCamera();
    }

    private void OnApplicationQuit()
    {
        StopCamera();
    }

    public static void StopAllCameras()
    {
        foreach (var inst in Instances)
            if (inst != null) inst.StopCamera();
    }

    public void StartCamera()
    {
        // Prevent double-start spam
        if (_isStarting || IsRunning) return;

        if (_startRoutine != null) StopCoroutine(_startRoutine);
        _startRoutine = StartCoroutine(StartCameraRoutine());
    }

    private IEnumerator StartCameraRoutine()
    {
        _isStarting = true;

#if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            Permission.RequestUserPermission(Permission.Camera);
            float t = 0f;
            while (!Permission.HasUserAuthorizedPermission(Permission.Camera) && t < 3f)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                Debug.LogError("[Camera] Permission not granted.");
                _isStarting = false;
                yield break;
            }
        }
#endif

        // If something left a texture alive, stop it first
        StopCameraInternalOnly();

        var devices = WebCamTexture.devices;
        if (devices == null || devices.Length == 0)
        {
            Debug.LogError("[Camera] No camera devices found.");
            _isStarting = false;
            yield break;
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

        Debug.Log($"[Camera] Starting: {WebCamTex.deviceName} req={requestedWidth}x{requestedHeight}@{requestedFPS}");

        // Wait until camera is really ready (avoid 16x16)
        float wait = 0f;
        while (WebCamTex != null && WebCamTex.isPlaying && WebCamTex.width <= 16 && wait < 3f)
        {
            wait += Time.unscaledDeltaTime;
            yield return null;
        }

        if (WebCamTex == null)
        {
            _isStarting = false;
            yield break;
        }

        Debug.Log($"[Camera] Running: {WebCamTex.width}x{WebCamTex.height} rot={WebCamTex.videoRotationAngle}");

        _isStarting = false;
    }

    public void StopCamera()
    {
        if (_startRoutine != null) StopCoroutine(_startRoutine);
        _startRoutine = null;
        _isStarting = false;

        StopCameraInternalOnly();
    }

    private void StopCameraInternalOnly()
    {
        if (WebCamTex == null) return;

        try
        {
            if (WebCamTex.isPlaying) WebCamTex.Stop();
        }
        catch { /* ignore */ }

        Destroy(WebCamTex);
        WebCamTex = null;
    }
}