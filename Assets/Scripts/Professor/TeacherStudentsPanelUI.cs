using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using PlayFab;
using PlayFab.ClientModels;
using SFB;

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
    [SerializeField] private string bulkCreateStudentsUrl;

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

        SetMessage("Loading courses...");

        LoadTeacherData(() =>
        {
            if (assignStudentPanel != null)
                assignStudentPanel.SetActive(true);

            SetMessage("Loading all students...");

            StartCoroutine(SearchStudentsRequest(""));
        });
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

    private void LoadTeacherData(System.Action onSuccess = null)
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

                onSuccess?.Invoke();
            },
            error =>
            {
                SetMessage("Could not load students.");
                Debug.LogError("Error loading teacher students: " + error.GenerateErrorReport());
            }
        );
    }

    public void ImportStudentsCsvButton()
    {
        if (!PlayfabManager.IsLoggedInWithEmail || !PlayfabManager.IsTeacher)
        {
            SetMessage("Only teacher accounts can import students.");
            return;
        }

        string[] paths = StandaloneFileBrowser.OpenFilePanel(
            "Select students CSV",
            "",
            "csv",
            false
        );

        if (paths == null || paths.Length == 0 || string.IsNullOrEmpty(paths[0]))
        {
            SetMessage("CSV import cancelled.");
            return;
        }

        string csvPath = paths[0];

        if (!File.Exists(csvPath))
        {
            SetMessage("CSV file was not found.");
            return;
        }

        string csvText = File.ReadAllText(csvPath, Encoding.UTF8);

        List<BulkStudentCsvRow> students = ParseStudentsCsv(csvText);

        if (students.Count == 0)
        {
            SetMessage("The CSV does not contain valid students.");
            return;
        }

        List<BulkStudentGroupData> groups = BuildStudentGroupsByNcr(students);

        if (groups.Count == 0)
        {
            SetMessage("No valid NCR groups were found in the CSV.");
            return;
        }

        SaveCoursesToPlayFab(() =>
        {
            StartCoroutine(BulkCreateStudentsByGroupsRequest(groups));
        });
    }

    private bool IsCsvRowCompletelyEmpty(string[] columns)
    {
        if (columns == null || columns.Length == 0)
            return true;

        foreach (string column in columns)
        {
            if (!string.IsNullOrWhiteSpace(column))
                return false;
        }

        return true;
    }

    private List<BulkStudentCsvRow> ParseStudentsCsv(string csvText)
    {
        List<BulkStudentCsvRow> students = new List<BulkStudentCsvRow>();

        if (string.IsNullOrWhiteSpace(csvText))
            return students;

        string[] lines = csvText.Split(new[] { "\r\n", "\n" }, System.StringSplitOptions.None);

        int headerLineIndex = -1;
        string[] headers = null;
        char separator = ',';

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();

            if (string.IsNullOrWhiteSpace(line))
                continue;

            separator = DetectCsvSeparator(line);
            headers = SplitCsvLine(line, separator);

            if (LooksLikeHeader(headers))
            {
                headerLineIndex = i;
                break;
            }
        }

        if (headerLineIndex < 0 || headers == null)
        {
            Debug.LogWarning("CSV must include headers: nombre, apellido, correo, banner, ncr.");
            return students;
        }

        Dictionary<string, int> map = BuildHeaderMap(headers);

        if (!map.ContainsKey("firstName") ||
            !map.ContainsKey("lastName") ||
            !map.ContainsKey("email") ||
            !map.ContainsKey("banner") ||
            !map.ContainsKey("ncr"))
        {
            Debug.LogWarning("CSV headers are incomplete. Required: nombre, apellido, correo, banner, ncr.");
            return students;
        }

        HashSet<string> usedEmails = new HashSet<string>();

        for (int i = headerLineIndex + 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();

            if (string.IsNullOrWhiteSpace(line))
                continue;

            string[] columns = SplitCsvLine(line, separator);

            if (IsCsvRowCompletelyEmpty(columns))
                continue;

            string firstName = GetColumnValue(columns, map["firstName"]);
            string lastName = GetColumnValue(columns, map["lastName"]);
            string email = GetColumnValue(columns, map["email"]).ToLower();
            string banner = GetColumnValue(columns, map["banner"]);
            string ncr = GetColumnValue(columns, map["ncr"]);

            if (string.IsNullOrWhiteSpace(firstName) ||
                string.IsNullOrWhiteSpace(lastName) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(banner) ||
                string.IsNullOrWhiteSpace(ncr))
            {
                Debug.LogWarning("CSV row has empty fields: " + line);
                continue;
            }

            if (!email.Contains("@"))
            {
                Debug.LogWarning("Invalid email: " + email);
                continue;
            }

            if (!IsValidNcr(ncr))
            {
                Debug.LogWarning("Invalid NCR. NCR must have exactly 4 numbers. Row: " + line);
                continue;
            }

            if (usedEmails.Contains(email))
            {
                Debug.LogWarning("Duplicated email in CSV skipped: " + email);
                continue;
            }

            usedEmails.Add(email);

            students.Add(new BulkStudentCsvRow
            {
                firstName = firstName,
                lastName = lastName,
                email = email,
                banner = banner,
                ncr = ncr
            });
        }

        return students;
    }

    private bool LooksLikeHeader(string[] headers)
    {
        if (headers == null || headers.Length == 0)
            return false;

        bool hasEmail = false;
        bool hasName = false;
        bool hasNcr = false;

        foreach (string header in headers)
        {
            string normalized = NormalizeHeader(header);

            if (normalized == "email")
                hasEmail = true;

            if (normalized == "firstName" || normalized == "lastName")
                hasName = true;

            if (normalized == "ncr")
                hasNcr = true;
        }

        return hasEmail && hasName && hasNcr;
    }

    private Dictionary<string, int> BuildHeaderMap(string[] headers)
    {
        Dictionary<string, int> map = new Dictionary<string, int>();

        for (int i = 0; i < headers.Length; i++)
        {
            string normalized = NormalizeHeader(headers[i]);

            if (string.IsNullOrEmpty(normalized))
                continue;

            if (!map.ContainsKey(normalized))
                map.Add(normalized, i);
        }

        return map;
    }

    private string NormalizeHeader(string header)
    {
        if (string.IsNullOrWhiteSpace(header))
            return "";

        string value = header.Trim().ToLower();

        value = value.Replace("á", "a");
        value = value.Replace("é", "e");
        value = value.Replace("í", "i");
        value = value.Replace("ó", "o");
        value = value.Replace("ú", "u");
        value = value.Replace("ñ", "n");

        value = value.Replace(" ", "");
        value = value.Replace("_", "");
        value = value.Replace("-", "");

        if (value == "nombre" ||
            value == "nombres" ||
            value == "firstname" ||
            value == "first")
            return "firstName";

        if (value == "apellido" ||
            value == "apellidos" ||
            value == "lastname" ||
            value == "last")
            return "lastName";

        if (value == "correo" ||
            value == "correoelectronico" ||
            value == "email" ||
            value == "mail")
            return "email";

        if (value == "banner" ||
            value == "numerobanner" ||
            value == "numerodebanner" ||
            value == "idbanner" ||
            value == "password" ||
            value == "contrasena")
            return "banner";

        if (value == "ncr" ||
            value == "codigoclase" ||
            value == "codigodeclase" ||
            value == "classcode" ||
            value == "coursecode")
            return "ncr";

        return "";
    }

    private string GetColumnValue(string[] columns, int index)
    {
        if (columns == null)
            return "";

        if (index < 0 || index >= columns.Length)
            return "";

        return columns[index].Trim();
    }

    private bool IsValidNcr(string ncr)
    {
        if (string.IsNullOrEmpty(ncr))
            return false;

        if (ncr.Length != 4)
            return false;

        for (int i = 0; i < ncr.Length; i++)
        {
            if (!char.IsDigit(ncr[i]))
                return false;
        }

        return true;
    }

    private char DetectCsvSeparator(string line)
    {
        int commaCount = 0;
        int semicolonCount = 0;

        foreach (char c in line)
        {
            if (c == ',')
                commaCount++;

            if (c == ';')
                semicolonCount++;
        }

        return semicolonCount > commaCount ? ';' : ',';
    }

    private string[] SplitCsvLine(string line, char separator)
    {
        List<string> values = new List<string>();
        StringBuilder current = new StringBuilder();
        bool insideQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                insideQuotes = !insideQuotes;
            }
            else if (c == separator && !insideQuotes)
            {
                values.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        values.Add(current.ToString());

        return values.ToArray();
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
            teacherSessionTicket = PlayfabManager.CurrentSessionTicket,
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

    private IEnumerator BulkCreateStudentsByGroupsRequest(List<BulkStudentGroupData> groups)
    {
        SetMessage("Creating student accounts...");

        int totalCreated = 0;
        int totalErrors = 0;

        foreach (BulkStudentGroupData group in groups)
        {
            if (group == null || group.course == null || group.students == null || group.students.Count == 0)
                continue;

            TeacherCourseData selectedCourse = group.course;

            BulkCreateStudentsRequestData data = new BulkCreateStudentsRequestData
            {
                teacherSessionTicket = PlayfabManager.CurrentSessionTicket,
                courseId = selectedCourse.courseId,
                courseName = GetCourseName(selectedCourse),
                courseCode = GetClassCode(selectedCourse),
                students = group.students
            };

            string json = JsonUtility.ToJson(data, true);

            Debug.Log("Bulk create students URL: " + bulkCreateStudentsUrl);

            UnityWebRequest request = CreateJsonPostRequest(bulkCreateStudentsUrl, json);
            request.timeout = 60;

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                totalErrors += group.students.Count;

                string backendText = request.downloadHandler != null ? request.downloadHandler.text : "";

                Debug.LogError(
                    "Bulk import failed for NCR " + GetClassCode(selectedCourse) +
                    ". HTTP Code: " + request.responseCode +
                    ". Error: " + request.error +
                    "\nBackend response: " + backendText
                );

                continue;
            }

            BulkCreateStudentsResponseData response =
                JsonUtility.FromJson<BulkCreateStudentsResponseData>(request.downloadHandler.text);

            if (response == null || !response.success)
            {
                totalErrors += group.students.Count;

                Debug.LogError("Bulk import failed for NCR " + GetClassCode(selectedCourse) + ": " +
                               (response != null ? response.message : "Invalid response"));

                continue;
            }

            totalCreated += response.createdCount;
            totalErrors += response.errorCount;

            if (response.students != null)
            {
                foreach (BulkCreatedStudentData student in response.students)
                {
                    AddOrUpdateAssignedStudent(student, selectedCourse);
                }
            }

            if (response.errors != null && response.errors.Length > 0)
            {
                foreach (string error in response.errors)
                    Debug.LogWarning("CSV import error: " + error);
            }
        }

        SaveAssignedStudentsToPlayFab(() =>
        {
            RefreshAssignedStudentsList();
            PopulateCourseDropdown();

            SetMessage(
                "Import finished. Assigned: " + totalCreated +
                ". Errors: " + totalErrors +
                ". Courses with pending subject can be edited in Courses."
            );
        });
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
            courseName = GetCourseName(selectedCourse),
            courseCode = GetClassCode(selectedCourse),
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
        existing.courseName = GetCourseName(selectedCourse);
        existing.courseCode = GetClassCode(selectedCourse);

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
            options.Add(GetCourseName(course) + " (" + GetClassCode(course) + ")");
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

    private List<BulkStudentGroupData> BuildStudentGroupsByNcr(List<BulkStudentCsvRow> students)
    {
        List<BulkStudentGroupData> groups = new List<BulkStudentGroupData>();

        foreach (BulkStudentCsvRow student in students)
        {
            if (student == null || !IsValidNcr(student.ncr))
                continue;

            TeacherCourseData course = GetOrCreateCourseByNcr(student.ncr);

            BulkStudentGroupData group = FindGroupByCourseId(groups, course.courseId);

            if (group == null)
            {
                group = new BulkStudentGroupData
                {
                    course = course,
                    students = new List<BulkStudentCsvRow>()
                };

                groups.Add(group);
            }

            group.students.Add(student);
        }

        return groups;
    }

    private BulkStudentGroupData FindGroupByCourseId(List<BulkStudentGroupData> groups, string courseId)
    {
        foreach (BulkStudentGroupData group in groups)
        {
            if (group.course != null && group.course.courseId == courseId)
                return group;
        }

        return null;
    }

    private TeacherCourseData GetOrCreateCourseByNcr(string ncr)
    {
        TeacherCourseData existingCourse = FindCourseByClassCode(ncr);

        if (existingCourse != null)
            return existingCourse;

        TeacherCourseData newCourse = new TeacherCourseData
        {
            courseId = "course_" + System.Guid.NewGuid().ToString("N"),

            subjectName = "Pending subject",
            subjectCode = "PENDING",
            period = 0,

            classCode = ncr,

            courseName = "Pending subject",
            courseCode = ncr,

            teacherPlayFabId = PlayfabManager.CurrentPlayFabId,
            status = "PENDING_SUBJECT"
        };

        courses.Add(newCourse);

        return newCourse;
    }

    private TeacherCourseData FindCourseByClassCode(string classCode)
    {
        foreach (TeacherCourseData course in courses)
        {
            if (GetClassCode(course) == classCode)
                return course;
        }

        return null;
    }

    private string GetClassCode(TeacherCourseData course)
    {
        if (course == null)
            return "";

        if (!string.IsNullOrEmpty(course.classCode))
            return course.classCode;

        return course.courseCode;
    }

    private string GetCourseName(TeacherCourseData course)
    {
        if (course == null)
            return "";

        if (!string.IsNullOrEmpty(course.subjectName))
            return course.subjectName;

        return course.courseName;
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
                SetMessage("Could not save courses.");
                Debug.LogError("Error saving teacher courses: " + error.GenerateErrorReport());
            }
        );
    }

    private void AddOrUpdateAssignedStudent(BulkCreatedStudentData student, TeacherCourseData selectedCourse)
    {
        if (student == null || selectedCourse == null)
            return;

        AssignedStudentData existing = FindAssignedStudent(student.playFabId);

        if (existing == null)
        {
            assignedStudents.Add(new AssignedStudentData
            {
                playFabId = student.playFabId,
                displayName = student.displayName,
                email = student.email,
                courseId = selectedCourse.courseId,
                courseName = GetCourseName(selectedCourse),
                courseCode = GetClassCode(selectedCourse),
                status = "ACTIVE"
            });

            return;
        }

        existing.displayName = student.displayName;
        existing.email = student.email;
        existing.courseId = selectedCourse.courseId;
        existing.courseName = GetCourseName(selectedCourse);
        existing.courseCode = GetClassCode(selectedCourse);
        existing.status = "ACTIVE";
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
    public string teacherSessionTicket;
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

[System.Serializable]
public class BulkStudentCsvRow
{
    public string firstName;
    public string lastName;
    public string email;
    public string banner;
    public string ncr;
}

[System.Serializable]
public class BulkCreateStudentsRequestData
{
    public string teacherSessionTicket;
    public string courseId;
    public string courseName;
    public string courseCode;
    public List<BulkStudentCsvRow> students;
}

[System.Serializable]
public class BulkCreateStudentsResponseData
{
    public bool success;
    public string message;
    public int createdCount;
    public int assignedCount;
    public int createdAccountCount;
    public int reusedAccountCount;
    public int errorCount;
    public BulkCreatedStudentData[] students;
    public string[] errors;
}

[System.Serializable]
public class BulkCreatedStudentData
{
    public string playFabId;
    public string displayName;
    public string email;
}

public class BulkStudentGroupData
{
    public TeacherCourseData course;
    public List<BulkStudentCsvRow> students = new List<BulkStudentCsvRow>();
}