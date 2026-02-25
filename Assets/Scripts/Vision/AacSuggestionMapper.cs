using System.Collections.Generic;
using UnityEngine;

public class AacSuggestionMapper
{
    public struct VisionSuggestion
    {
        public string wordId;      // your vocab id, e.g. "water"
        public float confidence;   // mapped score
    }

    // label -> (wordId, minConf)
    private readonly Dictionary<string, (string wordId, float minConf)> _map =
        new Dictionary<string, (string, float)>
        {
            { "bottle", ("water", 0.35f) },
            { "cup", ("water", 0.35f) },
            { "toilet", ("bathroom", 0.30f) },
            { "person", ("help", 0.35f) },
            { "apple", ("food", 0.35f) },
            { "sandwich", ("food", 0.35f) },
            { "pizza", ("food", 0.35f) },
        };

    public List<VisionSuggestion> MapDetections(List<YoloDecoder.Detection> dets)
    {
        var results = new List<VisionSuggestion>();
        if (dets == null) return results;

        // keep best per wordId
        var best = new Dictionary<string, float>();

        foreach (var d in dets)
        {
            if (d.classId < 0 || d.classId >= CocoLabelMap.Labels.Length) continue;
            string label = CocoLabelMap.Labels[d.classId];

            if (!_map.TryGetValue(label, out var rule)) continue;
            if (d.confidence < rule.minConf) continue;

            if (!best.TryGetValue(rule.wordId, out var cur) || d.confidence > cur)
                best[rule.wordId] = d.confidence;
        }

        foreach (var kv in best)
        {
            results.Add(new VisionSuggestion { wordId = kv.Key, confidence = kv.Value });
        }

        // highest confidence first
        results.Sort((a, b) => b.confidence.CompareTo(a.confidence));
        return results;
    }
}