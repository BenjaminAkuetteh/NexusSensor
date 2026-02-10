using UnityEngine;
using TMPro;

public class CoreAACController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text sentenceText;

    private ConversationState _state;

    private void Awake()
    {
        _state = new ConversationState();
        RefreshSentenceUI();
    }

    public void OnWordPressed(string word)
    {
        _state.AddToken(word);
        RefreshSentenceUI();
    }

    public void OnClearPressed()
    {
        _state.Clear();
        RefreshSentenceUI();
    }

    public void OnSpeakPressed()
    {
        var sentence = _state.GetSentence();
        TTSService.Speak(sentence);
    }

    private void RefreshSentenceUI()
    {
        var s = _state.GetSentence();
        sentenceText.text = string.IsNullOrEmpty(s) ? "…" : s;
    }
}
