using System;
using System.Collections.Generic;

[Serializable]
public class VocabDatabase
{
    public List<WordItem> words = new();
    public List<VocabPack> packs = new();
}
