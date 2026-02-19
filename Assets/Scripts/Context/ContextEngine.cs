using System.Collections.Generic;

public static class ContextEngine
{
    // Very simple: match most tags.
    public static string ChoosePackId(VocabRuntime vocab, ContextState ctx, string fallbackPackId)
    {
        if (vocab == null || ctx == null) return fallbackPackId;

        string bestPack = fallbackPackId;
        int bestScore = -1;

        foreach (var kv in vocab.PacksById)
        {
            var pack = kv.Value;
            int score = 0;

            if (pack.tags == null) continue;

            // Score tag matches
            foreach (var t in pack.tags)
            {
                if (t == ctx.locationTag) score++;
                if (t == ctx.timeTag) score++;
                if (t == ctx.stressTag) score++;
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestPack = pack.id;
            }
        }

        return bestPack;
    }

    // Convenience helper: map dropdown to tags (for now)
    public static ContextState FromDropdown(string option)
    {
        var ctx = new ContextState { stressTag = "normal" };

        switch (option)
        {
            case "Home (Default)":
                ctx.locationTag = "home"; ctx.timeTag = "any"; break;
            case "Cafeteria (Lunch)":
                ctx.locationTag = "cafeteria"; ctx.timeTag = "lunch"; break;
            case "Classroom":
                ctx.locationTag = "classroom"; ctx.timeTag = "morning"; break;
            case "Clinic":
                ctx.locationTag = "clinic"; ctx.timeTag = "any"; break;
            default:
                ctx.locationTag = "home"; ctx.timeTag = "any"; break;
        }

        return ctx;
    }
}
