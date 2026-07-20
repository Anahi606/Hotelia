using System;
using System.Collections;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.InputSystem;

public class Ollama_Handler : MonoBehaviour
{
    private static Ollama_Handler instance;

    public static Ollama_Handler Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<Ollama_Handler>(true);
            }

            return instance;
        }
    }

    [Header("Panel")]
    public GameObject panel;

    [Header("NPC Portrait")]
    public Image npcPortrait;

    [Header("Texts")]
    public TMP_InputField InputText;
    public TMP_Text OutputText;

    [Header("Teacher AI Parameters")]
    [SerializeField] private bool useTeacherAIParameters = true;

    [Tooltip("Maximum time the NPC waits for the student's teacher parameters to load.")]
    [Min(1f)]
    [SerializeField] private float teacherParametersLoadTimeout = 15f;

    private AIQuestionParametersData currentAIParameters;

    [Header("Azure Function")]
    [SerializeField] private string azureFunctionUrl;

    [Header("Player")]
    public PlayerMovement playerMovement;

    [Header("Gameplay Input Blocking")]
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private string interactActionName = "Interact";

    private InputAction interactAction;
    private bool interactActionWasEnabled;

    [Header("Auto Close")]
    public float closeDelay = 2f;

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

    [Header("Conversation Feedback")]
    public GameObject feedbackPanel;
    public TMP_Text feedbackTitleText;
    public TMP_Text feedbackScoreText;
    public TMP_Text feedbackDetailsText;
    public Button feedbackCloseButton;

    private int totalPlayerAnswers = 0;
    private int correctAnswerCount = 0;
    private int incorrectAnswerCount = 0;
    private int unclearAnswerCount = 0;
    private int rudeAnswerCount = 0;

    private bool usedGreeting = false;
    private bool usedCourtesy = false;
    private bool usedProfessionalClosing = false;
    private bool feedbackPanelOpen = false;

    private TourismKnowledgeItem currentTourismItem;
    private string currentNpcQuestion;
    private int wrongAnswerCount = 0;

    private enum AnswerCheckResult
    {
        Correct,
        Incorrect,
        Unclear,
        NoUsefulAnswer,
        Rude,
        Goodbye
    }

    [Serializable]
    private class TourismKnowledgeItem
    {
        public string topic;
        public string verifiedFact;
        public string questionGoal;
        public string[] correctKeywords;
        public string[] wrongKeywords;

        public TourismKnowledgeItem(
            string topic,
            string verifiedFact,
            string questionGoal,
            string[] correctKeywords,
            string[] wrongKeywords
        )
        {
            this.topic = topic;
            this.verifiedFact = verifiedFact;
            this.questionGoal = questionGoal;
            this.correctKeywords = correctKeywords;
            this.wrongKeywords = wrongKeywords;
        }
    }

    private readonly TourismKnowledgeItem[] tourismKnowledgeBase =
    {
        new TourismKnowledgeItem(
            "Loja coffee",
            "Loja is known for producing some of Ecuador's most appreciated high-quality coffee, and many people consider Loja coffee among the best in the country.",
            "Ask where tourists can find some of the best coffee in Ecuador.",
            new string[] { "loja", "lojano", "loja coffee", "cafe de loja", "café de loja" },
            new string[] { "galapagos", "galápagos", "quito only", "guayaquil only", "amazon only" }
        ),

        new TourismKnowledgeItem(
            "Capital of Ecuador",
            "Quito is the capital of Ecuador.",
            "Ask which city is the capital of Ecuador.",
            new string[] { "quito" },
            new string[] { "guayaquil", "cuenca", "manta" }
        ),

        new TourismKnowledgeItem(
            "Currency",
            "Ecuador uses the US dollar.",
            "Ask what currency tourists should use in Ecuador.",
            new string[] { "dollar", "dollars", "usd", "us dollar", "dólar", "dolares", "dólares" },
            new string[] { "euro", "euros", "sucre", "pesos", "peso", "sol" }
        ),

        new TourismKnowledgeItem(
            "Galapagos Islands",
            "The Galápagos Islands are famous for unique wildlife such as giant tortoises, marine iguanas, and birds.",
            "Ask why the Galápagos Islands are famous.",
            new string[] { "wildlife", "animals", "tortoise", "tortoises", "turtle", "turtles", "iguana", "iguanas", "birds", "unique species" },
            new string[] { "skyscrapers", "snow", "desert only", "nothing special" }
        ),

        new TourismKnowledgeItem(
            "Cotopaxi",
            "Cotopaxi is a famous volcano in Ecuador and a popular place to visit from Quito.",
            "Ask about visiting Cotopaxi from Quito.",
            new string[] { "cotopaxi", "volcano", "volcán", "from quito", "near quito" },
            new string[] { "peru", "colombia", "not ecuador", "beach" }
        ),

        new TourismKnowledgeItem(
            "Mitad del Mundo",
            "Mitad del Mundo is a popular tourist site near Quito related to the equator line.",
            "Ask what Mitad del Mundo is.",
            new string[] { "equator", "ecuador line", "middle of the world", "mitad del mundo", "line" },
            new string[] { "beach", "galapagos", "airport", "shopping mall" }
        ),

        new TourismKnowledgeItem(
            "Quito Historic Center",
            "Quito's Historic Center is known for colonial architecture, churches, plazas, and cultural heritage.",
            "Ask what tourists can see in Quito's Historic Center.",
            new string[] { "historic center", "old town", "church", "churches", "plaza", "colonial", "architecture", "heritage" },
            new string[] { "modern beach", "theme park", "snow resort" }
        ),

        new TourismKnowledgeItem(
            "Ecuadorian food",
            "Popular Ecuadorian foods include encebollado, ceviche, locro de papa, hornado, llapingachos, bolón, fanesca, guatita, bolon de verde, arveja con guineo, repe and cuy.",
            "Ask for a traditional Ecuadorian food recommendation.",
            new string[] { "encebollado", "ceviche", "locro", "hornado", "llapingacho", "llapingachos", "bolon", "bolón", "fanesca", "guatita", "bolon de verde", "arveja con guineo", "encebollado", "repe" },
            new string[] { "sushi", "ramen", "taco", "paella", "pizza only", "hamburger" }
        ),

        new TourismKnowledgeItem(
            "Weather in Quito",
            "Quito has mild weather because of its altitude, and tourists should be prepared for sun, clouds, and possible rain in the same day.",
            "Ask what kind of weather to expect in Quito.",
            new string[] { "mild", "altitude", "sun", "rain", "clouds", "jacket", "sweater", "layers", "same day" },
            new string[] { "always snow", "always hot", "tropical beach weather" }
        ),

        new TourismKnowledgeItem(
            "Safety in Quito",
            "Tourists in Quito should use official taxis or transport apps, keep belongings secure, and be careful with phones in public areas.",
            "Ask for a safety tip while moving around Quito.",
            new string[] { "official taxi", "taxi", "uber", "cabify", "metro", "belongings", "phone", "bag", "careful", "safe" },
            new string[] { "show money", "leave phone", "trust strangers", "unsafe" }
        ),

        new TourismKnowledgeItem(
            "Ecuador regions",
            "Ecuador has four main regions: Coast, Andes, Amazon, and Galápagos.",
            "Ask about the main regions of Ecuador.",
            new string[] { "coast", "andes", "amazon", "galapagos", "galápagos", "four regions", "4 regions" },
            new string[] { "only coast", "only jungle", "only mountains" }
        ),

        new TourismKnowledgeItem(
            "Amazon region",
            "The Ecuadorian Amazon is known for rainforest, rivers, biodiversity, indigenous cultures, and nature tourism.",
            "Ask what tourists can experience in the Ecuadorian Amazon.",
            new string[] { "rainforest", "jungle", "amazon", "rivers", "biodiversity", "nature", "indigenous", "wildlife" },
            new string[] { "snow", "desert", "skyscrapers", "galapagos only" }
        ),

        new TourismKnowledgeItem(
            "Otavalo Market",
            "Otavalo is known for its traditional market, textiles, crafts, and indigenous culture.",
            "Ask what Otavalo is famous for.",
            new string[] { "market", "textiles", "crafts", "indigenous", "otavalo", "ponchos", "handmade" },
            new string[] { "beach", "airport", "volcano only" }
        ),

        new TourismKnowledgeItem(
            "Baños",
            "Baños is known for waterfalls, adventure tourism, hot springs, and views near Tungurahua volcano.",
            "Ask what tourists can do in Baños.",
            new string[] { "waterfalls", "adventure", "hot springs", "thermal baths", "baños", "swing", "tungurahua" },
            new string[] { "galapagos", "capital city", "airport" }
        ),

        new TourismKnowledgeItem(
            "Cuenca",
            "Cuenca is known for colonial architecture, culture, churches, museums, and being a beautiful Andean city.",
            "Ask what Cuenca is known for.",
            new string[] { "colonial", "architecture", "churches", "culture", "museums", "andes", "cuenca" },
            new string[] { "capital", "beach", "galapagos" }
        ),

        new TourismKnowledgeItem(
            "Guayaquil",
            "Guayaquil is a major coastal city known for the Malecón 2000, riverfront areas, commerce, and warm weather.",
            "Ask what tourists can visit in Guayaquil.",
            new string[] {"malecon", "malecón", "malecon 2000", "malecón 2000", "riverfront", "guayaquil", "warm weather", "coastal", "las peñas", "las penas", "santa ana", "cerro santa ana", "parque historico", "parque histórico"},
            new string[] { "capital", "snow", "andes only" }
        ),

        new TourismKnowledgeItem(
            "Ecuador language",
            "Spanish is the official language of Ecuador, and some indigenous languages such as Kichwa are also spoken.",
            "Ask what language tourists should use in Ecuador.",
            new string[] { "spanish", "español", "kichwa", "official language" },
            new string[] { "english only", "french", "german only" }
        ),

        new TourismKnowledgeItem(
            "Altitude in Quito",
            "Quito is located at high altitude, so some tourists may feel tired or short of breath at first.",
            "Ask whether Quito's altitude can affect tourists.",
            new string[] { "altitude", "high", "tired", "breath", "short of breath", "take it easy", "water", "rest" },
            new string[] { "sea level", "no altitude", "beach altitude" }
        ),

        new TourismKnowledgeItem(
            "Public transport in Quito",
            "Quito has a metro system and other public transport options, but tourists should still be careful with belongings.",
            "Ask about public transport options in Quito.",
            new string[] { "metro", "bus", "public transport", "taxi", "transport", "belongings" },
            new string[] { "no transport", "only boats", "only airplanes" }
        ),

        new TourismKnowledgeItem(
            "Coast of Ecuador",
            "Ecuador's Coast region has beaches, seafood, warm weather, and coastal cities.",
            "Ask what the Coast region of Ecuador is like.",
            new string[] { "beach", "beaches", "seafood", "warm", "coast", "coastal", "ocean" },
            new string[] { "snow", "high altitude only", "amazon only" }
        ),

        new TourismKnowledgeItem(
            "Typical souvenir",
            "Common souvenirs from Ecuador include crafts, textiles, chocolate, coffee, Panama hats, and handmade items.",
            "Ask what souvenir a tourist could buy in Ecuador.",
            new string[] { "crafts", "textiles", "chocolate", "coffee", "panama hat", "hat", "handmade", "souvenir" },
            new string[] { "snowball", "euro coin", "japanese kimono" }
        )
    };

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning(
                "Se encontró otro Ollama_Handler. " +
                "El manager de la escena actual tomará el control."
            );
        }

        instance = this;

        Debug.Log(
            "Ollama_Handler registrado correctamente: " +
            gameObject.name +
            " | Escena: " +
            gameObject.scene.name
        );

        if (panel != null)
            panel.SetActive(false);
        else
            Debug.LogWarning("Falta asignar el panel en Ollama_Handler.");

        if (feedbackPanel != null)
            feedbackPanel.SetActive(false);

        if (feedbackCloseButton != null)
        {
            feedbackCloseButton.onClick.RemoveListener(CloseFeedbackPanel);
            feedbackCloseButton.onClick.AddListener(CloseFeedbackPanel);
        }

        ResolvePlayerReferences();
    }

    private void ResolvePlayerReferences()
    {
        GameObject playerObject = null;

        if (playerMovement == null)
        {
            playerObject = GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                playerMovement =
                    playerObject.GetComponent<PlayerMovement>();

                if (playerMovement == null)
                {
                    playerMovement =
                        playerObject.GetComponentInChildren<PlayerMovement>();
                }
            }
        }

        if (playerInput == null && playerMovement != null)
        {
            playerInput =
                playerMovement.GetComponent<PlayerInput>();

            if (playerInput == null)
            {
                playerInput =
                    playerMovement.GetComponentInParent<PlayerInput>();
            }

            if (playerInput == null)
            {
                playerInput =
                    playerMovement.GetComponentInChildren<PlayerInput>();
            }
        }

        if (playerInput == null && playerObject != null)
        {
            playerInput =
                playerObject.GetComponent<PlayerInput>();

            if (playerInput == null)
            {
                playerInput =
                    playerObject.GetComponentInChildren<PlayerInput>();
            }
        }

        if (playerInput == null)
        {
            playerInput = FindObjectOfType<PlayerInput>();
        }

        ResolveInteractAction();
    }

    private void ResolveInteractAction()
    {
        interactAction = null;

        if (playerInput == null)
        {
            Debug.LogWarning(
                "Ollama_Handler: No se encontró el PlayerInput."
            );

            return;
        }

        if (playerInput.actions == null)
        {
            Debug.LogWarning(
                "Ollama_Handler: PlayerInput no tiene Input Actions asignadas."
            );

            return;
        }

        interactAction = playerInput.actions.FindAction(
            interactActionName,
            false
        );

        if (interactAction == null)
        {
            Debug.LogWarning(
                $"Ollama_Handler: No se encontró la acción " +
                $"'{interactActionName}'. Revisa el nombre en Input Actions."
            );
        }
    }

    private void BlockPlayerInteraction()
    {
        if (interactAction == null)
        {
            ResolvePlayerReferences();
        }

        if (interactAction == null)
            return;

        interactActionWasEnabled = interactAction.enabled;

        if (interactAction.enabled)
        {
            interactAction.Disable();

            Debug.Log(
                "Interacción del jugador bloqueada durante el diálogo."
            );
        }
    }

    private void RestorePlayerInteraction()
    {
        if (interactAction == null)
            return;

        if (interactActionWasEnabled &&
            !interactAction.enabled)
        {
            interactAction.Enable();

            Debug.Log(
                "Interacción del jugador habilitada nuevamente."
            );
        }

        interactActionWasEnabled = false;
    }

    public async void OpenDialogue(Sprite npcSprite = null, Action finishedCallback = null)
    {
        if (IsOpen || isGenerating)
            return;

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

        BlockPlayerInteraction();

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
        FinishDialogueCleanup();
    }

    private void FinishDialogueCleanup()
    {
        Action finishedCallback = onConversationFinished;
        onConversationFinished = null;

        IsOpen = false;
        isGenerating = false;
        conversationStarted = false;
        clientRefusesToTalk = false;
        conversationEnding = false;
        feedbackPanelOpen = false;

        if (closeCoroutine != null)
        {
            StopCoroutine(closeCoroutine);
            closeCoroutine = null;
        }

        if (panel != null)
            panel.SetActive(false);

        if (feedbackPanel != null)
            feedbackPanel.SetActive(false);

        if (pauseRequestedByThisDialogue)
        {
            HotelGamePause.ReleasePause();
            pauseRequestedByThisDialogue = false;
        }

        ClearOllamaConversationData();
        RestorePlayerInteraction();

        if (playerMovement != null)
            playerMovement.SetMovementEnabled(true);

        SetInputEnabled(false);

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
        wrongAnswerCount = 0;
        ResetConversationFeedbackStats();

        InputText.text = "";
        SetInputEnabled(false);

        conversationHistory.Clear();

        // The loader uses an Azure coroutine. The NPC must wait for it before
        // deciding whether to use teacher parameters or the local tourism bank.
        await WaitForTeacherParametersIfNeeded();

        currentAIParameters = AIQuestionParametersRuntime.Instance != null
            ? AIQuestionParametersRuntime.Instance.CurrentParameters
            : null;

        if (IsUsingTeacherAIParameters())
        {
            Debug.Log(
                "Ollama_Handler: using teacher AI parameters. " +
                "Subject=" + currentAIParameters.subjectName +
                ", ClassCode=" + currentAIParameters.classCode +
                ", Goal=" + currentAIParameters.questionGoal
            );

            // StartTeacherAIConversation calls GenerateText(). GenerateText()
            // already decides whether Azure/OpenAI is available and, if not,
            // returns an empty value so the teacher-based local fallback is used.
            await StartTeacherAIConversation();
            return;
        }

        Debug.LogWarning(
            "Ollama_Handler: no active teacher AI parameters are available. " +
            "The default tourism question bank will be used."
        );

        currentTourismItem = GetRandomTourismItem();

        if (currentTourismItem == null)
        {
            OutputText.text = "Hi! I'm visiting Ecuador. Could you help me with some tourist information?";
            conversationStarted = true;
            isGenerating = false;
            SetInputEnabled(true);
            return;
        }

        string prompt =
    $@"You are a foreign tourist visiting Ecuador.
You are speaking with a hotel or hospitality worker.

Your goal:
{currentTourismItem.questionGoal}

Verified information for safety:
{currentTourismItem.verifiedFact}

Write ONE natural tourist line in English.

Rules:
- Start with a friendly greeting.
- Sound like a tourist, not like a quiz teacher.
- Ask one short question related to the goal.
- Do not answer your own question.
- Do not mention that you have verified information.
- Do not write labels like Tourist, Client, NPC, or Hotel Worker.
- Do not ask about hotel services like room keys, check-in, breakfast, or Wi-Fi.
- Keep it short and natural.";

        OutputText.text = "...";

        try
        {
            string npcMessage = await GenerateText(prompt);

            if (string.IsNullOrWhiteSpace(npcMessage))
                npcMessage = GetFallbackTouristQuestion(currentTourismItem);

            currentNpcQuestion = npcMessage;

            conversationHistory.AppendLine("Tourist: " + npcMessage);
            OutputText.text = npcMessage;

            conversationStarted = true;
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);

            string fallbackQuestion = GetFallbackTouristQuestion(currentTourismItem);

            currentNpcQuestion = fallbackQuestion;
            conversationHistory.AppendLine("Tourist: " + fallbackQuestion);
            OutputText.text = fallbackQuestion;

            conversationStarted = true;
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

        AnswerCheckResult result = IsUsingTeacherAIParameters()
            ? CheckPlayerTeacherAnswer(playerAnswer)
            : CheckPlayerTourismAnswer(playerAnswer);

        RecordPlayerPerformance(playerAnswer, result);

        if (result == AnswerCheckResult.Goodbye)
        {
            string goodbye = "Thank you for your help. Have a nice day. Goodbye.";

            conversationHistory.AppendLine("Tourist: " + goodbye);
            OutputText.text = goodbye;

            conversationEnding = true;
            conversationStarted = false;

            isGenerating = false;
            SetInputEnabled(false);
            StartCloseTimer();
            return;
        }

        if (result == AnswerCheckResult.Rude)
        {
            clientMood--;

            string rudeResponse;

            if (clientMood <= 0)
            {
                rudeResponse = "That was very rude. I do not want to continue this conversation. Goodbye.";
                clientRefusesToTalk = true;
                conversationEnding = true;
                conversationStarted = false;
            }
            else
            {
                rudeResponse = "That felt a little rude. Could you please answer politely?";
            }

            conversationHistory.AppendLine("Tourist: " + rudeResponse);
            OutputText.text = rudeResponse;

            isGenerating = false;

            if (conversationEnding)
            {
                SetInputEnabled(false);
                StartCloseTimer();
            }
            else
            {
                SetInputEnabled(true);
            }

            return;
        }

        if (result == AnswerCheckResult.NoUsefulAnswer)
        {
            string noUsefulResponse =
                "I understand, but that does not really answer my question. I will ask someone else. Thank you. Goodbye.";

            conversationHistory.AppendLine("Tourist: " + noUsefulResponse);
            OutputText.text = noUsefulResponse;

            conversationEnding = true;
            conversationStarted = false;
            clientRefusesToTalk = true;

            isGenerating = false;
            SetInputEnabled(false);
            StartCloseTimer();
            return;
        }

        if (result == AnswerCheckResult.Unclear)
        {
            if (unclearAnswerCount >= 2)
            {
                string unclearEndResponse =
                    "I'm sorry, I still do not understand the answer. I will ask someone else. Goodbye.";

                conversationHistory.AppendLine("Tourist: " + unclearEndResponse);
                OutputText.text = unclearEndResponse;

                conversationEnding = true;
                conversationStarted = false;
                clientRefusesToTalk = true;

                isGenerating = false;
                SetInputEnabled(false);
                StartCloseTimer();
                return;
            }

            string unclearResponse =
                "Sorry, could you give me a clearer and more specific answer?";

            conversationHistory.AppendLine("Tourist: " + unclearResponse);
            OutputText.text = unclearResponse;

            isGenerating = false;
            SetInputEnabled(true);
            return;
        }

        string prompt = IsUsingTeacherAIParameters()
            ? BuildTeacherAIReplyPrompt(playerAnswer, result)
            : BuildGroundedTouristReplyPrompt(playerAnswer, result);

        if (result == AnswerCheckResult.NoUsefulAnswer)
        {
            string noUsefulResponse =
                "I understand, but that does not really answer my question. I will ask someone else. Thank you. Goodbye.";

            conversationHistory.AppendLine("Tourist: " + noUsefulResponse);
            OutputText.text = noUsefulResponse;

            conversationEnding = true;
            conversationStarted = false;
            clientRefusesToTalk = true;

            isGenerating = false;
            SetInputEnabled(false);
            StartCloseTimer();
            return;
        }

        OutputText.text = "...";

        try
        {
            string npcResponse = await GenerateText(prompt);

            if (string.IsNullOrWhiteSpace(npcResponse))
                npcResponse = IsUsingTeacherAIParameters()
                    ? GetTeacherAIFallbackResponse(result)
                    : GetFallbackResponse(result);

            conversationHistory.AppendLine("Tourist: " + npcResponse);
            OutputText.text = npcResponse;

            if (result == AnswerCheckResult.Correct)
            {
                conversationEnding = true;
                conversationStarted = false;
            }
            else if (result == AnswerCheckResult.Incorrect)
            {
                wrongAnswerCount++;
                clientMood--;

                if (wrongAnswerCount >= 2 || clientMood <= 0)
                {
                    conversationEnding = true;
                    conversationStarted = false;
                    clientRefusesToTalk = true;
                }
            }

            if (conversationEnding || IsGoodbyeOrClosing(npcResponse))
            {
                SetInputEnabled(false);
                StartCloseTimer();
            }
            else
            {
                SetInputEnabled(true);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);

            string fallbackResponse = IsUsingTeacherAIParameters()
                ? GetTeacherAIFallbackResponse(result)
                : GetFallbackResponse(result);

            conversationHistory.AppendLine("Tourist: " + fallbackResponse);
            OutputText.text = fallbackResponse;

            if (result == AnswerCheckResult.Correct)
            {
                conversationEnding = true;
                conversationStarted = false;
                SetInputEnabled(false);
                StartCloseTimer();
            }
            else
            {
                SetInputEnabled(true);
            }
        }
        finally
        {
            isGenerating = false;
        }
    }

    private async Task WaitForTeacherParametersIfNeeded()
    {
        if (!useTeacherAIParameters)
            return;

        if (!PlayfabManager.IsLoggedInWithEmail ||
            !PlayfabManager.IsStudent ||
            PlayfabManager.IsTeacher)
        {
            return;
        }

        AIQuestionParametersRuntime runtime =
            AIQuestionParametersRuntime.Instance;

        bool alreadyHasActiveParameters =
            runtime != null &&
            runtime.CurrentParameters != null &&
            string.Equals(
                runtime.CurrentParameters.status,
                "ACTIVE",
                StringComparison.OrdinalIgnoreCase
            );

        if (alreadyHasActiveParameters)
            return;

        StudentAIQuestionParametersLoader loader =
            StudentAIQuestionParametersLoader.Instance;

        if (loader == null)
        {
            Debug.LogWarning(
                "Ollama_Handler: StudentAIQuestionParametersLoader was not found. " +
                "Add it to a persistent GameObject and assign its Azure URL."
            );

            return;
        }

        if (!loader.IsLoading)
        {
            Debug.Log(
                "Ollama_Handler: teacher parameters are not ready. " +
                "Starting a load for the current student."
            );

            loader.LoadForCurrentStudent();
        }

        float deadline =
            Time.realtimeSinceStartup + teacherParametersLoadTimeout;

        while (loader.IsLoading &&
               Time.realtimeSinceStartup < deadline)
        {
            await Task.Yield();
        }

        if (loader.IsLoading)
        {
            Debug.LogWarning(
                "Ollama_Handler: timed out while waiting for teacher AI parameters."
            );

            return;
        }

        runtime = AIQuestionParametersRuntime.Instance;

        bool loadedActiveParameters =
            runtime != null &&
            runtime.CurrentParameters != null &&
            string.Equals(
                runtime.CurrentParameters.status,
                "ACTIVE",
                StringComparison.OrdinalIgnoreCase
            );

        if (!loadedActiveParameters)
        {
            Debug.LogWarning(
                "Ollama_Handler: teacher AI parameters were not loaded. " +
                "Loader message: " + loader.LastMessage
            );
        }
    }

    private bool IsUsingTeacherAIParameters()
    {
        return useTeacherAIParameters &&
               currentAIParameters != null &&
               string.Equals(
                   currentAIParameters.status,
                   "ACTIVE",
                   StringComparison.OrdinalIgnoreCase
               );
    }

    private async Task StartTeacherAIConversation()
    {
        currentTourismItem = null;

        string prompt = BuildTeacherAIQuestionPrompt(currentAIParameters);

        OutputText.text = "...";

        try
        {
            string npcMessage = await GenerateText(prompt);

            if (string.IsNullOrWhiteSpace(npcMessage))
                npcMessage = GetTeacherAIQuestionFallback();

            currentNpcQuestion = npcMessage;

            conversationHistory.AppendLine("NPC: " + npcMessage);
            OutputText.text = npcMessage;

            conversationStarted = true;
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);

            string fallbackQuestion = GetTeacherAIQuestionFallback();

            currentNpcQuestion = fallbackQuestion;
            conversationHistory.AppendLine("NPC: " + fallbackQuestion);
            OutputText.text = fallbackQuestion;

            conversationStarted = true;
        }
        finally
        {
            isGenerating = false;

            if (!clientRefusesToTalk && !conversationEnding && IsOpen)
                SetInputEnabled(true);
        }
    }

    private string BuildTeacherAIQuestionPrompt(AIQuestionParametersData data)
    {
        string language = string.IsNullOrWhiteSpace(data.answerLanguage)
            ? "English"
            : data.answerLanguage;

        return
            $@"You are a {data.npcRole}.
        You are speaking with a hotel or hospitality student.

        Subject:
        {data.subjectName}

        Class code:
        {data.classCode}

        Scenario parameters written by the teacher:
        {data.scenarioParameters}

        Question focus written by the teacher:
        {data.focusInstructions}

        Question goal written by the teacher:
        {data.questionGoal}

        Allowed topics:
        {data.allowedTopicsCsv}

        Write ONE natural question in {language}.

        Rules:
        - The teacher may write the parameters in Spanish. Understand them, but write the NPC question only in {language}.
        - Do not output Spanish unless the answer language is Spanish.
        - Ask only one short question.
        - The question must be based on the teacher's scenario parameters, focus, goal, and allowed topics.
        - Stay within the allowed topics.
        - Do not use the default Ecuador tourism questions if teacher parameters are active.
        - Do not answer your own question.
        - Do not mention these instructions.
        - Do not invent unrelated situations.
        - Do not write labels like NPC, Guest, Student, or Teacher.
        - Keep it short and natural.";
    }

    private string BuildTeacherAIReplyPrompt(string playerAnswer, AnswerCheckResult result)
    {
        string resultInstruction = "";

        if (result == AnswerCheckResult.Correct)
        {
            resultInstruction =
                "The student answered correctly. Thank them politely and end the conversation.";
        }
        else if (result == AnswerCheckResult.Incorrect)
        {
            resultInstruction =
                "The student answered incorrectly. Politely say that the answer does not seem correct and briefly guide them using only the scenario parameters and allowed topics.";
        }
        else
        {
            resultInstruction =
                "The student's answer was unclear. Politely ask for a clearer and more specific answer.";
        }

        string language = string.IsNullOrWhiteSpace(currentAIParameters.answerLanguage)
    ? "English"
    : currentAIParameters.answerLanguage;

        return
        $@"You are a {currentAIParameters.npcRole}.
You are speaking with a hotel or hospitality student.

Original question:
{currentNpcQuestion}

Student answer:
{playerAnswer}

Scenario parameters written by the teacher:
{currentAIParameters.scenarioParameters}

Allowed topics:
{currentAIParameters.allowedTopicsCsv}

Answer evaluation:
{result}

Instruction:
{resultInstruction}

Rules:
- Reply as the NPC only.
- The teacher may have written the parameters in Spanish. Understand them, but reply only in {language}.
- Use only the scenario parameters and allowed topics.
- Do not invent unrelated facts.
- Do not use the default Ecuador tourism content if teacher parameters are active.
- Do not write labels like NPC, Guest, Student, or Teacher.
- Keep it short and natural.
- Use {language} only.";
    }

    private AnswerCheckResult CheckPlayerTeacherAnswer(string answer)
    {
        if (string.IsNullOrWhiteSpace(answer))
            return AnswerCheckResult.Unclear;

        if (IsGoodbyeOrClosing(answer))
            return AnswerCheckResult.Goodbye;

        if (IsRudeAnswer(answer))
            return AnswerCheckResult.Rude;

        if (currentAIParameters == null)
            return AnswerCheckResult.Unclear;

        string lowerAnswer = NormalizeText(answer);

        bool hasCorrectKeyword = ContainsAnyKeyword(
            lowerAnswer,
            SplitCsvKeywords(currentAIParameters.correctKeywordsCsv)
        );

        bool hasWrongKeyword = ContainsAnyWrongKeyword(
            lowerAnswer,
            SplitCsvKeywords(currentAIParameters.wrongKeywordsCsv)
        );

        if (hasCorrectKeyword && !hasWrongKeyword)
            return AnswerCheckResult.Correct;

        if (hasWrongKeyword)
            return AnswerCheckResult.Incorrect;

        if (IsNonAnswer(lowerAnswer))
            return AnswerCheckResult.NoUsefulAnswer;

        if (answer.Trim().Length < 10)
            return AnswerCheckResult.Unclear;

        return AnswerCheckResult.NoUsefulAnswer;
    }

    private string[] SplitCsvKeywords(string csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
            return new string[0];

        string[] parts = csv.Split(',');

        for (int i = 0; i < parts.Length; i++)
            parts[i] = parts[i].Trim();

        return parts;
    }

    private string GetTeacherAIQuestionFallback()
    {
        if (currentAIParameters == null)
            return "Hello. Could you help me with this situation?";

        return "Hello. " + currentAIParameters.questionGoal;
    }

    private string GetTeacherAIFallbackResponse(AnswerCheckResult result)
    {
        if (result == AnswerCheckResult.Correct)
            return "Thank you. That answer helps and sounds correct. Goodbye.";

        if (result == AnswerCheckResult.Incorrect)
            return "I am not sure that is the correct option. Please review the situation carefully. Goodbye.";

        if (result == AnswerCheckResult.NoUsefulAnswer)
            return "That does not really answer my question. I will ask someone else. Goodbye.";

        return "Sorry, could you give me a clearer answer?";
    }

    private TourismKnowledgeItem GetRandomTourismItem()
    {
        if (tourismKnowledgeBase == null || tourismKnowledgeBase.Length == 0)
            return null;

        int index = UnityEngine.Random.Range(0, tourismKnowledgeBase.Length);
        return tourismKnowledgeBase[index];
    }

    private AnswerCheckResult CheckPlayerTourismAnswer(string answer)
    {
        if (string.IsNullOrWhiteSpace(answer))
            return AnswerCheckResult.Unclear;

        if (IsGoodbyeOrClosing(answer))
            return AnswerCheckResult.Goodbye;

        if (IsRudeAnswer(answer))
            return AnswerCheckResult.Rude;

        if (currentTourismItem == null)
            return AnswerCheckResult.Unclear;

        string lowerAnswer = NormalizeText(answer);

        bool hasCorrectKeyword = ContainsAnyKeyword(
            lowerAnswer,
            currentTourismItem.correctKeywords
        );

        bool hasWrongKeyword = ContainsAnyWrongKeyword(
            lowerAnswer,
            currentTourismItem.wrongKeywords
        );

        if (hasCorrectKeyword && !hasWrongKeyword)
            return AnswerCheckResult.Correct;

        if (hasWrongKeyword)
            return AnswerCheckResult.Incorrect;

        if (IsNonAnswer(lowerAnswer))
            return AnswerCheckResult.NoUsefulAnswer;

        if (answer.Trim().Length < 10)
            return AnswerCheckResult.Unclear;

        return AnswerCheckResult.NoUsefulAnswer;
    }

    private bool IsNonAnswer(string normalizedAnswer)
    {
        if (string.IsNullOrWhiteSpace(normalizedAnswer))
            return true;

        normalizedAnswer = normalizedAnswer.Trim();

        // Respuestas que por sí solas no aportan nada.
        string[] exactNonAnswers =
        {
        "no idea",
        "i have no idea",
        "idk",
        "i dont know",
        "i don't know",
        "do not know",
        "dont know",
        "don't know",
        "not sure",
        "i am not sure",
        "im not sure",
        "i'm not sure",
        "no clue",
        "no se",
        "no sé",
        "ni idea",
        "no tengo idea",
        "quien sabe",
        "lol",
        "haha",
        "jaja",
        "xd",
        "whatever",
        "nonsense"
    };

        foreach (string phrase in exactNonAnswers)
        {
            string normalizedPhrase = NormalizeText(phrase);

            //Solo cuenta como no respuesta si la frase es casi toda la respuesta.
            if (normalizedAnswer == normalizedPhrase)
                return true;
        }

        string cleaned = normalizedAnswer
            .Replace("lol", "")
            .Replace("haha", "")
            .Replace("jaja", "")
            .Replace("xd", "")
            .Replace(".", "")
            .Replace(",", "")
            .Replace("!", "")
            .Replace("?", "")
            .Trim();

        foreach (string phrase in exactNonAnswers)
        {
            string normalizedPhrase = NormalizeText(phrase);

            if (cleaned == normalizedPhrase)
                return true;
        }

        if (normalizedAnswer.Contains("but") ||
            normalizedAnswer.Contains("pero") ||
            normalizedAnswer.Contains("maybe") ||
            normalizedAnswer.Contains("i think") ||
            normalizedAnswer.Contains("creo") ||
            normalizedAnswer.Contains("tal vez") ||
            normalizedAnswer.Contains("quizas") ||
            normalizedAnswer.Contains("quizás"))
        {
            return false;
        }

        return false;
    }

    private string BuildGroundedTouristReplyPrompt(string playerAnswer, AnswerCheckResult result)
    {
        string resultInstruction = "";

        if (result == AnswerCheckResult.Correct)
        {
            resultInstruction =
                "The hotel worker answered correctly. Thank them warmly and end the conversation politely.";
        }
        else if (result == AnswerCheckResult.Incorrect)
        {
            resultInstruction =
                "The hotel worker gave incorrect tourist information. Politely say that it does not sound correct, mention the verified information, and say goodbye if you feel uncomfortable.";
        }
        else
        {
            resultInstruction =
                "The hotel worker's answer was unclear. Politely ask them to explain more clearly.";
        }

        return
    $@"You are a foreign tourist visiting Ecuador.
You are speaking with a hotel or hospitality worker.

Original tourist question:
{currentNpcQuestion}

Hotel worker answer:
{playerAnswer}

Verified information:
{currentTourismItem.verifiedFact}

Answer evaluation:
{result}

Instruction:
{resultInstruction}

Rules:
- Reply as the tourist only.
- Use only the verified information above.
- Do not invent facts, places, prices, dates, laws, or travel warnings.
- Do not add tourist facts that are not in the verified information.
- Do not write labels like Tourist, Client, NPC, or Hotel Worker.
- Keep it short, emotional, and natural.
- Use English only.";
    }

    private string GetFallbackTouristQuestion(TourismKnowledgeItem item)
    {
        if (item == null)
        {
            return
                "Hi! I'm visiting Ecuador. " +
                "Could you help me with some tourist information?";
        }

        switch (item.topic)
        {
            case "Loja coffee":
                return
                    "Hi! I would love to try Ecuadorian coffee. " +
                    "Where can I find some of the best coffee in Ecuador?";

            case "Capital of Ecuador":
                return
                    "Hi! Could you tell me which city is the capital of Ecuador?";

            case "Currency":
                return
                    "Hello! What currency should I use while visiting Ecuador?";

            case "Galapagos Islands":
                return
                    "Hi! Why are the Galápagos Islands so famous?";

            case "Cotopaxi":
                return
                    "Hello! Can tourists visit Cotopaxi from Quito?";

            case "Mitad del Mundo":
                return
                    "Hi! Could you explain what Mitad del Mundo is?";

            case "Quito Historic Center":
                return
                    "Hello! What can tourists see in Quito's Historic Center?";

            case "Ecuadorian food":
                return
                    "Hi! Could you recommend a traditional Ecuadorian dish?";

            case "Weather in Quito":
                return
                    "Hello! What kind of weather should I expect in Quito?";

            case "Safety in Quito":
                return
                    "Hi! Could you give me a safety tip for moving around Quito?";

            case "Ecuador regions":
                return
                    "Hello! What are the main regions of Ecuador?";

            case "Amazon region":
                return
                    "Hi! What can tourists experience in the Ecuadorian Amazon?";

            case "Otavalo Market":
                return
                    "Hello! What is Otavalo famous for?";

            case "Baños":
                return
                    "Hi! What activities can tourists do in Baños?";

            case "Cuenca":
                return
                    "Hello! What is the city of Cuenca known for?";

            case "Guayaquil":
                return
                    "Hi! What places would you recommend visiting in Guayaquil?";

            case "Ecuador language":
                return
                    "Hello! What language should tourists use in Ecuador?";

            case "Altitude in Quito":
                return
                    "Hi! Can Quito's altitude affect tourists when they arrive?";

            case "Public transport in Quito":
                return
                    "Hello! What public transportation can tourists use in Quito?";

            case "Coast of Ecuador":
                return
                    "Hi! What is Ecuador's coastal region like?";

            case "Typical souvenir":
                return
                    "Hello! What traditional souvenir could I buy in Ecuador?";

            default:
                return
                    "Hi! I'm visiting Ecuador. " +
                    "Could you help me with some tourist information?";
        }
    }

    private string GetFallbackResponse(AnswerCheckResult result)
    {
        if (currentTourismItem == null)
            return "Thank you. I will ask someone else for more information.";

        if (result == AnswerCheckResult.Correct)
            return "Thank you, that helps a lot. Have a nice day!";

        if (result == AnswerCheckResult.Incorrect)
            return "Oh, that does not sound correct. " + currentTourismItem.verifiedFact + " I think I will ask someone else. Goodbye.";

        if (result == AnswerCheckResult.NoUsefulAnswer)
            return "I understand, but that does not really answer my question. I will ask someone else. Thank you. Goodbye.";

        return "Sorry, I did not understand clearly. Could you explain that again?";
    }

    private bool ContainsAnyKeyword(string lowerText, string[] keywords)
    {
        if (keywords == null)
            return false;

        foreach (string keyword in keywords)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                continue;

            string lowerKeyword = NormalizeText(keyword);

            if (lowerText.Contains(lowerKeyword))
                return true;
        }

        return false;
    }

    private bool ContainsAnyWrongKeyword(string lowerText, string[] keywords)
    {
        if (keywords == null)
            return false;

        foreach (string keyword in keywords)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                continue;

            string lowerKeyword = NormalizeText(keyword);

            if (!lowerText.Contains(lowerKeyword))
                continue;

            if (lowerText.Contains("not " + lowerKeyword) ||
                lowerText.Contains("not the " + lowerKeyword) ||
                lowerText.Contains("does not " + lowerKeyword) ||
                lowerText.Contains("doesn't " + lowerKeyword) ||
                lowerText.Contains("no " + lowerKeyword) ||
                lowerText.Contains("no es " + lowerKeyword))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private string NormalizeText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "";

        text = text.ToLowerInvariant();

        text = text.Replace("á", "a");
        text = text.Replace("é", "e");
        text = text.Replace("í", "i");
        text = text.Replace("ó", "o");
        text = text.Replace("ú", "u");
        text = text.Replace("ñ", "n");

        return text;
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
        ShowConversationFeedback();
    }

    private void ResetConversationFeedbackStats()
    {
        totalPlayerAnswers = 0;
        correctAnswerCount = 0;
        incorrectAnswerCount = 0;
        unclearAnswerCount = 0;
        rudeAnswerCount = 0;

        usedGreeting = false;
        usedCourtesy = false;
        usedProfessionalClosing = false;
        feedbackPanelOpen = false;
    }

    private void RecordPlayerPerformance(string playerAnswer, AnswerCheckResult result)
    {
        totalPlayerAnswers++;

        if (result == AnswerCheckResult.Correct)
            correctAnswerCount++;
        else if (result == AnswerCheckResult.Incorrect)
            incorrectAnswerCount++;
        else if (result == AnswerCheckResult.Unclear)
            unclearAnswerCount++;
        else if (result == AnswerCheckResult.NoUsefulAnswer)
            unclearAnswerCount++;
        else if (result == AnswerCheckResult.Rude)
            rudeAnswerCount++;

        string normalizedAnswer = NormalizeText(playerAnswer);

        if (ContainsAnyPhrase(normalizedAnswer, new string[]
        {
        "hello",
        "hi",
        "good morning",
        "good afternoon",
        "good evening",
        "hola",
        "buenos dias",
        "buenas tardes",
        "buenas noches"
        }))
        {
            usedGreeting = true;
        }

        if (ContainsAnyPhrase(normalizedAnswer, new string[]
        {
        "please",
        "thank you",
        "thanks",
        "you are welcome",
        "welcome",
        "of course",
        "sure",
        "certainly",
        "with pleasure",
        "my pleasure",
        "con gusto",
        "claro",
        "por supuesto"
        }))
        {
            usedCourtesy = true;
        }

        if (IsGoodbyeOrClosing(playerAnswer) ||
            ContainsAnyPhrase(normalizedAnswer, new string[]
            {
            "enjoy your trip",
            "enjoy your visit",
            "have a nice stay",
            "have a great stay",
            "have a good trip",
            "let me know if you need anything else"
            }))
        {
            usedProfessionalClosing = true;
        }
    }

    private bool ContainsAnyPhrase(string normalizedText, string[] phrases)
    {
        if (string.IsNullOrWhiteSpace(normalizedText) || phrases == null)
            return false;

        foreach (string phrase in phrases)
        {
            if (string.IsNullOrWhiteSpace(phrase))
                continue;

            string normalizedPhrase = NormalizeText(phrase);

            if (normalizedText.Contains(normalizedPhrase))
                return true;
        }

        return false;
    }

    private int CalculateHospitalityScore()
    {
        int score = 100;

        score -= incorrectAnswerCount * 25;
        score -= unclearAnswerCount * 15;
        score -= rudeAnswerCount * 40;

        if (!usedGreeting && totalPlayerAnswers > 0)
            score -= 10;

        if (!usedCourtesy && totalPlayerAnswers > 0)
            score -= 10;

        if (!usedProfessionalClosing && correctAnswerCount > 0)
            score -= 5;

        return Mathf.Clamp(score, 0, 100);
    }

    private string GetFeedbackTitle(int score)
    {
        if (score >= 90)
            return "Excellent hospitality!";

        if (score >= 75)
            return "Good service!";

        if (score >= 50)
            return "Needs improvement";

        return "Poor hospitality";
    }

    private string GetCurrentConversationTopic()
    {
        if (currentAIParameters != null &&
            currentAIParameters.status == "ACTIVE")
        {
            string subject = string.IsNullOrWhiteSpace(currentAIParameters.subjectName)
                ? "Teacher AI Parameters"
                : currentAIParameters.subjectName;

            string classCode = string.IsNullOrWhiteSpace(currentAIParameters.classCode)
                ? ""
                : " - Class " + currentAIParameters.classCode;

            return subject + classCode;
        }

        if (currentTourismItem != null)
            return currentTourismItem.topic;

        return "Tourism";
    }

    private string BuildConversationFeedbackText(int score)
    {
        StringBuilder feedback = new StringBuilder();

        feedback.AppendLine("Topic: " + GetCurrentConversationTopic());
        feedback.AppendLine("");

        feedback.AppendLine("Result:");
        feedback.AppendLine("- Correct answers: " + correctAnswerCount);
        feedback.AppendLine("- Incorrect answers: " + incorrectAnswerCount);
        feedback.AppendLine("- Unclear answers: " + unclearAnswerCount);
        feedback.AppendLine("- Rude answers: " + rudeAnswerCount);
        feedback.AppendLine("");

        feedback.AppendLine("Hospitality feedback:");

        if (correctAnswerCount > 0 && incorrectAnswerCount == 0 && unclearAnswerCount == 0)
            feedback.AppendLine("- You gave useful tourist information.");

        if (incorrectAnswerCount > 0)
            feedback.AppendLine("- Improve accuracy. A hotel worker should avoid giving false tourist information.");

        if (unclearAnswerCount > 0)
            feedback.AppendLine("- Be more specific. Tourists need clear and complete answers.");

        if (rudeAnswerCount > 0)
            feedback.AppendLine("- Avoid rude language. A guest should always feel respected.");

        if (!usedGreeting)
            feedback.AppendLine("- Try to greet the tourist politely, for example: \"Good morning!\" or \"Hello!\"");

        if (!usedCourtesy)
            feedback.AppendLine("- Use more courteous phrases, for example: \"Of course\", \"Please\", or \"You're welcome\".");

        if (!usedProfessionalClosing && correctAnswerCount > 0)
            feedback.AppendLine("- Add a professional closing, for example: \"Enjoy your visit!\"");

        feedback.AppendLine("");

        feedback.AppendLine("Recommended answer style:");

        if (currentTourismItem != null)
        {
            feedback.AppendLine(
                "\"Good morning! Of course. " +
                currentTourismItem.verifiedFact +
                " Please let me know if you need anything else. Enjoy your visit!\""
            );
        }
        else
        {
            feedback.AppendLine(
                "\"Good morning! Of course. I will be happy to help you. Please let me know if you need anything else.\""
            );
        }

        return feedback.ToString();
    }

    private void ShowConversationFeedback()
    {
        if (closeCoroutine != null)
        {
            StopCoroutine(closeCoroutine);
            closeCoroutine = null;
        }

        isGenerating = false;
        conversationStarted = false;
        conversationEnding = false;
        clientRefusesToTalk = false;
        feedbackPanelOpen = true;

        SetInputEnabled(false);

        if (panel != null)
            panel.SetActive(false);

        if (feedbackPanel == null)
        {
            CloseDialogue();
            return;
        }

        int score = CalculateHospitalityScore();

        if (feedbackTitleText != null)
            feedbackTitleText.text = GetFeedbackTitle(score);

        if (feedbackScoreText != null)
            feedbackScoreText.text = "Hospitality Score: " + score + "%";

        if (feedbackDetailsText != null)
            feedbackDetailsText.text = BuildConversationFeedbackText(score);

        feedbackPanel.SetActive(true);
    }

    public void CloseFeedbackPanel()
    {
        if (!feedbackPanelOpen)
            return;

        feedbackPanelOpen = false;

        if (feedbackPanel != null)
            feedbackPanel.SetActive(false);

        FinishDialogueCleanup();
    }

    private bool CanUseOnlineAI()
    {
        bool isLoggedInStudent =
            PlayfabManager.IsLoggedInWithEmail &&
            PlayfabManager.IsStudent;

        bool hasSessionTicket =
            !string.IsNullOrWhiteSpace(
                PlayfabManager.CurrentSessionTicket
            );

        bool hasInternet =
            Application.internetReachability !=
            NetworkReachability.NotReachable;

        return isLoggedInStudent &&
               hasSessionTicket &&
               hasInternet;
    }

    private async Task<string> GenerateText(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            return "";

        /*
         * Guest mode or no internet:
         * do not call Azure or OpenAI.
         * Returning an empty string activates the local fallback.
         */
        if (!CanUseOnlineAI())
        {
            Debug.Log(
                "NPC running in guest/offline mode. " +
                "Using local dialogue."
            );

            return "";
        }

        if (string.IsNullOrWhiteSpace(azureFunctionUrl))
        {
            Debug.LogWarning(
                "Azure Function URL is missing. " +
                "Using local NPC dialogue."
            );

            return "";
        }

        string sessionTicket =
            PlayfabManager.CurrentSessionTicket;

        AzureNpcRequest requestBody =
            new AzureNpcRequest
            {
                sessionTicket = sessionTicket,
                prompt = prompt
            };

        string jsonBody =
            JsonUtility.ToJson(requestBody);

        byte[] bodyRaw =
            Encoding.UTF8.GetBytes(jsonBody);

        using (
            UnityWebRequest request =
                new UnityWebRequest(
                    azureFunctionUrl,
                    "POST"
                )
        )
        {
            request.uploadHandler =
                new UploadHandlerRaw(bodyRaw);

            request.downloadHandler =
                new DownloadHandlerBuffer();

            request.SetRequestHeader(
                "Content-Type",
                "application/json"
            );

            request.timeout = 30;

            UnityWebRequestAsyncOperation operation =
                request.SendWebRequest();

            while (!operation.isDone)
                await Task.Yield();

            string responseText =
                request.downloadHandler != null
                    ? request.downloadHandler.text
                    : "";

            /*
             * Azure, PlayFab or OpenAI failed.
             * Continue with local dialogue instead of breaking the game.
             */
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning(
                    "Online NPC dialogue unavailable. " +
                    "Using local fallback.\n" +
                    "HTTP: " + request.responseCode +
                    "\nError: " + request.error +
                    "\nBackend response: " + responseText
                );

                return "";
            }

            AzureNpcResponse response = null;

            try
            {
                response =
                    JsonUtility.FromJson<AzureNpcResponse>(
                        responseText
                    );
            }
            catch (Exception parseError)
            {
                Debug.LogWarning(
                    "Could not parse Azure NPC response. " +
                    "Using local fallback. " +
                    parseError.Message
                );

                return "";
            }

            if (response == null)
            {
                Debug.LogWarning(
                    "Azure returned an invalid NPC response. " +
                    "Using local fallback."
                );

                return "";
            }

            if (!response.success)
            {
                Debug.LogWarning(
                    "Azure NPC error: " +
                    response.message +
                    ". Using local fallback."
                );

                return "";
            }

            if (string.IsNullOrWhiteSpace(response.text))
            {
                Debug.LogWarning(
                    "Azure returned empty NPC text. " +
                    "Using local fallback."
                );

                return "";
            }

            return CleanResponse(response.text);
        }
    }

    private string CleanResponse(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "";

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
        if (InputText == null)
            return;

        InputText.interactable = enabled;

        if (enabled)
        {
            InputText.Select();
            InputText.ActivateInputField();
            InputText.MoveTextEnd(false);
        }
        else
        {
            InputText.DeactivateInputField();
        }
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

        currentTourismItem = null;
        currentNpcQuestion = "";
        wrongAnswerCount = 0;
        ResetConversationFeedbackStats();

        onConversationFinished = null;
    }

    private void OnDisable()
    {
        if (pauseRequestedByThisDialogue)
        {
            HotelGamePause.ReleasePause();
            pauseRequestedByThisDialogue = false;
        }

        RestorePlayerInteraction();

        if (playerMovement != null)
        {
            playerMovement.SetMovementEnabled(true);
        }
    }

    private void OnDestroy()
    {
        Debug.Log(
            "Ollama_Handler destruido: " +
            gameObject.name +
            " | Escena: " +
            gameObject.scene.name
        );

        if (instance == this)
        {
            instance = null;
        }

        if (pauseRequestedByThisDialogue)
        {
            HotelGamePause.ReleasePause();
            pauseRequestedByThisDialogue = false;
        }

        RestorePlayerInteraction();

        if (playerMovement != null)
        {
            playerMovement.SetMovementEnabled(true);
        }
    }
}

[Serializable]
public class AzureNpcRequest
{
    public string sessionTicket;
    public string prompt;
}

[Serializable]
public class AzureNpcResponse
{
    public bool success;
    public string message;
    public string text;
}

