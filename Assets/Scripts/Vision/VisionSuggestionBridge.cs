using System.Collections.Generic;
using UnityEngine;

public class VisionSuggestionBridge : MonoBehaviour
{
    public static VisionSuggestionBridge Instance { get; private set; }

    public readonly List<string> WordIds = new();
    public readonly List<float> Confidences = new();

    public bool HasData => WordIds.Count > 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Set(List<string> ids, List<float> confs)
    {
        WordIds.Clear();
        Confidences.Clear();

        if (ids != null) WordIds.AddRange(ids);
        if (confs != null) Confidences.AddRange(confs);
    }

    public void Clear()
    {
        WordIds.Clear();
        Confidences.Clear();
    }
}