using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    private RectTransform _rt;
    private Rect _lastSafe;

    void Awake()
    {
        _rt = GetComponent<RectTransform>();
        Apply();
    }

    void Update()
    {
        if (Screen.safeArea != _lastSafe) Apply();
    }

    private void Apply()
    {
        _lastSafe = Screen.safeArea;

        var anchorMin = _lastSafe.position;
        var anchorMax = _lastSafe.position + _lastSafe.size;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        _rt.anchorMin = anchorMin;
        _rt.anchorMax = anchorMax;
        _rt.offsetMin = Vector2.zero;
        _rt.offsetMax = Vector2.zero;
    }
}
