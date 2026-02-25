using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Implements IVisionSuggestionSink so VisionPipeline can call SetVisionSuggestions(...)
public class AAC_UIControllerV2 : MonoBehaviour, IVisionSuggestionSink
{
    public enum Category { Home, Needs, Feelings, Activities, People, Questions }

    [Header("Sentence UI")]
    [SerializeField] private TMP_Text sentenceText;

    [Header("Action Buttons")]
    [SerializeField] private Button btnUndo;
    [SerializeField] private Button btnSpeak;
    [SerializeField] private Button btnClear;

    [Header("Category Buttons")]
    [SerializeField] private Button catHome;
    [SerializeField] private Button catNeeds;
    [SerializeField] private Button catFeelings;
    [SerializeField] private Button catActivities;
    [SerializeField] private Button catPeople;
    [SerializeField] private Button catQuestions;

    [Header("Word Grid")]
    [SerializeField] private Transform wordGridContent;
    [SerializeField] private WordButtonView wordButtonPrefab;

    [Header("Quick Phrases")]
    [SerializeField] private Button qpHelp;
    [SerializeField] private Button qpBathroom;
    [SerializeField] private Button qpBreak;
    [SerializeField] private Button qpThankYou;

    [Header("Vocab Loading (Resources JSON)")]
    [SerializeField] private string vocabResourcePath = "Vocab/vocab_db";
    [SerializeField] private string activePackId = "cafeteria_lunch";

    [Header("Dynamic Zone (Suggestions)")]
    [SerializeField] private Transform suggestionsContent;
    [SerializeField] private WordButtonView suggestionButtonPrefab; // Use your smaller suggestion prefab
    [SerializeField] private int suggestionSlots = 6;
    [SerializeField] private float suggestionsCooldownSec = 1.2f;
    [SerializeField] private float suggestionsFadeInSec = 0.5f;

    [Header("Vibe Toggle (Doc required)")]
    [SerializeField] private Toggle vibeToggle;     // ON = Formal, OFF = Casual
    [SerializeField] private TMP_Text vibeLabel;    // displays "Formal"/"Casual"

    [Header("Vision Suggestions (YOLO)")]
    [Tooltip("How long vision suggestions remain 'active' after the last update.")]
    [SerializeField] private float visionHoldSeconds = 2.0f;

    [Tooltip("Max number of vision items to reserve at the front of the Dynamic Zone.")]
    [SerializeField] private int maxVisionSlots = 3;

    private VibeMode _vibe = VibeMode.Formal;

    private ConversationState _state;
    private Category _currentCategory;

    private VocabRuntime _vocab;
    private List<WordItem> _activePackWords;

    // Exposed for ContextDropdownController if needed
    public VocabRuntime Vocab => _vocab;

    // Personalization
    private readonly Dictionary<string, int> _usageCounts = new();

    // Dynamic Zone fixed slots
    private readonly List<WordButtonView> _suggestionViews = new();
    private readonly List<CanvasGroup> _suggestionCanvasGroups = new();
    private readonly List<string> _lastSuggestionIds = new();
    private bool _canRefreshSuggestions = true;
    private Coroutine _suggestionCooldownRoutine;

    // Vibe-aware pack switching
    private string _baseContextId = "cafeteria_lunch";  // base context without vibe suffix

    // ===== Vision override state (wired to YOLO pipeline) =====
    private float _visionLastUpdate = -999f;
    private readonly Dictionary<string, float> _visionConfById = new(); // wordId -> confidence
    private readonly List<string> _visionOrderedIds = new();            // stable order (sorted by conf desc)

