using System.Collections.Generic;
using UnityEngine;

public class VisionPipelineBridgeSink : MonoBehaviour, IVisionSuggestionSink
{
    public void SetVisionSuggestions(List<string> wordIds, List<float> confidences)
    {
        if (VisionSuggestionBridge.Instance == null) return;
        VisionSuggestionBridge.Instance.Set(wordIds, confidences);
    }
}