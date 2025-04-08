using UnityEngine;
using UnityEngine.UIElements;

public class DialogueController : MonoBehaviour
{
    [Header("UI Document Reference")]
    public UIDocument uiDocument;

    [Header("Dialogue Content")]
    [TextArea] public string npcResponseText;
    public string optionOneText;
    public string optionTwoText;
    public string optionThreeText;

    [Header("Interaction Settings")]
    public Transform player;
    public Transform npc;
    public float interactionRadius = 5f;

    private VisualElement root;
    private Label npcResponseLabel;
    private Button optionOneButton;
    private Button optionTwoButton;
    private Button optionThreeButton;

    private bool isPlayerInRange = false;

    void Start()
    {
        // Access UI root
        root = uiDocument.rootVisualElement;

        // Get references by name
        npcResponseLabel = root.Q<Label>("NPCResponse");
        optionOneButton = root.Q<Button>("optionOne");
        optionTwoButton = root.Q<Button>("optionTwo");
        optionThreeButton = root.Q<Button>("optionThree");

        // Apply initial texts
        npcResponseLabel.text = npcResponseText;
        optionOneButton.text = optionOneText;
        optionTwoButton.text = optionTwoText;
        optionThreeButton.text = optionThreeText;

        // Hide UI at start
        uiDocument.gameObject.SetActive(false);
    }

    void Update()
    {
        float distance = Vector3.Distance(player.position, npc.position);

        if (distance <= interactionRadius && !isPlayerInRange)
        {
            isPlayerInRange = true;
            uiDocument.gameObject.SetActive(true);
            UnlockCursor();
        }
        else if (distance > interactionRadius && isPlayerInRange)
        {
            isPlayerInRange = false;
            uiDocument.gameObject.SetActive(false);
        }
    }

    void UnlockCursor()
    {
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
    }

}
