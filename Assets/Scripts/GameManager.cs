using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

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
            SceneManager.LoadScene("Game Over");
        }
    }
}
