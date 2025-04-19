using UnityEngine;
using Random = UnityEngine.Random;
using System.Collections.Generic;
using System.Collections;
using Unity.AI.Navigation;
using System.Linq;
using UnityEngine.AI;
using Unity.XR.CoreUtils;

public class MazeGenerator : MonoBehaviour
{
    [SerializeField] private List<GameObject> multiChunkPrefabs;  // Assign your chunk assets in inspector
    [SerializeField] private List<GameObject> singleChunkPrefabs;  // Chunks that will only be used once
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Material exitMaterial;
    [SerializeField] private GameObject exitPrefab;
    [SerializeField] private float enemySpawnDelay = 30f;
    [SerializeField] private int maxDepth = 20;

    private List<GameObject> chunkPrefabs = new List<GameObject>();
    private int currentDepth = 0;
    private List<ConnectionPoint> openConnections = new List<ConnectionPoint>();
    public List<GameObject> placedChunks = new List<GameObject>();
    private GameObject enemy;
    private ConnectionPoint exitConnection;
    private ConnectionPoint enemySpawnConnection;
    private GameObject exitInstance;

    private int regenAttempts = 0;

    void Start()
    {
        GenerateMaze(maxDepth);
        SpawnPlayer();
        StartCoroutine(SpawnEnemyAfterDelay(enemySpawnConnection, enemySpawnDelay));
        Transform oldDeadEndTransform = exitConnection.DeadEndPrefab.transform;
        Vector3 deadEndPosition = oldDeadEndTransform.position;
        Quaternion deadEndRotation = oldDeadEndTransform.rotation;

        Destroy(exitConnection.DeadEndPrefab);
        exitInstance = Instantiate(exitPrefab, deadEndPosition, deadEndRotation);
    }

    private void GenerateMaze(int maxDepth)
    {
        openConnections.Clear();
        foreach (GameObject chunk in placedChunks)
        {
            Destroy(chunk);
        }
        placedChunks.Clear();
        currentDepth = 0;

        chunkPrefabs.AddRange(multiChunkPrefabs);

        // Place first chunk at origin
        GameObject firstChunk = chunkPrefabs[0];
        GameObject firstInstance = Instantiate(firstChunk, Vector3.zero, Quaternion.identity);
        firstInstance.transform.parent = transform;

        // Add first chunk to placedChunks dictionary
        placedChunks.Add(firstInstance);

        // Add single use chunks to chunkPrefabs (they will be removed after being placed)
        chunkPrefabs.AddRange(singleChunkPrefabs);

        ConnectionPoint[] connections = firstInstance.GetComponentsInChildren<ConnectionPoint>();
        openConnections.AddRange(connections);

        int genAttempts = 0;
        while (openConnections.Count > 0 && currentDepth < maxDepth && genAttempts < 100)
        {
            int connectionIndex = Random.Range(0, openConnections.Count);
            ConnectionPoint currentConnection = openConnections[connectionIndex];
            GameObject randomChunk = chunkPrefabs[Random.Range(0, chunkPrefabs.Count)];

            if (TestConnection(randomChunk, currentConnection))
            {
                currentDepth++;  // Increment depth when adding a new chunk
                openConnections.RemoveAt(connectionIndex);
            }
            else
            {
                genAttempts++;
            }
        }

        GetComponent<NavMeshSurface>().BuildNavMesh();

        // Check if all single use chunks have been placed
        foreach (GameObject singleChunk in singleChunkPrefabs)
        {
            if (chunkPrefabs.Contains(singleChunk))
            {
                Debug.Log($"Single chunk {singleChunk.name} not placed.");
                RegenerateMaze(); // Regenerate if any single chunk is not placed
                return; // Exit the method to avoid further processing
            }
        }

        if (openConnections.Count == 0)
        {
            Debug.Log("No open connections left. Regenerating maze.");
            RegenerateMaze();
            return; // Exit the method to avoid further processing
        }
        Debug.Log($"Open connections: {openConnections.Count}");
        exitConnection = openConnections[Random.Range(0, openConnections.Count)];

        // Pick exit
        if (placedChunks.Count > 0)
        {
            // Pick a random chunk from all placed chunks.
            GameObject randomChunk = placedChunks[Random.Range(0, placedChunks.Count)];

            // Get all connection points in that chunk.
            ConnectionPoint[] randomConnections = randomChunk.GetComponentsInChildren<ConnectionPoint>();

            int maxAttempts = 100;  // Prevent infinite loop
            int attempts = 0;

            if (randomConnections.Length > 0)
            {
                // Pick a random connection point.
                enemySpawnConnection = randomConnections[Random.Range(0, randomConnections.Length)];

                // Find a connection point for the enemy spawn
                float distanceToOrigin = enemySpawnConnection.transform.position.magnitude;
                while (enemySpawnConnection.DeadEndPrefab.activeSelf == true && distanceToOrigin < 5 && attempts < maxAttempts)
                {
                    enemySpawnConnection = randomConnections[Random.Range(0, randomConnections.Length)];
                    randomChunk = placedChunks[Random.Range(0, placedChunks.Count)];
                    randomConnections = randomChunk.GetComponentsInChildren<ConnectionPoint>();
                    distanceToOrigin = enemySpawnConnection.transform.position.magnitude;
                    attempts++;
                }
            }
        }
    }

