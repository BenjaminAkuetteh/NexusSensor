using System;
using System.Collections.Generic;

[Serializable]
public class VocabPack
{
    public string id;                 // "cafeteria_lunch"
    public string displayName;        // "Cafeteria (Lunch)"
    public List<string> tags;         // ["cafeteria","lunch"]
    public List<string> wordIds;      // ["food","water","please"...]
}
