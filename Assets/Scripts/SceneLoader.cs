using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string sceneToLoad;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private AudioSource audioSource;

    public void LoadScene()
    {
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            if (clickSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(clickSound);
            }

            // Load the scene *after* the sound finishes
            float delay = (clickSound != null) ? clickSound.length : 0f;
            Invoke(nameof(ActuallyLoadScene), delay);
        }
        else
        {
            Debug.LogWarning("SceneLoader: No scene name set in the Inspector.");
        }
    }

    private void ActuallyLoadScene()
    {
        SceneManager.LoadScene(sceneToLoad);
    }
}
