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


public class NPCDialogueController : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject dialogueUI;
    public TMP_Text reputationText;
    public TMP_Text npcResponseText;
    public Button[] responseButtons;
    public TMP_Text[] responseButtonTexts;

    [Header("NPC Settings")]
    public float interactionRadius = 5f;
    public string systemPrompt;
    public string[] initialNPCResponses;

    public float rotationSpeed = 180f;

    public List<ChatMessage> conversationHistory = new List<ChatMessage>();

    private Transform player;
    private bool isConversationStarted = false;
    private bool isProcessing = false;
    private int reputation = 10;
    private int[] optionReputationDeltas = new int[3]; // +1, -1, -1, etc.

    private string currentNPCLine = "";
    private string[] currentOptions = new string[3];

    private const string OpenAiApiKey = ""; // Add key
    private const string OpenAiApiUrl = "https://api.openai.com/v1/chat/completions";

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

        if (distance <= interactionRadius)
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

            if (!isConversationStarted)
            {
                isConversationStarted = true;
                currentNPCLine = initialNPCResponses[UnityEngine.Random.Range(0, initialNPCResponses.Length)];
                npcResponseText.text = currentNPCLine;

                Debug.Log($"[Start] NPC greeting: {currentNPCLine}");
                StartCoroutine(HandleInitialDialogue(currentNPCLine));

            }
            else
            {
                npcResponseText.text = currentNPCLine;
                for (int i = 0; i < 3; i++)
                {
                    responseButtonTexts[i].text = currentOptions[i];
                }
            }
        }
        else if (distance > interactionRadius)
        {
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
                responseButtonTexts[i].text = CleanOptionForButton(currentOptions[i]);
            else
                responseButtonTexts[i].text = "MISSING OPTION";
        }

        UpdateReputationText();
    }


    async void OnResponseClick(int index)
    {
        if (isProcessing) return;
        isProcessing = true;

        if (string.IsNullOrEmpty(currentOptions[index]))
        {
            Debug.LogWarning($"[Click] No valid option at index {index}");
            isProcessing = false;
            return;
        }

        string selected = currentOptions[index];
        Debug.Log($"[Click] Player selected: {selected}");
        conversationHistory.Add(new ChatMessage("user", selected));

        reputation += optionReputationDeltas[index]; // ✅ Apply rep change based on clicked option
        UpdateReputationText();

        var result = await SendMessageToChatGPT(selected);
        currentNPCLine = result.Item1;
        npcResponseText.text = currentNPCLine;

        currentOptions = result.Item2;
        for (int i = 0; i < 3; i++)
        {
            if (!string.IsNullOrWhiteSpace(currentOptions[i]))
            {
                string displayText = CleanOptionForButton(currentOptions[i]);
                responseButtonTexts[i].text = displayText;
                Debug.Log($"[UI] Button {i}: '{displayText}'");
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
        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {OpenAiApiKey}");

            var requestBody = new
            {
                model = "gpt-4o-mini",
                messages = conversationHistory,
                max_tokens = 250,
                response_format = new
                {
                    type = "json_schema",
                    json_schema = new
                    {
                        name = "dialogue_options_with_reputation",
                        description = "Generates NPC dialgoue and three short player dialogue options, each with text and a +/- 1 reputation change.",
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
            Debug.Log(json);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            Debug.Log($"[API] Sending: {message}");

            try
            {
                HttpResponseMessage response = await client.PostAsync(OpenAiApiUrl, content);
                string jsonResult = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var data = JsonConvert.DeserializeObject<ChatGPTResponse>(jsonResult);
                    string reply = data.choices[0].message.content.Trim();

                    Debug.Log($"[API Response] Raw: {reply}");

                    return ParseChatGPTResponse(reply);
                }
                else
                {
                    Debug.LogError($"[API] Failed: {response}");
                    return ("Error: Failed API call", new string[3], 0);
                }
            }
            catch (HttpRequestException e)
            {
                Debug.LogError($"[API] Exception: {e.Message}");
                return ("Error: Exception", new string[3], 0);
            }
        }
    }

    (string, string[], int) ParseChatGPTResponse(string fullResponse)
    {
        string npcLine = "";
        string[] options = new string[3] { // Default values
            "Option Error (-1)",
            "Option Error (-1)",
            "Option Error (-1)"
        };
        int repDelta = 0;

        optionReputationDeltas = new int[3] { -1, -1, -1 };

        

        try
        {
            // 1. Define an anonymous type matching the expected JSON structure
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

            // 2. Attempt to deserialize the JSON string
            var parsedJson = JsonConvert.DeserializeAnonymousType(fullResponse, definition);

            // 3. Process the parsed data if successful
            if (parsedJson != null && parsedJson.dialogue_options != null && parsedJson.dialogue_options.Length > 0)
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

    [System.Serializable]
    private class ChatGPTResponse
    {
        public Choice[] choices;
    }

    [System.Serializable]
    private class Choice
    {
        public Message message;
    }

    [System.Serializable]
    private class Message
    {
        public string role;
        public string content;
    }
    private string CleanOptionForButton(string option)
    {
        // Remove (+1) or (-1)
        option = option.Replace("(+1)", "").Replace("(-1)", "").Trim();

        // Remove square brackets
        if (option.StartsWith("[") && option.EndsWith("]"))
        {
            option = option.Substring(1, option.Length - 2).Trim();
        }

        // Remove surrounding quotes
        if ((option.StartsWith("\"") && option.EndsWith("\"")) ||
            (option.StartsWith("'") && option.EndsWith("'")))
        {
            option = option.Substring(1, option.Length - 2).Trim();
        }

        // Remove leading dash
        if (option.StartsWith("- "))
        {
            option = option.Substring(2).Trim();
        }

        return option;
    }

    void InitializeConversation()
    {
        conversationHistory.Clear();
        conversationHistory.Add(new ChatMessage("system", systemPrompt));
        conversationHistory.Add(new ChatMessage("user", "Hello?"));
    }
}