    private void Awake()
    {
        _state = new ConversationState();

        // Load JSON
        var db = VocabLoader.LoadFromResources(vocabResourcePath);
        _vocab = new VocabRuntime(db);
        _activePackWords = _vocab.GetWordsForPack(activePackId);

        // Actions
        if (btnUndo) btnUndo.onClick.AddListener(RemoveLast);
        if (btnClear) btnClear.onClick.AddListener(ClearSentence);
        if (btnSpeak) btnSpeak.onClick.AddListener(Speak);

        // Categories
        if (catHome) catHome.onClick.AddListener(() => SetCategory(Category.Home));
        if (catNeeds) catNeeds.onClick.AddListener(() => SetCategory(Category.Needs));
        if (catFeelings) catFeelings.onClick.AddListener(() => SetCategory(Category.Feelings));
        if (catActivities) catActivities.onClick.AddListener(() => SetCategory(Category.Activities));
        if (catPeople) catPeople.onClick.AddListener(() => SetCategory(Category.People));
        if (catQuestions) catQuestions.onClick.AddListener(() => SetCategory(Category.Questions));

        // Quick phrases
        if (qpHelp) qpHelp.onClick.AddListener(() => SetSentence(new[] { "I", "need", "help" }, forceSuggestions: true));
        if (qpBathroom) qpBathroom.onClick.AddListener(() => SetSentence(new[] { "I", "want", "bathroom" }, forceSuggestions: true));
        if (qpBreak) qpBreak.onClick.AddListener(() => SetSentence(new[] { "I", "need", "break" }, forceSuggestions: true));
        if (qpThankYou) qpThankYou.onClick.AddListener(() => SetSentence(new[] { "Thank", "you" }, forceSuggestions: true));

        // Vibe toggle
        if (vibeToggle)
        {
            vibeToggle.onValueChanged.RemoveAllListeners();
            vibeToggle.onValueChanged.AddListener(OnVibeChanged);
            OnVibeChanged(vibeToggle.isOn); // init
        }
        else
        {
            if (vibeLabel) vibeLabel.text = "Formal";
            _vibe = VibeMode.Formal;
        }

        BuildSuggestionSlots();

        SetCategory(Category.Home);
        RefreshAll(forceSuggestions: true);
    }

    // ===== Step 2.5 Doc: Global Undo Gesture entry point =====
    public void HardReset()
    {
        _state.Clear();
        _usageCounts.Clear();

        // also reset vision memory (doc "Last Intent memory to zero" equivalent for vision)
        _visionConfById.Clear();
        _visionOrderedIds.Clear();
        _visionLastUpdate = -999f;

        RefreshAll(forceSuggestions: true);
    }

    // ===== Step 2.3/2.2: Context Engine entry point =====
    public void SetActivePack(string baseContextId)
    {
        if (_vocab == null) return;

        _baseContextId = baseContextId;
        LoadContextWithVibe(force: true);
    }

    private void OnVibeChanged(bool isFormal)
    {
        _vibe = isFormal ? VibeMode.Formal : VibeMode.Casual;
        if (vibeLabel) vibeLabel.text = isFormal ? "Formal" : "Casual";

        LoadContextWithVibe(force: true);
    }

    private void SetCategory(Category category)
    {
        _currentCategory = category;
        RebuildWordGrid();
        UpdateCategoryStyles();
        RefreshSuggestions(force: true);
    }

    private void RebuildWordGrid()
    {
        for (int i = wordGridContent.childCount - 1; i >= 0; i--)
            Destroy(wordGridContent.GetChild(i).gameObject);

        if (_vocab == null) return;

        string cat = CategoryToString(_currentCategory);

        // 1) Active pack filtered by category
        var filtered = (_activePackWords ?? new List<WordItem>())
            .Where(w => w.category == cat)
            .ToList();

        // 2) Never blank: fallback to global category words
        if (filtered.Count == 0)
            filtered = GetGlobalWordsByCategory(cat);

        // Stable order (prevents perceived reshuffling)
        filtered = filtered.OrderBy(w => w.label).ToList();

        foreach (var w in filtered)
        {
            var btn = Instantiate(wordButtonPrefab, wordGridContent);
            btn.Set(w.sprite, w.label, () => AddWordFromWordItem(w));
        }
    }

    private List<WordItem> GetGlobalWordsByCategory(string category)
    {
        var list = new List<WordItem>();
        if (_vocab == null) return list;

        foreach (var w in _vocab.WordsById.Values)
            if (w.category == category)
                list.Add(w);

        return list;
    }

    private static string CategoryToString(Category c)
    {
        return c switch
        {
            Category.Home => "Home",
            Category.Needs => "Needs",
            Category.Feelings => "Feelings",
            Category.Activities => "Activities",
            Category.People => "People",
            Category.Questions => "Questions",
            _ => "Home"
        };
    }

    private void AddWordFromWordItem(WordItem item)
    {
        if (item == null) return;

        if (_usageCounts.ContainsKey(item.id)) _usageCounts[item.id]++;
        else _usageCounts[item.id] = 1;

        _state.AddToken(item.label);
        RefreshAll(forceSuggestions: false);
    }

