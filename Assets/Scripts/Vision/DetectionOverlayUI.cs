using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DetectionOverlayUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private RectTransform overlayRoot;     // where boxes live
    [SerializeField] private RectTransform cameraRect;      // RawImage rect
    [SerializeField] private RawImage cameraRawImage;       // RawImage component (for texture aspect + rotation)
    [SerializeField] private GameObject boxPrefab;

    [Header("Pool")]
    [SerializeField] private int poolSize = 20;

    private readonly List<GameObject> _pool = new();
    private int _activeCount = 0;

    private Canvas _canvas;
    private Camera _uiCam;

    private void Awake()
    {
        _canvas = overlayRoot != null ? overlayRoot.GetComponentInParent<Canvas>() : GetComponentInParent<Canvas>();
        _uiCam = (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? _canvas.worldCamera : null;

        WarmPool();
    }

    private void OnDisable()
    {
        SafeHideAll();
    }

    private void WarmPool()
    {
        if (overlayRoot == null || boxPrefab == null) return;

        _pool.RemoveAll(x => x == null);

        while (_pool.Count < poolSize)
        {
            var go = Instantiate(boxPrefab, overlayRoot);
            go.SetActive(false);
            _pool.Add(go);
        }
    }

    public void Render(List<YoloDecoder.Detection> dets, float minConf, int maxShown)
    {
        SafeHideAll();
        if (dets == null) return;

        WarmPool();

        int shown = 0;
        for (int i = 0; i < dets.Count && shown < maxShown; i++)
        {
            var d = dets[i];
            if (d.confidence < minConf) continue;

            var go = Get();
            if (go == null) break;

            Setup(go, d);
            shown++;
        }
    }

    // -----------------------------------------------------------------------
    // Return the WebCamTexture rotation angle (0, 90, 180, 270) and whether
    // the camera preview is mirrored horizontally.
    // -----------------------------------------------------------------------
    private void GetCameraTransform(out int rotAngle, out bool mirrorX)
    {
        rotAngle = 0;
        mirrorX = false;

        if (cameraRawImage == null) return;

        // RawImage localScale.x < 0 means horizontally mirrored
        mirrorX = cameraRawImage.rectTransform.localScale.x < 0f;

        // localEulerAngles.z is set to -videoRotationAngle in CameraPreviewUI
        float zAngle = cameraRawImage.rectTransform.localEulerAngles.z;
        // Normalize to 0-360
        zAngle = ((zAngle % 360f) + 360f) % 360f;

        // The preview applies -videoRotationAngle, so we infer videoRotationAngle = -zAngle
        float videoRot = ((-zAngle) % 360f + 360f) % 360f;

        // Round to nearest 90
        rotAngle = Mathf.RoundToInt(videoRot / 90f) * 90 % 360;
    }

    // -----------------------------------------------------------------------
    // Remap a normalized YOLO point (0-1, top-left origin, x=right, y=down)
    // into the displayed image's normalized space AFTER accounting for
    // videoRotationAngle and horizontal mirror — matching what the RawImage
    // actually shows on screen.
    // -----------------------------------------------------------------------
    private Vector2 RemapNormPoint(Vector2 p, int rotAngle, bool mirrorX)
    {
        float x = p.x, y = p.y;

        // Apply rotation (YOLO origin is top-left, y-down)
        switch (rotAngle)
        {
            case 90:
                (x, y) = (y, 1f - x);
                break;
            case 180:
                (x, y) = (1f - x, 1f - y);
                break;
            case 270:
                (x, y) = (1f - y, x);
                break;
            // 0: no change
        }

        // Apply horizontal mirror
        if (mirrorX) x = 1f - x;

        return new Vector2(x, y);
    }

    // -----------------------------------------------------------------------
    // Convert a remapped normalized point (top-left origin, y-down → y-up for
    // Unity UI) into a local position inside overlayRoot.
    // -----------------------------------------------------------------------
    private Vector2 NormToOverlayLocal(Vector2 normPt, Rect camDrawLocal)
    {
        // Unity UI y increases upward, but YOLO y increases downward.
        // camDrawLocal is in Unity local space (y-up), so yMin is the BOTTOM.
        float lx = camDrawLocal.xMin + normPt.x * camDrawLocal.width;
        float ly = camDrawLocal.yMin + (1f - normPt.y) * camDrawLocal.height; // flip y

        Vector2 worldPt = cameraRect.TransformPoint(new Vector3(lx, ly, 0));
        Vector2 screenPt = RectTransformUtility.WorldToScreenPoint(_uiCam, worldPt);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(overlayRoot, screenPt, _uiCam, out Vector2 local);
        return local;
    }

    // -----------------------------------------------------------------------
    // The displayed sub-rect (aspect-fit, accounting for rotation) in
    // cameraRect LOCAL coordinates.
    // -----------------------------------------------------------------------
    private Rect GetCameraDrawRectLocal(int rotAngle)
    {
        Rect cr = cameraRect.rect;

        float texW = 0f, texH = 0f;
        if (cameraRawImage != null && cameraRawImage.texture != null)
        {
            texW = cameraRawImage.texture.width;
            texH = cameraRawImage.texture.height;
        }

        if (texW <= 16 || texH <= 16)
            return cr;

        // After rotation 90/270, width and height are swapped visually
        float displayTexW = (rotAngle == 90 || rotAngle == 270) ? texH : texW;
        float displayTexH = (rotAngle == 90 || rotAngle == 270) ? texW : texH;

        float rectAspect = cr.width / cr.height;
        float texAspect  = displayTexW / displayTexH;

        float drawW, drawH, offsetX = 0f, offsetY = 0f;

        if (texAspect > rectAspect)
        {
            drawW   = cr.width;
            drawH   = cr.width / texAspect;
            offsetY = (cr.height - drawH) * 0.5f;
        }
        else
        {
            drawH   = cr.height;
            drawW   = cr.height * texAspect;
            offsetX = (cr.width - drawW) * 0.5f;
        }

        return new Rect(cr.xMin + offsetX, cr.yMin + offsetY, drawW, drawH);
    }

    // -----------------------------------------------------------------------
    private void Setup(GameObject go, YoloDecoder.Detection d)
    {
        GetCameraTransform(out int rotAngle, out bool mirrorX);
        Rect camDrawLocal = GetCameraDrawRectLocal(rotAngle);

        // Remap the four corners of the YOLO rect into displayed-image space
        Vector2 topLeft     = RemapNormPoint(new Vector2(d.rect01.xMin, d.rect01.yMin), rotAngle, mirrorX);
        Vector2 topRight    = RemapNormPoint(new Vector2(d.rect01.xMax, d.rect01.yMin), rotAngle, mirrorX);
        Vector2 bottomLeft  = RemapNormPoint(new Vector2(d.rect01.xMin, d.rect01.yMax), rotAngle, mirrorX);
        Vector2 bottomRight = RemapNormPoint(new Vector2(d.rect01.xMax, d.rect01.yMax), rotAngle, mirrorX);

        // Convert all four to overlay-local positions, then take the AABB
        Vector2 oTL = NormToOverlayLocal(topLeft,     camDrawLocal);
        Vector2 oTR = NormToOverlayLocal(topRight,    camDrawLocal);
        Vector2 oBL = NormToOverlayLocal(bottomLeft,  camDrawLocal);
        Vector2 oBR = NormToOverlayLocal(bottomRight, camDrawLocal);

        float minX = Mathf.Min(oTL.x, oTR.x, oBL.x, oBR.x);
        float maxX = Mathf.Max(oTL.x, oTR.x, oBL.x, oBR.x);
        float minY = Mathf.Min(oTL.y, oTR.y, oBL.y, oBR.y);
        float maxY = Mathf.Max(oTL.y, oTR.y, oBL.y, oBR.y);

        float cx = (minX + maxX) * 0.5f;
        float cy = (minY + maxY) * 0.5f;
        float w  = maxX - minX;
        float h  = maxY - minY;

        var rt = (RectTransform)go.transform;
        // ScreenPointToLocalPointInRectangle returns coordinates in the parent's
        // LOCAL space (pivot-relative). anchoredPosition with anchor=Vector2.zero
        // measures from the bottom-left CORNER — a completely different origin.
        // localPosition directly accepts pivot-relative local-space coordinates,
        // so we bypass the anchor mismatch entirely.
        rt.anchorMin    = new Vector2(0.5f, 0.5f);
        rt.anchorMax    = new Vector2(0.5f, 0.5f);
        rt.pivot        = new Vector2(0.5f, 0.5f);
        rt.localPosition = new Vector3(cx, cy, 0f);
        rt.sizeDelta    = new Vector2(w, h);

        var label = go.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            string name = (d.classId >= 0 && d.classId < CocoLabelMap.Labels.Length)
                ? CocoLabelMap.Labels[d.classId]
                : "?";
            label.text = $"{name} {(d.confidence * 100f):0}%";
        }

        go.SetActive(true);
    }

    private GameObject Get()
    {
        _pool.RemoveAll(x => x == null);

        if (_activeCount >= _pool.Count)
        {
            if (overlayRoot == null || boxPrefab == null) return null;

            var go = Instantiate(boxPrefab, overlayRoot);
            go.SetActive(false);
            _pool.Add(go);
        }

        return _pool[_activeCount++];
    }

    private void SafeHideAll()
    {
        int max = Mathf.Min(_activeCount, _pool.Count);
        for (int i = 0; i < max; i++)
        {
            var go = _pool[i];
            if (go != null) go.SetActive(false);
        }
        _activeCount = 0;
        _pool.RemoveAll(x => x == null);
    }
}