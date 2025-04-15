using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public int studentsHelped = 0;
    public int studentsToHelp = 3;

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
        }
    }

    void Start()
    {
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
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene("Game Over");
    }

    public void ShowWinUI()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene("You Won");
    }
}
