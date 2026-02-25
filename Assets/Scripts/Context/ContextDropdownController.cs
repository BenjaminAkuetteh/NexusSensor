using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ContextDropdownController : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown dropdown;
    [SerializeField] private AAC_UIControllerV2 aac;

    // Dropdown text -> base context id (NO vibe suffix here)
    private readonly Dictionary<string, string> _map = new Dictionary<string, string>
    {
        { "Home (Morning)", "home_morning" },
        { "Cafeteria (Lunch)", "cafeteria_lunch" },
        { "Classroom", "classroom_morning" },
        { "Clinic", "clinic_any" }
    };

    private void Awake()
    {
        if (dropdown != null)
        {
            dropdown.onValueChanged.RemoveAllListeners();
            dropdown.onValueChanged.AddListener(OnChanged);
        }
    }

    private void Start()
    {
        // Apply initial selection on start (so it loads immediately)
        if (dropdown != null) OnChanged(dropdown.value);
    }

    private void OnChanged(int idx)
    {
        if (dropdown == null || aac == null) return;

        string label = dropdown.options[idx].text;

        // If the label isn't in the mapping, do nothing (prevents bad ids)
        if (!_map.TryGetValue(label, out var baseContextId))
        {
            Debug.LogWarning($"ContextDropdownController: No mapping for dropdown option '{label}'.");
            return;
        }

        // This MUST exist on AAC_UIControllerV2 (base id, vibe handled inside)
        aac.SetActivePack(baseContextId);
    }
}