using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class StudentAIQuestionParametersLoader : MonoBehaviour
{
    [Header("Azure Function")]
    [SerializeField] private string getAIQuestionParametersForStudentUrl;

    [Header("Optional UI")]
    [SerializeField] private TMP_Text messageText;

    private bool isLoading = false;

    public void LoadParametersForSavedClassCode()
    {
        string classCode = PlayerPrefs.GetString("Hotelia_StudentClassCode", "");

        LoadParametersForClassCode(classCode);
    }

    public void LoadParametersForClassCode(string classCode)
    {
        if (isLoading)
            return;

        if (!PlayfabManager.IsLoggedInWithEmail || !PlayfabManager.IsStudent)
        {
            SetMessage("Only student accounts can load AI question parameters.");
            return;
        }

        if (string.IsNullOrWhiteSpace(PlayfabManager.CurrentSessionTicket))
        {
            SetMessage("Missing PlayFab session. Please log in again.");
            return;
        }

        if (string.IsNullOrWhiteSpace(classCode))
        {
            SetMessage("Missing class code.");
            return;
        }

        StartCoroutine(LoadParametersRequest(classCode.Trim()));
    }

    private IEnumerator LoadParametersRequest(string classCode)
    {
        if (string.IsNullOrWhiteSpace(getAIQuestionParametersForStudentUrl))
        {
            SetMessage("Missing get AI parameters Azure Function URL.");
            yield break;
        }

        isLoading = true;
        SetMessage("Loading AI question parameters...");

        GetAIQuestionParametersForStudentRequestData requestData =
            new GetAIQuestionParametersForStudentRequestData
            {
                sessionTicket = PlayfabManager.CurrentSessionTicket,
                classCode = classCode
            };

        string json = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        UnityWebRequest request =
            new UnityWebRequest(getAIQuestionParametersForStudentUrl, "POST");

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        string responseText = request.downloadHandler.text;

        isLoading = false;

        if (request.result != UnityWebRequest.Result.Success)
        {
            SetMessage("Could not load AI question parameters.");
            Debug.LogError("Get AI parameters request failed: " + request.error);
            Debug.LogError("Backend response: " + responseText);
            yield break;
        }

        GetAIQuestionParametersForStudentResponseData response =
            JsonUtility.FromJson<GetAIQuestionParametersForStudentResponseData>(responseText);

        if (response == null)
        {
            SetMessage("Invalid response from Azure Function.");
            Debug.LogError("Invalid Azure response: " + responseText);
            yield break;
        }

        if (!response.success)
        {
            string message = string.IsNullOrWhiteSpace(response.message)
                ? "Could not load AI question parameters."
                : response.message;

            SetMessage(message);
            Debug.LogWarning("Azure load failed: " + responseText);
            yield break;
        }

        if (response.parameters == null)
        {
            SetMessage("AI parameters response was empty.");
            Debug.LogWarning("AI parameters were null.");
            yield break;
        }

        if (AIQuestionParametersRuntime.Instance == null)
        {
            SetMessage("AIQuestionParametersRuntime is missing in the scene.");
            Debug.LogError("AIQuestionParametersRuntime is missing in the scene.");
            yield break;
        }

        AIQuestionParametersRuntime.Instance.SetCurrentParameters(response.parameters);

        SetMessage("AI question parameters loaded successfully.");

        Debug.Log(
            "Loaded AI parameters for subject: " +
            response.parameters.subjectName +
            " / class: " +
            response.parameters.classCode
        );
    }

    private void SetMessage(string message)
    {
        if (messageText != null)
            messageText.text = message;

        Debug.Log("Student AI Parameters Loader: " + message);
    }
}