using UnityEngine;
using UnityEngine.XR;
using UnityEngine.InputSystem;
using Unity.XR.CoreUtils; // For XROrigin

public class XRPlayerController : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float sprintMultiplier = 1.5f;

    private CharacterController controller;
    private XROrigin xrOrigin;

    private InputAction moveAction;
    private InputAction sprintAction;

    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobAmount = 0.02f;
    private float timer = 0;
    private float defaultYPos;

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] footstepSounds;
    private float footstepTimer = 0f;
    private float footstepRate = 0.5f;

    private Transform cameraTransform;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        xrOrigin = GetComponent<XROrigin>();

        if (xrOrigin == null)
            Debug.LogError("XROrigin component not found!");

        // Set up movement input
        var actionMap = new InputActionMap("PlayerControls");
        moveAction = actionMap.AddAction("Move", binding: "<XRController>/primary2DAxis");
        sprintAction = actionMap.AddAction("Sprint", binding: "<XRController>/secondaryButton"); // B or Y button

        moveAction.Enable();
        sprintAction.Enable();

        audioSource = gameObject.AddComponent<AudioSource>();

        // Get camera position for head bobbing
        cameraTransform = xrOrigin.Camera.transform;
        defaultYPos = cameraTransform.localPosition.y;
    }

    void Update()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();

        float currentMoveSpeed = moveSpeed;
        if (sprintAction.IsPressed())
        {
            currentMoveSpeed *= sprintMultiplier;
        }

        Vector3 move = new Vector3(input.x, 0, input.y);
        move = transform.TransformDirection(move);
        controller.Move(move * currentMoveSpeed * Time.deltaTime);

        // Head Bobbing Effect
        if (move.magnitude > 0.1f) // Moving
        {
            timer += Time.deltaTime * bobSpeed;
            float bobOffset = Mathf.Sin(timer) * bobAmount;
            cameraTransform.localPosition = new Vector3(cameraTransform.localPosition.x, defaultYPos + bobOffset, cameraTransform.localPosition.z);
        }
        else // Not Moving
        {
            timer = 0;
            cameraTransform.localPosition = new Vector3(cameraTransform.localPosition.x, defaultYPos, cameraTransform.localPosition.z);
        }

        // Footstep Sounds
        if (move.magnitude > 0.1f)
        {
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
}
