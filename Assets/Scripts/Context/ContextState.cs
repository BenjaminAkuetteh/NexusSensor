using System;

[Serializable]
public class ContextState
{
    public string locationTag; // "home", "cafeteria", "classroom", "clinic"
    public string timeTag;     // "morning", "lunch", "evening"
    public string stressTag;   // "normal", "rising", "high"
}
