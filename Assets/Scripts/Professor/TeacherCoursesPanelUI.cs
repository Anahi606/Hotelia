using System.Collections.Generic;
using TMPro;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;

public class TeacherCoursesPanelUI : MonoBehaviour
{
    private const string TeacherCoursesKey = "Hotelia_TeacherCourses";

    [Header("Course Form")]
    [SerializeField] private GameObject courseFormPanel;
    [SerializeField] private TMP_InputField courseNameInput;
    [SerializeField] private TMP_InputField courseCodeInput;

    [Header("Course List")]
    [SerializeField] private Transform courseListContainer;
    [SerializeField] private GameObject courseRowPrefab;

    [Header("Messages")]
    [SerializeField] private TMP_Text messageText;

    private readonly List<TeacherCourseData> courses = new List<TeacherCourseData>();

    private string editingCourseId = "";
    private string selectedCourseId = "";

    public string SelectedCourseId => selectedCourseId;

    private void Start()
    {
        HideCourseForm();
        LoadCoursesFromPlayFab();
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
        editingCourseId = "";

        ClearCourseInputs();

        if (courseFormPanel != null)
            courseFormPanel.SetActive(false);

        SetMessage("");
    }

    public void SaveCourseButton()
    {
        if (!PlayfabManager.IsLoggedInWithEmail || !PlayfabManager.IsTeacher)
        {
            SetMessage("Only teacher accounts can create courses.");
            return;
        }

        string courseName = courseNameInput != null ? courseNameInput.text.Trim() : "";
        string courseCode = courseCodeInput != null ? courseCodeInput.text.Trim() : "";

        if (string.IsNullOrEmpty(courseName))
        {
            SetMessage("Enter a course name.");
            return;
        }

        if (!IsValidCourseCode(courseCode))
        {
            SetMessage("Course code must have exactly 4 numbers.");
            return;
        }

        if (CourseCodeExists(courseCode, editingCourseId))
        {
            SetMessage("A course with this code already exists.");
            return;
        }

        if (string.IsNullOrEmpty(editingCourseId))
        {
            CreateCourse(courseName, courseCode);
        }
        else
        {
            UpdateCourse(courseName, courseCode);
        }
    }

    private void CreateCourse(string courseName, string courseCode)
    {
        TeacherCourseData newCourse = new TeacherCourseData
        {
            courseId = "course_" + System.DateTime.UtcNow.Ticks,
            courseName = courseName,
            courseCode = courseCode,
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

            SetMessage("Course created successfully.");
        });
    }

    private void UpdateCourse(string courseName, string courseCode)
    {
        TeacherCourseData course = FindCourseById(editingCourseId);

        if (course == null)
        {
            SetMessage("Course not found.");
            return;
        }

        course.courseName = courseName;
        course.courseCode = courseCode;

        SaveCoursesToPlayFab(() =>
        {
            editingCourseId = "";

            ClearCourseInputs();

            if (courseFormPanel != null)
                courseFormPanel.SetActive(false);

            RefreshCourseList();

            SetMessage("Course updated successfully.");
        });
    }

    public void EditCourse(string courseId)
    {
        TeacherCourseData course = FindCourseById(courseId);

        if (course == null)
        {
            SetMessage("Course not found.");
            return;
        }

        editingCourseId = course.courseId;

        if (courseNameInput != null)
            courseNameInput.text = course.courseName;

        if (courseCodeInput != null)
            courseCodeInput.text = course.courseCode;

        if (courseFormPanel != null)
            courseFormPanel.SetActive(true);

        SetMessage("Editing course: " + course.courseName);
    }

    public void DeleteCourse(string courseId)
    {
        TeacherCourseData course = FindCourseById(courseId);

        if (course == null)
        {
            SetMessage("Course not found.");
            return;
        }

        courses.Remove(course);

        if (selectedCourseId == courseId)
            selectedCourseId = "";

        if (editingCourseId == courseId)
            editingCourseId = "";

        SaveCoursesToPlayFab(() =>
        {
            RefreshCourseList();
            SetMessage("Course deleted successfully.");
        });
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

        SetMessage("Selected course: " + course.courseName);

        Debug.Log("Selected course id: " + selectedCourseId);
    }

    private bool IsValidCourseCode(string courseCode)
    {
        if (string.IsNullOrEmpty(courseCode))
            return false;

        if (courseCode.Length != 4)
            return false;

        for (int i = 0; i < courseCode.Length; i++)
        {
            if (!char.IsDigit(courseCode[i]))
                return false;
        }

        return true;
    }

    private bool CourseCodeExists(string courseCode, string ignoredCourseId)
    {
        foreach (TeacherCourseData course in courses)
        {
            if (course.courseId == ignoredCourseId)
                continue;

            if (course.courseCode == courseCode)
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
                        courses.AddRange(savedData.courses);
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

    private void SaveCoursesToPlayFab(System.Action onSuccess)
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
    }

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
        if (courseNameInput != null)
            courseNameInput.text = "";

        if (courseCodeInput != null)
            courseCodeInput.text = "";
    }

    private void SetMessage(string message)
    {
        if (messageText != null)
            messageText.text = message;

        Debug.Log("Teacher Courses Panel: " + message);
    }
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
    public string courseName;
    public string courseCode;
    public string teacherPlayFabId;
    public string status;
}