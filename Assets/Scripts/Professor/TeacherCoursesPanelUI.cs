using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using PlayFab;
using PlayFab.ClientModels;

public class TeacherCoursesPanelUI : MonoBehaviour
{
    private const string TeacherCoursesKey = "Hotelia_TeacherCourses";
    private const string SubjectCatalogKey = "Hotelia_SubjectCatalog";

    [Header("Course Form")]
    [SerializeField] private GameObject courseFormPanel;
    [SerializeField] private TMP_Dropdown subjectDropdown;
    [SerializeField] private TMP_InputField classCodeInput;

    [Header("Course List")]
    [SerializeField] private Transform courseListContainer;
    [SerializeField] private GameObject courseRowPrefab;

    [Header("Messages")]
    [SerializeField] private TMP_Text messageText;

    [Header("Related Panels")]
    [SerializeField] private TeacherStudentsPanelUI studentsPanelUI;

    [Header("Azure Backend")]
    [SerializeField] private string manageTeacherCourseUrl;

    private readonly List<TeacherCourseData> courses = new List<TeacherCourseData>();
    private readonly List<TeacherSubjectCatalogData> subjectCatalog = new List<TeacherSubjectCatalogData>();

    private string editingCourseId = "";
    private string selectedCourseId = "";
    private bool isManagingCourse;

    public string SelectedCourseId => selectedCourseId;

    private void Start()
    {
        HideCourseForm();
        LoadSubjectCatalogFromPlayFab();
        LoadCoursesFromPlayFab();
    }

    private void LoadSubjectCatalogFromPlayFab()
    {
        SetMessage("Loading subject catalog...");

        var request = new GetTitleDataRequest
        {
            Keys = new List<string> { SubjectCatalogKey }
        };

        PlayFabClientAPI.GetTitleData(
            request,
            result =>
            {
                subjectCatalog.Clear();

                if (result.Data == null || !result.Data.ContainsKey(SubjectCatalogKey))
                {
                    SetMessage("Subject catalog not found in PlayFab Title Data.");
                    return;
                }

                string json = result.Data[SubjectCatalogKey];

                TeacherSubjectCatalogListData catalogData =
                    JsonUtility.FromJson<TeacherSubjectCatalogListData>(json);

                if (catalogData == null || catalogData.subjects == null || catalogData.subjects.Count == 0)
                {
                    SetMessage("Subject catalog is empty or invalid.");
                    return;
                }

                foreach (TeacherSubjectCatalogData subject in catalogData.subjects)
                {
                    if (subject != null && subject.status == "ACTIVE")
                    {
                        subjectCatalog.Add(subject);
                    }
                }

                PopulateSubjectDropdown();

                SetMessage("Subject catalog loaded.");
            },
            error =>
            {
                SetMessage("Could not load subject catalog.");
                Debug.LogError("Error loading subject catalog: " + error.GenerateErrorReport());
            }
        );
    }

    private void RefreshStudentsAfterCourseChange()
    {
        if (studentsPanelUI == null)
        {
            Debug.LogWarning(
                "TeacherStudentsPanelUI is not assigned in TeacherCoursesPanelUI."
            );

            return;
        }

        Debug.Log("[Courses] Reloading students after course change.");

        studentsPanelUI.RefreshStudentsPanel();
    }

    private void PopulateSubjectDropdown()
    {
        if (subjectDropdown == null)
        {
            Debug.LogWarning("SubjectDropdown is not assigned in the Inspector.");
            return;
        }

        subjectDropdown.ClearOptions();

        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();

        foreach (TeacherSubjectCatalogData subject in subjectCatalog)
        {
            string optionText = subject.subjectName + " (" + subject.subjectCode + ")";
            options.Add(new TMP_Dropdown.OptionData(optionText));
        }

        subjectDropdown.AddOptions(options);

        if (subjectDropdown.options.Count > 0)
            subjectDropdown.value = 0;

        subjectDropdown.RefreshShownValue();
    }

