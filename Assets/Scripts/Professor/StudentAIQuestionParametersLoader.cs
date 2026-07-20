using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class StudentAIQuestionParametersLoader : MonoBehaviour
{
    public static StudentAIQuestionParametersLoader Instance { get; private set; }

    [Header("Azure Function")]
    [SerializeField] private string getAIQuestionParametersForStudentUrl;

    [Header("Load")]
    [SerializeField] private bool loadOnStart = true;

    [Tooltip("Maximum time to wait for PlayFab login, session ticket and class code.")]
    [Min(1f)]
    [SerializeField] private float startupWaitTimeout = 15f;

    [Header("Optional UI")]
    [SerializeField] private TMP_Text messageText;

    public bool IsLoading { get; private set; }
    public bool HasFinishedLoading { get; private set; }
    public bool LastLoadSucceeded { get; private set; }
    public string LastMessage { get; private set; }

    private Coroutine loadCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        HasFinishedLoading = !loadOnStart;
    }

    private IEnumerator Start()
    {
        if (!loadOnStart)
            yield break;

        yield return StartCoroutine(WaitForStudentSessionAndLoadRoutine());
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void LoadForCurrentStudent()
    {
        LoadForClassCode(StudentClassRuntime.GetClassCode());
    }

    public void LoadForClassCode(string classCode)
    {
        if (!string.IsNullOrWhiteSpace(classCode))
            StudentClassRuntime.SetClassCode(classCode.Trim());

        if (IsLoading)
        {
            Debug.Log("Student AI Parameters Loader: a load is already in progress.");
            return;
        }

        if (loadCoroutine != null)
            StopCoroutine(loadCoroutine);

        loadCoroutine = StartCoroutine(LoadForCurrentStudentRoutine());
    }

    private IEnumerator WaitForStudentSessionAndLoadRoutine()
    {
        HasFinishedLoading = false;
        LastLoadSucceeded = false;

        float deadline = Time.realtimeSinceStartup + startupWaitTimeout;

        while (Time.realtimeSinceStartup < deadline)
        {
            bool loginReady =
                PlayfabManager.IsLoggedInWithEmail &&
                PlayfabManager.IsStudent &&
                !PlayfabManager.IsTeacher;

            bool sessionReady =
                !string.IsNullOrWhiteSpace(PlayfabManager.CurrentSessionTicket);

            bool classReady =
                !string.IsNullOrWhiteSpace(StudentClassRuntime.GetClassCode());

            bool runtimeReady = AIQuestionParametersRuntime.Instance != null;

            if (loginReady && sessionReady && classReady && runtimeReady)
            {
                LoadForCurrentStudent();

                while (IsLoading)
                    yield return null;

                yield break;
            }

            yield return null;
        }

        SetMessage(
            "AI parameters were not loaded at startup because the student login, " +
            "session ticket, class code or runtime was not ready. They will be retried when the NPC opens."
        );

        FinishLoading(false);
    }

    private IEnumerator LoadForCurrentStudentRoutine()
    {
        IsLoading = true;
        HasFinishedLoading = false;
        LastLoadSucceeded = false;

        if (string.IsNullOrWhiteSpace(getAIQuestionParametersForStudentUrl))
        {
            SetMessage("Missing getAIQuestionParametersForStudentUrl in Inspector.");
            FinishLoading(false);
            yield break;
        }

        if (!PlayfabManager.IsLoggedInWithEmail)
        {
            SetMessage("Not logged in with PlayFab. AI teacher parameters will not be loaded.");
            FinishLoading(false);
            yield break;
        }

        if (!PlayfabManager.IsStudent || PlayfabManager.IsTeacher)
        {
            SetMessage("The current account is not a student. AI teacher parameters will not be loaded.");
            FinishLoading(false);
            yield break;
        }

        if (string.IsNullOrWhiteSpace(PlayfabManager.CurrentSessionTicket))
        {
            SetMessage("Missing PlayFab session ticket. AI teacher parameters will not be loaded.");
            FinishLoading(false);
            yield break;
        }

        string classCode = StudentClassRuntime.GetClassCode();

        if (string.IsNullOrWhiteSpace(classCode))
        {
            SetMessage("Missing student class code/NRC. AI teacher parameters will not be loaded.");
            FinishLoading(false);
            yield break;
        }

        classCode = classCode.Trim();

        if (AIQuestionParametersRuntime.Instance == null)
        {
            SetMessage("Missing AIQuestionParametersRuntime in scene.");
            FinishLoading(false);
            yield break;
        }

        // Clear only when a real reload is about to begin.
        AIQuestionParametersRuntime.Instance.ClearCurrentParameters();

        GetStudentAIParametersRequestData requestData =
            new GetStudentAIParametersRequestData
            {
                sessionTicket = PlayfabManager.CurrentSessionTicket,
                classCode = classCode
            };

        string json = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest request = new UnityWebRequest(
                   getAIQuestionParametersForStudentUrl,
                   UnityWebRequest.kHttpVerbPOST))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 15;

            SetMessage("Loading AI parameters for class: " + classCode);

            yield return request.SendWebRequest();

            string responseText = request.downloadHandler != null
                ? request.downloadHandler.text
                : "";

            if (request.result != UnityWebRequest.Result.Success)
            {
                SetMessage(
                    "Could not load student AI parameters." +
                    "\nHTTP: " + request.responseCode +
                    "\nError: " + request.error
                );

                Debug.LogWarning("Backend response: " + responseText);
                FinishLoading(false);
                yield break;
            }

            GetStudentAIParametersResponseData response;

            try
            {
                response = JsonUtility.FromJson<GetStudentAIParametersResponseData>(responseText);
            }
            catch (Exception ex)
            {
                SetMessage("Invalid AI parameters response.");
                Debug.LogError("Invalid AI parameters response: " + ex.Message);
                Debug.LogError("Response: " + responseText);

                FinishLoading(false);
                yield break;
            }

            if (response == null || !response.success)
            {
                SetMessage(
                    "AI parameters not loaded: " +
                    (response != null ? response.message : "Invalid response.")
                );

                Debug.LogWarning("AI parameters response: " + responseText);
                FinishLoading(false);
                yield break;
            }

            if (response.parameters == null ||
                !string.Equals(
                    response.parameters.status,
                    "ACTIVE",
                    StringComparison.OrdinalIgnoreCase))
            {
                SetMessage("No active AI parameters found for this student/class.");
                FinishLoading(false);
                yield break;
            }

            AIQuestionParametersRuntime.Instance.SetCurrentParameters(response.parameters);

            SetMessage(
                "AI question parameters loaded successfully: " +
                response.parameters.subjectName +
                " - Class " +
                response.parameters.classCode
            );

            Debug.Log(
                "Teacher AI parameters ready for NPC. " +
                "CourseId=" + response.parameters.courseId +
                ", ClassCode=" + response.parameters.classCode +
                ", Goal=" + response.parameters.questionGoal
            );

            FinishLoading(true);
        }
    }

    private void FinishLoading(bool succeeded)
    {
        IsLoading = false;
        HasFinishedLoading = true;
        LastLoadSucceeded = succeeded;
        loadCoroutine = null;
    }

    private void SetMessage(string message)
    {
        LastMessage = message;

        if (messageText != null)
            messageText.text = message;

        Debug.Log("Student AI Parameters Loader: " + message);
    }

    [Serializable]
    private class GetStudentAIParametersRequestData
    {
        public string sessionTicket;
        public string classCode;
    }

    [Serializable]
    private class GetStudentAIParametersResponseData
    {
        public bool success;
        public string message;
        public AIQuestionParametersData parameters;
    }
}
