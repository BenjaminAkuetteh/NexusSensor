using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WordButtonView : MonoBehaviour
{
    [SerializeField] private TMP_Text emojiText;
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private Button button;

    public void Set(string spriteName, string label, Action onClick)
    {
        // Make sure Rich Text is enabled (it is by default, but keep safe)
        emojiText.richText = true;

        // Use TMP sprite tag
        emojiText.text = $"<sprite name=\"{spriteName}\">";

        labelText.text = label;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClick?.Invoke());
    }
}
