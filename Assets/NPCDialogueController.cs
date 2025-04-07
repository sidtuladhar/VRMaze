using UnityEngine;
using UnityEngine.UIElements;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Linq;
using System.Collections.Generic;


public class NPCDialogueController : MonoBehaviour
{
    
    [Header("UI Toolkit")]
    public UIDocument uiDocument;

    [Header("NPC Settings")]
    public string systemPrompt = "You are a student NPC in a game called School Social Anxiety simulator...";
    public float interactionRadius = 5f;

    private bool hasTalkedToPlayer = false;

    [Header("Initial Dialogue")]
    [TextArea] public string initialNPCResponse = "I got class in 5 minutes, what's up?";
    public string[] initialOptions = new string[3];
    public int[] initialReputations = new int[3];


    private VisualElement root;
    private Label reputationLabel;
    private Label npcResponseLabel;
    private Button[] optionButtons;

    private Transform player;
    private bool isPlayerInRange = false;
    private int reputation = 10;
    private bool isProcessing = false;

    private const string OpenAiApiKey = ""; // Insert your API key here
    private const string OpenAiApiUrl = "https://api.openai.com/v1/chat/completions";

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        root = uiDocument.rootVisualElement;
        uiDocument.gameObject.SetActive(false);

        reputationLabel = root.Q<Label>("reputation");
        npcResponseLabel = root.Q<Label>("NPCResponse");
        optionButtons = new Button[]
        {
            root.Q<Button>("optionOne"),
            root.Q<Button>("optionTwo"),
            root.Q<Button>("optionThree")
        };

        if (reputationLabel == null) Debug.LogError("Could not find #reputation label!");
        if (npcResponseLabel == null) Debug.LogError("Could not find #NPCResponse label!");
        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (optionButtons[i] == null) Debug.LogError($"Option button {i} not found!");
        }
        foreach (var button in optionButtons)
        {
            button.clicked += () => OnOptionSelected(button.text);
        }

        UpdateReputationUI();
    }

    void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        if (distance <= interactionRadius && !isPlayerInRange)
        {
            isPlayerInRange = true;
            uiDocument.gameObject.SetActive(true);

            if (!hasTalkedToPlayer)
                GenerateInitialDialogue();
        }

        else if (distance > interactionRadius && isPlayerInRange)
        {
            isPlayerInRange = false;
            uiDocument.gameObject.SetActive(false);
        }
    }

    private void OnOptionSelected(string playerChoice)
    {
        if (isProcessing) return;
        isProcessing = true;
        ProcessDialogue(playerChoice);
    }

    private void UpdateReputationUI()
    {
        reputationLabel.text = $"Reputation: {reputation}";
    }

    private void GenerateInitialDialogue()
    {
        npcResponseLabel.text = initialNPCResponse;

        string[] options = (string[])initialOptions.Clone();
        int[] reps = (int[])initialReputations.Clone();
        ShuffleOptions(options, reps);

        for (int i = 0; i < 3; i++)
        {
            int index = i;
            string optionText = options[index];
            int reputationChange = reps[index];

            // Try both direct text and inner label
            optionButtons[index].text = optionText;
            var innerLabel = optionButtons[index].Q<Label>();
            if (innerLabel != null)
            {
                innerLabel.text = optionText;
                Debug.Log($"[InnerLabel] Set button {index} label to: {optionText}");
            }
            else
            {
                Debug.Log($"[ButtonText] Set button {index} text to: {optionText}");
            }

            ResetButtonCallback(optionButtons[index]);
            optionButtons[index].clicked += () =>
            {
                reputation += reputationChange;
                UpdateReputationUI();
                hasTalkedToPlayer = true;
                ProcessDialogue(optionText);
            };
        }

        Debug.Log("Initial dialogue UI set.");
    }



    private void ResetButtonCallback(Button button)
    {
        // Remove all callbacks by replacing the button with a new one with the same name
        // This is a workaround for lack of `clicked -= all` in UI Toolkit
        var parent = button.parent;
        if (parent == null) return;

        var newButton = new Button();
        newButton.name = button.name;
        newButton.text = button.text;
        newButton.style.flexGrow = 1;
        parent.Insert(parent.IndexOf(button), newButton);
        parent.Remove(button);

        // Replace the reference in optionButtons array
        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (optionButtons[i].name == newButton.name)
            {
                optionButtons[i] = newButton;
                break;
            }
        }
    }



    private async void ProcessDialogue(string playerResponse)
    {
        string npcRawResponse = await SendMessageToChatGPT(playerResponse);

        (string npcResponse, string[] options, int[] reputations) = ParseAIResponse(npcRawResponse);
        npcResponseLabel.text = npcResponse;

        ShuffleOptions(options, reputations);
        for (int i = 0; i < 3; i++)
        {
            optionButtons[i].text = options[i];
            int delta = reputations[i]; // Store in closure
            optionButtons[i].clicked -= null;
            optionButtons[i].clicked += () =>
            {
                reputation += delta;
                UpdateReputationUI();
                ProcessDialogue(optionButtons[i].text);
            };
        }

        UpdateReputationUI();
        isProcessing = false;
    }

    private async Task GetAIResponse(string playerMessage)
    {
        string npcRawResponse = await SendMessageToChatGPT(playerMessage);
        (string npcResponse, string[] options, int[] reputations) = ParseAIResponse(npcRawResponse);

        npcResponseLabel.text = npcResponse;
        ShuffleOptions(options, reputations);

        for (int i = 0; i < 3; i++)
        {
            optionButtons[i].text = options[i];
            int delta = reputations[i];
            optionButtons[i].clicked -= null;
            optionButtons[i].clicked += () =>
            {
                reputation += delta;
                UpdateReputationUI();
                ProcessDialogue(optionButtons[i].text);
            };
        }

        isProcessing = false;
    }

    private async Task<string> SendMessageToChatGPT(string message)
    {
        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {OpenAiApiKey}");

            var requestBody = new
            {
                model = "gpt-4o",
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = message }
                },
                max_tokens = 200
            };

            string json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync(OpenAiApiUrl, content);
            string responseText = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonConvert.DeserializeObject<ChatGPTResponse>(responseText);
                return result.choices[0].message.content;
            }

            Debug.LogError($"ChatGPT error: {response.StatusCode} - {responseText}");
            return "Sorry, I’m having trouble responding.";
        }
    }

    private (string, string[], int[]) ParseAIResponse(string response)
    {
        string[] lines = response.Split('\n').Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();

        if (lines.Length < 4) return ("Error: AI didn't return enough lines", new[] { "Option A", "Option B", "Option C" }, new[] { 0, 0, 0 });

        string npcLine = lines[0];
        string[] options = new string[3];
        int[] reputations = new int[3];

        for (int i = 0; i < 3; i++)
        {
            string line = lines[i + 1].Trim();
            if (line.EndsWith("+1"))
            {
                options[i] = line.Replace("+1", "").Trim();
                reputations[i] = 1;
            }
            else if (line.EndsWith("-1"))
            {
                options[i] = line.Replace("-1", "").Trim();
                reputations[i] = -1;
            }
            else
            {
                options[i] = line;
                reputations[i] = 0;
            }
        }

        return (npcLine, options, reputations);
    }

    private void ShuffleOptions(string[] options, int[] reps)
    {
        for (int i = 0; i < options.Length; i++)
        {
            int rnd = Random.Range(i, options.Length);
            (options[i], options[rnd]) = (options[rnd], options[i]);
            (reps[i], reps[rnd]) = (reps[rnd], reps[i]);
        }
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
        public string content;
        public string role;
    }
}
