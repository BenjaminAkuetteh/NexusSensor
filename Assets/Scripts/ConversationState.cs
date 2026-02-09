using System.Collections.Generic;
using System.Text;

public class ConversationState
{
    private readonly List<string> _tokens = new();

    public IReadOnlyList<string> Tokens => _tokens;

    public void AddToken(string token)
    {
        if (!string.IsNullOrWhiteSpace(token))
            _tokens.Add(token.Trim());
    }

    public void Clear() => _tokens.Clear();

    public string GetSentence()
    {
        if (_tokens.Count == 0) return "";
        var sb = new StringBuilder();
        for (int i = 0; i < _tokens.Count; i++)
        {
            if (i > 0) sb.Append(" ");
            sb.Append(_tokens[i]);
        }
        return sb.ToString();
    }
}
