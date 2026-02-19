using System.Collections.Generic;
using System.Linq;

public enum VibeMode { Formal, Casual }

public static class SuggestionsEngine
{
    // Intent transition boosts (big win for relevance)
    // key = last word id, value = next ids to strongly prefer
    private static readonly Dictionary<string, string[]> NextBoost = new()
    {
        { "i_want",        new[] { "food", "water", "bathroom", "help", "break", "please" } },
        { "i_need",        new[] { "help", "bathroom", "break", "water", "please" } },
        { "i_would_like",  new[] { "food", "water", "please", "thank_you" } },
        { "could_i_have",  new[] { "food", "water", "please", "thank_you" } },
        { "can_i_get",     new[] { "food", "water", "bathroom", "thanks" } },
        { "food",          new[] { "please", "thank_you", "water" } },
        { "water",         new[] { "please", "thank_you" } },
        { "bathroom",      new[] { "please", "thank_you", "help" } },
        { "help",          new[] { "please", "thank_you" } }
    };

    public static List<WordItem> GetTop(
        List<WordItem> activePackWords,
        IReadOnlyList<string> tokens,
        Dictionary<string, int> usageCounts,
        int topN,
        VibeMode vibe)
    {
        if (activePackWords == null) return new List<WordItem>();
        if (usageCounts == null) usageCounts = new Dictionary<string, int>();

        var byId = activePackWords.Where(w => w != null && !string.IsNullOrWhiteSpace(w.id))
                                  .GroupBy(w => w.id)
                                  .ToDictionary(g => g.Key, g => g.First());

        bool empty = tokens == null || tokens.Count == 0;

        // IMPORTANT: your sentence tokens are labels; map last label -> id if possible
        string lastId = "";
        if (!empty)
        {
            string lastLabel = tokens[tokens.Count - 1].ToLowerInvariant();
            var match = activePackWords.FirstOrDefault(w => w.label.ToLowerInvariant() == lastLabel);
            lastId = match != null ? match.id : "";
        }

        float Score(WordItem w)
        {
            float s = 0f;

            // personalization
            if (usageCounts.TryGetValue(w.id, out int c))
                s += c * 0.06f;

            // vibe boosts
            if (vibe == VibeMode.Formal)
            {
                if (w.id == "please") s += 0.9f;
                if (w.id == "thank_you") s += 0.7f;
                if (w.id == "i_would_like") s += 0.6f;
                if (w.id == "could_i_have") s += 0.6f;
            }
            else
            {
                if (w.id == "thanks") s += 0.7f;
                if (w.id == "can_i_get") s += 0.6f;
                if (w.id == "hey") s += 0.5f;
            }

            // starter set
            if (empty)
            {
                if (w.id == "hello" || w.id == "hey") s += 1.2f;
                if (w.id == "i_want" || w.id == "i_need" || w.id == "i_would_like" || w.id == "can_i_get") s += 1.0f;
                if (w.id == "help") s += 0.9f;
                if (w.id == "yes" || w.id == "no") s += 0.7f;
            }

            // transition boosts (main relevance upgrade)
            if (!string.IsNullOrEmpty(lastId) && NextBoost.TryGetValue(lastId, out var nextIds))
            {
                // strong preference for top next words
                if (nextIds.Contains(w.id)) s += 2.0f;
            }

            // avoid repeats
            if (!empty)
            {
                int repeats = tokens.Count(t => t.ToLowerInvariant() == w.label.ToLowerInvariant());
                s -= repeats * 0.8f;
            }

            return s;
        }

        return activePackWords
            .Select(w => (w, score: Score(w)))
            .OrderByDescending(x => x.score)
            .ThenBy(x => x.w.label) // stable
            .Select(x => x.w)
            .GroupBy(w => w.id)
            .Select(g => g.First())
            .Take(topN)
            .ToList();
    }
}
