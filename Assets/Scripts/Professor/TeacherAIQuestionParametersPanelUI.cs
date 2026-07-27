using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using TMPro;
using UnityEngine;
using System.IO;
using UnityEngine.Networking;
using PlayFab;
using PlayFab.ClientModels;

public class TeacherAIQuestionParametersPanelUI : MonoBehaviour
{
    private const string TeacherCoursesKey = "Hotelia_TeacherCourses";
    private const string AIQuestionParametersKey = "Hotelia_AIQuestionParameters";

    [Header("Root Panel")]
    [SerializeField] private GameObject panel;

    [Header("Views")]
    [SerializeField] private GameObject courseListPanel;
    [SerializeField] private GameObject parametersFormPanel;

    [Header("Course List ScrollView")]
    [SerializeField] private Transform courseListContent;
    [SerializeField] private TeacherAIQuestionParameterCourseRowUI courseRowPrefab;

    [Header("Course Selection")]
    [SerializeField] private TMP_Dropdown courseDropdown;

    [Header("Form Inputs")]
    [SerializeField] private TMP_InputField scenarioParametersInput;
    [SerializeField] private TMP_InputField focusInstructionsInput;
    [SerializeField] private TMP_InputField questionGoalInput;
    [SerializeField] private TMP_InputField allowedTopicsInput;
    [SerializeField] private TMP_InputField correctKeywordsInput;
    [SerializeField] private TMP_InputField wrongKeywordsInput;

    [Header("Azure Functions")]
    [SerializeField] private string saveAIQuestionParametersUrl;

    [Header("Messages")]
    [SerializeField] private TMP_Text messageText;

    private readonly List<TeacherCourseData> courses = new List<TeacherCourseData>();
    private readonly List<AIQuestionParametersData> parameters = new List<AIQuestionParametersData>();

    private bool isLoadingDropdown = false;
    private bool isSaving = false;
    private bool dataLoaded = false;
    private bool openCreateFormAfterLoad = false;

    private void Start()
    {
        HidePanel();
    }

    public void ShowPanel()
    {
        if (panel != null)
            panel.SetActive(true);

        ShowCourseListView();
        LoadAllData();
    }

    public void HidePanel()
    {
        if (panel != null)
            panel.SetActive(false);

        SetMessage("");
    }

    public void CloseWithoutSavingButton()
    {
        ClearForm();

        if (parametersFormPanel != null)
            parametersFormPanel.SetActive(false);

        SetMessage("");
    }

    public void OpenCreateParametersFormButton()
    {
        if (!PlayfabManager.IsLoggedInWithEmail || !PlayfabManager.IsTeacher)
        {
            SetMessage("Only teacher accounts can create AI question parameters.");
            return;
        }

        if (!dataLoaded)
        {
            openCreateFormAfterLoad = true;
            SetMessage("Loading courses first...");
            LoadAllData();
            return;
        }

        OpenCreateFormNow();
    }

    private void OpenCreateFormNow()
    {
        if (GetActiveCourseCount() == 0)
        {
            SetMessage("No active courses found. Create a course first.");
            return;
        }

        PopulateCourseDropdown();

        if (courseDropdown != null && courseDropdown.options.Count > 0)
        {
            courseDropdown.value = 0;
            courseDropdown.RefreshShownValue();
        }

        ClearForm();

        if (parametersFormPanel != null)
            parametersFormPanel.SetActive(true);
        else
            Debug.LogError("Parameters Form Panel is not assigned in the Inspector.");

        SetMessage("");
    }

    public void LoadAllData()
    {
        if (!PlayfabManager.IsLoggedInWithEmail || !PlayfabManager.IsTeacher)
        {
            SetMessage("Only teacher accounts can edit AI question parameters.");
            return;
        }

        dataLoaded = false;
        SetMessage("Loading AI question parameters...");

        LoadCoursesFromPlayFab(() =>
        {
            LoadParametersFromPlayFab(() =>
            {
                PopulateCourseDropdown();
                RefreshCourseRows();

                dataLoaded = true;

                if (openCreateFormAfterLoad)
                {
                    openCreateFormAfterLoad = false;
                    OpenCreateFormNow();
                    return;
                }

                if (GetActiveCourseCount() == 0)
                {
                    ClearForm();
                    ShowCourseListView();
                    SetMessage("No active courses found. Create a course first.");
                    return;
                }

                ShowCourseListView();
                SetMessage("AI question parameters loaded.");
            });
        });
    }

