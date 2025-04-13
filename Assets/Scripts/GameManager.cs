using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameObject Death;
    public GameObject BatteryBar;
    public GameObject GameOver;

    public float fadeDuration = 2.0f;

    void Awake()
    {
        // Set up the Singleton instance
        if (Instance != null && Instance != this)
        {
            // If another instance exists, destroy this one
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // Optional: If your GameManager needs to persist across scenes
            // DontDestroyOnLoad(gameObject);
        }
    }

    void Start()
    {
        if (Death != null && BatteryBar != null && GameOver != null)
        {
            // Deactivate the UI elements at the start
            Death.SetActive(false);
            BatteryBar.SetActive(true);
            GameOver.SetActive(false);
        }
        else
        {
            // Add a warning if any of the UI objects haven't been linked in the Inspector
            Debug.LogWarning("One or more UI GameObjects are not assigned to the GameManager in the Inspector!");
        }
    }

    public void ShowGameOverUI()
    {
        Debug.Log("GameManager: ShowGameOverUI called.");
        // Activate the entire Game Over UI object
        FadeInObject(GameOver, fadeDuration);

        // You can add other simple game over logic here if needed later, like:
        // Time.timeScale = 0f; // Pause game
    }

    public void ShowDeathUI()
    {
        Debug.Log("GameManager: ShowDeathUI called.");
        // Activate the entire Game Over UI object
        FadeInObject(Death, fadeDuration);

        // You can add other simple game over logic here if needed later, like:
        // Time.timeScale = 0f; // Pause game
    }

    private IEnumerator FadeInObject(GameObject objectToFade, float duration)
    {
        CanvasGroup canvasGroup = objectToFade.GetComponent<CanvasGroup>();

        // Set alpha to 0 and make sure the object is active to start the fade
        canvasGroup.alpha = 0f;
        objectToFade.SetActive(true); // Activate the object now

        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Clamp01(timer / duration); // Calculate alpha
            yield return null;
        }

        // Ensure it's fully opaque at the end
        canvasGroup.alpha = 1f;
        Debug.Log("Fade-in complete for: " + objectToFade.name);
    }
}
