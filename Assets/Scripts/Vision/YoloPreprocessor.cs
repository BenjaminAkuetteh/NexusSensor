using UnityEngine;
using Unity.InferenceEngine;

public class YoloPreprocessor : MonoBehaviour
{
    public const int Size = 640;

    private RenderTexture _rt;
    private Tensor<float> _input;

    public Tensor<float> InputTensor => _input;
    public RenderTexture ResizedRT => _rt;

    private void Awake()
    {
        EnsureResources();
    }

    private void OnEnable()
    {
        EnsureResources();
    }

    private void EnsureResources()
    {
        // Create RT if missing or invalid
        if (_rt == null || !_rt.IsCreated() || _rt.width != Size || _rt.height != Size)
        {
            if (_rt != null)
            {
                _rt.Release();
                Destroy(_rt);
            }

            _rt = new RenderTexture(Size, Size, 0, RenderTextureFormat.ARGB32)
            {
                enableRandomWrite = false,
                useMipMap = false,
                autoGenerateMips = false,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            _rt.Create();
        }

        // Create tensor if missing
        if (_input == null)
        {
            // NCHW: (N,C,H,W) = (1,3,640,640)
            _input = new Tensor<float>(new TensorShape(1, 3, Size, Size));
        }
    }

    private void OnDestroy()
    {
        _input?.Dispose();

        if (_rt != null)
        {
            if (_rt.IsCreated()) _rt.Release();
            Destroy(_rt);
        }
    }

    /// <summary>
    /// Resize source -> 640x640 (GPU) then convert to float tensor NCHW normalized 0..1.
    /// </summary>
    public void Preprocess(Texture source)
    {
        if (source == null)
        {
            Debug.LogWarning("YoloPreprocessor.Preprocess called with null source.");
            return;
        }

        EnsureResources();

        if (_rt == null || !_rt.IsCreated())
        {
            Debug.LogError("YoloPreprocessor: RenderTexture not created.");
            return;
        }

        // Always blit into RT to ensure GPU-backed texture for TextureConverter
        Graphics.Blit(source, _rt);

        // Explicit transform: none (we already blit; if you need flip/rotate later, do it here)
        var transform = new TextureTransform();

        // Convert RT -> Tensor (NCHW float)
        // This avoids Texture2D CPU-readability issues and fixes the nullref path.
        TextureConverter.ToTensor(_rt, _input, transform);
    }
}