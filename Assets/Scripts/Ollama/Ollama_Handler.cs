using System;
using System.Collections;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using OllamaSharp;
using UnityEngine.Networking;

public class Ollama_Handler : MonoBehaviour
{
    public static Ollama_Handler Instance { get; private set; }

    [Header("Panel")]
    public GameObject panel;

    [Header("NPC Portrait")]
    public Image npcPortrait;

    [Header("Texts")]
    public TMP_InputField InputText;
    public TMP_Text OutputText;

    [Header("Player")]
    public PlayerMovement playerMovement;

    [Header("Auto Close")]
    public float closeDelay = 2f;

    private OllamaApiClient ollama;

    private StringBuilder conversationHistory = new StringBuilder();

    private bool isGenerating = false;
    private bool conversationStarted = false;
    private bool clientRefusesToTalk = false;
    private bool conversationEnding = false;

    // 2 = normal, 1 = incómodo/triste, 0 = ya no quiere hablar
    private int clientMood = 2;

    public bool IsOpen { get; private set; }

    private Action onConversationFinished;
    private Coroutine closeCoroutine;

    private bool pauseRequestedByThisDialogue = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        ollama = new OllamaApiClient(new Uri("http://localhost:11434"));
        ollama.SelectedModel = "qwen3:8b";

        if (panel != null)
            panel.SetActive(false);
        else
            Debug.LogWarning("Falta asignar el panel en Ollama_Handler.");

