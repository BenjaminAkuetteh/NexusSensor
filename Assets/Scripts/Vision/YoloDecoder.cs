using System;
using System.Collections.Generic;
using UnityEngine;

public static class YoloDecoder
{
    public struct Detection
    {
        public int classId;
        public float confidence;
        public Rect rect01; // normalized 0..1
    }

    private const int Anchors = 8400;
    private const int Channels = 84; // 4 + 80
    private const int Classes = 80;

    private enum Layout { ChannelMajor, AnchorMajor }

    public static List<Detection> Decode(
        float[] output,
        float confThreshold,
        float iouThreshold,
        int maxDetections = 50)
    {
        if (output == null || output.Length < Anchors * Channels)
            return new List<Detection>();

        // Auto-detect layout by sampling a handful of anchors
        Layout layout = DetectLayout(output);

        var dets = new List<Detection>(256);

        for (int i = 0; i < Anchors; i++)
        {
            float cx = Get(output, layout, i, 0);
            float cy = Get(output, layout, i, 1);
            float w  = Get(output, layout, i, 2);
            float h  = Get(output, layout, i, 3);

            // If values look normalized (0..1), treat as normalized pixels
            // If values look like pixels (0..640), treat as pixels
            bool looksNormalized = (cx <= 2f && cy <= 2f && w <= 2f && h <= 2f);

            float x0, y0, x1, y1;
            if (looksNormalized)
            {
                // normalized center/size
                x0 = cx - w * 0.5f;
                y0 = cy - h * 0.5f;
                x1 = cx + w * 0.5f;
                y1 = cy + h * 0.5f;
            }
            else
            {
                // pixel center/size in 640-space
                x0 = (cx - w * 0.5f) / 640f;
                y0 = (cy - h * 0.5f) / 640f;
                x1 = (cx + w * 0.5f) / 640f;
                y1 = (cy + h * 0.5f) / 640f;
            }

            x0 = Mathf.Clamp01(x0); y0 = Mathf.Clamp01(y0);
            x1 = Mathf.Clamp01(x1); y1 = Mathf.Clamp01(y1);

            float bw = x1 - x0;
            float bh = y1 - y0;

            // skip near-zero boxes
            if (bw < 0.002f || bh < 0.002f) continue;

            // best class score
            int best = -1;
            float bestScore = 0f;

            for (int c = 0; c < Classes; c++)
            {
                float s = Get(output, layout, i, 4 + c);
                if (s > bestScore)
                {
                    bestScore = s;
                    best = c;
                }
            }

            if (bestScore < confThreshold) continue;

            dets.Add(new Detection
            {
                classId = best,
                confidence = bestScore,
                rect01 = Rect.MinMaxRect(x0, y0, x1, y1)
            });
        }

        dets.Sort((a, b) => b.confidence.CompareTo(a.confidence));
        return Nms(dets, iouThreshold, maxDetections);
    }

    // -------- layout helpers --------

    private static Layout DetectLayout(float[] o)
    {
        // Sample some anchors; count how many produce "sane" width/height
        int saneChannelMajor = 0;
        int saneAnchorMajor = 0;

        for (int i = 0; i < 50; i += 5)
        {
            float wC = Get(o, Layout.ChannelMajor, i, 2);
            float hC = Get(o, Layout.ChannelMajor, i, 3);
            float wA = Get(o, Layout.AnchorMajor, i, 2);
            float hA = Get(o, Layout.AnchorMajor, i, 3);

            if (IsSaneWH(wC, hC)) saneChannelMajor++;
            if (IsSaneWH(wA, hA)) saneAnchorMajor++;
        }

        // Choose the layout with more sane samples
        return (saneAnchorMajor > saneChannelMajor) ? Layout.AnchorMajor : Layout.ChannelMajor;
    }

    private static bool IsSaneWH(float w, float h)
    {
        // either normalized (0..1-ish) or pixels (0..640-ish), but not ~0
        if (w <= 0f || h <= 0f) return false;
        if (w < 0.0001f || h < 0.0001f) return false;
        if (w > 2000f || h > 2000f) return false;
        return true;
    }

    // Get value at anchor i, channel c from flattened array
    private static float Get(float[] o, Layout layout, int anchor, int channel)
    {
        if (layout == Layout.ChannelMajor)
        {
            // o[channel, anchor] => channel*Anchors + anchor
            return o[channel * Anchors + anchor];
        }
        else
        {
            // o[anchor, channel] => anchor*Channels + channel
            return o[anchor * Channels + channel];
        }
    }

    // -------- NMS --------

    private static List<Detection> Nms(List<Detection> dets, float iouThresh, int maxDet)
    {
        var res = new List<Detection>(Mathf.Min(maxDet, dets.Count));
        var sup = new bool[dets.Count];

        for (int i = 0; i < dets.Count && res.Count < maxDet; i++)
        {
            if (sup[i]) continue;
            var a = dets[i];
            res.Add(a);

            for (int j = i + 1; j < dets.Count; j++)
            {
                if (sup[j]) continue;
                if (IoU(a.rect01, dets[j].rect01) > iouThresh)
                    sup[j] = true;
            }
        }

        return res;
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

        float union = a.width * a.height + b.width * b.height - inter;
        if (union <= 0) return 0;
        return inter / union;
    }
}