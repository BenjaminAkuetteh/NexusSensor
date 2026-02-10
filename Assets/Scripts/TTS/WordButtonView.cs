using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WordButtonView : MonoBehaviour
{
    [SerializeField] private TMP_Text emojiText;
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private Button button;

    public void Set(string emoji, string label, Action onClick)
    {
        emojiText.text = emoji;
        labelText.text = label;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClick?.Invoke());
    }
}