    public void ShowCourseForm()
    {
        editingCourseId = "";

        ClearCourseInputs();

        if (courseFormPanel != null)
            courseFormPanel.SetActive(true);

        SetMessage("");
    }

    public void HideCourseForm()
    {
        Debug.Log("CLICK EN CANCEL");

        editingCourseId = "";

        ClearCourseInputs();

        if (courseFormPanel != null)
            courseFormPanel.SetActive(false);

        SetMessage("");
    }

    public void SaveCourseButton()
    {
        Debug.Log("CLICK EN SAVE");

        if (isManagingCourse)
            return;

        if (!PlayfabManager.IsLoggedInWithEmail ||
            !PlayfabManager.IsTeacher)
        {
            SetMessage("Only teacher accounts can create courses.");
            return;
        }

        if (string.IsNullOrWhiteSpace(
            PlayfabManager.CurrentSessionTicket))
        {
            SetMessage(
                "Missing PlayFab session. Please log in again."
            );

            return;
        }

        if (subjectCatalog.Count == 0)
        {
            SetMessage(
                "Subject catalog has not loaded yet."
            );

            return;
        }

        TeacherSubjectCatalogData selectedSubject =
            GetSelectedSubject();

        if (selectedSubject == null)
        {
            SetMessage("Select a valid subject.");
            return;
        }

        string classCode =
            classCodeInput != null
                ? classCodeInput.text.Trim()
                : "";

        if (!IsValidClassCode(classCode))
        {
            SetMessage(
                "NCR must have exactly 4 numbers."
            );

            return;
        }

        if (ClassCodeExists(
            classCode,
            editingCourseId))
        {
            SetMessage(
                "A course with this NCR already exists."
            );

            return;
        }

        StartCoroutine(
            ManageCourseRequest(
                "save",
                editingCourseId,
                selectedSubject.subjectCode,
                classCode
            )
        );
    }

    /*private void CreateCourse(TeacherSubjectCatalogData subject, string classCode)
    {
        TeacherCourseData newCourse = new TeacherCourseData
        {
            courseId = "course_" + System.DateTime.UtcNow.Ticks,

            subjectName = subject.subjectName,
            subjectCode = subject.subjectCode,
            period = subject.period,
            classCode = classCode,

            courseName = subject.subjectName,
            courseCode = classCode,

            teacherPlayFabId = PlayfabManager.CurrentPlayFabId,
            status = "ACTIVE"
        };

        courses.Add(newCourse);

        SaveCoursesToPlayFab(() =>
        {
            ClearCourseInputs();

            if (courseFormPanel != null)
                courseFormPanel.SetActive(false);

            RefreshCourseList();
            RefreshStudentsAfterCourseChange();

            SetMessage("Course created successfully.");
        });
    }

    private void UpdateCourse(TeacherSubjectCatalogData subject, string classCode)
    {
        TeacherCourseData course = FindCourseById(editingCourseId);

        if (course == null)
        {
            SetMessage("Course not found.");
            return;
        }

        course.subjectName = subject.subjectName;
        course.subjectCode = subject.subjectCode;
        course.period = subject.period;
        course.classCode = classCode;

        course.courseName = subject.subjectName;
        course.courseCode = classCode;

        course.teacherPlayFabId = PlayfabManager.CurrentPlayFabId;
        course.status = "ACTIVE";

        SaveCoursesToPlayFab(() =>
        {
            editingCourseId = "";

            ClearCourseInputs();

            if (courseFormPanel != null)
                courseFormPanel.SetActive(false);

            RefreshCourseList();

            RefreshStudentsAfterCourseChange();

            SetMessage("Course updated successfully.");
        });
    }
    */
    public void EditCourse(string courseId)
    {
        TeacherCourseData course = FindCourseById(courseId);

        if (course == null)
        {
            SetMessage("Course not found.");
            return;
        }

        editingCourseId = course.courseId;

        if (subjectDropdown != null)
        {
            int subjectIndex = FindSubjectIndexByCode(course.subjectCode);
            subjectDropdown.value = subjectIndex;
            subjectDropdown.RefreshShownValue();
        }

        if (classCodeInput != null)
            classCodeInput.text = GetClassCode(course);

        if (courseFormPanel != null)
            courseFormPanel.SetActive(true);

        SetMessage("Editing course: " + GetCourseName(course));
    }