    private void RemoveLast()
    {
        var tokens = new List<string>(_state.Tokens);
        if (tokens.Count == 0) return;
        tokens.RemoveAt(tokens.Count - 1);

        _state.Clear();
        foreach (var t in tokens) _state.AddToken(t);

        RefreshAll(forceSuggestions: false);
    }

    private void ClearSentence()
    {
        _state.Clear();
        RefreshAll(forceSuggestions: true);
    }

    private void SetSentence(string[] words, bool forceSuggestions)
    {
        _state.Clear();
        foreach (var w in words) _state.AddToken(w);
        RefreshAll(forceSuggestions: forceSuggestions);
    }

    private void Speak()
    {
        var s = _state.GetSentence();
        TTSService.Speak(s);
    }

    private void RefreshAll(bool forceSuggestions)
    {
        var s = _state.GetSentence();
        if (sentenceText)
            sentenceText.text = string.IsNullOrEmpty(s) ? "Tap buttons to build your message..." : s;

        bool hasText = !string.IsNullOrEmpty(s);
        if (btnUndo) btnUndo.interactable = hasText;
        if (btnClear) btnClear.interactable = hasText;
        if (btnSpeak) btnSpeak.interactable = hasText;

        RefreshSuggestions(forceSuggestions);
    }

    // ===========================
    // Doc-compliant Dynamic Zone
    // ===========================

    private void BuildSuggestionSlots()
    {
        if (suggestionsContent == null) return;

        var prefab = suggestionButtonPrefab != null ? suggestionButtonPrefab : wordButtonPrefab;
        if (prefab == null) return;

        for (int i = suggestionsContent.childCount - 1; i >= 0; i--)
            Destroy(suggestionsContent.GetChild(i).gameObject);

        _suggestionViews.Clear();
        _suggestionCanvasGroups.Clear();
        _lastSuggestionIds.Clear();

        for (int i = 0; i < suggestionSlots; i++)
        {
            var view = Instantiate(prefab, suggestionsContent);

            var cg = view.GetComponent<CanvasGroup>();
            if (cg == null) cg = view.gameObject.AddComponent<CanvasGroup>();

            cg.alpha = 0f;
            view.gameObject.SetActive(false);

            _suggestionViews.Add(view);
            _suggestionCanvasGroups.Add(cg);
            _lastSuggestionIds.Add("");
        }
    }

    private void RefreshSuggestions(bool force)
    {
        if (suggestionsContent == null) return;
        if (!force && !_canRefreshSuggestions) return;

        // 1) Base suggestions (context + vibe)
        var baseSuggestions = SuggestionsEngine.GetTop(
            _activePackWords ?? new List<WordItem>(),
            _state.Tokens,
            _usageCounts,
            suggestionSlots,
            _vibe
        );

        // 2) Vision override (top of the Dynamic Zone), merged deterministically
        var merged = MergeVisionWithBase(baseSuggestions);

        for (int i = 0; i < _suggestionViews.Count; i++)
        {
            var view = _suggestionViews[i];
            var cg = _suggestionCanvasGroups[i];

            if (i >= merged.Count)
            {
                view.gameObject.SetActive(false);
                cg.alpha = 0f;
                _lastSuggestionIds[i] = "";
                continue;
            }

            var item = merged[i];

            view.gameObject.SetActive(true);
            view.Set(item.sprite, item.label, () => AddWordFromWordItem(item));

            bool changed = _lastSuggestionIds[i] != item.id;
            _lastSuggestionIds[i] = item.id;

            if (changed)
            {
                cg.alpha = 0f;
                StartCoroutine(FadeInSlot(cg, suggestionsFadeInSec));
            }
            else
            {
                cg.alpha = 1f;
            }
        }

        if (!force)
            StartSuggestionsCooldown();
    }

