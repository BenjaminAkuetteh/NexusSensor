using UnityEngine;
using UnityEngine.SceneManagement;

public class OpenVisionSceneButton : MonoBehaviour
{
    [SerializeField] private string visionSceneName = "VisionCamera";

    public void OpenVision()
    {
        // Ensure any camera from previous scene is stopped
        CameraFrameProvider.StopAllCameras();

        SceneManager.LoadScene(visionSceneName);
    }
}