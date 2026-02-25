using System.Collections.Generic;

public class StabilityGate
{
    private readonly int _window;
    private readonly int _required;
    private readonly Queue<HashSet<string>> _history;

    public StabilityGate(int window = 3, int required = 2)
    {
        _window = window;
        _required = required;
        _history = new Queue<HashSet<string>>(window);
    }

    public List<string> FilterStable(List<string> currentIds)
    {
        var set = new HashSet<string>(currentIds);

        _history.Enqueue(set);
        while (_history.Count > _window) _history.Dequeue();

        var counts = new Dictionary<string, int>();
        foreach (var frame in _history)
        {
            foreach (var id in frame)
            {
                counts.TryGetValue(id, out int c);
                counts[id] = c + 1;
            }
        }

        var stable = new List<string>();
        foreach (var kv in counts)
        {
            if (kv.Value >= _required)
                stable.Add(kv.Key);
        }

        return stable;
    }

    public void Reset()
    {
        _history.Clear();
    }
}