    private List<WordItem> MergeVisionWithBase(List<WordItem> baseSuggestions)
    {
        var result = new List<WordItem>(suggestionSlots);
        var used = new HashSet<string>();

        // Vision active?
        bool visionActive = (Time.unscaledTime - _visionLastUpdate) <= visionHoldSeconds;

        if (visionActive && _vocab != null && _visionOrderedIds.Count > 0)
        {
            int take = Mathf.Clamp(maxVisionSlots, 0, suggestionSlots);

            for (int i = 0; i < _visionOrderedIds.Count && result.Count < take; i++)
            {
                string id = _visionOrderedIds[i];
                if (string.IsNullOrWhiteSpace(id)) continue;

                if (_vocab.WordsById.TryGetValue(id, out var w) && w != null)
                {
                    if (used.Add(w.id))
                        result.Add(w);
                }
            }
        }

        // Fill remaining from base engine (excluding vision duplicates)
        if (baseSuggestions != null)
        {
            foreach (var w in baseSuggestions)
            {
                if (w == null) continue;
                if (used.Add(w.id))
                    result.Add(w);
                if (result.Count >= suggestionSlots) break;
            }
        }

        // In case baseSuggestions was empty, still ensure we don't exceed slots.
        if (result.Count > suggestionSlots)
            result = result.Take(suggestionSlots).ToList();

        return result;
    }

    private void StartSuggestionsCooldown()
    {
        if (_suggestionCooldownRoutine != null) StopCoroutine(_suggestionCooldownRoutine);
        _suggestionCooldownRoutine = StartCoroutine(SuggestionsCooldownRoutine());
    }

    private IEnumerator SuggestionsCooldownRoutine()
    {
        _canRefreshSuggestions = false;
        yield return new WaitForSeconds(suggestionsCooldownSec);
        _canRefreshSuggestions = true;
    }

    private IEnumerator FadeInSlot(CanvasGroup cg, float sec)
    {
        float d = Mathf.Max(0.5f, sec); // doc minimum
        float t = 0f;

        while (t < d)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Clamp01(t / d);
            yield return null;
        }

        cg.alpha = 1f;
    }

    private void UpdateCategoryStyles()
    {
        SetSelected(catHome, _currentCategory == Category.Home);
        SetSelected(catNeeds, _currentCategory == Category.Needs);
        SetSelected(catFeelings, _currentCategory == Category.Feelings);
        SetSelected(catActivities, _currentCategory == Category.Activities);
        SetSelected(catPeople, _currentCategory == Category.People);
        SetSelected(catQuestions, _currentCategory == Category.Questions);
    }

    private static void SetSelected(Button b, bool selected)
    {
        if (b == null) return;
        b.transform.localScale = selected ? new Vector3(1.05f, 1.05f, 1f) : Vector3.one;
    }

    private void LoadContextWithVibe(bool force)
    {
        if (_vocab == null) return;

        string vibeSuffix = (_vibe == VibeMode.Formal) ? "_formal" : "_casual";
        string candidate = _baseContextId + vibeSuffix;

        // Prefer vibe-specific pack if it exists, otherwise fall back to base id
        string finalId = _vocab.PacksById.ContainsKey(candidate) ? candidate : _baseContextId;

        activePackId = finalId;
        _activePackWords = _vocab.GetWordsForPack(activePackId);

        RebuildWordGrid();
        RefreshAll(forceSuggestions: force);
    }

    // =========================================================
    // YOLO → UI hook (VisionPipeline calls this)
    // =========================================================
    public void SetVisionSuggestions(List<string> wordIds, List<float> confidences)
    {
        // Defensive: pipeline may send nulls
        if (wordIds == null) wordIds = new List<string>();
        if (confidences == null) confidences = new List<float>();

        _visionConfById.Clear();
        _visionOrderedIds.Clear();

        // Keep best confidence per id
        for (int i = 0; i < wordIds.Count; i++)
        {
            var id = wordIds[i];
            if (string.IsNullOrWhiteSpace(id)) continue;

            float conf = (i < confidences.Count) ? confidences[i] : 0f;

            if (_visionConfById.TryGetValue(id, out var cur))
            {
                if (conf > cur) _visionConfById[id] = conf;
            }
            else
            {
                _visionConfById[id] = conf;
            }
        }

        // Sort deterministically: confidence desc then id
        foreach (var kv in _visionConfById.OrderByDescending(k => k.Value).ThenBy(k => k.Key))
            _visionOrderedIds.Add(kv.Key);

        _visionLastUpdate = Time.unscaledTime;

        // Force-refresh dynamic zone to show it immediately
        RefreshAll(forceSuggestions: true);
    }
}