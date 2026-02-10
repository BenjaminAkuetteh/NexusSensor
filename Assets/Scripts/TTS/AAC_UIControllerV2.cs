using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AAC_UIControllerV2 : MonoBehaviour
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

    private ConversationState _state;
    private Category _currentCategory;

    private readonly Dictionary<Category, List<(string emoji, string text)>> _words =
        new Dictionary<Category, List<(string, string)>>()
    {
        { Category.Home, new() {
            ("👋","Hello"),("🙏","Please"),("❤️","Thank you"),
            ("✅","Yes"),("❌","No"),("🆘","Help me"),("👆","I want"),("💭","I need"),("👋","Goodbye")
        }},
        { Category.Needs, new() {
            ("🚻","Bathroom"),("💧","Water"),("🍎","Food"),("⏸️","Break"),
            ("🤫","Quiet"),("🙋","Help"),("⏰","More time"),("🛑","Stop")
        }},
        { Category.Feelings, new() {
            ("😊","Happy"),("😢","Sad"),("😠","Angry"),("😴","Tired"),
            ("😨","Scared"),("🤩","Excited"),("😕","Confused"),("😌","Calm")
        }},
        { Category.Activities, new() {
            ("🎮","Play"),("📖","Read"),("🎨","Draw"),("🎵","Music"),
            ("🌳","Outside"),("💻","Computer"),("📺","Watch"),("💬","Talk")
        }},
        { Category.People, new() {
            ("👨‍🏫","Teacher"),("👩","Mom"),("👨","Dad"),("👦","Friend"),
            ("👨‍⚕️","Doctor"),("👥","Everyone")
        }},
        { Category.Questions, new() {
            ("❓","What?"),("🗺️","Where?"),("🕐","When?"),("👤","Who?"),
            ("🤔","Why?"),("⚙️","How?"),("🙏","Can I?")
        }},
    };

    private void Awake()
    {
        _state = new ConversationState();

        // Actions
        btnUndo.onClick.AddListener(RemoveLast);
        btnClear.onClick.AddListener(ClearSentence);
        btnSpeak.onClick.AddListener(Speak);

        // Categories
        catHome.onClick.AddListener(() => SetCategory(Category.Home));
        catNeeds.onClick.AddListener(() => SetCategory(Category.Needs));
        catFeelings.onClick.AddListener(() => SetCategory(Category.Feelings));
        catActivities.onClick.AddListener(() => SetCategory(Category.Activities));
        catPeople.onClick.AddListener(() => SetCategory(Category.People));
        catQuestions.onClick.AddListener(() => SetCategory(Category.Questions));

        // Quick phrases
        qpHelp.onClick.AddListener(() => SetSentence(new[] { "I", "need", "help" }));
        qpBathroom.onClick.AddListener(() => SetSentence(new[] { "I", "want", "bathroom" }));
        qpBreak.onClick.AddListener(() => SetSentence(new[] { "I", "need", "break" }));
        qpThankYou.onClick.AddListener(() => SetSentence(new[] { "Thank", "you" }));

        SetCategory(Category.Home);
        RefreshAll();
    }

    private void SetCategory(Category category)
    {
        _currentCategory = category;
        RebuildWordGrid();
        UpdateCategoryStyles();
    }

    private void RebuildWordGrid()
    {
        // Clear existing
        for (int i = wordGridContent.childCount - 1; i >= 0; i--)
            Destroy(wordGridContent.GetChild(i).gameObject);

        var list = _words[_currentCategory];
        foreach (var (emoji, text) in list)
        {
            var btn = Instantiate(wordButtonPrefab, wordGridContent);
            btn.Set(emoji, text, () => AddWord(text));
        }
    }

    private void AddWord(string word)
    {
        _state.AddToken(word);
        RefreshAll();
    }

    private void RemoveLast()
    {
        // ConversationState doesn't expose RemoveLast yet; easiest: rebuild list
        var tokens = new List<string>(_state.Tokens);
        if (tokens.Count == 0) return;
        tokens.RemoveAt(tokens.Count - 1);

        _state.Clear();
        foreach (var t in tokens) _state.AddToken(t);

        RefreshAll();
    }

    private void ClearSentence()
    {
        _state.Clear();
        RefreshAll();
    }

    private void SetSentence(string[] words)
    {
        _state.Clear();
        foreach (var w in words) _state.AddToken(w);
        RefreshAll();
    }

    private void Speak()
    {
        var s = _state.GetSentence();
        TTSService.Speak(s);
    }

    private void RefreshAll()
    {
        var s = _state.GetSentence();
        sentenceText.text = string.IsNullOrEmpty(s) ? "Tap buttons to build your message..." : s;

        bool hasText = !string.IsNullOrEmpty(s);
        btnUndo.interactable = hasText;
        btnClear.interactable = hasText;
        btnSpeak.interactable = hasText;
    }

    private void UpdateCategoryStyles()
    {
        // Minimal “selected” effect: scale selected button slightly
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
}
