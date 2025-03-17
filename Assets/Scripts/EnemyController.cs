using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class EnemyController : MonoBehaviour
{
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float visionAngle = 120f; // Half angle of vision cone
    [SerializeField] private float killRange = 1.5f;
    [SerializeField] private float normalSpeed = 3f;
    [SerializeField] private float chaseSpeed = 6f;
    [SerializeField] private float normalAcceleration = 8f;
    [SerializeField] private float chaseAcceleration = 16f;
    private NavMeshAgent agent;
    private Animator animator;
    private Transform player;
    private MazeGenerator mazeGenerator;
    private bool isChasing = false;
    private TextMeshProUGUI gameOverText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        gameOverText = GameObject.Find("GameOver").GetComponent<TextMeshProUGUI>();
        if (gameOverText != null)
        {
            gameOverText = gameOverText.GetComponent<TextMeshProUGUI>();
            gameOverText.gameObject.SetActive(false);
        }

        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        mazeGenerator = FindFirstObjectByType<MazeGenerator>();

        if (agent == null)
        {
            agent = gameObject.AddComponent<NavMeshAgent>();
            agent.speed = normalSpeed;
            agent.angularSpeed = 120f;
            agent.acceleration = 8f;
            agent.stoppingDistance = 0.5f;
            Debug.Log("NavMeshAgent component not found, adding one automatically");
        }

        // Set initial destination
        SetRandomDestination();
    }

    // Update is called once per frame
    void Update()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (IsPlayerLookingAtEnemy())
        {
            // Player detected, start chasing
            Debug.Log("Player is looking at enemy");
            isChasing = true;
            agent.SetDestination(player.position);
            agent.speed = chaseSpeed;
            agent.acceleration = chaseAcceleration;

            // Set chase animation
            if (animator != null)
            {
                animator.SetBool("isChasing", true);
                animator.SetBool("isWalking", false);
            }

            // Check if close enough to kill player
            if (distanceToPlayer <= killRange)
            {
                animator.SetBool("isAttack", true);
                startKillPlayer();
            }
        }
        else if (isChasing)
        {
            // Lost sight of player, go back to roaming
            isChasing = false;
            agent.speed = normalSpeed;
            agent.acceleration = normalAcceleration;
            SetRandomDestination();

            animator.SetBool("isChasing", false);
            animator.SetBool("isWalking", true);
        }
        else
        {
            // Check if we've reached the current destination
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                // Set new random destination
                SetRandomDestination();
            }

            // Set walking animation
            if (animator != null && agent.velocity.magnitude > 0.1f)
            {
                animator.SetBool("isWalking", true);
            }
        }
    }

    private void SetRandomDestination()
    {
        if (mazeGenerator == null || mazeGenerator.placedChunks.Count == 0)
        {
            Debug.LogWarning("MazeGenerator not found or no chunks placed");
            return;
        }

        int maxAttempts = 10;
        int attempts = 0;

        while (attempts < maxAttempts)
        {
            // Get a random chunk
            GameObject randomChunk = mazeGenerator.placedChunks[Random.Range(0, mazeGenerator.placedChunks.Count)];

            // Get all connection points in that chunk
            ConnectionPoint[] connections = randomChunk.GetComponentsInChildren<ConnectionPoint>();

            if (connections.Length > 0)
            {
                // Pick a random connection point
                ConnectionPoint randomConnection = connections[Random.Range(0, connections.Length)];

                // Check if it's an open connection (DeadEndPrefab is inactive)
                if (randomConnection.DeadEndPrefab != null && !randomConnection.DeadEndPrefab.activeSelf)
                {
                    agent.SetDestination(randomConnection.transform.position);
                    return;
                }
            }

            attempts++;
        }
        Debug.LogWarning("Failed to find a valid destination after " + maxAttempts + " attempts");
    }

    private bool IsPlayerLookingAtEnemy()
    {
        // Get the direction from player to enemy
        Vector3 playerToEnemyDirection = (transform.position - player.position).normalized;

        // Get the player's forward direction (where they're looking)
        Vector3 playerForwardDirection = player.forward;

        // Calculate the angle between these two vectors
        float angle = Vector3.Angle(playerForwardDirection, playerToEnemyDirection);

        // Check if player is looking at enemy (within a certain angle)
        float playerVisionAngle = 80f; // Player's field of view angle

        // Check distance too
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (angle <= playerVisionAngle && distanceToPlayer <= detectionRange)
        {
            // Check line of sight with SphereCast
            RaycastHit hit;
            if (Physics.SphereCast(player.position + Vector3.up * 1.0f, 0.5f,
                                  playerToEnemyDirection, out hit, detectionRange))
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void startKillPlayer()
    {
        StartCoroutine(KillPlayer());
    }

    private System.Collections.IEnumerator KillPlayer()
    {
        // Enable fog if not already enabled
        RenderSettings.fog = true;

        // Gradually increase fog density
        float elapsedTime = 0f;
        float currentFogDensity = RenderSettings.fogDensity;

        // Show game over text
        if (gameOverText != null)
        {
            gameOverText.color = new Color(1f, 0f, 0f, 0f);
            gameOverText.gameObject.SetActive(true);
        }

        // Disable player control
        PlayerController player = GetComponent<PlayerController>();
        if (player != null)
        {
            player.enabled = false;
        }


        // Gradually transition to dense fog
        while (elapsedTime < 2)
        {
            float t = elapsedTime / 2f;
            RenderSettings.fogDensity = Mathf.Lerp(currentFogDensity, 0.5f, t);

            if (gameOverText != null)
            {
                float textAlpha = Mathf.Lerp(0f, 1f, t * 2f - 0.5f); // Start fading in at 25% through the transition
                gameOverText.color = new Color(1f, 0f, 0f, Mathf.Clamp01(textAlpha));
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Game over - reload scene or show game over screen
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Optional: Visualize detection range in editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Draw vision cone
        Gizmos.color = Color.red;
        Vector3 forward = transform.forward * detectionRange;
        Quaternion leftRayRotation = Quaternion.AngleAxis(-visionAngle, Vector3.up);
        Quaternion rightRayRotation = Quaternion.AngleAxis(visionAngle, Vector3.up);
        Vector3 leftRayDirection = leftRayRotation * forward;
        Vector3 rightRayDirection = rightRayRotation * forward;
        Gizmos.DrawRay(transform.position, leftRayDirection);
        Gizmos.DrawRay(transform.position, rightRayDirection);

        Gizmos.color = Color.green;
        float sphereRadius = detectionRange; // Same radius used in your SphereCast
        Vector3 sphereStartPosition = transform.position + Vector3.up * 1.0f; // Starting position of your SphereCast

        // Draw the starting sphere
        Gizmos.DrawWireSphere(sphereStartPosition, sphereRadius);

        // If player is in scene, draw the direction and end sphere
        if (player != null)
        {
            Vector3 directionToPlayer = (player.position - transform.position).normalized;
            Vector3 sphereEndPosition = sphereStartPosition + directionToPlayer * detectionRange;

            // Draw line representing the SphereCast direction
            Gizmos.DrawLine(sphereStartPosition, sphereEndPosition);

            // Draw the end sphere
            Gizmos.DrawWireSphere(sphereEndPosition, sphereRadius);
        }
    }
}
