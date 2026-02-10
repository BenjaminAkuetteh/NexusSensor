using UnityEngine;

public static class AndroidTTS
{
    private static AndroidJavaObject _tts;
    private static bool _initRequested;
    private static string _queuedText;

    public static void Speak(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

#if UNITY_ANDROID && !UNITY_EDITOR
        // If not initialized yet, queue and init once
        if (_tts == null)
        {
            _queuedText = text;
            if (!_initRequested)
                Init();
            return;
        }

        // QUEUE_FLUSH = 0
        _tts.Call<int>("speak", text, 0, null, "uttId");
#else
        Debug.Log("[AndroidTTS Editor] " + text);
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private static void Init()
    {
        _initRequested = true;

        try
        {
            using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            _tts = new AndroidJavaObject("android.speech.tts.TextToSpeech", activity, new InitListener());
        }
        catch (System.Exception e)
        {
            Debug.LogError("AndroidTTS init failed: " + e);
            _tts = null;
        }
    }

    private class InitListener : AndroidJavaProxy
    {
        public InitListener() : base("android.speech.tts.TextToSpeech$OnInitListener") { }

        void onInit(int status)
        {
            // SUCCESS = 0
            if (status != 0)
            {
                Debug.LogError("AndroidTTS init status not success: " + status);
                _tts = null;
                return;
            }

            Debug.Log("AndroidTTS initialized.");

            // Speak anything queued while initializing
            if (!string.IsNullOrWhiteSpace(_queuedText) && _tts != null)
            {
                var text = _queuedText;
                _queuedText = null;
                _tts.Call<int>("speak", text, 0, null, "uttId");
            }
        }
    }
#endif
}
