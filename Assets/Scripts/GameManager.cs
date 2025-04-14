using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameObject Death;

    public int studentsHelped = 0;
    public int studentsToHelp = 2;

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
        if (Death != null)
        {
            // Deactivate the UI elements at the start
            Death.SetActive(false);
        }
        else
        {
            Debug.LogWarning("One or more UI GameObjects are not assigned to the GameManager in the Inspector!");
        }
    }

    void Update()
    {
        // Check for game over condition
        if (studentsHelped == studentsToHelp)
        {
            // Create a coroutine to handle the win game sequence
            MazeGenerator mazeGenerator = FindFirstObjectByType<MazeGenerator>();
            if (mazeGenerator != null)
            {
                mazeGenerator.WinGame();
            }
            else
            {
                Debug.LogError("MazeGenerator not found in the scene.");
            }
        }
    }

    public void ShowDeathUI()
    {
        Debug.Log("GameManager: ShowDeathUI called.");
        // Activate the entire Game Over UI object
        if (Death != null)
        {
            StartCoroutine(FadeInObject(Death, fadeDuration));
        }
    }

    private IEnumerator FadeInObject(GameObject objectToFade, float duration)
    {
        CanvasGroup canvasGroup = objectToFade.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            Debug.LogError($"FadeInObject Error: GameObject '{objectToFade.name}' is missing a CanvasGroup component!");
            yield break; // Stop if no CanvasGroup
        }

        canvasGroup.alpha = 0.0f;
        objectToFade.SetActive(true); // Activate the object now


        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Clamp01(timer / duration);
            yield return null;
        }

        // Ensure it's fully opaque at the end
        canvasGroup.alpha = 1f;
    }
}
