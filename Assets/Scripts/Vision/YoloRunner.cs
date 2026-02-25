using System;
using System.Reflection;
using UnityEngine;
using Unity.InferenceEngine;

public class YoloRunner : MonoBehaviour
{
    [Header("Model")]
    [SerializeField] private ModelAsset modelAsset;
    [SerializeField] private BackendType backend = BackendType.CPU;

    [Header("I/O Names (from logs)")]
    [SerializeField] private string inputName = "images";
    [SerializeField] private string outputName = "output0";

    private Model _model;
    private Worker _worker;

    // Reflection fallback: some versions expose Execute() instead of stable Schedule()
    private MethodInfo _miExecute;

    public bool IsReady => _worker != null;

    private void Awake() => TryInit();
    private void OnEnable() { if (_worker == null) TryInit(); }

    private void TryInit()
    {
        if (modelAsset == null)
        {
            Debug.LogError("YoloRunner: ModelAsset not assigned in Inspector.");
            return;
        }

        try
        {
            _model = ModelLoader.Load(modelAsset);
            if (_model == null)
            {
                Debug.LogError("YoloRunner: ModelLoader.Load returned null. Model import likely failed.");
                return;
            }

            _worker = new Worker(_model, backend);
            if (_worker == null)
            {
                Debug.LogError("YoloRunner: Worker creation returned null.");
                return;
            }

            // Cache Execute() if present
            _miExecute = _worker.GetType().GetMethod("Execute", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            Debug.Log($"[YoloRunner] Ready. Backend={backend}, Input='{inputName}', Output='{outputName}'. Execute()={( _miExecute!=null ? "yes":"no")}");
        }
        catch (Exception e)
        {
            Debug.LogError("[YoloRunner] Init exception: " + e);
            _worker = null;
        }
    }

    private void OnDestroy()
    {
        _worker?.Dispose();
        _worker = null;
    }

    public Tensor<float> Run(Tensor<float> input)
    {
        if (_worker == null)
        {
            Debug.LogError("YoloRunner.Run: worker is null.");
            return null;
        }

        if (input == null)
        {
            Debug.LogError("YoloRunner.Run: input tensor is null.");
            return null;
        }

        // Version-safe shape check (NCHW)
        var s = input.shape;
        int b = s[0], c = s[1], h = s[2], w = s[3];
        if (b != 1 || c != 3 || h != 640 || w != 640)
        {
            Debug.LogError($"YoloRunner.Run: input shape mismatch. Got ({b},{c},{h},{w}) expected (1,3,640,640).");
            return null;
        }

        try
        {
            _worker.SetInput(inputName, input);

            // Try Schedule first (fast path)
            try
            {
                _worker.Schedule();
            }
            catch (Exception scheduleEx)
            {
                // Fallback to Execute if available
                if (_miExecute != null)
                {
                    try
                    {
                        _miExecute.Invoke(_worker, null);
                    }
                    catch (Exception execEx)
                    {
                        Debug.LogError("[YoloRunner] Execute() failed after Schedule() failed.\nSchedule(): " + scheduleEx + "\nExecute(): " + execEx);
                        return null;
                    }
                }
                else
                {
                    Debug.LogError("[YoloRunner] Schedule() failed and Execute() is not available: " + scheduleEx);
                    return null;
                }
            }

            var outTensor = _worker.PeekOutput(outputName) as Tensor<float>;
            if (outTensor == null)
            {
                Debug.LogError($"YoloRunner.Run: PeekOutput('{outputName}') returned null. Check outputName.");
                return null;
            }

            return outTensor;
        }
        catch (Exception e)
        {
            Debug.LogError("[YoloRunner] Inference exception: " + e);
            return null;
        }
    }
}