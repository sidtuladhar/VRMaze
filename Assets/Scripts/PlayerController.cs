using UnityEngine;
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 10f;
    public float mouseSensitivity = 2f;
    private CharacterController controller;
    private Camera playerCamera;
    private float xRotation = 0f;

    private float lastUpTapTime = 0f;
    private float doubleTapThreshold = 0.2f; // seconds within which a second tap counts as a double tap
    private bool isSprinting = false;
    public float sprintMultiplier = 1.5f; // how much faster you move when sprinting

    [SerializeField] private float bobSpeed = 14f;
    [SerializeField] private float bobAmount = 0.05f;
    private float defaultYPos = 0;
    private float timer = 0;

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] footstepSounds;
    private float footstepTimer = 0f;
    private float footstepRate = 0.5f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        playerCamera = Camera.main;
        playerCamera.transform.localPosition = new Vector3(0, 2.0f, 0);
        playerCamera.fieldOfView = 50f;
        playerCamera.allowHDR = true;

        // Lock the cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        defaultYPos = playerCamera.transform.localPosition.y;
        audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Update()
    {
        // Mouse Look
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.Rotate(Vector3.up * mouseX);
        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        // Multiply forward movement with sprintMultiplier if sprinting is enabled
        float currentMoveSpeed = moveSpeed;
        if (isSprinting && moveZ > 0) // only sprint when moving forward
        {
            currentMoveSpeed *= sprintMultiplier;
        }

        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        controller.Move(move * currentMoveSpeed * Time.deltaTime);


        // Sprint
        if (Input.GetKeyDown(KeyCode.W))
        {
            // If the time since the last W tap is less than the threshold, start sprinting
            if (Time.time - lastUpTapTime < doubleTapThreshold)
            {
                isSprinting = true;
            }
            lastUpTapTime = Time.time;
        }

        // Optionally, you may want to disable sprinting when the W key is released:
        if (Input.GetKeyUp(KeyCode.W))
        {
            // You may decide when to stop sprintng, e.g., immediately or after a while.
            // For example, here we'll just disable sprinting on release.
            isSprinting = false;
        }

        // Add head bob when moving
        if (moveX != 0 || moveZ != 0)
        {
            // Walking bob
            timer += Time.deltaTime * bobSpeed;
            playerCamera.transform.localPosition = new Vector3(
                playerCamera.transform.localPosition.x,
                defaultYPos + Mathf.Sin(timer) * bobAmount,
                playerCamera.transform.localPosition.z);

            footstepTimer += Time.deltaTime;
            if (footstepTimer >= footstepRate)
            {
                footstepTimer = 0f;
                if (footstepSounds.Length > 0)
                {
                    audioSource.PlayOneShot(footstepSounds[Random.Range(0, footstepSounds.Length)]);
                }
            }
        }
    }

    public static PlayerController Instance; // Singleton instance

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
