using System.Collections.Generic;

public interface IVisionSuggestionSink
{
    void SetVisionSuggestions(List<string> wordIds, List<float> confidences);
}