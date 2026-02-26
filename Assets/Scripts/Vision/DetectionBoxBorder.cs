using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Draws a hollow rectangle border around the detection box by creating
/// four thin Image strips (top, bottom, left, right).
/// Replaces the built-in Unity Outline shadow effect, which renders filled
/// offset copies of the graphic and produces a solid-looking rectangle.
/// </summary>
[DisallowMultipleComponent]
public class DetectionBoxBorder : MonoBehaviour
{
    [SerializeField] public Color  borderColor     = new Color(1f, 0f, 0.28f, 1f); // hot-pink
    [SerializeField] public float  borderThickness = 3f;

    // The four border strips (top, bottom, left, right)
    private Image[] _strips = new Image[4];

    private void Awake()
    {
        // Disable the solid Image fill on this object so nothing is drawn inside the box.
        var fill = GetComponent<Image>();
        if (fill != null) fill.enabled = false;

        // Also disable any Outline/Shadow effects that cause the solid look.
        foreach (var shadow in GetComponents<Shadow>())
            shadow.enabled = false;

        BuildStrips();
    }

    private void BuildStrips()
    {
        string[] names = { "Border_Top", "Border_Bottom", "Border_Left", "Border_Right" };

        for (int i = 0; i < 4; i++)
        {
            // Re-use existing child strip if already present (e.g. pool reuse)
            Transform existing = transform.Find(names[i]);
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject(names[i], typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(transform, false);
            }

            _strips[i] = go.GetComponent<Image>();
            _strips[i].color         = borderColor;
            _strips[i].raycastTarget = false;
        }

        LayoutStrips();
    }

    private void LayoutStrips()
    {
        float t = borderThickness;

        // Top strip  — anchored to full width at the top
        SetRect(_strips[0].rectTransform,
            ancMin: new Vector2(0, 1), ancMax: new Vector2(1, 1),
            offsetMin: new Vector2(0, -t), offsetMax: Vector2.zero);

        // Bottom strip — anchored to full width at the bottom
        SetRect(_strips[1].rectTransform,
            ancMin: Vector2.zero, ancMax: new Vector2(1, 0),
            offsetMin: Vector2.zero, offsetMax: new Vector2(0, t));

        // Left strip  — anchored to full height on the left
        SetRect(_strips[2].rectTransform,
            ancMin: Vector2.zero, ancMax: new Vector2(0, 1),
            offsetMin: Vector2.zero, offsetMax: new Vector2(t, 0));

        // Right strip — anchored to full height on the right
        SetRect(_strips[3].rectTransform,
            ancMin: new Vector2(1, 0), ancMax: Vector2.one,
            offsetMin: new Vector2(-t, 0), offsetMax: Vector2.zero);
    }

    private static void SetRect(RectTransform rt,
        Vector2 ancMin, Vector2 ancMax,
        Vector2 offsetMin, Vector2 offsetMax)
    {
        rt.anchorMin  = ancMin;
        rt.anchorMax  = ancMax;
        rt.offsetMin  = offsetMin;
        rt.offsetMax  = offsetMax;
    }

    /// <summary>Call this if you want to change the border color at runtime.</summary>
    public void SetColor(Color c)
    {
        borderColor = c;
        foreach (var s in _strips)
            if (s != null) s.color = c;
    }
}
