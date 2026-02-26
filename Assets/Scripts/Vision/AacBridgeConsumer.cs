using UnityEngine;

public class AacBridgeConsumer : MonoBehaviour
{
    [SerializeField] private AAC_UIControllerV2 aac;

    private float _lastAppliedTime = -999f;
    [SerializeField] private float applyInterval = 0.6f;

    private void Update()
    {
        var bridge = VisionSuggestionBridge.Instance;
        if (bridge == null || !bridge.HasData || aac == null) return;

        if (Time.unscaledTime - _lastAppliedTime < applyInterval)
            return;

        _lastAppliedTime = Time.unscaledTime;

        // Push into your UI controller
        aac.SetVisionSuggestions(bridge.WordIds, bridge.Confidences);

        // Clear after applying so it doesn't re-apply forever
        bridge.Clear();
    }
}