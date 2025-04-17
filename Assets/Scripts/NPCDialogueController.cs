using UnityEngine;
using System;
using TMPro;
using UnityEngine.UI;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections;
using System.Threading;

// Can be a class or struct. Serializable might be useful if you want to save/load history.
[Serializable]
public class ChatMessage
{
    [JsonProperty("role")]
    public string Role { get; set; }

    [JsonProperty("content")]
    public string Content { get; set; }

    // Constructor for easy creation
    public ChatMessage(string role, string content)
    {
        Role = role;
        Content = content;
    }
}

[Serializable]
public class ChatGPTResponse
{
    public Choice[] choices;
}

[Serializable]
public class Choice
{
    public Message message;
}

[Serializable]
public class Message
{
    public string role;
    public string content;
}

public class NPCDialogueController : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject dialogueUI;
    public TMP_Text reputationText;
    public TMP_Text npcResponseText;
    public Button[] responseButtons;
    public TMP_Text[] responseButtonTexts;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip smallTalkClip;
    public AudioClip mediumTalkClip;
    public AudioClip buttonClickClip;

    [Header("NPC Settings")]
    public float interactionRadius = 5f;
    public string systemPrompt;
    public string[] initialNPCResponses;
    public int interactions = 0;
    public float cooldownTime = 120f;
    private bool isOnCooldown = false;
    public int maxAttempts = 10;
    public int maxReputation = 10;
    public float rotationSpeed = 180f;

    public List<ChatMessage> conversationHistory = new List<ChatMessage>();

    private Transform player;
    private bool isProcessing = false;
    private bool isInteracting = false;

    private bool lastRepIncrease = true;
    private int reputation = 0;
    private int[] optionReputationDeltas = new int[3]; // +1, -1, -1

    private int repDelta = 0;

    private string currentNPCLine = "";

    private string[] currentOptions = new string[3];

    private const string OpenAiApiKey = ""; // Add API key
    private const string OpenAiApiUrl = "https://api.openai.com/v1/chat/completions";
    private static readonly HttpClient client = InitializeHttpClient();

    private static HttpClient InitializeHttpClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {OpenAiApiKey}");
        return client;
    }

    void Start()
    {
        dialogueUI.SetActive(false);
        player = GameObject.FindGameObjectWithTag("Player").transform;

        for (int i = 0; i < responseButtons.Length; i++)
        {
            int index = i;
            responseButtons[i].onClick.AddListener(() => OnResponseClick(index));
        }
        InitializeConversation();
        UpdateReputationText();
    }

    void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (!isInteracting && distance <= interactionRadius)
        {

            Vector3 directionToPlayer = player.position - transform.position;
            directionToPlayer.y = 0; // Keep the rotation horizontal (don't tilt up/down)

            if (directionToPlayer != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            UnlockCursor();

            dialogueUI.SetActive(true);

            if (reputation != maxReputation && interactions < maxAttempts && !isOnCooldown)
            {
                npcResponseText.text = currentNPCLine;
                for (int i = 0; i < 3; i++)
                {
                    responseButtonTexts[i].text = currentOptions[i];
                }
            }
            else
            {
                for (int i = 0; i < 3; i++)
                {
                    responseButtons[i].gameObject.SetActive(false);
                }
            }

            isInteracting = true;
        }
        else if (isInteracting && distance > interactionRadius)
        {
            isInteracting = false;
            dialogueUI.SetActive(false);
            LockCursor();
        }
    }
    private IEnumerator HandleInitialDialogue(string message)
    {
        var task = SendMessageToChatGPT(message);
        while (!task.IsCompleted) yield return null;

        var result = task.Result;
        currentNPCLine = result.Item1;
        currentOptions = result.Item2;

        for (int i = 0; i < 3; i++)
        {
            if (!string.IsNullOrWhiteSpace(currentOptions[i]))
                responseButtonTexts[i].text = currentOptions[i];
            else
                responseButtonTexts[i].text = "MISSING OPTION";
        }

        UpdateReputationText();
    }

    async void OnResponseClick(int index)
    {

        if (isProcessing) return;
        isProcessing = true;

        //Play button click sound if set
        if (audioSource != null && buttonClickClip != null)
        {
            audioSource.PlayOneShot(buttonClickClip);
        }

        if (string.IsNullOrEmpty(currentOptions[index]))
        {
            Debug.LogWarning($"[Click] No valid option at index {index}");
            isProcessing = false;
            return;
        }

        string selected = currentOptions[index];
        conversationHistory.Add(new ChatMessage("user", selected));

        reputation += optionReputationDeltas[index]; // ✅ Apply rep change based on clicked option
        UpdateReputationText();
        interactions++;

        if (reputation == maxReputation && GameManager.Instance.studentsHelped < GameManager.Instance.studentsToHelp)
        {
            GameManager.Instance.studentsHelped++;
            npcResponseText.text = "Glad we had a chance to talk. You know, there are other students here who might have some interesting things to say...";
            for (int i = 0; i < 3; i++)
            {
                responseButtons[i].gameObject.SetActive(false);
            }
            return;
        }
        else if (reputation == maxReputation && GameManager.Instance.studentsHelped == GameManager.Instance.studentsToHelp)
        {
            npcResponseText.text = "Thanks for spending time to talk think you should leave now. The exit is over there, and it's safe for you to go.";
            for (int i = 0; i < 3; i++)
            {
                responseButtons[i].gameObject.SetActive(false);
            }
            return;
        }
        else if (interactions == maxAttempts)
        {
            npcResponseText.text = "Agh you're annoying me... I need a break from you. Go bother someone else.";
            for (int i = 0; i < 3; i++)
            {
                responseButtons[i].gameObject.SetActive(false);
            }
            isOnCooldown = true;
            StartCoroutine(InteractionCooldown());
            return;
        }

        var result = await SendMessageToChatGPT(selected);
        currentNPCLine = result.Item1;
        npcResponseText.text = result.Item1;

        // Determine if the most recent rep change was positive
        lastRepIncrease = repDelta > 0;

        // Play sound based on rep change
        if (audioSource != null)
        {
            if (lastRepIncrease && smallTalkClip != null)
            {
                audioSource.PlayOneShot(smallTalkClip);
            }
            else if (!lastRepIncrease && mediumTalkClip != null)
            {
                audioSource.PlayOneShot(mediumTalkClip);
            }
        }

        currentOptions = result.Item2;
        for (int i = 0; i < 3; i++)
        {
            if (!string.IsNullOrWhiteSpace(currentOptions[i]))
            {
                responseButtonTexts[i].text = currentOptions[i];
            }
            else
            {
                responseButtonTexts[i].text = "MISSING OPTION";
                Debug.LogWarning($"[UI] Button {i}: MISSING");
            }
        }

        isProcessing = false;
    }

    async Task<(string, string[], int)> SendMessageToChatGPT(string message)
    {
        var requestBody = new
        {
            model = "gpt-4o-mini",
            messages = conversationHistory,
            response_format = new
            {
                type = "json_schema",
                json_schema = new
                {
                    name = "dialogue_options_with_reputation",
                    description = "Generates personalized NPC dialgoue and three short player dialogue options, each with text and a +/- 1 reputation change. The options should be morally difficult.",
                    schema = new
                    {
                        type = "object",
                        properties = new
                        {
                            npc_dialogue = new
                            {
                                type = "string",
                                description = "The NPC's dialogue."
                            },
                            dialogue_options = new
                            {
                                type = "array",
                                description = "A list containing exactly three dialogue options.",
                                items = new
                                {
                                    type = "object",
                                    properties = new
                                    {
                                        option_text = new
                                        {
                                            type = "string",
                                            description = "The text of the dialogue option."
                                        },
                                        reputation_change = new
                                        {
                                            type = "integer",
                                            description = "Reputation change: +1 or -1.",
                                            @enum = new int[] { 1, -1 }
                                        }
                                    },
                                    required = new string[] { "option_text", "reputation_change" }, // Constraint: Require both inside item
                                    additionalProperties = false // Constraint: Disallow extra fields inside item
                                }
                            }
                        },
                        required = new string[] { "npc_dialogue", "dialogue_options" },
                        additionalProperties = false
                    },
                    strict = true // Constraint: Enforce schema strictly
                }
            }
        };

        string json = JsonConvert.SerializeObject(requestBody);

        var content = new StringContent(json, Encoding.UTF8, "application/json");

        HttpResponseMessage response = null;
        for (int attempt = 1; attempt <= 3; attempt++)
        {
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(90)))
            {
                try
                {
                    response = await client.PostAsync(OpenAiApiUrl, content, cts.Token);
                    string responseBody = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var data = JsonConvert.DeserializeObject<ChatGPTResponse>(responseBody);
                        string reply = data.choices[0].message.content.Trim();

                        Debug.Log($"[API Response] Raw: {reply}");
                        return ParseChatGPTResponse(reply);
                    }
                }
                catch (HttpRequestException e)
                {
                    Debug.LogError($"[API] Exception: {e.Message}");
                    return ("Error: Exception", new string[3], 0);
                }
            }
            Debug.Log("retrying API");
        }
        return ("Everything broke", new string[3], 0);
    }

    (string, string[], int) ParseChatGPTResponse(string fullResponse)
    {
        string npcLine = npcResponseText.text; // Default value
        string[] options = currentOptions; // Default value 
        repDelta = 0; // Used to determine sound logic

        optionReputationDeltas = new int[3] { -1, -1, -1 };

        try
        {
            var definition = new
            {
                npc_dialogue = "",
                dialogue_options = new[] { // Array of anonymous objects
                    new {
                        option_text = "",
                        reputation_change = 0
                    }
                }
            };

            var parsedJson = JsonConvert.DeserializeAnonymousType(fullResponse, definition);

            if (parsedJson != null && parsedJson.dialogue_options != null && parsedJson.dialogue_options.Length > 0 && parsedJson.npc_dialogue != null)
            {
                npcLine = parsedJson.npc_dialogue ?? "NPC dialogue missing in JSON.";

                // Determine single repDelta based on the *first* option (mimicking old logic)
                repDelta = (parsedJson.dialogue_options[0].reputation_change > 0) ? 1 : 0; // Or set to -1 if change is -1? Original code was ambiguous. Let's do 1 if positive, else 0.

                // Populate the 'options' array and the side-effect 'optionReputationDeltas' array
                for (int i = 0; i < options.Length; i++)
                {
                    if (i < parsedJson.dialogue_options.Length)
                    {
                        string text = parsedJson.dialogue_options[i].option_text ?? "Text missing";
                        int repChange = parsedJson.dialogue_options[i].reputation_change;

                        // Format option string to match old output style
                        options[i] = $"{text}";

                        // Populate side-effect array
                        optionReputationDeltas[i] = repChange;
                    }
                    else
                    {
                        optionReputationDeltas[i] = -1; // Default rep in side-effect array
                        Debug.Log($"  - Option {i + 1}: Using default '{options[i]}' (Side effect Rep: {optionReputationDeltas[i]})");
                    }
                }
                conversationHistory.Add(new ChatMessage("assistant", npcLine));

            }
            else
            {
                Debug.LogWarning("[ParseChatGPTResponse_Replicated] JSON parsed but 'dialogue_options' array is missing, null, or empty.");
            }
        }
        catch (JsonException jsonEx)
        {
            Debug.LogError($"[ParseChatGPTResponse_Replicated] Failed to parse JSON: {jsonEx.Message}");
            Debug.LogError($"[ParseChatGPTResponse_Replicated] Input string: {fullResponse}");
        }

        return (npcLine, options, repDelta);
    }

    void UpdateReputationText()
    {
        reputationText.text = $"Reputation: {reputation}";
    }

    void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void InitializeConversation()
    {
        conversationHistory.Clear();
        conversationHistory.Add(new ChatMessage("system", systemPrompt));
        conversationHistory.Add(new ChatMessage("user", "Hello?"));

        currentNPCLine = initialNPCResponses[UnityEngine.Random.Range(0, initialNPCResponses.Length)];
        npcResponseText.text = currentNPCLine;
        StartCoroutine(HandleInitialDialogue(currentNPCLine));
    }
    private IEnumerator InteractionCooldown()
    {
        yield return new WaitForSeconds(cooldownTime);

        isOnCooldown = false;
        interactions = 0;
        reputation = 0;
        UpdateReputationText();
        conversationHistory.Clear();
        InitializeConversation();
        for (int i = 0; i < responseButtons.Length; i++)
        {
            responseButtons[i].gameObject.SetActive(true);
        }
        Debug.Log("NPC cooldown finished. Interaction allowed.");
    }
}