    private bool TestConnection(GameObject randomChunk, ConnectionPoint connectionPoint)
    {
        Vector3 targetPosition = transform.TransformPoint(connectionPoint.transform.position) + connectionPoint.connectionOffset;

        ConnectionPoint[] testChunkConnections = randomChunk.GetComponentsInChildren<ConnectionPoint>();
        ConnectionPoint testConnection = testChunkConnections[Random.Range(0, testChunkConnections.Length)];

        // Get the world position of the potential connection point
        Vector3 matchPosition = testConnection.connectionOffset;

        Vector3 alignmentOffset = targetPosition - matchPosition;

        int[] rotations = { 0, 1, 2, 3 };
        System.Random rng = new System.Random();
        rotations = rotations.OrderBy(x => rng.Next()).ToArray();

        foreach (int rotation in rotations)
        {
            GameObject testChunk2 = Instantiate(randomChunk);

            testChunk2.transform.position = alignmentOffset;

            Quaternion rot = Quaternion.AngleAxis(90f * rotation, Vector3.up);

            ConnectionPoint[] tempConnectionPoints = testChunk2.GetComponentsInChildren<ConnectionPoint>();

            foreach (ConnectionPoint conn in tempConnectionPoints)
            {
                conn.connectionOffset = rot * conn.connectionOffset;
            }

            testChunk2.transform.RotateAround(targetPosition, Vector3.up, 90f * rotation);

            Collider testCollider = testChunk2.GetComponent<Collider>();
            bool collisionFound = false;

            foreach (GameObject placedChunk in placedChunks)
            {
                Collider placedCollider = placedChunk.GetComponent<Collider>();
                if (placedCollider == null) continue;

                Vector3 direction;
                float distance;
                float margin = 1f;

                bool overlapped = Physics.ComputePenetration(
                    testCollider, testChunk2.transform.position, testChunk2.transform.rotation,
                    placedCollider, placedChunk.transform.position, placedChunk.transform.rotation,
                    out direction, out distance);

                if (overlapped && distance >= margin)
                {
                    collisionFound = true;
                    Destroy(testChunk2);
                }
            }
            if (!collisionFound) // If no collision was found, place the chunk
            {
                testChunk2.transform.parent = transform;
                placedChunks.Add(testChunk2);
                var testChunk2Connections = testChunk2.GetComponentsInChildren<ConnectionPoint>();

                Vector3 tempConnectionOffset = rot * testConnection.connectionOffset;
                testChunk2Connections = testChunk2Connections
                    .Where(conn => conn.connectionOffset != tempConnectionOffset).ToArray();

                openConnections.AddRange(testChunk2Connections); // Add new connections to the list
                if (connectionPoint.DeadEndPrefab != null)
                {
                    connectionPoint.DeadEndPrefab.SetActive(false);
                    ConnectionPoint matchingConnection = tempConnectionPoints
                        .FirstOrDefault(conn => conn.DeadEndPrefab != null &&
                                        conn.DeadEndPrefab.name == testConnection.DeadEndPrefab.name);
                    if (matchingConnection != null)
                    {
                        matchingConnection.DeadEndPrefab.SetActive(false);
                    }
                }

                // If the chunk is a single use chunk, remove it from the list
                if (singleChunkPrefabs.Contains(randomChunk))
                {
                    chunkPrefabs.Remove(randomChunk);
                }
                return true;
            }
        }
        return false;
    }

    public void RegenerateMaze()
    {
        if (regenAttempts >= 10)
        {
            Debug.Log($"Max regeneration attempts reached: {regenAttempts}");
            return;  // Exit if max attempts are reached
        }
        regenAttempts++;

        GameObject existingPlayer = GameObject.FindWithTag("Player");
        if (enemy != null)
        {
            Destroy(enemy);
            enemy = null;
        }

        // Clean up existing maze
        foreach (GameObject chunk in placedChunks)
        {
            chunk.SetActive(false);
            Destroy(chunk);
        }
        placedChunks.Clear();
        openConnections.Clear();
        currentDepth = 0;

        // Generate new maze
        NavMesh.RemoveAllNavMeshData();
        chunkPrefabs.Clear();
        GenerateMaze(maxDepth);

        if (existingPlayer != null)
        {
            CharacterController controller = existingPlayer.GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.enabled = false;
            }

            existingPlayer.transform.position = new Vector3(0f, 1f, 0f);

            if (controller != null)
            {
                controller.enabled = true;
            }
        }
    }

    private void SpawnPlayer()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("Player prefab not assigned!");
            return;
        }

        Vector3 spawnPosition = placedChunks[0].transform.position + new Vector3(0, 1f, 0);
        Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
    }

    public GameObject SpawnEnemy(ConnectionPoint spawnPoint)
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("Enemy prefab not assigned!");
        }

        Vector3 spawnPosition = spawnPoint.transform.position;

        GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        enemy.transform.parent = transform;
        return enemy;
    }

    private IEnumerator SpawnEnemyAfterDelay(ConnectionPoint spawnPoint, float delay)
    {
        // Wait for the specified delay
        yield return new WaitForSeconds(delay);

        // Check if the script/object is still active before spawning
        // (e.g., if RegenerateMaze was called during the delay)
        if (this != null && enabled && spawnPoint != null)
        {
            Debug.Log($"Timer finished. Spawning enemy at {spawnPoint.transform.position}...");
            // Call the original SpawnEnemy function and store the reference
            enemy = SpawnEnemy(spawnPoint);
        }
    }

    public void WinGame()
    {
        Destroy(enemy);
        enemy = null;

        exitInstance.GetNamedChild("openDoor").GetComponent<BoxCollider>().isTrigger = true;

        AudioSource audioSource = exitInstance.GetComponentInChildren<AudioSource>();
        if (audioSource != null)
        {
            audioSource.Play();
        }
        Animator animator = exitInstance.GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.SetTrigger("Open");
        }
    }
}