    private void LoadCoursesFromPlayFab(Action onSuccess)
    {
        var request = new GetUserDataRequest
        {
            Keys = new List<string> { TeacherCoursesKey }
        };

        PlayFabClientAPI.GetUserData(
            request,
            result =>
            {
                courses.Clear();

                if (result.Data != null && result.Data.ContainsKey(TeacherCoursesKey))
                {
                    string json = result.Data[TeacherCoursesKey].Value;

                    TeacherCourseListData savedData =
                        JsonUtility.FromJson<TeacherCourseListData>(json);

                    if (savedData != null && savedData.courses != null)
                    {
                        courses.AddRange(savedData.courses);

                        foreach (TeacherCourseData course in courses)
                            NormalizeLoadedCourse(course);
                    }
                }

                onSuccess?.Invoke();
            },
            error =>
            {
                dataLoaded = false;
                SetMessage("Could not load teacher courses.");
                Debug.LogError("Error loading teacher courses: " + error.GenerateErrorReport());
            }
        );
    }

    private void LoadParametersFromPlayFab(Action onSuccess)
    {
        var request = new GetUserDataRequest
        {
            Keys = new List<string> { AIQuestionParametersKey }
        };

        PlayFabClientAPI.GetUserData(
            request,
            result =>
            {
                parameters.Clear();

                if (result.Data != null && result.Data.ContainsKey(AIQuestionParametersKey))
                {
                    string json = result.Data[AIQuestionParametersKey].Value;

                    AIQuestionParametersListData savedData =
                        JsonUtility.FromJson<AIQuestionParametersListData>(json);

                    if (savedData != null && savedData.parameters != null)
                        parameters.AddRange(savedData.parameters);
                }

                onSuccess?.Invoke();
            },
            error =>
            {
                dataLoaded = false;
                SetMessage("Could not load AI question parameters.");
                Debug.LogError("Error loading AI parameters: " + error.GenerateErrorReport());
            }
        );
    }

    private void PopulateCourseDropdown()
    {
        if (courseDropdown == null)
        {
            Debug.LogWarning("Course dropdown is not assigned.");
            return;
        }

        isLoadingDropdown = true;

        courseDropdown.ClearOptions();

        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();

        foreach (TeacherCourseData course in courses)
        {
            if (course == null || course.status != "ACTIVE")
                continue;

            string optionText = GetCourseName(course) + " - Class " + GetClassCode(course);
            options.Add(new TMP_Dropdown.OptionData(optionText));
        }

        courseDropdown.AddOptions(options);

        if (courseDropdown.options.Count > 0)
            courseDropdown.value = 0;

        courseDropdown.RefreshShownValue();

        isLoadingDropdown = false;
    }

    public void OnCourseDropdownChanged()
    {
        if (isLoadingDropdown)
            return;

        if (parametersFormPanel == null || !parametersFormPanel.activeSelf)
            return;

        FillFormFromSelectedCourse();
    }

    public void EditParametersForCourse(string courseId)
    {
        if (string.IsNullOrEmpty(courseId))
            return;

        bool found = SetDropdownToCourse(courseId);

        if (!found)
        {
            SetMessage("Course not found.");
            return;
        }

        FillFormFromSelectedCourse();
        ShowParametersFormView();
    }

    public void DeleteParametersForCourse(string courseId)
    {
        AIQuestionParametersData parameter = FindParametersByCourseId(courseId);

        if (parameter == null)
        {
            SetMessage("This course has no AI parameters to delete.");
            return;
        }

        parameter.status = "DELETED";
        parameter.updatedUtc = DateTime.UtcNow.ToString("o");

        RefreshCourseRows();

        SetMessage("Parameters deleted locally. Backend delete can be added later.");
    }

