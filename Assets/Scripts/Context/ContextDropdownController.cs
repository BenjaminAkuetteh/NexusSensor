using TMPro;
using UnityEngine;

public class ContextDropdownController : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown dropdown;
    [SerializeField] private AAC_UIControllerV2 aacUi;

    // These should match your pack ids in JSON
    [SerializeField] private string fallbackPackId = "cafeteria_lunch"; // or "home_default" if you add it

    private VocabRuntime _vocab;

    public void Init(VocabRuntime vocab)
    {
        _vocab = vocab;
        if (dropdown != null)
        {
            dropdown.onValueChanged.RemoveAllListeners();
            dropdown.onValueChanged.AddListener(OnChanged);
        }
    }

    private void OnChanged(int idx)
    {
        if (_vocab == null || dropdown == null || aacUi == null) return;

        var option = dropdown.options[idx].text;
        var ctx = ContextEngine.FromDropdown(option);

        var chosen = ContextEngine.ChoosePackId(_vocab, ctx, fallbackPackId);
        aacUi.SetActivePack(chosen);
    }
}
