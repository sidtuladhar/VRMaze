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
    [SerializeField] private AudioClip walkingSound;
    private AudioSource enemyAudio;
    private FlashlightSystem flashlightSystem;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        enemyAudio = GetComponent<AudioSource>();
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

        flashlightSystem = player.GetComponent<FlashlightSystem>();
        if (flashlightSystem == null)
        {
            Debug.LogWarning("FlashlightSystem not found on player");
        }
    }

    // Update is called once per frame
    void Update()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= detectionRange * 2.0f && !enemyAudio.isPlaying)
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
                if (flashlightSystem != null)
                {
                    flashlightSystem.isOn = false; // Turn off flashlight
                }
                GameManager.Instance.ShowDeathUI();
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


        if (angle <= visionAngle && distanceToPlayer <= detectionRange)
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
                rightRay,
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
}
