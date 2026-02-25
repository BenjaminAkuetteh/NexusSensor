using System;
using System.Collections.Generic;
using UnityEngine;

public static class YoloDecoder
{
    public struct Detection
    {
        public int classId;
        public float confidence;
        public Rect rect01; // normalized (0..1) in 640-space
    }

    // output layout: [1, 84, 8400] in row-major.
    // index for channel c and anchor i: c*8400 + i (batch=0)
    public static List<Detection> Decode(
        ReadOnlySpan<float> output, // length = 84*8400
        float confThreshold,
        float iouThreshold,
        int maxDetections = 50)
    {
        const int anchors = 8400;
        const int channels = 84;
        const int classes = 80;

        if (output.Length < channels * anchors)
            return new List<Detection>();

        var dets = new List<Detection>(128);

        for (int i = 0; i < anchors; i++)
        {
            float cx = output[0 * anchors + i];
            float cy = output[1 * anchors + i];
            float w  = output[2 * anchors + i];
            float h  = output[3 * anchors + i];

            // best class
            int best = -1;
            float bestScore = 0f;

            int classBase = 4 * anchors;
            for (int c = 0; c < classes; c++)
            {
                float score = output[classBase + c * anchors + i];
                if (score > bestScore)
                {
                    bestScore = score;
                    best = c;
                }
            }

            float conf = bestScore;
            if (conf < confThreshold) continue;

            // YOLOv8 coords are typically in pixels relative to 640
            // Convert to normalized rect 0..1
            float x0 = (cx - w * 0.5f) / 640f;
            float y0 = (cy - h * 0.5f) / 640f;
            float x1 = (cx + w * 0.5f) / 640f;
            float y1 = (cy + h * 0.5f) / 640f;

            x0 = Mathf.Clamp01(x0); y0 = Mathf.Clamp01(y0);
            x1 = Mathf.Clamp01(x1); y1 = Mathf.Clamp01(y1);

            var rect = Rect.MinMaxRect(x0, y0, x1, y1);

            dets.Add(new Detection
            {
                classId = best,
                confidence = conf,
                rect01 = rect
            });
        }

        // Sort by confidence desc
        dets.Sort((a, b) => b.confidence.CompareTo(a.confidence));

        // NMS
        var results = new List<Detection>(Mathf.Min(maxDetections, dets.Count));
        var suppressed = new bool[dets.Count];

        for (int i = 0; i < dets.Count && results.Count < maxDetections; i++)
        {
            if (suppressed[i]) continue;

            var a = dets[i];
            results.Add(a);

            for (int j = i + 1; j < dets.Count; j++)
            {
                if (suppressed[j]) continue;

                var b = dets[j];

                // class-agnostic NMS (better stability). If you want per-class, add classId check.
                if (IoU(a.rect01, b.rect01) > iouThreshold)
                    suppressed[j] = true;
            }
        }

        return results;
    }

    private static float IoU(Rect a, Rect b)
    {
        float xA = Mathf.Max(a.xMin, b.xMin);
        float yA = Mathf.Max(a.yMin, b.yMin);
        float xB = Mathf.Min(a.xMax, b.xMax);
        float yB = Mathf.Min(a.yMax, b.yMax);

        float interW = Mathf.Max(0, xB - xA);
        float interH = Mathf.Max(0, yB - yA);
        float inter = interW * interH;

        float areaA = a.width * a.height;
        float areaB = b.width * b.height;

        float union = areaA + areaB - inter;
        if (union <= 0) return 0;
        return inter / union;
    }
}