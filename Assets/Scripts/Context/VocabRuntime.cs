using System.Collections.Generic;
using UnityEngine;

public class VocabRuntime
{
    public readonly Dictionary<string, WordItem> WordsById = new();
    public readonly Dictionary<string, VocabPack> PacksById = new();

    public VocabRuntime(VocabDatabase db)
    {
        if (db == null) return;

        foreach (var w in db.words)
        {
            if (!string.IsNullOrWhiteSpace(w.id))
                WordsById[w.id] = w;
        }

        foreach (var p in db.packs)
        {
            if (!string.IsNullOrWhiteSpace(p.id))
                PacksById[p.id] = p;
        }
    }

    public List<WordItem> GetWordsForPack(string packId)
    {
        var result = new List<WordItem>();
        if (!PacksById.TryGetValue(packId, out var pack)) return result;

        foreach (var id in pack.wordIds)
        {
            if (WordsById.TryGetValue(id, out var w))
                result.Add(w);
        }

        return result;
    }

    public List<WordItem> FilterByCategory(List<WordItem> list, string category)
    {
        if (category == "All") return list;

        var result = new List<WordItem>();
        foreach (var w in list)
            if (w.category == category) result.Add(w);

        return result;
    }
}