    public void CreateParametersButton()
    {
        if (isSaving)
            return;

        if (!PlayfabManager.IsLoggedInWithEmail || !PlayfabManager.IsTeacher)
        {
            SetMessage("Only teacher accounts can create AI question parameters.");
            return;
        }

        if (string.IsNullOrWhiteSpace(PlayfabManager.CurrentSessionTicket))
        {
            SetMessage("Missing PlayFab session. Please log in again.");
            return;
        }

        TeacherCourseData selectedCourse = GetSelectedCourse();

        if (selectedCourse == null)
        {
            SetMessage("Select a valid course first.");
            return;
        }

        string scenarioParameters = GetInputText(scenarioParametersInput);
        string focusInstructions = GetInputText(focusInstructionsInput);
        string questionGoal = GetInputText(questionGoalInput);
        string allowedTopics = GetInputText(allowedTopicsInput);
        string correctKeywords = GetInputText(correctKeywordsInput);
        string wrongKeywords = GetInputText(wrongKeywordsInput);

        if (string.IsNullOrWhiteSpace(scenarioParameters))
        {
            SetMessage("Write the scenario parameters.");
            return;
        }

        if (string.IsNullOrWhiteSpace(focusInstructions))
        {
            SetMessage("Write what the questions should focus on.");
            return;
        }

        if (string.IsNullOrWhiteSpace(questionGoal))
        {
            SetMessage("Write the question goal.");
            return;
        }

        if (string.IsNullOrWhiteSpace(allowedTopics))
        {
            SetMessage("Write at least one allowed topic.");
            return;
        }

        if (string.IsNullOrWhiteSpace(correctKeywords))
        {
            SetMessage("Write at least one correct answer keyword.");
            return;
        }

        AIQuestionParametersData parameter = FindParametersByCourseId(selectedCourse.courseId);

        if (parameter == null)
        {
            parameter = new AIQuestionParametersData
            {
                parameterId = "ai_params_" + DateTime.UtcNow.Ticks
            };

            parameters.Add(parameter);
        }

        parameter.courseId = selectedCourse.courseId;
        parameter.subjectCode = selectedCourse.subjectCode;
        parameter.subjectName = GetCourseName(selectedCourse);
        parameter.classCode = GetClassCode(selectedCourse);
        parameter.teacherPlayFabId = PlayfabManager.CurrentPlayFabId;

        parameter.scenarioParameters = scenarioParameters;
        parameter.focusInstructions = focusInstructions;
        parameter.questionGoal = questionGoal;
        parameter.allowedTopicsCsv = allowedTopics;
        parameter.correctKeywordsCsv = correctKeywords;
        parameter.wrongKeywordsCsv = wrongKeywords;

        parameter.npcRole = "hotel guest";
        parameter.answerLanguage = "English";

        parameter.status = "ACTIVE";
        parameter.updatedUtc = DateTime.UtcNow.ToString("o");

        SaveParametersThroughBackend(parameter);
    }

    private void SaveParametersThroughBackend(AIQuestionParametersData parameter)
    {
        StartCoroutine(SaveAIQuestionParametersRequest(parameter));
    }

    private IEnumerator SaveAIQuestionParametersRequest(AIQuestionParametersData parameter)
    {
        if (string.IsNullOrWhiteSpace(saveAIQuestionParametersUrl))
        {
            SetMessage("Missing Azure Function URL.");
            yield break;
        }

        isSaving = true;
        SetMessage("Saving AI question parameters...");

        SaveAIQuestionParametersRequestData requestData =
            new SaveAIQuestionParametersRequestData
            {
                sessionTicket = PlayfabManager.CurrentSessionTicket,

                courseId = parameter.courseId,
                subjectCode = parameter.subjectCode,
                subjectName = parameter.subjectName,
                classCode = parameter.classCode,

                scenarioParameters = parameter.scenarioParameters,
                focusInstructions = parameter.focusInstructions,
                questionGoal = parameter.questionGoal,
                allowedTopicsCsv = parameter.allowedTopicsCsv,
                correctKeywordsCsv = parameter.correctKeywordsCsv,
                wrongKeywordsCsv = parameter.wrongKeywordsCsv,

                npcRole = parameter.npcRole,
                answerLanguage = parameter.answerLanguage
            };

        string json = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        UnityWebRequest request = new UnityWebRequest(saveAIQuestionParametersUrl, "POST");

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        string responseText = request.downloadHandler.text;

        isSaving = false;

        if (request.result != UnityWebRequest.Result.Success)
        {
            SetMessage("Could not save AI question parameters.");
            Debug.LogError("Save AI parameters request failed: " + request.error);
            Debug.LogError("Backend response: " + responseText);
            yield break;
        }

        SaveAIQuestionParametersResponseData response =
            JsonUtility.FromJson<SaveAIQuestionParametersResponseData>(responseText);

        if (response == null)
        {
            SetMessage("Invalid response from Azure Function.");
            Debug.LogError("Invalid Azure response: " + responseText);
            yield break;
        }

        if (!response.success)
        {
            string message = string.IsNullOrWhiteSpace(response.message)
                ? "Could not save AI question parameters."
                : response.message;

            SetMessage(message);
            Debug.LogWarning("Azure save failed: " + responseText);
            yield break;
        }

        AIQuestionParametersData finalParameter =
            response.parameters != null &&
            !string.IsNullOrEmpty(response.parameters.courseId)
                ? response.parameters
                : parameter;

        UpsertLocalParameter(finalParameter);

        if (AIQuestionParametersRuntime.Instance != null)
            AIQuestionParametersRuntime.Instance.SetCurrentParameters(finalParameter);

        ClearForm();

        if (parametersFormPanel != null)
            parametersFormPanel.SetActive(false);

        RefreshCourseRows();

        SetMessage("AI question parameters saved successfully.");
    }

