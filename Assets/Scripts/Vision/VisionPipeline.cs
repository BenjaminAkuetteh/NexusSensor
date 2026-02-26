using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Unity.InferenceEngine;

public class VisionPipeline : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private CameraFrameProvider cameraProvider;
    [SerializeField] private YoloPreprocessor preprocessor;
    [SerializeField] private YoloRunner runner;

    [Header("Output Hook (assign a component that implements IVisionSuggestionSink)")]
    [SerializeField] private MonoBehaviour suggestionSinkBehaviour;

    [Header("Runtime")]
    [SerializeField] private float inferenceHz = 3f;          // 2–5 recommended
    [SerializeField] private float confThreshold = 0.35f;
    [SerializeField] private float iouThreshold = 0.45f;

    [Header("Editor Test Image")]
    [SerializeField] private bool useTestImageInEditor = false;
    [SerializeField] private Texture2D testTexture;

    [Header("Overlay (VisionCamera scene)")]
    [SerializeField] private DetectionOverlayUI overlayUI;
    [SerializeField] private float overlayMinConf = 0.35f;
    [SerializeField] private int overlayMaxShown = 5;

    [Header("Debug")]
    [SerializeField] private bool logTopDetections = false;
    [SerializeField] private int logTopN = 3;
    [SerializeField] private float logIntervalSec = 2f;
    private float _nextLogTime = 0f;

    private IVisionSuggestionSink _sink;
    private AacSuggestionMapper _mapper;
    private StabilityGate _stability;

    private float[] _outputBuffer;
    private const int OutputLen = 84 * 8400;

    // Reflection cached method: Tensor<float>.DownloadToArray(float[]) if available
    private MethodInfo _miDownloadToArrayInto;

    private void Awake()
    {
        _sink = suggestionSinkBehaviour as IVisionSuggestionSink;
        _mapper = new AacSuggestionMapper();
        _stability = new StabilityGate(window: 3, required: 2);
        _outputBuffer = new float[OutputLen];
    }

    private void OnEnable()
    {
        CacheTensorMethods();
        StartCoroutine(Loop());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        _stability.Reset();

        // Hide overlay when pipeline stops
        overlayUI?.Render(null, 0f, 0); // safe no-op hide
    }

    public void UseTestImageInEditor(bool enabled) => useTestImageInEditor = enabled;

    private void CacheTensorMethods()
    {
        var t = typeof(Tensor<float>);
        _miDownloadToArrayInto = t.GetMethod("DownloadToArray", new[] { typeof(float[]) });

        if (_miDownloadToArrayInto != null)
            Debug.Log("[Vision] Found non-alloc Tensor<float>.DownloadToArray(float[])");
        else
            Debug.Log("[Vision] Non-alloc DownloadToArray(float[]) not found. Will use DownloadToArray() fallback.");
    }

    private IEnumerator Loop()
    {
        // wait 1 frame so graphics/RTs are ready
        yield return null;

        var wait = new WaitForSeconds(1f / Mathf.Max(1f, inferenceHz));

        while (true)
        {
            if (preprocessor == null || runner == null)
            {
                yield return wait;
                continue;
            }

            if (!runner.IsReady)
            {
                yield return wait;
                continue;
            }

            // 1) Select source texture
            Texture source = null;

#if UNITY_EDITOR
            if (useTestImageInEditor && testTexture != null)
                source = testTexture;
            else
#endif
            {
                if (cameraProvider != null && cameraProvider.HasFrame)
                    source = cameraProvider.Texture;
            }

            if (source == null)
            {
                yield return wait;
                continue;
            }

            // 2) Preprocess -> fills input tensor
            preprocessor.Preprocess(source);

            // 3) Inference
            Tensor<float> output = runner.Run(preprocessor.InputTensor);
            if (output == null)
            {
                // backoff if model/backend failing
                yield return new WaitForSeconds(1f);
                continue;
            }

            // 4) Download output into reusable buffer
            if (!TryDownloadOutputToBuffer(output, _outputBuffer))
            {
                yield return wait;
                continue;
            }

            // 5) Decode + NMS
            var dets = YoloDecoder.Decode(_outputBuffer, confThreshold, iouThreshold, maxDetections: 50);

            // ✅ NEW: draw boxes + labels on the camera feed (VisionCamera scene)
            overlayUI?.Render(dets, overlayMinConf, overlayMaxShown);

            // Throttled logging (no console spam)
            if (logTopDetections && dets != null && Time.unscaledTime >= _nextLogTime)
            {
                _nextLogTime = Time.unscaledTime + logIntervalSec;

                for (int i = 0; i < Mathf.Min(logTopN, dets.Count); i++)
                {
                    var d = dets[i];
                    var label = (d.classId >= 0 && d.classId < CocoLabelMap.Labels.Length)
                        ? CocoLabelMap.Labels[d.classId]
                        : "?";
                    Debug.Log($"[YOLO] {label} {d.confidence:0.00} rect={d.rect01}");
                }
            }

            // 6) Map -> AAC word ids
            var mapped = _mapper.MapDetections(dets);

            var ids = new List<string>(mapped.Count);
            var confs = new List<float>(mapped.Count);
            foreach (var m in mapped)
            {
                ids.Add(m.wordId);
                confs.Add(m.confidence);
            }

            // 7) Stability (2 of last 3)
            var stable = _stability.FilterStable(ids);

            var stableConfs = new List<float>(stable.Count);
            foreach (var sid in stable)
            {
                int idx = ids.IndexOf(sid);
                stableConfs.Add(idx >= 0 ? confs[idx] : 0f);
            }

            // 8) Push to UI (suggest only)
            _sink?.SetVisionSuggestions(stable, stableConfs);

            yield return wait;
        }
    }

    private bool TryDownloadOutputToBuffer(Tensor<float> output, float[] dst)
    {
        if (output == null || dst == null || dst.Length != OutputLen) return false;

        // Best path if available: DownloadToArray(float[]) to avoid allocations
        if (_miDownloadToArrayInto != null)
        {
            try
            {
                _miDownloadToArrayInto.Invoke(output, new object[] { dst });
                return true;
            }
            catch
            {
                // fall through to alloc path
            }
        }

        // Fallback path: allocates a new float[] each tick in this package version
        var arr = output.DownloadToArray();
        if (arr == null || arr.Length != OutputLen) return false;

        System.Array.Copy(arr, dst, OutputLen);
        return true;
    }
}