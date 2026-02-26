using UnityEngine;
using UnityEngine.SceneManagement;

public class BackToAacButton : MonoBehaviour
{
    [SerializeField] private string aacSceneName = "CoreAAC";

    public void Back()
    {
        SceneManager.LoadScene(aacSceneName);
    }
}