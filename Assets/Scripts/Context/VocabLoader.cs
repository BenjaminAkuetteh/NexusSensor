using UnityEngine;

public static class VocabLoader
{
    public static VocabDatabase LoadFromResources(string resourcePath)
    {
        // resourcePath example: "Vocab/vocab_db"
        var json = Resources.Load<TextAsset>(resourcePath);
        if (json == null)
        {
            Debug.LogError($"VocabLoader: Could not load TextAsset at Resources/{resourcePath}.json");
            return null;
        }

        try
        {
            var db = JsonUtility.FromJson<VocabDatabase>(json.text);
            if (db == null)
            {
                Debug.LogError("VocabLoader: JsonUtility returned null (invalid JSON?)");
                return null;
            }
            return db;
        }
        catch (System.Exception e)
        {
            Debug.LogError("VocabLoader: Failed to parse JSON: " + e);
            return null;
        }
    }
}
