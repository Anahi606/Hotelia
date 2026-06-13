using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using PlayFab;
using PlayFab.ClientModels;

public class TeacherStudentsPanelUI : MonoBehaviour
{
    private const string TeacherCoursesKey = "Hotelia_TeacherCourses";
    private const string TeacherStudentsKey = "Hotelia_TeacherStudents";

    [Header("Main Panel")]
    [SerializeField] private GameObject assignStudentPanel;
    [SerializeField] private Transform assignedStudentsContainer;
    [SerializeField] private GameObject assignedStudentRowPrefab;

    [Header("Assign Student Panel")]
    [SerializeField] private TMP_InputField searchInput;
    [SerializeField] private TMP_Dropdown courseDropdown;
    [SerializeField] private TMP_Text selectedStudentText;
    [SerializeField] private Transform searchResultsContainer;
    [SerializeField] private GameObject searchStudentRowPrefab;

    [Header("Messages")]
    [SerializeField] private TMP_Text messageText;

    [Header("Azure URLs")]
    [SerializeField] private string searchStudentsUrl;

    private readonly List<TeacherCourseData> courses = new List<TeacherCourseData>();
    private readonly List<AssignedStudentData> assignedStudents = new List<AssignedStudentData>();

    private StudentProfileData selectedStudent;
    private string editingStudentPlayFabId = "";

    private void Start()
    {
        CloseAssignStudentPanel();
        LoadTeacherData();
    }

    public void OpenAssignStudentPanel()
    {
        selectedStudent = null;
        editingStudentPlayFabId = "";

        if (searchInput != null)
            searchInput.text = "";

        if (selectedStudentText != null)
            selectedStudentText.text = "Selected student: none";

        ClearSearchResults();

        PopulateCourseDropdown();

        if (assignStudentPanel != null)
            assignStudentPanel.SetActive(true);

        SetMessage("Loading all students...");

        StartCoroutine(SearchStudentsRequest(""));
    }

    public void CloseAssignStudentPanel()
    {
        selectedStudent = null;
        editingStudentPlayFabId = "";

        if (assignStudentPanel != null)
            assignStudentPanel.SetActive(false);

        ClearSearchResults();
        SetMessage("");
    }

    private void LoadTeacherData()
    {
        if (!PlayfabManager.IsLoggedInWithEmail || !PlayfabManager.IsTeacher)
        {
            SetMessage("Only teacher accounts can manage students.");
            return;
        }

        SetMessage("Loading students...");

        var request = new GetUserDataRequest
        {
            Keys = new List<string>
            {
                TeacherCoursesKey,
                TeacherStudentsKey
            }
        };

        PlayFabClientAPI.GetUserData(
            request,
            result =>
            {
                courses.Clear();
                assignedStudents.Clear();

                if (result.Data != null && result.Data.ContainsKey(TeacherCoursesKey))
                {
                    TeacherCourseListData courseData =
                        JsonUtility.FromJson<TeacherCourseListData>(result.Data[TeacherCoursesKey].Value);

                    if (courseData != null && courseData.courses != null)
                        courses.AddRange(courseData.courses);
                }

                if (result.Data != null && result.Data.ContainsKey(TeacherStudentsKey))
                {
                    AssignedStudentListData studentData =
                        JsonUtility.FromJson<AssignedStudentListData>(result.Data[TeacherStudentsKey].Value);

                    if (studentData != null && studentData.students != null)
                        assignedStudents.AddRange(studentData.students);
                }

                PopulateCourseDropdown();
                RefreshAssignedStudentsList();

                SetMessage("Students loaded.");
            },
            error =>
            {
                SetMessage("Could not load students.");
                Debug.LogError("Error loading teacher students: " + error.GenerateErrorReport());
            }
        );
    }

    public void SearchStudentButton()
    {
        string query = searchInput != null ? searchInput.text.Trim() : "";

        StartCoroutine(SearchStudentsRequest(query));
    }

    private IEnumerator SearchStudentsRequest(string query)
    {
        SetMessage(string.IsNullOrEmpty(query) ? "Loading all students..." : "Searching students...");
        SearchStudentRequestData data = new SearchStudentRequestData
        {
            query = query
        };

        string json = JsonUtility.ToJson(data);

        UnityWebRequest request = CreateJsonPostRequest(searchStudentsUrl, json);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            SetMessage("Could not search students.");
            Debug.LogError("Search students request failed: " + request.error);
            Debug.LogError("Backend response: " + request.downloadHandler.text);
            yield break;
        }

