using UnityEngine;

public static class TTSService
{
    public static void Speak(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidTTS.Speak(text);
#elif UNITY_IOS && !UNITY_EDITOR
        iOSTTS.Speak(text);
#else
        Debug.Log("[TTS] " + text);
#endif
    }
}