    public void DeleteCourse(string courseId)
    {
        if (isManagingCourse)
            return;

        TeacherCourseData course =
            FindCourseById(courseId);

        if (course == null)
        {
            SetMessage("Course not found.");
            return;
        }

        if (string.IsNullOrWhiteSpace(
            PlayfabManager.CurrentSessionTicket))
        {
            SetMessage(
                "Missing PlayFab session. Please log in again."
            );

            return;
        }

        StartCoroutine(
            ManageCourseRequest(
                "delete",
                courseId,
                "",
                GetClassCode(course)
            )
        );
    }

    private IEnumerator ManageCourseRequest(
    string action,
    string courseId,
    string subjectCode,
    string classCode)
    {
        if (string.IsNullOrWhiteSpace(
            manageTeacherCourseUrl))
        {
            SetMessage(
                "Missing manage teacher course URL."
            );

            yield break;
        }

        isManagingCourse = true;

        SetMessage(
            action == "delete"
                ? "Deleting course..."
                : "Validating NCR and saving course..."
        );

        ManageTeacherCourseRequestData requestData =
            new ManageTeacherCourseRequestData
            {
                sessionTicket =
                    PlayfabManager.CurrentSessionTicket,

                action = action,
                courseId = courseId ?? "",
                subjectCode = subjectCode ?? "",
                classCode = classCode ?? ""
            };

        string json =
            JsonUtility.ToJson(requestData);

        byte[] bodyRaw =
            Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest request =
            new UnityWebRequest(
                manageTeacherCourseUrl,
                "POST"))
        {
            request.uploadHandler =
                new UploadHandlerRaw(bodyRaw);

            request.downloadHandler =
                new DownloadHandlerBuffer();

            request.timeout = 30;

            request.SetRequestHeader(
                "Content-Type",
                "application/json"
            );

            yield return request.SendWebRequest();

            string responseText =
                request.downloadHandler != null
                    ? request.downloadHandler.text
                    : "";

            ManageTeacherCourseResponseData response =
                ParseManageCourseResponse(
                    responseText
                );

            isManagingCourse = false;

            /*
             * Por ejemplo, si Azure responde HTTP 409 porque
             * el NCR pertenece a otro profesor, se muestra
             * el mensaje específico que envió el backend.
             */
            if (response != null &&
                !response.success)
            {
                SetMessage(
                    string.IsNullOrWhiteSpace(
                        response.message)
                        ? "Could not manage course."
                        : response.message
                );

                Debug.LogWarning(
                    "Manage course rejected. HTTP " +
                    request.responseCode +
                    ": " +
                    responseText
                );

                yield break;
            }

            if (request.result !=
                UnityWebRequest.Result.Success)
            {
                SetMessage(
                    "Could not connect to the course service."
                );

                Debug.LogError(
                    "Manage course request failed. HTTP " +
                    request.responseCode +
                    ": " +
                    request.error
                );

                Debug.LogError(
                    "Backend response: " +
                    responseText
                );

                yield break;
            }

            if (response == null ||
                !response.success)
            {
                SetMessage(
                    "Invalid response from Azure Function."
                );

                Debug.LogError(
                    "Invalid manage course response: " +
                    responseText
                );

                yield break;
            }

            if (action == "delete")
            {
                courses.RemoveAll(
                    item =>
                        item != null &&
                        item.courseId == courseId
                );

                if (selectedCourseId == courseId)
                    selectedCourseId = "";

                if (editingCourseId == courseId)
                    editingCourseId = "";

                RefreshCourseList();
                RefreshStudentsAfterCourseChange();

                SetMessage(
                    string.IsNullOrWhiteSpace(
                        response.message)
                        ? "Course deleted successfully."
                        : response.message
                );

                yield break;
            }

            if (response.course == null ||
                string.IsNullOrWhiteSpace(
                    response.course.courseId))
            {
                SetMessage(
                    "The backend did not return the saved course."
                );

                yield break;
            }

            NormalizeLoadedCourse(
                response.course
            );

            UpsertLocalCourse(
                response.course
            );

            editingCourseId = "";

            ClearCourseInputs();

            if (courseFormPanel != null)
                courseFormPanel.SetActive(false);

            RefreshCourseList();
            RefreshStudentsAfterCourseChange();

            SetMessage(
                string.IsNullOrWhiteSpace(
                    response.message)
                    ? "Course saved successfully."
                    : response.message
            );
        }
    }

    private ManageTeacherCourseResponseData
    ParseManageCourseResponse(
        string responseText)
    {
        if (string.IsNullOrWhiteSpace(
            responseText))
        {
            return null;
        }

        try
        {
            return JsonUtility.FromJson<
                ManageTeacherCourseResponseData
            >(responseText);
        }
        catch (System.Exception exception)
        {
            Debug.LogError(
                "Could not parse manage course response: " +
                exception.Message
            );

            return null;
        }
    }

    private void UpsertLocalCourse(
    TeacherCourseData savedCourse)
    {
        if (savedCourse == null ||
            string.IsNullOrWhiteSpace(
                savedCourse.courseId))
        {
            return;
        }

        TeacherCourseData existing =
            FindCourseById(
                savedCourse.courseId
            );

        if (existing == null)
        {
            courses.Add(savedCourse);
            return;
        }

        existing.subjectName =
            savedCourse.subjectName;

        existing.subjectCode =
            savedCourse.subjectCode;

        existing.period =
            savedCourse.period;

        existing.classCode =
            savedCourse.classCode;

        existing.teacherPlayFabId =
            savedCourse.teacherPlayFabId;

        existing.status =
            savedCourse.status;

        existing.courseName =
            savedCourse.courseName;

        existing.courseCode =
            savedCourse.courseCode;
    }

    public void SelectCourse(string courseId)
    {
        TeacherCourseData course = FindCourseById(courseId);

        if (course == null)
        {
            SetMessage("Course not found.");
            return;
        }

        selectedCourseId = course.courseId;

        SetMessage("Selected course: " + GetCourseName(course));

        Debug.Log("Selected course id: " + selectedCourseId);
    }

    private TeacherSubjectCatalogData GetSelectedSubject()
    {
        if (subjectDropdown == null)
            return null;

        if (subjectCatalog.Count == 0)
            return null;

        int index = subjectDropdown.value;

        if (index < 0 || index >= subjectCatalog.Count)
            return null;

        return subjectCatalog[index];
    }

    private int FindSubjectIndexByCode(string subjectCode)
    {
        if (string.IsNullOrEmpty(subjectCode))
            return 0;

        for (int i = 0; i < subjectCatalog.Count; i++)
        {
            if (subjectCatalog[i].subjectCode == subjectCode)
                return i;
        }

        return 0;
    }

    private bool IsValidClassCode(string classCode)
    {
        if (string.IsNullOrEmpty(classCode))
            return false;

        if (classCode.Length != 4)
            return false;

        for (int i = 0; i < classCode.Length; i++)
        {
            if (!char.IsDigit(classCode[i]))
                return false;
        }

        return true;
    }

    private bool ClassCodeExists(string classCode, string ignoredCourseId)
    {
        foreach (TeacherCourseData course in courses)
        {
            if (course.courseId == ignoredCourseId)
                continue;

            if (GetClassCode(course) == classCode)
                return true;
        }

        return false;
    }

    private TeacherCourseData FindCourseById(string courseId)
    {
        foreach (TeacherCourseData course in courses)
        {
            if (course.courseId == courseId)
                return course;
        }

        return null;
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

        if (string.IsNullOrEmpty(course.subjectName) && !string.IsNullOrEmpty(course.courseName))
            course.subjectName = course.courseName;

        if (string.IsNullOrEmpty(course.classCode) && !string.IsNullOrEmpty(course.courseCode))
            course.classCode = course.courseCode;

        if (string.IsNullOrEmpty(course.subjectCode))
            course.subjectCode = "UNKNOWN";
    }

    private void LoadCoursesFromPlayFab()
    {
        if (!PlayfabManager.IsLoggedInWithEmail || !PlayfabManager.IsTeacher)
        {
            SetMessage("Only teacher accounts can load courses.");
            return;
        }

        SetMessage("Loading courses...");

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

                    TeacherCourseListData savedData = JsonUtility.FromJson<TeacherCourseListData>(json);

                    if (savedData != null && savedData.courses != null)
                    {
                        courses.AddRange(savedData.courses);

                        foreach (TeacherCourseData course in courses)
                            NormalizeLoadedCourse(course);
                    }
                }

                RefreshCourseList();
                SetMessage("Courses loaded.");
            },
            error =>
            {
                SetMessage("Could not load courses.");
                Debug.LogError("Error loading teacher courses: " + error.GenerateErrorReport());
            }
        );
    }

    /*private void SaveCoursesToPlayFab(System.Action onSuccess)
    {
        TeacherCourseListData data = new TeacherCourseListData
        {
            courses = courses
        };

        string json = JsonUtility.ToJson(data, true);

        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                { TeacherCoursesKey, json }
            },
            Permission = UserDataPermission.Private
        };

        PlayFabClientAPI.UpdateUserData(
            request,
            result =>
            {
                Debug.Log("Teacher courses saved to PlayFab.");
                onSuccess?.Invoke();
            },
            error =>
            {
                SetMessage("Could not save course.");
                Debug.LogError("Error saving teacher courses: " + error.GenerateErrorReport());
            }
        );
    }*/

    private void RefreshCourseList()
    {
        ClearCourseList();

        foreach (TeacherCourseData course in courses)
        {
            GameObject rowObject = Instantiate(courseRowPrefab, courseListContainer);

            TeacherCourseRowUI rowUI = rowObject.GetComponent<TeacherCourseRowUI>();

            if (rowUI != null)
            {
                rowUI.Setup(course, this);
            }
            else
            {
                Debug.LogWarning("CourseRowPrefab does not have TeacherCourseRowUI attached.");
            }
        }
    }

    private void ClearCourseList()
    {
        if (courseListContainer == null)
            return;

        foreach (Transform child in courseListContainer)
            Destroy(child.gameObject);
    }

    private void ClearCourseInputs()
    {
        if (subjectDropdown != null && subjectDropdown.options.Count > 0)
        {
            subjectDropdown.value = 0;
            subjectDropdown.RefreshShownValue();
        }

        if (classCodeInput != null)
            classCodeInput.text = "";
    }

    private void SetMessage(string message)
    {
        if (messageText != null)
            messageText.text = message;

        Debug.Log("Teacher Courses Panel: " + message);
    }
}

[System.Serializable]
public class ManageTeacherCourseRequestData
{
    public string sessionTicket;
    public string action;
    public string courseId;
    public string subjectCode;
    public string classCode;
}

[System.Serializable]
public class ManageTeacherCourseResponseData
{
    public bool success;
    public string message;
    public TeacherCourseData course;
}

[System.Serializable]
public class TeacherCourseListData
{
    public List<TeacherCourseData> courses = new List<TeacherCourseData>();
}

[System.Serializable]
public class TeacherCourseData
{
    public string courseId;

    public string subjectName;
    public string subjectCode;
    public int period;

    public string classCode;

    public string teacherPlayFabId;
    public string status;

    public string courseName;
    public string courseCode;
}

[System.Serializable]
public class TeacherSubjectCatalogListData
{
    public List<TeacherSubjectCatalogData> subjects = new List<TeacherSubjectCatalogData>();
}

[System.Serializable]
public class TeacherSubjectCatalogData
{
    public string subjectCode;
    public string subjectName;
    public int period;
    public string status;
    public List<string> relatedMinigames = new List<string>();
    public List<string> relatedResultAreas = new List<string>();
}