        SearchStudentResponseData response =
            JsonUtility.FromJson<SearchStudentResponseData>(request.downloadHandler.text);

        if (response == null || !response.success)
        {
            SetMessage(response != null ? response.message : "Could not search students.");
            yield break;
        }

        ClearSearchResults();

        if (response.students == null || response.students.Length == 0)
        {
            SetMessage("No students found.");
            yield break;
        }

        foreach (StudentProfileData student in response.students)
        {
            GameObject rowObject = Instantiate(searchStudentRowPrefab, searchResultsContainer);
            SearchStudentRowUI rowUI = rowObject.GetComponent<SearchStudentRowUI>();

            if (rowUI != null)
                rowUI.Setup(student, this);
        }

        SetMessage(string.IsNullOrEmpty(query) ? "All students loaded. Select one." : "Select a student from the results.");
    }

    public void SelectStudentFromSearch(StudentProfileData student)
    {
        if (student == null)
            return;

        selectedStudent = student;

        if (selectedStudentText != null)
            selectedStudentText.text = "Selected student: " + student.displayName + " / " + student.email;

        SetMessage("Student selected. Choose a course and save.");
    }

    public void SaveAssignmentButton()
    {
        if (courseDropdown == null || courses.Count == 0)
        {
            SetMessage("Create a course first.");
            return;
        }

        int index = courseDropdown.value;

        if (index < 0 || index >= courses.Count)
        {
            SetMessage("Select a valid course.");
            return;
        }

        TeacherCourseData selectedCourse = courses[index];

        if (string.IsNullOrEmpty(editingStudentPlayFabId))
        {
            SaveNewStudentAssignment(selectedCourse);
        }
        else
        {
            SaveEditedStudentAssignment(selectedCourse);
        }
    }

    private void SaveNewStudentAssignment(TeacherCourseData selectedCourse)
    {
        if (selectedStudent == null)
        {
            SetMessage("Select a student first.");
            return;
        }

        AssignedStudentData existing = FindAssignedStudent(selectedStudent.playFabId);

        if (existing != null)
        {
            SetMessage("This student is already assigned to a course. Use Edit to change the course.");
            return;
        }

        AssignedStudentData assigned = new AssignedStudentData
        {
            playFabId = selectedStudent.playFabId,
            displayName = selectedStudent.displayName,
            email = selectedStudent.email,
            courseId = selectedCourse.courseId,
            courseName = selectedCourse.courseName,
            courseCode = selectedCourse.courseCode,
            status = "ACTIVE"
        };

        assignedStudents.Add(assigned);

        SaveAssignedStudentsToPlayFab(() =>
        {
            RefreshAssignedStudentsList();
            CloseAssignStudentPanel();
            SetMessage("Student assigned to course successfully.");
        });
    }

    private void SaveEditedStudentAssignment(TeacherCourseData selectedCourse)
    {
        AssignedStudentData existing = FindAssignedStudent(editingStudentPlayFabId);

        if (existing == null)
        {
            SetMessage("Assigned student not found.");
            return;
        }

        existing.courseId = selectedCourse.courseId;
        existing.courseName = selectedCourse.courseName;
        existing.courseCode = selectedCourse.courseCode;

        SaveAssignedStudentsToPlayFab(() =>
        {
            RefreshAssignedStudentsList();
            CloseAssignStudentPanel();
            SetMessage("Student course updated successfully.");
        });
    }

    public void EditStudentAssignment(string studentPlayFabId)
    {
        AssignedStudentData student = FindAssignedStudent(studentPlayFabId);

        if (student == null)
        {
            SetMessage("Student not found.");
            return;
        }

        editingStudentPlayFabId = student.playFabId;

        selectedStudent = new StudentProfileData
        {
            playFabId = student.playFabId,
            displayName = student.displayName,
            email = student.email
        };

        if (assignStudentPanel != null)
            assignStudentPanel.SetActive(true);

        if (selectedStudentText != null)
            selectedStudentText.text = "Editing: " + student.displayName + " / " + student.email;

        if (searchInput != null)
            searchInput.text = student.email;

        ClearSearchResults();
        PopulateCourseDropdown();
        SetDropdownToCourse(student.courseId);

        SetMessage("Choose a new course and save.");
    }

    public void RemoveStudentFromCourse(string studentPlayFabId)
    {
        AssignedStudentData student = FindAssignedStudent(studentPlayFabId);

        if (student == null)
        {
            SetMessage("Student not found.");
            return;
        }

        assignedStudents.Remove(student);

        SaveAssignedStudentsToPlayFab(() =>
        {
            RefreshAssignedStudentsList();
            SetMessage("Student removed from course. The student account was not deleted.");
        });
    }

    private void SaveAssignedStudentsToPlayFab(System.Action onSuccess)
    {
        AssignedStudentListData data = new AssignedStudentListData
        {
            students = assignedStudents
        };

        string json = JsonUtility.ToJson(data, true);

        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                { TeacherStudentsKey, json }
            },
            Permission = UserDataPermission.Private
        };

        PlayFabClientAPI.UpdateUserData(
            request,
            result =>
            {
                Debug.Log("Assigned students saved to teacher PlayFab data.");
                onSuccess?.Invoke();
            },
            error =>
            {
                SetMessage("Could not save assigned students.");
                Debug.LogError("Error saving assigned students: " + error.GenerateErrorReport());
            }
        );
    }

    private void RefreshAssignedStudentsList()
    {
        ClearAssignedStudentsList();

        foreach (AssignedStudentData student in assignedStudents)
        {
            GameObject rowObject = Instantiate(assignedStudentRowPrefab, assignedStudentsContainer);
            AssignedStudentRowUI rowUI = rowObject.GetComponent<AssignedStudentRowUI>();

            if (rowUI != null)
            {
                rowUI.Setup(student, this);
            }
            else
            {
                Debug.LogWarning("AssignedStudentRowPrefab does not have AssignedStudentRowUI attached.");
            }
        }
    }

    private void PopulateCourseDropdown()
    {
        if (courseDropdown == null)
            return;

        courseDropdown.ClearOptions();

        List<string> options = new List<string>();

        foreach (TeacherCourseData course in courses)
        {
            options.Add(course.courseName + " (" + course.courseCode + ")");
        }

        if (options.Count == 0)
            options.Add("No courses available");

        courseDropdown.AddOptions(options);
        courseDropdown.value = 0;
        courseDropdown.RefreshShownValue();
    }

    private void SetDropdownToCourse(string courseId)
    {
        if (courseDropdown == null)
            return;

        for (int i = 0; i < courses.Count; i++)
        {
            if (courses[i].courseId == courseId)
            {
                courseDropdown.value = i;
                courseDropdown.RefreshShownValue();
                return;
            }
        }
    }

    private AssignedStudentData FindAssignedStudent(string playFabId)
    {
        foreach (AssignedStudentData student in assignedStudents)
        {
            if (student.playFabId == playFabId)
                return student;
        }

        return null;
    }

    private UnityWebRequest CreateJsonPostRequest(string url, string json)
    {
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        return request;
    }

    private void ClearAssignedStudentsList()
    {
        if (assignedStudentsContainer == null)
            return;

        foreach (Transform child in assignedStudentsContainer)
            Destroy(child.gameObject);
    }

    private void ClearSearchResults()
    {
        if (searchResultsContainer == null)
            return;

        foreach (Transform child in searchResultsContainer)
            Destroy(child.gameObject);
    }

    private void SetMessage(string message)
    {
        if (messageText != null)
            messageText.text = message;

        Debug.Log("Teacher Students Panel: " + message);
    }
}

[System.Serializable]
public class AssignedStudentListData
{
    public List<AssignedStudentData> students = new List<AssignedStudentData>();
}

[System.Serializable]
public class AssignedStudentData
{
    public string playFabId;
    public string displayName;
    public string email;
    public string courseId;
    public string courseName;
    public string courseCode;
    public string status;
}

[System.Serializable]
public class SearchStudentRequestData
{
    public string query;
}

[System.Serializable]
public class SearchStudentResponseData
{
    public bool success;
    public string message;
    public StudentProfileData[] students;
}

[System.Serializable]
public class StudentProfileData
{
    public string playFabId;
    public string displayName;
    public string email;
}