        if (playerMovement == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

            if (playerObj != null)
                playerMovement = playerObj.GetComponent<PlayerMovement>();
        }
    }

    public async void OpenDialogue(Sprite npcSprite = null, Action finishedCallback = null)
    {
        if (IsOpen || isGenerating)
            return;

        // Si otro panel ya pausó el juego, el NPC no puede abrir diálogo
        if (HotelGamePause.IsPaused)
            return;

        if (!ValidateReferences())
        {
            Debug.LogWarning("Faltan referencias en Ollama_Handler. Revisa el Inspector.");
            return;
        }

        if (closeCoroutine != null)
        {
            StopCoroutine(closeCoroutine);
            closeCoroutine = null;
        }

        onConversationFinished = finishedCallback;

        IsOpen = true;
        conversationEnding = false;

        // El diálogo también pausa el juego
        HotelGamePause.RequestPause();
        pauseRequestedByThisDialogue = true;

        panel.SetActive(true);

        if (playerMovement != null)
            playerMovement.SetMovementEnabled(false);

        if (npcPortrait != null && npcSprite != null)
            npcPortrait.sprite = npcSprite;

        SetInputEnabled(false);

        await StartNpcConversation();
    }

    public void CloseDialogue()
    {
        if (!IsOpen)
            return;

        IsOpen = false;
        isGenerating = false;
        conversationStarted = false;
        clientRefusesToTalk = false;
        conversationEnding = false;

        if (closeCoroutine != null)
        {
            StopCoroutine(closeCoroutine);
            closeCoroutine = null;
        }

        if (panel != null)
            panel.SetActive(false);

        //Liberar la pausa que pidió este diálogo
        if (pauseRequestedByThisDialogue)
        {
            HotelGamePause.ReleasePause();
            pauseRequestedByThisDialogue = false;
        }

        ClearOllamaConversationData();

        if (playerMovement != null)
            playerMovement.SetMovementEnabled(true);

        SetInputEnabled(false);

        Action finishedCallback = onConversationFinished;
        onConversationFinished = null;

        if (finishedCallback != null)
        {
            try
            {
                finishedCallback.Invoke();
            }
            catch (MissingReferenceException ex)
            {
                Debug.LogWarning("El NPC que abrió el diálogo ya no existe o fue desactivado. Se cerró solo el panel. " + ex.Message);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Error al terminar conversación con NPC: " + ex.Message);
            }
        }
    }

    public void ClearFinishedCallbackOnly()
    {
        onConversationFinished = null;
    }

    public async void Run()
    {
        if (!IsOpen)
            return;

        if (isGenerating)
            return;

        if (conversationEnding)
            return;

        if (clientRefusesToTalk)
        {
            OutputText.text = "The tourist does not want to continue the conversation.";
            SetInputEnabled(false);
            StartCloseTimer();
            return;
        }

        if (!conversationStarted)
        {
            await StartNpcConversation();
            return;
        }

        await ProcessPlayerAnswer();
    }

    private async Task StartNpcConversation()
    {
        isGenerating = true;
        conversationStarted = false;
        clientRefusesToTalk = false;
        conversationEnding = false;
        clientMood = 2;

        InputText.text = "";
        SetInputEnabled(false);

        conversationHistory.Clear();

        string prompt =
@"You are a tourist client visiting Ecuador.
You are speaking with a hotel or hospitality worker.

Start the conversation with one short, natural question.
The question must be about Ecuador: culture, food, safety, transportation, places to visit, weather, or local customs.

Do not write labels like Tourist, Client, NPC, or Hotel Worker.
Do not be repetitive.
Do not explain anything.
Do not answer your own question.
Do not ask about hotel services like breakfast, room keys, check-in, or Wi-Fi.
Use English only.
Keep it short and natural.";

        OutputText.text = "...";

        try
        {
            string npcMessage = await GenerateText(prompt);

            conversationHistory.AppendLine("Tourist: " + npcMessage);

            OutputText.text = npcMessage;

            conversationStarted = true;
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
            OutputText.text = "Error: " + ex.Message;
        }
        finally
        {
            isGenerating = false;

            if (!clientRefusesToTalk && !conversationEnding && IsOpen)
                SetInputEnabled(true);
        }
    }

    private async Task ProcessPlayerAnswer()
    {
        string playerAnswer = InputText.text;

        if (string.IsNullOrWhiteSpace(playerAnswer))
            return;

        isGenerating = true;
        SetInputEnabled(false);

        InputText.text = "";

        conversationHistory.AppendLine("Hotel worker: " + playerAnswer);

        bool playerWantsToEnd = IsGoodbyeOrClosing(playerAnswer);

        if (IsRudeAnswer(playerAnswer))
        {
            clientMood--;

            if (clientMood <= 0)
            {
                clientRefusesToTalk = true;
                conversationStarted = false;
                conversationEnding = true;

                string angryResponse = "That was very rude. I do not want to continue this conversation. Goodbye.";

                conversationHistory.AppendLine("Tourist: " + angryResponse);
                OutputText.text = angryResponse;

                isGenerating = false;
                SetInputEnabled(false);
                StartCloseTimer();
                return;
            }
        }

        string moodInstruction = GetMoodInstruction();

        string endingInstruction = playerWantsToEnd
            ? "The hotel worker is ending the conversation. Reply with a short polite goodbye and do not ask another question."
            : "The conversation may continue naturally unless it clearly feels finished.";

        string conversationPrompt =
$@"You are a tourist client visiting Ecuador.
You are having a natural conversation with a hotel or hospitality worker.

Conversation so far:
{conversationHistory}

Current tourist mood:
{moodInstruction}

Conversation ending instruction:
{endingInstruction}

Now reply as the tourist client.

Rules:
- Reply naturally to what the hotel worker said.
- Do not repeat the same question unless the worker did not answer it.
- You may ask a short follow-up question if it makes sense.
- If the worker is helpful, polite, and clear, respond positively.
- If the worker says goodbye, bye, see you, have a nice day, or anything that closes the conversation, answer with a short polite goodbye and stop.
- If the worker is rude, scary, offensive, insulting, or gives a bad answer, react sad, uncomfortable, disappointed, or upset.
- If the worker insults you, do not continue normally.
- If the worker has been rude more than once, say goodbye and stop wanting to talk.
- If your reply includes goodbye, bye, see you, thank you goodbye, or have a nice day, it should be your final reply.
- Do not write labels like Tourist, Client, NPC, or Hotel Worker.
- Do not explain your reasoning.
- Use English only.
- Keep it short and natural.";

        OutputText.text = "...";

        try
        {
            string npcResponse = await GenerateText(conversationPrompt);

            conversationHistory.AppendLine("Tourist: " + npcResponse);

            OutputText.text = npcResponse;

            bool npcWantsToEnd = IsGoodbyeOrClosing(npcResponse);

            if (playerWantsToEnd || npcWantsToEnd || clientMood <= 0)
            {
                conversationEnding = true;
                conversationStarted = false;
                SetInputEnabled(false);
                StartCloseTimer();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
            OutputText.text = "Error: " + ex.Message;
        }
        finally
        {
            isGenerating = false;

            if (!clientRefusesToTalk && !conversationEnding && IsOpen)
                SetInputEnabled(true);
        }
    }

    private bool IsRudeAnswer(string text)
    {
        string lowerText = text.ToLower();

        string[] rudeWords =
        {
            "stupid",
            "idiot",
            "shut up",
            "dumb",
            "i hate you",
            "go away",
            "ugly",
            "useless",
            "fool",
            "moron",
            "annoying",
            "bad tourist",
            "you are stupid",
            "you are dumb",
            "i don't care",
            "leave me alone",
            "pendejo",
            "fuck you",
            "fuck u"
        };

        foreach (string word in rudeWords)
        {
            if (lowerText.Contains(word))
                return true;
        }

        return false;
    }

    private bool IsGoodbyeOrClosing(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        string lowerText = text.ToLower();

        string[] closingPhrases =
        {
            "goodbye",
            "good bye",
            "bye",
            "bye bye",
            "see you",
            "see ya",
            "farewell",
            "take care",
            "have a nice day",
            "have a good day",
            "have a great day",
            "thanks, bye",
            "thank you, bye",
            "thanks goodbye",
            "thank you goodbye",
            "thank you. goodbye",
            "thanks. goodbye",
            "that's all",
            "that is all",
            "we are done",
            "conversation is over",
            "i have to go",
            "i need to go",
            "i should go",
            "talk later"
        };

        foreach (string phrase in closingPhrases)
        {
            if (lowerText.Contains(phrase))
                return true;
        }

        return false;
    }

    private string GetMoodInstruction()
    {
        if (clientMood >= 2)
            return "The tourist feels normal and open to conversation.";

        if (clientMood == 1)
            return "The tourist feels uncomfortable, sad, or disappointed, but may still answer briefly.";

        return "The tourist is upset and does not want to continue talking.";
    }

    private void StartCloseTimer()
    {
        if (closeCoroutine != null)
            StopCoroutine(closeCoroutine);

        closeCoroutine = StartCoroutine(CloseAfterDelay());
    }

    private IEnumerator CloseAfterDelay()
    {
        yield return new WaitForSecondsRealtime(closeDelay);
        CloseDialogue();
    }

    private async Task<string> GenerateText(string prompt)
    {
        StringBuilder result = new StringBuilder();

        await foreach (var chunk in ollama.GenerateAsync(prompt))
        {
            result.Append(chunk.Response);
        }

        return CleanResponse(result.ToString());
    }

    private string CleanResponse(string text)
    {
        text = text.Trim();

        text = text.Replace("Tourist:", "");
        text = text.Replace("Client:", "");
        text = text.Replace("NPC:", "");
        text = text.Replace("Hotel Worker:", "");
        text = text.Replace("Hotel worker:", "");
        text = text.Replace("Worker:", "");

        return text.Trim();
    }

    private void SetInputEnabled(bool enabled)
    {
        if (InputText != null)
            InputText.interactable = enabled;
    }

    private bool ValidateReferences()
    {
        return panel != null &&
               InputText != null &&
               OutputText != null;
    }
    public void ClearOllamaConversationData()
    {
        conversationHistory.Clear();

        if (InputText != null)
            InputText.text = "";

        if (OutputText != null)
            OutputText.text = "";

        if (npcPortrait != null)
            npcPortrait.sprite = null;

        isGenerating = false;
        conversationStarted = false;
        clientRefusesToTalk = false;
        conversationEnding = false;
        clientMood = 2;

        onConversationFinished = null;
    }

    private void OnDisable()
    {
        if (pauseRequestedByThisDialogue)
        {
            HotelGamePause.ReleasePause();
            pauseRequestedByThisDialogue = false;
        }

        if (playerMovement != null)
            playerMovement.SetMovementEnabled(true);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (pauseRequestedByThisDialogue)
        {
            HotelGamePause.ReleasePause();
            pauseRequestedByThisDialogue = false;
        }

        if (playerMovement != null)
            playerMovement.SetMovementEnabled(true);
    }

    public void UnloadOllamaModel()
    {
        StartCoroutine(UnloadOllamaModelCoroutine());
    }

    private IEnumerator UnloadOllamaModelCoroutine()
    {
        string url = "http://localhost:11434/api/generate";

        string json =
            "{\"model\":\"qwen3:8b\",\"prompt\":\"\",\"keep_alive\":0,\"stream\":false}";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("No se pudo descargar el modelo de Ollama: " + request.error);
            }
            else
            {
                Debug.Log("Modelo de Ollama descargado de memoria.");
            }
        }
    }
}