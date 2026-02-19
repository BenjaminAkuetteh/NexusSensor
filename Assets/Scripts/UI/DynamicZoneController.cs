using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicZoneController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Transform content;
    [SerializeField] private WordButtonView buttonPrefab;

    [Header("Doc Rules")]
    [SerializeField] private int slots = 6;               // fixed slots
    [SerializeField] private float fadeInSec = 0.5f;      // >= 0.5s
    [SerializeField] private float cooldownSec = 1.2f;    // global cooldown

    private readonly List<WordButtonView> _slotViews = new();
    private readonly List<CanvasGroup> _slotCG = new();

    private bool _canRefresh = true;
    private Coroutine _cooldownRoutine;

    private List<string> _lastIds = new(); // slot-by-slot ids last shown

    private void Awake()
    {
        BuildSlots();
    }

    private void BuildSlots()
    {
        if (content == null || buttonPrefab == null) return;

        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);

        _slotViews.Clear();
        _slotCG.Clear();
        _lastIds.Clear();

        for (int i = 0; i < slots; i++)
        {
            var view = Instantiate(buttonPrefab, content);

            var cg = view.GetComponent<CanvasGroup>();
            if (cg == null) cg = view.gameObject.AddComponent<CanvasGroup>();

            cg.alpha = 0f;
            view.gameObject.SetActive(false);

            _slotViews.Add(view);
            _slotCG.Add(cg);
            _lastIds.Add(""); // empty
        }
    }

    /// <summary>
    /// Updates the Dynamic Zone with a fixed list (up to slots).
    /// Does NOT reshuffle slots; always writes slot 0..N.
    /// </summary>
    public void SetSuggestions(List<WordItem> suggestions, System.Action<WordItem> onClick, bool force = false)
    {
        if (!force && !_canRefresh) return;

        if (suggestions == null) suggestions = new List<WordItem>();

        for (int i = 0; i < _slotViews.Count; i++)
        {
            var view = _slotViews[i];
            var cg = _slotCG[i];

            WordItem item = (i < suggestions.Count) ? suggestions[i] : null;

            if (item == null)
            {
                view.gameObject.SetActive(false);
                cg.alpha = 0f;
                _lastIds[i] = "";
                continue;
            }

            view.gameObject.SetActive(true);
            view.Set(item.sprite, item.label, () => onClick?.Invoke(item));

            // Fade only when the item in this slot changes
            bool changed = _lastIds[i] != item.id;
            _lastIds[i] = item.id;

            if (changed)
            {
                StopAllCoroutines(); // keep it simple + deterministic
                cg.alpha = 0f;
                StartCoroutine(FadeIn(cg));
            }
            else
            {
                cg.alpha = 1f;
            }
        }

        if (!force)
            StartCooldown();
    }

    private void StartCooldown()
    {
        if (_cooldownRoutine != null) StopCoroutine(_cooldownRoutine);
        _cooldownRoutine = StartCoroutine(Cooldown());
    }

    private IEnumerator Cooldown()
    {
        _canRefresh = false;
        yield return new WaitForSeconds(cooldownSec);
        _canRefresh = true;
    }

    private IEnumerator FadeIn(CanvasGroup cg)
    {
        float t = 0f;
        while (t < fadeInSec)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / fadeInSec);
            cg.alpha = u; // linear
            yield return null;
        }
        cg.alpha = 1f;
    }
}
