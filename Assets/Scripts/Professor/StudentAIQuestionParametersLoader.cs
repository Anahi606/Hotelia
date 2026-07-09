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

    [Header("Optional UI")]
    [SerializeField] private TMP_Text messageText;

    public bool IsLoading { get; private set; }
    public bool HasFinishedLoading { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private IEnumerator Start()
    {
        if (!loadOnStart)
            yield break;

        yield return null;
        yield return null;

        LoadForCurrentStudent();
    }

    public void LoadForCurrentStudent()
    {
        string classCode = StudentClassRuntime.GetClassCode();
        LoadForClassCode(classCode);
    }

    public void LoadForClassCode(string classCode)
    {
        if (IsLoading)
            return;

        if (!string.IsNullOrWhiteSpace(classCode))
            StudentClassRuntime.SetClassCode(classCode);

        StartCoroutine(LoadForCurrentStudentRoutine());
    }

    private IEnumerator LoadForCurrentStudentRoutine()
    {
        IsLoading = true;
        HasFinishedLoading = false;

        if (AIQuestionParametersRuntime.Instance != null)
            AIQuestionParametersRuntime.Instance.ClearCurrentParameters();

        if (string.IsNullOrWhiteSpace(getAIQuestionParametersForStudentUrl))
        {
            SetMessage("Missing getAIQuestionParametersForStudentUrl in Inspector.");
            FinishLoading();
            yield break;
        }

        if (!PlayfabManager.IsLoggedInWithEmail)
        {
            SetMessage("Not logged in with PlayFab. AI teacher parameters will not be loaded.");
            FinishLoading();
            yield break;
        }

        if (PlayfabManager.IsTeacher)
        {
            SetMessage("Teacher account detected. Student AI parameters will not be loaded.");
            FinishLoading();
            yield break;
        }

        if (string.IsNullOrWhiteSpace(PlayfabManager.CurrentSessionTicket))
        {
            SetMessage("Missing PlayFab session ticket. AI teacher parameters will not be loaded.");
            FinishLoading();
            yield break;
        }

        string classCode = StudentClassRuntime.GetClassCode();

        if (string.IsNullOrWhiteSpace(classCode))
        {
            SetMessage("Missing student class code/NRC. AI teacher parameters will not be loaded.");
            FinishLoading();
            yield break;
        }

        GetStudentAIParametersRequestData requestData =
            new GetStudentAIParametersRequestData
            {
                sessionTicket = PlayfabManager.CurrentSessionTicket,
                classCode = classCode
            };

        string json = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        UnityWebRequest request = new UnityWebRequest(
            getAIQuestionParametersForStudentUrl,
            "POST"
        );

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        SetMessage("Loading AI parameters for class: " + classCode);

        yield return request.SendWebRequest();

        string responseText = request.downloadHandler.text;

        if (request.result != UnityWebRequest.Result.Success)
        {
            SetMessage(
                "Could not load student AI parameters." +
                "\nHTTP: " + request.responseCode +
                "\nError: " + request.error
            );

            Debug.LogWarning("Backend response: " + responseText);

            FinishLoading();
            yield break;
        }

        GetStudentAIParametersResponseData response = null;

        try
        {
            response = JsonUtility.FromJson<GetStudentAIParametersResponseData>(responseText);
        }
        catch (Exception ex)
        {
            SetMessage("Invalid AI parameters response.");
            Debug.LogError("Invalid AI parameters response: " + ex.Message);
            Debug.LogError("Response: " + responseText);

            FinishLoading();
            yield break;
        }

        if (response == null || !response.success)
        {
            SetMessage(
                "AI parameters not loaded: " +
                (response != null ? response.message : "Invalid response.")
            );

            FinishLoading();
            yield break;
        }

        if (response.parameters == null || response.parameters.status != "ACTIVE")
        {
            SetMessage("No active AI parameters found for this student/class.");
            FinishLoading();
            yield break;
        }

        if (AIQuestionParametersRuntime.Instance == null)
        {
            SetMessage("Missing AIQuestionParametersRuntime in scene.");
            FinishLoading();
            yield break;
        }

        AIQuestionParametersRuntime.Instance.SetCurrentParameters(response.parameters);

        SetMessage(
            "AI question parameters loaded successfully: " +
            response.parameters.subjectName +
            " - Class " +
            response.parameters.classCode
        );

        FinishLoading();
    }

    private void FinishLoading()
    {
        IsLoading = false;
        HasFinishedLoading = true;
    }

    private void SetMessage(string message)
    {
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