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
    private TextMeshProUGUI deathText;
    [SerializeField] private AudioClip walkingSound;
    private AudioSource enemyAudio;
    private FlashlightSystem flashlightSystem;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        enemyAudio = gameObject.AddComponent<AudioSource>();
        deathText = GameObject.Find("Death").GetComponent<TextMeshProUGUI>();
        if (deathText != null)
        {
            deathText = deathText.GetComponent<TextMeshProUGUI>();
            deathText.gameObject.SetActive(false);
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

        flashlightSystem = GameObject.Find("Player").GetComponent<FlashlightSystem>();
    }

    // Update is called once per frame
    void Update()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= detectionRange * 1.5f)
        {
            enemyAudio.PlayOneShot(walkingSound);
        }

        if (IsPlayerLookingAtEnemy())
        {
            // Player detected, start chasing
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
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                isChasing = false;
                agent.speed = normalSpeed;
                agent.acceleration = normalAcceleration;
                animator.SetBool("isChasing", false);
                animator.SetBool("isWalking", true);
                SetRandomDestination();
            }

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
        Vector3 playerForwardDirection = Camera.main.transform.forward;

        // Calculate the angle between these two vectors
        float angle = Vector3.Angle(playerForwardDirection, playerToEnemyDirection);

        // Check distance too
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);


        if (angle <= visionAngle && distanceToPlayer <= detectionRange && flashlightSystem.isOn)
        {

            // Cast multiple rays at slightly different positions
            Vector3 rayStart = player.position + Vector3.up * 1.0f;

            // Define ray offsets (center, slightly up, slightly down, slightly left, slightly right)
            // Instead of position offsets, use angle offsets
            Vector3 centerRay = playerToEnemyDirection;
            Vector3 leftRay = Quaternion.AngleAxis(-10, Vector3.up) * centerRay;
            Vector3 rightRay = Quaternion.AngleAxis(10, Vector3.up) * centerRay;

            Vector3[] rayDirections = new Vector3[] {
                centerRay,
                leftRay,
                rightRay
            };

            foreach (Vector3 direction in rayDirections)
            {
                RaycastHit hit;
                if (Physics.Raycast(rayStart, direction, out hit, detectionRange))
                {
                    if (hit.transform == transform || hit.transform.IsChildOf(transform))
                    {
                        return true;
                    }
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
        if (deathText != null)
        {
            deathText.color = new Color(1f, 0f, 0f, 0f);
            deathText.gameObject.SetActive(true);
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

            if (deathText != null)
            {
                float textAlpha = Mathf.Lerp(0f, 1f, t * 2f - 0.5f); // Start fading in at 25% through the transition
                deathText.color = new Color(1f, 0f, 0f, Mathf.Clamp01(textAlpha));
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
        if (player == null) return;

        // Draw detection range around player
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(player.position, detectionRange);

        // Draw the player's vision cone
        Vector3 playerForwardDirection = Camera.main != null ? Camera.main.transform.forward : player.forward;
        Gizmos.color = Color.blue;
        Vector3 playerForward = playerForwardDirection * detectionRange;
        Quaternion leftRayRotation = Quaternion.AngleAxis(-visionAngle, Vector3.up);
        Quaternion rightRayRotation = Quaternion.AngleAxis(visionAngle, Vector3.up);
        Vector3 leftRayDirection = leftRayRotation * playerForward;
        Vector3 rightRayDirection = rightRayRotation * playerForward;
        Gizmos.DrawRay(player.position, leftRayDirection);
        Gizmos.DrawRay(player.position, rightRayDirection);

        // Draw the multiple raycasts from player to enemy using angular offsets
        Vector3 playerToEnemyDirection = (transform.position - player.position).normalized;
        Vector3 rayStart = player.position + Vector3.up * 1.0f;

        // Define ray directions with angular offsets
        Vector3 centerRay = playerToEnemyDirection;
        Vector3 leftRay = Quaternion.AngleAxis(-10, Vector3.up) * centerRay;
        Vector3 rightRay = Quaternion.AngleAxis(10, Vector3.up) * centerRay;

        Vector3[] rayDirections = new Vector3[] {
        centerRay,
        leftRay,
        rightRay
    };

        // Draw each ray with a different color
        Color[] rayColors = new Color[] {
        Color.red,
        Color.cyan,
        Color.magenta
    };

        for (int i = 0; i < rayDirections.Length; i++)
        {
            Gizmos.color = rayColors[i];
            Gizmos.DrawSphere(rayStart, 0.05f); // Small sphere at ray start
            Gizmos.DrawRay(rayStart, rayDirections[i] * detectionRange);
        }

        // Draw kill range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, killRange);
    }
}
