using UnityEngine;

public class ExitTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Find the MazeGenerator and regenerate
            MazeGenerator mazeGenerator = FindFirstObjectByType<MazeGenerator>();
            if (mazeGenerator != null)
            {
                // Clean up existing maze before regenerating
                mazeGenerator.RegenerateMaze();
            }
        }
    }
}