    private void UpsertLocalParameter(AIQuestionParametersData parameter)
    {
        if (parameter == null || string.IsNullOrEmpty(parameter.courseId))
            return;

        AIQuestionParametersData existing = FindParametersByCourseId(parameter.courseId);

        if (existing == null)
        {
            parameters.Add(parameter);
            return;
        }

        existing.parameterId = parameter.parameterId;
        existing.courseId = parameter.courseId;
        existing.subjectCode = parameter.subjectCode;
        existing.subjectName = parameter.subjectName;
        existing.classCode = parameter.classCode;
        existing.teacherPlayFabId = parameter.teacherPlayFabId;

        existing.scenarioParameters = parameter.scenarioParameters;
        existing.focusInstructions = parameter.focusInstructions;
        existing.questionGoal = parameter.questionGoal;
        existing.allowedTopicsCsv = parameter.allowedTopicsCsv;
        existing.correctKeywordsCsv = parameter.correctKeywordsCsv;
        existing.wrongKeywordsCsv = parameter.wrongKeywordsCsv;

        existing.npcRole = parameter.npcRole;
        existing.answerLanguage = parameter.answerLanguage;

        existing.status = parameter.status;
        existing.updatedUtc = parameter.updatedUtc;
    }

    private void RefreshCourseRows()
    {
        ClearCourseRows();

        if (courseListContent == null || courseRowPrefab == null)
        {
            Debug.LogWarning(
                "Course list content or course row prefab is not assigned."
            );

            return;
        }

        List<AIQuestionParametersData> orderedParameters =
            parameters
                .Where(parameter =>
                    parameter != null &&
                    string.Equals(
                        parameter.status,
                        "ACTIVE",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                .OrderBy(parameter =>
                    GetNrcSortValue(parameter.classCode)
                )
                .ThenBy(
                    parameter => parameter.classCode ?? "",
                    StringComparer.OrdinalIgnoreCase
                )
                .ToList();

        foreach (AIQuestionParametersData parameter in orderedParameters)
        {
            TeacherAIQuestionParameterCourseRowUI row =
                Instantiate(
                    courseRowPrefab,
                    courseListContent
                );

            row.Setup(parameter, this);
        }
    }

    private int GetNrcSortValue(string classCode)
    {
        if (string.IsNullOrWhiteSpace(classCode))
            return int.MaxValue;

        if (int.TryParse(classCode.Trim(), out int nrc))
            return nrc;

        return int.MaxValue;
    }

    private void ClearCourseRows()
    {
        if (courseListContent == null)
            return;

        for (int i = courseListContent.childCount - 1; i >= 0; i--)
        {
            Destroy(courseListContent.GetChild(i).gameObject);
        }
    }

    private void ShowCourseListView()
    {
        if (courseListPanel != null)
            courseListPanel.SetActive(true);

        if (parametersFormPanel != null)
            parametersFormPanel.SetActive(false);
    }

    private void ShowParametersFormView()
    {
        if (parametersFormPanel == null)
        {
            Debug.LogError("Parameters Form Panel is not assigned in the Inspector.");
            return;
        }

        parametersFormPanel.SetActive(true);

        Debug.Log("Parameters form panel activated.");
    }

    private void FillFormFromSelectedCourse()
    {
        TeacherCourseData selectedCourse = GetSelectedCourse();

        if (selectedCourse == null)
        {
            ClearForm();
            return;
        }

        AIQuestionParametersData savedParameters =
            FindParametersByCourseId(selectedCourse.courseId);

        if (savedParameters == null || savedParameters.status != "ACTIVE")
        {
            ClearForm();
            SetMessage("Creating AI parameters for: " + GetCourseName(selectedCourse));
            return;
        }

        if (scenarioParametersInput != null)
            scenarioParametersInput.text = savedParameters.scenarioParameters;

        if (focusInstructionsInput != null)
            focusInstructionsInput.text = savedParameters.focusInstructions;

        if (questionGoalInput != null)
            questionGoalInput.text = savedParameters.questionGoal;

        if (allowedTopicsInput != null)
            allowedTopicsInput.text = savedParameters.allowedTopicsCsv;

        if (correctKeywordsInput != null)
            correctKeywordsInput.text = savedParameters.correctKeywordsCsv;

        if (wrongKeywordsInput != null)
            wrongKeywordsInput.text = savedParameters.wrongKeywordsCsv;

        SetMessage("Editing AI parameters for: " + GetCourseName(selectedCourse));
    }

    private void ClearForm()
    {
        if (scenarioParametersInput != null)
            scenarioParametersInput.text = "";

        if (focusInstructionsInput != null)
            focusInstructionsInput.text = "";

        if (questionGoalInput != null)
            questionGoalInput.text = "";

        if (allowedTopicsInput != null)
            allowedTopicsInput.text = "";

        if (correctKeywordsInput != null)
            correctKeywordsInput.text = "";

        if (wrongKeywordsInput != null)
            wrongKeywordsInput.text = "";
    }

    private bool SetDropdownToCourse(string courseId)
    {
        if (courseDropdown == null)
            return false;

        int activeIndex = -1;

        for (int i = 0; i < courses.Count; i++)
        {
            if (courses[i] == null || courses[i].status != "ACTIVE")
                continue;

            activeIndex++;

            if (courses[i].courseId == courseId)
            {
                courseDropdown.value = activeIndex;
                courseDropdown.RefreshShownValue();
                return true;
            }
        }

        return false;
    }

    private TeacherCourseData GetSelectedCourse()
    {
        if (courseDropdown == null || courses.Count == 0)
            return null;

        int activeIndex = -1;

        for (int i = 0; i < courses.Count; i++)
        {
            if (courses[i] == null || courses[i].status != "ACTIVE")
                continue;

            activeIndex++;

            if (activeIndex == courseDropdown.value)
                return courses[i];
        }

        return null;
    }

    private AIQuestionParametersData FindParametersByCourseId(string courseId)
    {
        if (string.IsNullOrEmpty(courseId))
            return null;

        foreach (AIQuestionParametersData item in parameters)
        {
            if (item != null && item.courseId == courseId)
                return item;
        }

        return null;
    }

    private int GetActiveCourseCount()
    {
        int count = 0;

        foreach (TeacherCourseData course in courses)
        {
            if (course != null && course.status == "ACTIVE")
                count++;
        }

        return count;
    }

    private string GetInputText(TMP_InputField input)
    {
        return input != null ? input.text.Trim() : "";
    }

    private string GetCourseName(TeacherCourseData course)
    {
        if (course == null)
            return "";

        if (!string.IsNullOrEmpty(course.subjectName))
            return course.subjectName;

        return course.courseName;
    }

    private string GetClassCode(TeacherCourseData course)
    {
        if (course == null)
            return "";

        if (!string.IsNullOrEmpty(course.classCode))
            return course.classCode;

        return course.courseCode;
    }

    private void NormalizeLoadedCourse(TeacherCourseData course)
    {
        if (course == null)
            return;

        if (string.IsNullOrEmpty(course.subjectName) &&
            !string.IsNullOrEmpty(course.courseName))
        {
            course.subjectName = course.courseName;
        }

        if (string.IsNullOrEmpty(course.classCode) &&
            !string.IsNullOrEmpty(course.courseCode))
        {
            course.classCode = course.courseCode;
        }

        if (string.IsNullOrEmpty(course.subjectCode))
            course.subjectCode = "UNKNOWN";

        if (string.IsNullOrEmpty(course.status))
            course.status = "ACTIVE";
    }

    private void SetMessage(string message)
    {
        if (messageText != null)
            messageText.text = message;

        Debug.Log("Teacher AI Question Parameters Panel: " + message);
    }
}