using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using PlayFab;
using PlayFab.ClientModels;
using XCharts.Runtime;
using UnityEngine.UI;

public class TeacherPerformancePanelUI : MonoBehaviour
{
    private const string TeacherCoursesKey = "Hotelia_TeacherCourses";
    private const string TeacherStudentsKey = "Hotelia_TeacherStudents";
    private const string SubjectCatalogKey = "Hotelia_SubjectCatalog";

    [Header("Filters")]
    [SerializeField] private TMP_Dropdown courseDropdown;
    [SerializeField] private TMP_Dropdown dayDropdown;

    private readonly List<CloudDailyResultData> currentSubjectResults = new List<CloudDailyResultData>();
    private readonly List<SubjectCoursePerformanceData> currentSubjectCourseSummaries = new List<SubjectCoursePerformanceData>();

    private bool isViewingSubjectSummary = false;
    private int currentSubjectCurrentDay = 1;
    private Coroutine subjectSummaryCoroutine;

    [Header("Students List")]
    [SerializeField] private Transform studentsContainer;
    [SerializeField] private GameObject performanceStudentRowPrefab;

    [Header("Report Texts")]
    [SerializeField] private TMP_Text selectedStudentText;

    [Header("KPI Values")]
    [SerializeField] private TMP_Text generalPerformanceText;
    [SerializeField] private TMP_Text totalRevenueText;
    [SerializeField] private TMP_Text totalErrorsText;
    [SerializeField] private TMP_Text activitiesText;

    [Header("KPI Labels")]
    [SerializeField] private TMP_Text performanceLabelText;
    [SerializeField] private TMP_Text revenueLabelText;
    [SerializeField] private TMP_Text errorsLabelText;
    [SerializeField] private TMP_Text activitiesLabelText;

    [Header("Result Rows")]
    [SerializeField] private Transform resultsContainer;
    [SerializeField] private GameObject performanceResultRowPrefab;

    [Header("Charts")]
    [SerializeField] private LineChart performanceLineChart;
    [SerializeField] private BarChart activityScoreBarChart;
    [SerializeField] private BarChart revenueBarChart;
    [SerializeField] private BarChart errorsBarChart;

    [Header("PDF Export")]
    [SerializeField] private Button downloadReportButton;
    [SerializeField] private TMP_Text downloadReportButtonText;
    [SerializeField] private ScrollRect reportScrollRect;
    [SerializeField] private RectTransform[] pdfCapturePages;
    [SerializeField] private GameObject[] objectsToHideWhileExporting;
    [SerializeField] private string pdfReportsFolderName = "HoteliaReports";

    private Coroutine pdfExportCoroutine;
    private bool canDownloadPdf = false;
    private string disabledPdfDownloadText = "Select a report first";

    [Header("Azure URLs")]
    [SerializeField] private string getStudentPerformanceUrl;

    private readonly List<TeacherCourseData> courses = new List<TeacherCourseData>();
    private readonly List<AssignedStudentData> assignedStudents = new List<AssignedStudentData>();
    private readonly List<CloudDailyResultData> currentStudentResults = new List<CloudDailyResultData>();
    private readonly List<TeacherSubjectCatalogData> subjectCatalog = new List<TeacherSubjectCatalogData>();

    private AssignedStudentData currentSelectedStudent;
    private int currentStudentCurrentDay = 1;

    private readonly List<CloudDailyResultData> currentCourseResults = new List<CloudDailyResultData>();
    private readonly List<CourseStudentPerformanceData> currentCourseStudentSummaries = new List<CourseStudentPerformanceData>();

    private bool isViewingCourseSummary = false;
    private int currentCourseCurrentDay = 1;
    private Coroutine courseSummaryCoroutine;

    private void OnEnable()
    {
        LoadSubjectCatalogFromPlayFab();
    }

    private void LoadSubjectCatalogFromPlayFab()
    {
        var request = new GetTitleDataRequest
        {
            Keys = new List<string> { SubjectCatalogKey }
        };

        PlayFabClientAPI.GetTitleData(
            request,
            result =>
            {
                subjectCatalog.Clear();

                if (result.Data != null && result.Data.ContainsKey(SubjectCatalogKey))
                {
                    string json = result.Data[SubjectCatalogKey];

                    TeacherSubjectCatalogListData catalogData =
                        JsonUtility.FromJson<TeacherSubjectCatalogListData>(json);

                    if (catalogData != null && catalogData.subjects != null)
                    {
                        foreach (TeacherSubjectCatalogData subject in catalogData.subjects)
                        {
                            if (subject != null && subject.status == "ACTIVE")
                                subjectCatalog.Add(subject);
                        }
                    }
                }

                LoadTeacherData();
            },
            error =>
            {
                Debug.LogError("Error loading subject catalog: " + error.GenerateErrorReport());
                LoadTeacherData();
            }
        );
    }

    private void LoadTeacherData()
    {

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
                RefreshStudentsBySelectedCourse();
                ClearReport();
            },
            error =>
            {
                Debug.LogError("Error loading performance data: " + error.GenerateErrorReport());
            }
        );
    }

    private void PopulateCourseDropdown()
    {
        if (courseDropdown == null)
            return;

        courseDropdown.onValueChanged.RemoveAllListeners();
        courseDropdown.ClearOptions();

        List<string> options = new List<string>();

        foreach (TeacherCourseData course in courses)
        {
            string optionText = GetCourseName(course) + " (" + GetSubjectCode(course) + ") - Class " + GetClassCode(course);
            options.Add(optionText);
        }

        if (options.Count == 0)
            options.Add("No courses available");

        courseDropdown.AddOptions(options);
        courseDropdown.value = 0;
        courseDropdown.RefreshShownValue();

        courseDropdown.onValueChanged.AddListener(delegate
        {
            UpdateKpiLabelsForSelectedCourse();
            RefreshStudentsBySelectedCourse();
            ClearReport();
        });
    }

    private void RefreshStudentsBySelectedCourse()
    {
        ClearStudentsList();

        if (courses.Count == 0)
        {
            return;
        }

        int index = courseDropdown != null ? courseDropdown.value : 0;

        if (index < 0 || index >= courses.Count)
            return;

        string selectedCourseId = courses[index].courseId;

        int count = 0;

        foreach (AssignedStudentData student in assignedStudents)
        {
            if (student.courseId != selectedCourseId)
                continue;

            GameObject rowObject = Instantiate(performanceStudentRowPrefab, studentsContainer);
            PerformanceStudentRowUI rowUI = rowObject.GetComponent<PerformanceStudentRowUI>();

            if (rowUI != null)
                rowUI.Setup(student, this);

            count++;
        }

    }

    private List<AssignedStudentData> GetStudentsForSelectedCourse()
    {
        List<AssignedStudentData> students = new List<AssignedStudentData>();

        TeacherCourseData selectedCourse = GetSelectedCourse();

        if (selectedCourse == null)
            return students;

        foreach (AssignedStudentData student in assignedStudents)
        {
            if (student == null)
                continue;

            if (student.courseId == selectedCourse.courseId)
                students.Add(student);
        }

        return students;
    }

    private List<TeacherCourseData> GetCoursesForSelectedSubject()
    {
        List<TeacherCourseData> subjectCourses = new List<TeacherCourseData>();

        TeacherCourseData selectedCourse = GetSelectedCourse();

        if (selectedCourse == null)
            return subjectCourses;

        string selectedSubjectCode = GetSubjectCode(selectedCourse);

        foreach (TeacherCourseData course in courses)
        {
            if (course == null)
                continue;

            if (GetSubjectCode(course) == selectedSubjectCode)
                subjectCourses.Add(course);
        }

        return subjectCourses;
    }

    private List<AssignedStudentData> GetStudentsForCourse(TeacherCourseData course)
    {
        List<AssignedStudentData> students = new List<AssignedStudentData>();

        if (course == null)
            return students;

        foreach (AssignedStudentData student in assignedStudents)
        {
            if (student == null)
                continue;

            if (student.courseId == course.courseId)
                students.Add(student);
        }

        return students;
    }

    public void LoadStudentPerformance(AssignedStudentData student)
    {
        if (student == null)
            return;

        if (courseSummaryCoroutine != null)
        {
            StopCoroutine(courseSummaryCoroutine);
            courseSummaryCoroutine = null;
        }

        if (subjectSummaryCoroutine != null)
        {
            StopCoroutine(subjectSummaryCoroutine);
            subjectSummaryCoroutine = null;
        }

        isViewingCourseSummary = false;
        isViewingSubjectSummary = false;
        currentSubjectResults.Clear();
        currentSubjectCourseSummaries.Clear();
        currentSubjectCurrentDay = 1;
        currentCourseResults.Clear();
        currentCourseStudentSummaries.Clear();
        currentCourseCurrentDay = 1;

        currentSelectedStudent = student;
        SetPdfDownloadState(false, "Loading results...");
        StartCoroutine(GetStudentPerformanceRequest(student));
    }

    public void LoadSelectedCourseSummary()
    {
        if (subjectSummaryCoroutine != null)
        {
            StopCoroutine(subjectSummaryCoroutine);
            subjectSummaryCoroutine = null;
        }

        if (courseSummaryCoroutine != null)
            StopCoroutine(courseSummaryCoroutine);

        isViewingSubjectSummary = false;
        isViewingCourseSummary = true;
        SetPdfDownloadState(false, "Loading results...");
        currentSubjectResults.Clear();
        currentSubjectCourseSummaries.Clear();
        currentSubjectCurrentDay = 1;

        courseSummaryCoroutine = StartCoroutine(GetSelectedCourseSummaryRequest());
    }

    public void LoadSelectedSubjectSummary()
    {
        if (courseSummaryCoroutine != null)
        {
            StopCoroutine(courseSummaryCoroutine);
            courseSummaryCoroutine = null;
        }

        if (subjectSummaryCoroutine != null)
            StopCoroutine(subjectSummaryCoroutine);

        isViewingCourseSummary = false;
        isViewingSubjectSummary = true;

        SetPdfDownloadState(false, "Loading results...");

        currentCourseResults.Clear();
        currentCourseStudentSummaries.Clear();
        currentCourseCurrentDay = 1;

        subjectSummaryCoroutine = StartCoroutine(GetSelectedSubjectSummaryRequest());
    }

    private IEnumerator GetSelectedSubjectSummaryRequest()
    {
        TeacherCourseData selectedCourse = GetSelectedCourse();

        if (selectedCourse == null)
            yield break;

        if (string.IsNullOrWhiteSpace(getStudentPerformanceUrl))
        {
            Debug.LogError("Get Student Performance URL is empty. Assign it in the Inspector.");

            if (selectedStudentText != null)
                selectedStudentText.text = GetCourseName(selectedCourse) + " subject comparison";

            if (generalPerformanceText != null)
                generalPerformanceText.text = "URL missing";

            if (totalRevenueText != null)
                totalRevenueText.text = "-";

            if (totalErrorsText != null)
                totalErrorsText.text = "-";

            if (activitiesText != null)
                activitiesText.text = "-";

            ClearResultRows();
            ClearCharts();

            yield break;
        }

        List<TeacherCourseData> subjectCourses = GetCoursesForSelectedSubject();

        isViewingSubjectSummary = true;
        isViewingCourseSummary = false;
        currentSelectedStudent = null;

        currentStudentResults.Clear();
        currentStudentCurrentDay = 1;

        currentCourseResults.Clear();
        currentCourseStudentSummaries.Clear();
        currentCourseCurrentDay = 1;

        currentSubjectResults.Clear();
        currentSubjectCourseSummaries.Clear();
        currentSubjectCurrentDay = 1;

        string subjectName = GetCourseName(selectedCourse);
        string subjectCode = GetSubjectCode(selectedCourse);

        if (selectedStudentText != null)
            selectedStudentText.text = subjectName + "Comparison";

        if (generalPerformanceText != null)
            generalPerformanceText.text = "Loading...";

        if (totalRevenueText != null)
            totalRevenueText.text = "-";

        if (totalErrorsText != null)
            totalErrorsText.text = "-";

        if (activitiesText != null)
            activitiesText.text = "-";

        ClearResultRows();
        ClearCharts();

        if (subjectCourses.Count == 0)
        {
            if (generalPerformanceText != null)
                generalPerformanceText.text = "0%";

            if (totalRevenueText != null)
                totalRevenueText.text = "$0";

            if (totalErrorsText != null)
                totalErrorsText.text = "0";

            if (activitiesText != null)
                activitiesText.text = "0";

            PopulateDayDropdown(currentSubjectResults);
            subjectSummaryCoroutine = null;

            yield break;
        }

        foreach (TeacherCourseData course in subjectCourses)
        {
            SubjectCoursePerformanceData courseSummary = new SubjectCoursePerformanceData
            {
                course = course,
                displayName = GetClassCode(course),
                currentDay = 1,
                studentSummaries = new List<CourseStudentPerformanceData>(),
                relatedResults = new List<CloudDailyResultData>()
            };

            currentSubjectCourseSummaries.Add(courseSummary);

            List<AssignedStudentData> courseStudents = GetStudentsForCourse(course);

            foreach (AssignedStudentData student in courseStudents)
            {
                CourseStudentPerformanceData studentSummary = new CourseStudentPerformanceData
                {
                    student = student,
                    currentDay = 1,
                    relatedResults = new List<CloudDailyResultData>()
                };

                courseSummary.studentSummaries.Add(studentSummary);

                if (student == null || string.IsNullOrWhiteSpace(student.playFabId))
                    continue;

                StudentPerformanceRequestData data = new StudentPerformanceRequestData
                {
                    studentPlayFabId = student.playFabId
                };

                string json = JsonUtility.ToJson(data);

                UnityWebRequest request = CreateJsonPostRequest(getStudentPerformanceUrl, json);
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning("Subject comparison skipped student " + student.displayName + ": " + request.error);
                    continue;
                }

                StudentPerformanceResponseData response =
                    JsonUtility.FromJson<StudentPerformanceResponseData>(request.downloadHandler.text);

                if (response == null || !response.success)
                    continue;

                studentSummary.currentDay = Mathf.Max(1, response.currentDay);
                courseSummary.currentDay = Mathf.Max(courseSummary.currentDay, studentSummary.currentDay);
                currentSubjectCurrentDay = Mathf.Max(currentSubjectCurrentDay, studentSummary.currentDay);

                List<CloudDailyResultData> rawResults = new List<CloudDailyResultData>();

                if (response.results != null)
                    rawResults.AddRange(response.results);

                List<CloudDailyResultData> relatedResults = GetResultsRelatedToSelectedCourse(rawResults);

                studentSummary.relatedResults.AddRange(relatedResults);
                courseSummary.relatedResults.AddRange(relatedResults);
                currentSubjectResults.AddRange(relatedResults);
            }
        }

        currentSubjectResults.Sort((a, b) =>
        {
            int dayCompare = a.day.CompareTo(b.day);

            if (dayCompare != 0)
                return dayCompare;

            return string.Compare(a.minigameName, b.minigameName);
        });

        PopulateDayDropdown(currentSubjectResults);
        UpdateReportWithSelectedDay();

        if (currentSubjectResults.Count > 0)
            SetPdfDownloadState(true);
        else
            SetPdfDownloadState(false, "No comparison data to download");

        subjectSummaryCoroutine = null;
    }

    private IEnumerator GetSelectedCourseSummaryRequest()
    {
        TeacherCourseData selectedCourse = GetSelectedCourse();

        if (selectedCourse == null)
            yield break;

        if (string.IsNullOrWhiteSpace(getStudentPerformanceUrl))
        {
            Debug.LogError("Get Student Performance URL is empty. Assign it in the Inspector.");

            if (selectedStudentText != null)
                selectedStudentText.text = GetCourseName(selectedCourse) + " course summary";

            if (generalPerformanceText != null)
                generalPerformanceText.text = "URL missing";

            if (totalRevenueText != null)
                totalRevenueText.text = "-";

            if (totalErrorsText != null)
                totalErrorsText.text = "-";

            if (activitiesText != null)
                activitiesText.text = "-";

            ClearResultRows();
            ClearCharts();

            yield break;
        }

        List<AssignedStudentData> courseStudents = GetStudentsForSelectedCourse();

        isViewingSubjectSummary = false;
        isViewingCourseSummary = true;
        currentSelectedStudent = null;

        currentStudentResults.Clear();
        currentStudentCurrentDay = 1;

        currentCourseResults.Clear();
        currentCourseStudentSummaries.Clear();
        currentCourseCurrentDay = 1;

        if (selectedStudentText != null)
            selectedStudentText.text = GetCourseName(selectedCourse) + " course summary";

        if (generalPerformanceText != null)
            generalPerformanceText.text = "Loading...";

        if (totalRevenueText != null)
            totalRevenueText.text = "-";

        if (totalErrorsText != null)
            totalErrorsText.text = "-";

        if (activitiesText != null)
            activitiesText.text = "-";

        ClearResultRows();
        ClearCharts();

        if (courseStudents.Count == 0)
        {
            if (generalPerformanceText != null)
                generalPerformanceText.text = "0%";

            if (totalRevenueText != null)
                totalRevenueText.text = "$0";

            if (totalErrorsText != null)
                totalErrorsText.text = "0";

            if (activitiesText != null)
                activitiesText.text = "0";

            PopulateDayDropdown(currentCourseResults);
            courseSummaryCoroutine = null;

            yield break;
        }

        foreach (AssignedStudentData student in courseStudents)
        {
            CourseStudentPerformanceData studentSummary = new CourseStudentPerformanceData
            {
                student = student,
                currentDay = 1,
                relatedResults = new List<CloudDailyResultData>()
            };

            currentCourseStudentSummaries.Add(studentSummary);

            if (student == null || string.IsNullOrWhiteSpace(student.playFabId))
                continue;

            StudentPerformanceRequestData data = new StudentPerformanceRequestData
            {
                studentPlayFabId = student.playFabId
            };

            string json = JsonUtility.ToJson(data);

            UnityWebRequest request = CreateJsonPostRequest(getStudentPerformanceUrl, json);
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("Course summary skipped student " + student.displayName + ": " + request.error);
                continue;
            }

            StudentPerformanceResponseData response =
                JsonUtility.FromJson<StudentPerformanceResponseData>(request.downloadHandler.text);

            if (response == null || !response.success)
                continue;

            studentSummary.currentDay = Mathf.Max(1, response.currentDay);
            currentCourseCurrentDay = Mathf.Max(currentCourseCurrentDay, studentSummary.currentDay);

            List<CloudDailyResultData> rawResults = new List<CloudDailyResultData>();

            if (response.results != null)
                rawResults.AddRange(response.results);

            List<CloudDailyResultData> relatedResults = GetResultsRelatedToSelectedCourse(rawResults);

            studentSummary.relatedResults.AddRange(relatedResults);
            currentCourseResults.AddRange(relatedResults);
        }

        currentCourseResults.Sort((a, b) =>
        {
            int dayCompare = a.day.CompareTo(b.day);

            if (dayCompare != 0)
                return dayCompare;

            return string.Compare(a.minigameName, b.minigameName);
        });

        PopulateDayDropdown(currentCourseResults);
        UpdateReportWithSelectedDay();

        if (currentCourseResults.Count > 0)
            SetPdfDownloadState(true);
        else
            SetPdfDownloadState(false, "No course data to download");

        courseSummaryCoroutine = null;
    }

    private IEnumerator GetStudentPerformanceRequest(AssignedStudentData student)
    {
        if (student == null)
            yield break;

        if (string.IsNullOrWhiteSpace(getStudentPerformanceUrl))
        {
            Debug.LogError("Get Student Performance URL is empty. Assign it in the Inspector.");

            if (selectedStudentText != null)
                selectedStudentText.text = student.displayName + " results";

            if (generalPerformanceText != null)
                generalPerformanceText.text = "URL missing";

            if (totalRevenueText != null)
                totalRevenueText.text = "-";

            if (totalErrorsText != null)
                totalErrorsText.text = "-";

            if (activitiesText != null)
                activitiesText.text = "-";

            ClearResultRows();
            ClearCharts();

            yield break;
        }

        StudentPerformanceRequestData data = new StudentPerformanceRequestData
        {
            studentPlayFabId = student.playFabId
        };

        string json = JsonUtility.ToJson(data);

        UnityWebRequest request = CreateJsonPostRequest(getStudentPerformanceUrl, json);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Student performance request failed: " + request.error);
            Debug.LogError("Backend response: " + request.downloadHandler.text);

            if (selectedStudentText != null)
                selectedStudentText.text = student.displayName + " results";

            if (generalPerformanceText != null)
                generalPerformanceText.text = "Request failed";

            if (totalRevenueText != null)
                totalRevenueText.text = "-";

            if (totalErrorsText != null)
                totalErrorsText.text = "-";

            if (activitiesText != null)
                activitiesText.text = "-";

            ClearResultRows();
            ClearCharts();

            SetPdfDownloadState(false, "Could not load results");
            yield break;
        }

        StudentPerformanceResponseData response =
            JsonUtility.FromJson<StudentPerformanceResponseData>(request.downloadHandler.text);

        if (response == null)
        {
            Debug.LogError("Could not parse student performance response.");

            if (generalPerformanceText != null)
                generalPerformanceText.text = "Invalid response";

            ClearResultRows();
            ClearCharts();

            yield break;
        }

        if (!response.success)
        {
            Debug.LogError("Student performance response failed: " + response.message);

            if (selectedStudentText != null)
                selectedStudentText.text = student.displayName + " results";

            if (generalPerformanceText != null)
                generalPerformanceText.text = "No data";

            if (totalRevenueText != null)
                totalRevenueText.text = "$0";

            if (totalErrorsText != null)
                totalErrorsText.text = "0";

            if (activitiesText != null)
                activitiesText.text = "0";

            ClearResultRows();
            ClearCharts();

            yield break;
        }

        currentStudentCurrentDay = Mathf.Max(1, response.currentDay);

        currentStudentResults.Clear();

        if (response.results != null)
            currentStudentResults.AddRange(response.results);

        currentStudentResults.Sort((a, b) =>
        {
            int dayCompare = a.day.CompareTo(b.day);

            if (dayCompare != 0)
                return dayCompare;

            return string.Compare(a.minigameName, b.minigameName);
        });

        List<CloudDailyResultData> relatedResults = GetResultsRelatedToSelectedCourse();

        PopulateDayDropdown(relatedResults);
        UpdateReportWithSelectedDay();

        if (relatedResults.Count > 0)
            SetPdfDownloadState(true);
        else
            SetPdfDownloadState(false, "No data to download");
    }

    private void PopulateDayDropdown(List<CloudDailyResultData> sourceResults)
    {
        if (dayDropdown == null)
            return;

        dayDropdown.onValueChanged.RemoveAllListeners();
        dayDropdown.ClearOptions();

        List<string> options = new List<string>();
        options.Add("All Days");

        List<int> days = GetVisibleDays(sourceResults);

        foreach (int day in days)
            options.Add("Day " + day);

        dayDropdown.AddOptions(options);
        dayDropdown.value = 0;
        dayDropdown.RefreshShownValue();

        dayDropdown.onValueChanged.AddListener(delegate
        {
            UpdateReportWithSelectedDay();
        });
    }

    private void UpdateReportWithSelectedDay()
    {
        if (isViewingSubjectSummary)
        {
            UpdateSubjectSummaryReport();
            return;
        }

        if (isViewingCourseSummary)
        {
            List<CloudDailyResultData> filteredCourseResults = GetFilteredCourseResultsBySelectedDay();
            UpdateCourseSummaryReport(filteredCourseResults);
            return;
        }

        if (currentSelectedStudent == null)
        {
            ClearReport();
            return;
        }

        List<CloudDailyResultData> filteredResults = GetFilteredResultsBySelectedDay();

        UpdateReport(currentSelectedStudent, filteredResults);
    }

    private List<CloudDailyResultData> GetFilteredResultsBySelectedDay()
    {
        List<CloudDailyResultData> relatedResults = GetResultsRelatedToSelectedCourse();

        if (dayDropdown == null || dayDropdown.value == 0)
            return relatedResults;

        List<CloudDailyResultData> filtered = new List<CloudDailyResultData>();

        string option = dayDropdown.options[dayDropdown.value].text;
        int selectedDay = ExtractDayNumber(option);

        foreach (CloudDailyResultData result in relatedResults)
        {
            if (result.day == selectedDay)
                filtered.Add(result);
        }

        return filtered;
    }

    private List<CloudDailyResultData> GetFilteredCourseResultsBySelectedDay()
    {
        if (dayDropdown == null || dayDropdown.value == 0)
            return new List<CloudDailyResultData>(currentCourseResults);

        List<CloudDailyResultData> filtered = new List<CloudDailyResultData>();

        string option = dayDropdown.options[dayDropdown.value].text;
        int selectedDay = ExtractDayNumber(option);

        foreach (CloudDailyResultData result in currentCourseResults)
        {
            if (result != null && result.day == selectedDay)
                filtered.Add(result);
        }

        return filtered;
    }

    private int ExtractDayNumber(string option)
    {
        if (string.IsNullOrEmpty(option))
            return 0;

        option = option.Replace("Day", "").Trim();

        int day;
        if (int.TryParse(option, out day))
            return day;

        return 0;
    }

    private List<int> GetVisibleDays(List<CloudDailyResultData> sourceResults)
    {
        int activeCurrentDay = 1;

        if (isViewingSubjectSummary)
            activeCurrentDay = Mathf.Max(1, currentSubjectCurrentDay);
        else if (isViewingCourseSummary)
            activeCurrentDay = Mathf.Max(1, currentCourseCurrentDay);
        else
            activeCurrentDay = Mathf.Max(1, currentStudentCurrentDay);

        int lastDay = activeCurrentDay;

        if (sourceResults != null)
        {
            foreach (CloudDailyResultData result in sourceResults)
            {
                if (result == null)
                    continue;

                lastDay = Mathf.Max(lastDay, result.day);
            }
        }

        List<int> days = new List<int>();

        for (int day = 1; day <= lastDay; day++)
            days.Add(day);

        return days;
    }

    private bool IsViewingAllDays()
    {
        return dayDropdown == null || dayDropdown.value == 0;
    }

    private List<int> GetDaysForCurrentChartView(List<CloudDailyResultData> results)
    {
        if (!IsViewingAllDays() && dayDropdown != null && dayDropdown.value < dayDropdown.options.Count)
        {
            int selectedDay = ExtractDayNumber(dayDropdown.options[dayDropdown.value].text);

            if (selectedDay > 0)
                return new List<int> { selectedDay };
        }

        if (isViewingSubjectSummary)
            return GetVisibleDays(currentSubjectResults);

        if (isViewingCourseSummary)
            return GetVisibleDays(currentCourseResults);

        return GetVisibleDays(GetResultsRelatedToSelectedCourse());
    }

    private int GetAveragePerformanceIncludingEmptyDays(List<CloudDailyResultData> results)
    {
        Dictionary<int, DayPerformanceData> dayData = isViewingCourseSummary
            ? BuildCourseDayPerformance()
            : BuildDayPerformance(results);

        List<int> visibleDays = GetDaysForCurrentChartView(results);

        if (visibleDays.Count == 0)
            return 0;

        int totalScore = 0;

        foreach (int day in visibleDays)
        {
            if (dayData.ContainsKey(day))
                totalScore += dayData[day].AverageScore;
            else
                totalScore += 0;
        }

        return Mathf.RoundToInt(totalScore / (float)visibleDays.Count);
    }

    private void UpdateReport(AssignedStudentData student, List<CloudDailyResultData> results)
    {
        ClearResultRows();
        UpdateKpiLabelsForSelectedCourse();

        if (selectedStudentText != null)
            selectedStudentText.text = student.displayName + " results";

        if (results == null)
            results = new List<CloudDailyResultData>();

        int totalRevenue = 0;
        int totalErrors = 0;

        foreach (CloudDailyResultData result in results)
        {
            if (result == null)
                continue;

            int subjectErrors = GetSubjectErrorsForResult(result);

            totalRevenue += result.revenue;
            totalErrors += subjectErrors;

            GameObject rowObject = Instantiate(performanceResultRowPrefab, resultsContainer);
            PerformanceResultRowUI rowUI = rowObject.GetComponent<PerformanceResultRowUI>();

            if (rowUI != null)
                rowUI.Setup(result);
        }

        int averageScore = GetAveragePerformanceIncludingEmptyDays(results);

        if (generalPerformanceText != null)
            generalPerformanceText.text = averageScore + "%";

        if (totalRevenueText != null)
            totalRevenueText.text = "$" + totalRevenue;

        if (totalErrorsText != null)
            totalErrorsText.text = totalErrors.ToString();

        if (activitiesText != null)
            activitiesText.text = results.Count.ToString();

        RefreshDownloadReportButtonLabel();
        UpdateCharts(results);
    }

    private void UpdateCourseSummaryReport(List<CloudDailyResultData> results)
    {
        ClearResultRows();
        UpdateKpiLabelsForSelectedCourse();

        TeacherCourseData selectedCourse = GetSelectedCourse();

        string courseName = selectedCourse != null ? GetCourseName(selectedCourse) : "Course";

        if (selectedStudentText != null)
        {
            selectedStudentText.text =
                courseName + " course summary (" + currentCourseStudentSummaries.Count + " students)";
        }

        if (results == null)
            results = new List<CloudDailyResultData>();

        int totalRevenue = 0;
        int totalErrors = 0;

        foreach (CloudDailyResultData result in results)
        {
            if (result == null)
                continue;

            totalRevenue += result.revenue;
            totalErrors += GetSubjectErrorsForResult(result);

            GameObject rowObject = Instantiate(performanceResultRowPrefab, resultsContainer);
            PerformanceResultRowUI rowUI = rowObject.GetComponent<PerformanceResultRowUI>();

            if (rowUI != null)
                rowUI.Setup(result);
        }

        int averageScore = GetAveragePerformanceIncludingEmptyDays(results);

        if (generalPerformanceText != null)
            generalPerformanceText.text = averageScore + "%";

        if (totalRevenueText != null)
            totalRevenueText.text = "$" + totalRevenue;

        if (totalErrorsText != null)
            totalErrorsText.text = totalErrors.ToString();

        if (activitiesText != null)
            activitiesText.text = results.Count.ToString();

        RefreshDownloadReportButtonLabel();
        UpdateCharts(results);
    }

    private void UpdateSubjectSummaryReport()
    {
        ClearResultRows();

        TeacherCourseData selectedCourse = GetSelectedCourse();

        string comparedCodes = GetComparedCourseCodesText();

        List<int> days = GetDaysForCurrentChartView(currentSubjectResults);

        int totalCourseScores = 0;
        int validCourseCount = 0;

        int bestScore = -1;
        string bestScoreCourse = "-";

        int bestRevenue = -1;
        string bestRevenueCourse = "-";

        int lowestErrors = int.MaxValue;
        string lowestErrorsCourse = "-";
        bool foundErrorsCandidate = false;

        int bestActivities = -1;
        string bestActivitiesCourse = "-";

        foreach (SubjectCoursePerformanceData courseSummary in currentSubjectCourseSummaries)
        {
            if (courseSummary == null)
                continue;

            string courseCode = GetCourseComparisonCode(courseSummary);

            int courseScore = CalculateCourseAverageScore(courseSummary, days);
            int courseRevenue = CalculateCourseRevenue(courseSummary, days);
            int courseErrors = CalculateCourseErrors(courseSummary, days);
            int courseActivities = CalculateCourseActivities(courseSummary, days);

            totalCourseScores += courseScore;
            validCourseCount++;

            if (courseScore > bestScore)
            {
                bestScore = courseScore;
                bestScoreCourse = courseCode;
            }

            if (courseRevenue > bestRevenue)
            {
                bestRevenue = courseRevenue;
                bestRevenueCourse = courseCode;
            }

            if (courseActivities > 0 && courseErrors < lowestErrors)
            {
                lowestErrors = courseErrors;
                lowestErrorsCourse = courseCode;
                foundErrorsCandidate = true;
            }

            if (courseActivities > bestActivities)
            {
                bestActivities = courseActivities;
                bestActivitiesCourse = courseCode;
            }
        }

        if (!foundErrorsCandidate)
        {
            lowestErrors = 0;
            lowestErrorsCourse = bestScoreCourse;
        }

        if (bestRevenue < 0)
        {
            bestRevenue = 0;
            bestRevenueCourse = bestScoreCourse;
        }

        if (bestActivities < 0)
        {
            bestActivities = 0;
            bestActivitiesCourse = bestScoreCourse;
        }

        int averageScore = validCourseCount > 0
            ? Mathf.RoundToInt(totalCourseScores / (float)validCourseCount)
            : 0;

        if (selectedStudentText != null)
        {
            selectedStudentText.text =
                comparedCodes + " - Best: " + bestScoreCourse + " (" + Mathf.Max(0, bestScore) + "%)";
        }

        if (performanceLabelText != null)
            performanceLabelText.text = "Best Score";

        if (revenueLabelText != null)
            revenueLabelText.text = "Best Revenue";

        if (errorsLabelText != null)
            errorsLabelText.text = "Least Errors";

        if (activitiesLabelText != null)
            activitiesLabelText.text = "Most Tasks";

        if (generalPerformanceText != null)
            generalPerformanceText.text = bestScoreCourse + "\n" + Mathf.Max(0, bestScore) + "%";

        if (totalRevenueText != null)
            totalRevenueText.text = bestRevenueCourse + "\n$" + bestRevenue;

        if (totalErrorsText != null)
            totalErrorsText.text = lowestErrorsCourse + "\n" + lowestErrors;

        if (activitiesText != null)
            activitiesText.text = bestActivitiesCourse + "\n" + bestActivities;

        RefreshDownloadReportButtonLabel();
        UpdateCharts(currentSubjectResults);
    }

    private string GetComparedCourseCodesText()
    {
        List<string> codes = new List<string>();

        foreach (SubjectCoursePerformanceData courseSummary in currentSubjectCourseSummaries)
        {
            if (courseSummary == null)
                continue;

            string code = GetCourseComparisonCode(courseSummary);

            if (!string.IsNullOrWhiteSpace(code) && !codes.Contains(code))
                codes.Add(code);
        }

        if (codes.Count == 0)
            return "No courses";

        if (codes.Count <= 3)
            return string.Join(" / ", codes.ToArray());

        return codes[0] + " / " + codes[1] + " / " + codes[2] + " +" + (codes.Count - 3);
    }

    private string GetCourseComparisonCode(SubjectCoursePerformanceData courseSummary)
    {
        if (courseSummary == null)
            return "-";

        if (!string.IsNullOrWhiteSpace(courseSummary.displayName))
            return courseSummary.displayName;

        return GetClassCode(courseSummary.course);
    }

    private void UpdateCharts(List<CloudDailyResultData> results)
    {
        if (isViewingSubjectSummary)
        {
            UpdateSubjectSummaryCharts();
            return;
        }

        UpdatePerformanceLineChart(results);
        UpdateActivityScoreBarChart(results);
        UpdateRevenueBarChart(results);
        UpdateErrorsBarChart(results);
    }

    private void UpdatePerformanceLineChart(List<CloudDailyResultData> results)
    {
        if (performanceLineChart == null)
            return;

        ClearChartSeries(performanceLineChart);

        string courseName = GetCourseName(GetSelectedCourse());

        SetChartTitle(performanceLineChart, courseName + " - Performance Trend (%)");
        SetLegend(performanceLineChart, false);

        performanceLineChart.AddSerie<Line>("Performance");

        Dictionary<int, DayPerformanceData> dayData = isViewingCourseSummary
            ? BuildCourseDayPerformance()
            : BuildDayPerformance(results); List<int> days = GetDaysForCurrentChartView(results);

        foreach (int day in days)
        {
            int score = dayData.ContainsKey(day) ? dayData[day].AverageScore : 0;

            performanceLineChart.AddXAxisData("Day " + day);
            performanceLineChart.AddData(0, score);
        }

        performanceLineChart.RefreshChart();
    }

    private void UpdateActivityScoreBarChart(List<CloudDailyResultData> results)
    {
        if (activityScoreBarChart == null)
            return;

        ClearChartSeries(activityScoreBarChart);

        string courseName = GetCourseName(GetSelectedCourse());

        SetChartTitle(activityScoreBarChart, courseName + " - Score by Related Activity (%)");
        SetLegend(activityScoreBarChart, false);

        activityScoreBarChart.AddSerie<Bar>("Score");

        Dictionary<string, ActivityPerformanceData> activityData = BuildActivityPerformance(results);

        foreach (KeyValuePair<string, ActivityPerformanceData> pair in activityData)
        {
            activityScoreBarChart.AddXAxisData(pair.Key);
            activityScoreBarChart.AddData(0, pair.Value.AverageScore);
        }

        activityScoreBarChart.RefreshChart();
    }

    private void UpdateRevenueBarChart(List<CloudDailyResultData> results)
    {
        if (revenueBarChart == null)
            return;

        ClearChartSeries(revenueBarChart);

        string courseName = GetCourseName(GetSelectedCourse());

        SetChartTitle(revenueBarChart, courseName + " - Revenue by Day");
        SetLegend(revenueBarChart, false);

        revenueBarChart.AddSerie<Bar>("Revenue");

        Dictionary<int, DayPerformanceData> dayData = isViewingCourseSummary
            ? BuildCourseDayPerformance()
            : BuildDayPerformance(results); List<int> days = GetDaysForCurrentChartView(results);

        foreach (int day in days)
        {
            int revenue = dayData.ContainsKey(day) ? dayData[day].totalRevenue : 0;

            revenueBarChart.AddXAxisData("Day " + day);
            revenueBarChart.AddData(0, revenue);
        }

        revenueBarChart.RefreshChart();
    }

    private void UpdateErrorsBarChart(List<CloudDailyResultData> results)
    {
        if (errorsBarChart == null)
            return;

        ClearChartSeries(errorsBarChart);

        string courseName = GetCourseName(GetSelectedCourse());

        SetChartTitle(errorsBarChart, courseName + " - Errors by Day");
        SetLegend(errorsBarChart, false);

        errorsBarChart.AddSerie<Bar>("Errors");

        Dictionary<int, DayPerformanceData> dayData = isViewingCourseSummary
            ? BuildCourseDayPerformance()
            : BuildDayPerformance(results); List<int> days = GetDaysForCurrentChartView(results);

        foreach (int day in days)
        {
            int errors = dayData.ContainsKey(day) ? dayData[day].totalErrors : 0;

            errorsBarChart.AddXAxisData("Day " + day);
            errorsBarChart.AddData(0, errors);
        }

        errorsBarChart.RefreshChart();
    }

    private void UpdateSubjectSummaryCharts()
    {
        List<int> days = GetDaysForCurrentChartView(currentSubjectResults);

        UpdateSubjectPerformanceLineChart(days);
        UpdateSubjectCourseScoreBarChart(days);
        UpdateSubjectCourseRevenueBarChart(days);
        UpdateSubjectCourseErrorsBarChart(days);
    }

    private void UpdateSubjectPerformanceLineChart(List<int> days)
    {
        if (performanceLineChart == null)
            return;

        ClearChartSeries(performanceLineChart);

        TeacherCourseData selectedCourse = GetSelectedCourse();
        string subjectCode = selectedCourse != null ? GetSubjectCode(selectedCourse) : "Subject";

        SetChartTitle(performanceLineChart, subjectCode + " - Score Trend by Course");
        SetLegend(performanceLineChart, true);

        for (int i = 0; i < currentSubjectCourseSummaries.Count; i++)
        {
            SubjectCoursePerformanceData courseSummary = currentSubjectCourseSummaries[i];

            string serieName = GetCourseComparisonCode(courseSummary);
            performanceLineChart.AddSerie<Line>(serieName);
        }

        foreach (int day in days)
        {
            performanceLineChart.AddXAxisData("Day " + day);

            for (int i = 0; i < currentSubjectCourseSummaries.Count; i++)
            {
                int score = CalculateCourseAverageScore(
                    currentSubjectCourseSummaries[i],
                    new List<int> { day }
                );

                performanceLineChart.AddData(i, score);
            }
        }

        performanceLineChart.RefreshChart();
    }

    private void UpdateSubjectCourseScoreBarChart(List<int> days)
    {
        if (activityScoreBarChart == null)
            return;

        ClearChartSeries(activityScoreBarChart);

        TeacherCourseData selectedCourse = GetSelectedCourse();
        string subjectCode = selectedCourse != null ? GetSubjectCode(selectedCourse) : "Subject";

        SetChartTitle(activityScoreBarChart, subjectCode + " - Average Score by Course");
        SetLegend(activityScoreBarChart, false);

        activityScoreBarChart.AddSerie<Bar>("Score");

        foreach (SubjectCoursePerformanceData courseSummary in currentSubjectCourseSummaries)
        {
            if (courseSummary == null)
                continue;

            string courseCode = GetCourseComparisonCode(courseSummary);
            int score = CalculateCourseAverageScore(courseSummary, days);

            activityScoreBarChart.AddXAxisData(courseCode);
            activityScoreBarChart.AddData(0, score);
        }

        activityScoreBarChart.RefreshChart();
    }

    private void UpdateSubjectCourseRevenueBarChart(List<int> days)
    {
        if (revenueBarChart == null)
            return;

        ClearChartSeries(revenueBarChart);

        TeacherCourseData selectedCourse = GetSelectedCourse();
        string subjectCode = selectedCourse != null ? GetSubjectCode(selectedCourse) : "Subject";

        SetChartTitle(revenueBarChart, subjectCode + " - Revenue by Course");
        SetLegend(revenueBarChart, false);

        revenueBarChart.AddSerie<Bar>("Revenue");

        foreach (SubjectCoursePerformanceData courseSummary in currentSubjectCourseSummaries)
        {
            if (courseSummary == null)
                continue;

            string courseCode = GetCourseComparisonCode(courseSummary);
            int revenue = CalculateCourseRevenue(courseSummary, days);

            revenueBarChart.AddXAxisData(courseCode);
            revenueBarChart.AddData(0, revenue);
        }

        revenueBarChart.RefreshChart();
    }

    private void UpdateSubjectCourseErrorsBarChart(List<int> days)
    {
        if (errorsBarChart == null)
            return;

        ClearChartSeries(errorsBarChart);

        TeacherCourseData selectedCourse = GetSelectedCourse();
        string subjectCode = selectedCourse != null ? GetSubjectCode(selectedCourse) : "Subject";

        SetChartTitle(errorsBarChart, subjectCode + " - Errors by Course");
        SetLegend(errorsBarChart, false);

        errorsBarChart.AddSerie<Bar>("Errors");

        foreach (SubjectCoursePerformanceData courseSummary in currentSubjectCourseSummaries)
        {
            if (courseSummary == null)
                continue;

            string courseCode = GetCourseComparisonCode(courseSummary);
            int errors = CalculateCourseErrors(courseSummary, days);

            errorsBarChart.AddXAxisData(courseCode);
            errorsBarChart.AddData(0, errors);
        }

        errorsBarChart.RefreshChart();
    }

    private Dictionary<int, DayPerformanceData> BuildDayPerformance(List<CloudDailyResultData> results)
    {
        Dictionary<int, DayPerformanceData> data = new Dictionary<int, DayPerformanceData>();

        if (results == null)
            return data;

        foreach (CloudDailyResultData result in results)
        {
            if (result == null)
                continue;

            if (!data.ContainsKey(result.day))
            {
                data[result.day] = new DayPerformanceData
                {
                    day = result.day,
                    count = 0,
                    totalScore = 0,
                    totalRevenue = 0,
                    totalErrors = 0
                };
            }

            data[result.day].count++;
            data[result.day].totalScore += GetSubjectScoreForResult(result);
            data[result.day].totalRevenue += result.revenue;
            data[result.day].totalErrors += GetSubjectErrorsForResult(result);
        }

        return data;
    }

    private Dictionary<int, DayPerformanceData> BuildCourseDayPerformance()
    {
        Dictionary<int, DayPerformanceData> data = new Dictionary<int, DayPerformanceData>();

        List<int> days = GetVisibleDays(currentCourseResults);

        foreach (int day in days)
        {
            DayPerformanceData dayPerformance = new DayPerformanceData
            {
                day = day,
                count = 0,
                totalScore = 0,
                totalRevenue = 0,
                totalErrors = 0
            };

            foreach (CourseStudentPerformanceData studentSummary in currentCourseStudentSummaries)
            {
                if (studentSummary == null)
                    continue;

                List<CloudDailyResultData> studentDayResults = new List<CloudDailyResultData>();

                foreach (CloudDailyResultData result in studentSummary.relatedResults)
                {
                    if (result != null && result.day == day)
                        studentDayResults.Add(result);
                }

                int studentDayScore = 0;

                if (studentDayResults.Count > 0)
                {
                    int studentTotalScore = 0;

                    foreach (CloudDailyResultData result in studentDayResults)
                    {
                        studentTotalScore += GetSubjectScoreForResult(result);
                        dayPerformance.totalRevenue += result.revenue;
                        dayPerformance.totalErrors += GetSubjectErrorsForResult(result);
                    }

                    studentDayScore = Mathf.RoundToInt(studentTotalScore / (float)studentDayResults.Count);
                }

                dayPerformance.totalScore += studentDayScore;
                dayPerformance.count++;
            }

            data[day] = dayPerformance;
        }

        return data;
    }

    private int CalculateCourseAverageScore(SubjectCoursePerformanceData courseSummary, List<int> days)
    {
        if (courseSummary == null || courseSummary.studentSummaries == null || courseSummary.studentSummaries.Count == 0)
            return 0;

        if (days == null || days.Count == 0)
            return 0;

        int totalScore = 0;
        int count = 0;

        foreach (int day in days)
        {
            foreach (CourseStudentPerformanceData studentSummary in courseSummary.studentSummaries)
            {
                totalScore += CalculateStudentDayScore(studentSummary, day);
                count++;
            }
        }

        if (count <= 0)
            return 0;

        return Mathf.RoundToInt(totalScore / (float)count);
    }

    private int CalculateStudentDayScore(CourseStudentPerformanceData studentSummary, int day)
    {
        if (studentSummary == null || studentSummary.relatedResults == null)
            return 0;

        int totalScore = 0;
        int count = 0;

        foreach (CloudDailyResultData result in studentSummary.relatedResults)
        {
            if (result == null || result.day != day)
                continue;

            totalScore += GetSubjectScoreForResult(result);
            count++;
        }

        if (count <= 0)
            return 0;

        return Mathf.RoundToInt(totalScore / (float)count);
    }

    private int CalculateCourseRevenue(SubjectCoursePerformanceData courseSummary, List<int> days)
    {
        if (courseSummary == null || courseSummary.relatedResults == null)
            return 0;

        int total = 0;

        foreach (CloudDailyResultData result in courseSummary.relatedResults)
        {
            if (result == null)
                continue;

            if (ShouldUseDay(result.day, days))
                total += result.revenue;
        }

        return total;
    }

    private int CalculateCourseErrors(SubjectCoursePerformanceData courseSummary, List<int> days)
    {
        if (courseSummary == null || courseSummary.relatedResults == null)
            return 0;

        int total = 0;

        foreach (CloudDailyResultData result in courseSummary.relatedResults)
        {
            if (result == null)
                continue;

            if (ShouldUseDay(result.day, days))
                total += GetSubjectErrorsForResult(result);
        }

        return total;
    }

    private int CalculateCourseActivities(SubjectCoursePerformanceData courseSummary, List<int> days)
    {
        if (courseSummary == null || courseSummary.relatedResults == null)
            return 0;

        int total = 0;

        foreach (CloudDailyResultData result in courseSummary.relatedResults)
        {
            if (result == null)
                continue;

            if (ShouldUseDay(result.day, days))
                total++;
        }

        return total;
    }

    private bool ShouldUseDay(int day, List<int> days)
    {
        if (days == null || days.Count == 0)
            return false;

        foreach (int validDay in days)
        {
            if (validDay == day)
                return true;
        }

        return false;
    }

    private Dictionary<string, ActivityPerformanceData> BuildActivityPerformance(List<CloudDailyResultData> results)
    {
        Dictionary<string, ActivityPerformanceData> data = new Dictionary<string, ActivityPerformanceData>();

        List<string> relatedAreas = GetRelatedResultAreasForSelectedCourse();

        foreach (CloudDailyResultData result in results)
        {
            if (relatedAreas.Count == 0 || !ShouldUseDetailedAreas(result))
            {
                string key = result.minigameName;

                if (!data.ContainsKey(key))
                {
                    data[key] = new ActivityPerformanceData
                    {
                        activityName = key,
                        count = 0,
                        totalScore = 0
                    };
                }

                data[key].count++;
                data[key].totalScore += result.finalScore;

                continue;
            }

            foreach (string area in relatedAreas)
            {
                int areaScore = GetAreaScore(result, area);

                if (areaScore < 0)
                    continue;

                string key = GetAreaDisplayName(area);

                if (!data.ContainsKey(key))
                {
                    data[key] = new ActivityPerformanceData
                    {
                        activityName = key,
                        count = 0,
                        totalScore = 0
                    };
                }

                data[key].count++;
                data[key].totalScore += areaScore;
            }
        }

        return data;
    }

    private void ClearReport()
    {
        currentSelectedStudent = null;
        currentStudentResults.Clear();
        currentStudentCurrentDay = 1;

        isViewingCourseSummary = false;
        currentCourseResults.Clear();
        currentCourseStudentSummaries.Clear();
        currentCourseCurrentDay = 1;

        isViewingSubjectSummary = false;
        currentSubjectResults.Clear();
        currentSubjectCourseSummaries.Clear();
        currentSubjectCurrentDay = 1;

        UpdateKpiLabelsForSelectedCourse();

        if (selectedStudentText != null)
            selectedStudentText.text = "Student results";

        if (generalPerformanceText != null)
            generalPerformanceText.text = "-";

        if (totalRevenueText != null)
            totalRevenueText.text = "-";

        if (totalErrorsText != null)
            totalErrorsText.text = "-";

        if (activitiesText != null)
            activitiesText.text = "-";

        if (dayDropdown != null)
        {
            dayDropdown.ClearOptions();
            dayDropdown.AddOptions(new List<string> { "All Days" });
            dayDropdown.value = 0;
            dayDropdown.RefreshShownValue();
        }

        ClearResultRows();
        ClearCharts();
        SetPdfDownloadState(false, "Select a report first");
    }

    private void ClearCharts()
    {
        ClearChartSeries(performanceLineChart);
        ClearChartSeries(activityScoreBarChart);
        ClearChartSeries(revenueBarChart);
        ClearChartSeries(errorsBarChart);
    }

    private void ClearStudentsList()
    {
        if (studentsContainer == null)
            return;

        foreach (Transform child in studentsContainer)
            Destroy(child.gameObject);
    }

    private void ClearResultRows()
    {
        if (resultsContainer == null)
            return;

        foreach (Transform child in resultsContainer)
            Destroy(child.gameObject);
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

    private void ClearChartSeries(BaseChart chart)
    {
        if (chart == null)
            return;

        chart.ClearData();

        while (chart.GetSerie(0) != null)
            chart.RemoveSerie(0);
    }

    private void SetChartTitle(BaseChart chart, string text)
    {
        if (chart == null)
            return;

        Title title = chart.EnsureChartComponent<Title>();

        if (title != null)
        {
            title.show = true;
            title.text = text;
        }
    }

    private void SetLegend(BaseChart chart, bool show)
    {
        if (chart == null)
            return;

        Legend legend = chart.EnsureChartComponent<Legend>();

        if (legend != null)
            legend.show = show;
    }

    private List<CloudDailyResultData> GetResultsRelatedToSelectedCourse()
    {
        return GetResultsRelatedToSelectedCourse(currentStudentResults);
    }

    private List<CloudDailyResultData> GetResultsRelatedToSelectedCourse(List<CloudDailyResultData> sourceResults)
    {
        List<CloudDailyResultData> filtered = new List<CloudDailyResultData>();

        List<string> relatedMinigames = GetRelatedMinigamesForSelectedCourse();

        if (relatedMinigames.Count == 0 || sourceResults == null)
            return filtered;

        foreach (CloudDailyResultData result in sourceResults)
        {
            if (result == null)
                continue;

            if (IsMinigameRelated(result.minigameName, relatedMinigames))
                filtered.Add(result);
        }

        return filtered;
    }

    private List<string> GetRelatedMinigamesForSelectedCourse()
    {
        List<string> relatedMinigames = new List<string>();

        TeacherCourseData selectedCourse = GetSelectedCourse();

        if (selectedCourse == null)
            return relatedMinigames;

        TeacherSubjectCatalogData subject = FindSubjectByCode(selectedCourse.subjectCode);

        if (subject == null || subject.relatedMinigames == null)
            return relatedMinigames;

        foreach (string minigameName in subject.relatedMinigames)
        {
            if (string.IsNullOrEmpty(minigameName))
                continue;

            string cleanName = minigameName.Trim();

            if (!string.IsNullOrEmpty(cleanName))
                relatedMinigames.Add(cleanName);
        }

        return relatedMinigames;
    }

    private bool IsMinigameRelated(string resultMinigameName, List<string> relatedMinigames)
    {
        string normalizedResultName = NormalizeText(resultMinigameName);

        foreach (string relatedMinigame in relatedMinigames)
        {
            if (NormalizeText(relatedMinigame) == normalizedResultName)
                return true;
        }

        return false;
    }

    private TeacherSubjectCatalogData FindSubjectByCode(string subjectCode)
    {
        if (string.IsNullOrEmpty(subjectCode))
            return null;

        foreach (TeacherSubjectCatalogData subject in subjectCatalog)
        {
            if (subject.subjectCode == subjectCode)
                return subject;
        }

        return null;
    }

    private TeacherCourseData GetSelectedCourse()
    {
        if (courses.Count == 0)
            return null;

        int index = courseDropdown != null ? courseDropdown.value : 0;

        if (index < 0 || index >= courses.Count)
            return null;

        return courses[index];
    }

    private string GetCourseName(TeacherCourseData course)
    {
        if (course == null)
            return "";

        if (!string.IsNullOrEmpty(course.subjectName))
            return course.subjectName;

        return course.courseName;
    }

    private string GetSubjectCode(TeacherCourseData course)
    {
        if (course == null)
            return "";

        if (!string.IsNullOrEmpty(course.subjectCode))
            return course.subjectCode;

        return "UNKNOWN";
    }

    private string GetClassCode(TeacherCourseData course)
    {
        if (course == null)
            return "";

        if (!string.IsNullOrEmpty(course.classCode))
            return course.classCode;

        return course.courseCode;
    }

    private string NormalizeText(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        return value.Trim().ToLowerInvariant();
    }

    private void UpdateKpiLabelsForSelectedCourse()
    {
        TeacherCourseData selectedCourse = GetSelectedCourse();

        string subjectCode = selectedCourse != null ? GetSubjectCode(selectedCourse) : "";

        SubjectKpiLabels labels = GetKpiLabelsBySubject(subjectCode);

        if (performanceLabelText != null)
            performanceLabelText.text = labels.performanceLabel;

        if (revenueLabelText != null)
            revenueLabelText.text = labels.revenueLabel;

        if (errorsLabelText != null)
            errorsLabelText.text = labels.errorsLabel;

        if (activitiesLabelText != null)
            activitiesLabelText.text = labels.activitiesLabel;
    }

    private SubjectKpiLabels GetKpiLabelsBySubject(string subjectCode)
    {
        switch (subjectCode)
        {
            case "HOSZ4485":
                return new SubjectKpiLabels(
                    "FO + Housekeeping",
                    "Room Revenue",
                    "Service Errors",
                    "Hotel Tasks"
                );

            case "HOSZ2188":
                return new SubjectKpiLabels(
                    "Room Division",
                    "Room Revenue",
                    "Room Errors",
                    "Room Tasks"
                );

            case "GSTR4494":
                return new SubjectKpiLabels(
                    "A&B Service",
                    "A&B Revenue",
                    "Order Errors",
                    "Tickets Done"
                );

            case "GSTR4491":
                return new SubjectKpiLabels(
                    "Hygiene Score",
                    "Safe Service",
                    "Hygiene Errors",
                    "Safety Tasks"
                );

            case "HOSZ4492":
                return new SubjectKpiLabels(
                    "Revenue Score",
                    "Total Revenue",
                    "Revenue Errors",
                    "Revenue Cases"
                );

            case "TITA0911":
                return new SubjectKpiLabels(
                    "PMS Use",
                    "Booking Revenue",
                    "System Errors",
                    "PMS Tasks"
                );

            case "EHTZ3479":
                return new SubjectKpiLabels(
                    "Guest Experience",
                    "Service Value",
                    "Guest Issues",
                    "Guest Cases"
                );

            case "EHTZ4070":
                return new SubjectKpiLabels(
                    "Quality Score",
                    "Service Value",
                    "Quality Issues",
                    "Quality Checks"
                );

            case "HOSZ4488":
                return new SubjectKpiLabels(
                    "Product Design",
                    "Package Revenue",
                    "Design Errors",
                    "Packages Done"
                );

            case "HTUR0009":
                return new SubjectKpiLabels(
                    "Event Planning",
                    "Event Revenue",
                    "Planning Errors",
                    "Event Cases"
                );

            default:
                return new SubjectKpiLabels(
                    "Hospitality Score",
                    "Hotel Revenue",
                    "Service Errors",
                    "Practices Done"
                );
        }
    }

    private TeacherSubjectCatalogData GetSelectedSubject()
    {
        TeacherCourseData selectedCourse = GetSelectedCourse();

        if (selectedCourse == null)
            return null;

        return FindSubjectByCode(selectedCourse.subjectCode);
    }

    private List<string> GetRelatedResultAreasForSelectedCourse()
    {
        List<string> areas = new List<string>();

        TeacherSubjectCatalogData subject = GetSelectedSubject();

        if (subject == null || subject.relatedResultAreas == null)
            return areas;

        foreach (string area in subject.relatedResultAreas)
        {
            if (string.IsNullOrWhiteSpace(area))
                continue;

            areas.Add(area.Trim());
        }

        return areas;
    }

    private int GetSubjectScoreForResult(CloudDailyResultData result)
    {
        List<string> areas = GetRelatedResultAreasForSelectedCourse();

        if (areas.Count == 0 || !ShouldUseDetailedAreas(result))
            return result.finalScore;

        int total = 0;
        int count = 0;

        foreach (string area in areas)
        {
            int areaScore = GetAreaScore(result, area);

            if (areaScore < 0)
                continue;

            total += areaScore;
            count++;
        }

        if (count <= 0)
            return result.finalScore;

        return Mathf.RoundToInt(total / (float)count);
    }

    private int GetSubjectErrorsForResult(CloudDailyResultData result)
    {
        List<string> areas = GetRelatedResultAreasForSelectedCourse();

        if (areas.Count == 0 || !ShouldUseDetailedAreas(result))
            return result.errors;

        int errors = 0;
        int validAreas = 0;

        foreach (string area in areas)
        {
            int areaScore = GetAreaScore(result, area);

            if (areaScore < 0)
                continue;

            validAreas++;

            if (areaScore < 100)
                errors++;
        }

        if (validAreas <= 0)
            return result.errors;

        return errors;
    }

    private int GetAreaScore(CloudDailyResultData result, string area)
    {
        string normalizedArea = NormalizeArea(area);

        switch (normalizedArea)
        {
            case "room":
                return result.roomScore;

            case "stp":
            case "segment":
            case "segmentation":
                return result.stpScore;

            case "offer":
                return result.offerScore;

            case "tourismextra":
            case "extra":
                return result.tourismExtraScore;

            case "budget":
                return result.budgetScore;

            default:
                return -1;
        }
    }

    private string GetAreaDisplayName(string area)
    {
        string normalizedArea = NormalizeArea(area);

        switch (normalizedArea)
        {
            case "room":
                return "Room Assignment";

            case "stp":
            case "segment":
            case "segmentation":
                return "STP Segmentation";

            case "offer":
                return "Commercial Offer";

            case "tourismextra":
            case "extra":
                return "Tourism Extra";

            case "budget":
                return "Budget Fit";

            default:
                return area;
        }
    }

    private string NormalizeArea(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        return value
            .Trim()
            .ToLowerInvariant()
            .Replace(" ", "")
            .Replace("_", "")
            .Replace("-", "");
    }

    private bool ShouldUseDetailedAreas(CloudDailyResultData result)
    {
        if (result == null)
            return false;

        if (!IsCheckInResult(result))
            return false;

        return result.hasDetailedCheckInScores;
    }

    private bool IsCheckInResult(CloudDailyResultData result)
    {
        if (result == null || string.IsNullOrEmpty(result.minigameName))
            return false;

        string name = result.minigameName
            .Trim()
            .ToLowerInvariant()
            .Replace(" ", "")
            .Replace("_", "")
            .Replace("-", "");

        return name == "checkin";
    }

    public void DownloadCurrentReportPdf()
    {
        if (!canDownloadPdf || !HasReportReadyForPdf())
        {
            SetPdfDownloadState(false, "Load results first");
            Debug.LogWarning("PDF Export: Cannot download because no report data is loaded.");
            return;
        }

        if (pdfExportCoroutine != null)
            return;

        SetPdfDownloadState(false, "Generating PDF...");
        pdfExportCoroutine = StartCoroutine(DownloadCurrentReportPdfRoutine());
    }

    private IEnumerator DownloadCurrentReportPdfRoutine()
    {
        if (pdfCapturePages == null || pdfCapturePages.Length == 0)
        {
            Debug.LogError("PDF Export: Assign at least one PDF Capture Page in the Inspector.");
            pdfExportCoroutine = null;
            yield break;
        }

        RefreshDownloadReportButtonLabel();

        Canvas.ForceUpdateCanvases();

        List<bool> previousStates = new List<bool>();

        if (objectsToHideWhileExporting != null)
        {
            foreach (GameObject objectToHide in objectsToHideWhileExporting)
            {
                if (objectToHide == null)
                    continue;

                previousStates.Add(objectToHide.activeSelf);
                objectToHide.SetActive(false);
            }
        }

        Canvas.ForceUpdateCanvases();

        yield return new WaitForEndOfFrame();

        List<Texture2D> capturedPages = new List<Texture2D>();

        float originalVerticalScroll = 1f;
        float originalHorizontalScroll = 0f;

        if (reportScrollRect != null)
        {
            originalVerticalScroll = reportScrollRect.verticalNormalizedPosition;
            originalHorizontalScroll = reportScrollRect.horizontalNormalizedPosition;
        }

        foreach (RectTransform capturePage in pdfCapturePages)
        {
            if (capturePage == null)
                continue;

            if (reportScrollRect != null)
            {
                ScrollToPdfCapturePage(capturePage);
                Canvas.ForceUpdateCanvases();

                yield return null;
                yield return new WaitForEndOfFrame();
            }

            Texture2D pageTexture = CaptureRectTransform(capturePage);

            if (pageTexture != null)
                capturedPages.Add(pageTexture);
        }

        if (reportScrollRect != null)
        {
            reportScrollRect.verticalNormalizedPosition = originalVerticalScroll;
            reportScrollRect.horizontalNormalizedPosition = originalHorizontalScroll;
            reportScrollRect.velocity = Vector2.zero;
            Canvas.ForceUpdateCanvases();
        }

        int stateIndex = 0;

        if (objectsToHideWhileExporting != null)
        {
            foreach (GameObject objectToHide in objectsToHideWhileExporting)
            {
                if (objectToHide == null)
                    continue;

                if (stateIndex < previousStates.Count)
                    objectToHide.SetActive(previousStates[stateIndex]);

                stateIndex++;
            }
        }

        Canvas.ForceUpdateCanvases();

        if (capturedPages.Count == 0)
        {
            Debug.LogError("PDF Export: Could not capture any report page.");
            pdfExportCoroutine = null;
            yield break;
        }

        string folderPath = Path.Combine(Application.persistentDataPath, pdfReportsFolderName);

        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        string fileName = "Resultados_" + SanitizeFileName(GetCurrentReportTargetName()) + ".pdf";
        string fullPath = Path.Combine(folderPath, fileName);

        string pdfDataText = BuildCurrentPdfDataText();
        
        string pdfFeedbackText = BuildCurrentReportFeedback();

        string fullPdfText =
            pdfDataText +
            "\n\n----------------------------------------\n\n" +
            pdfFeedbackText;

        SimplePdfWriter.SaveTexturesAndFeedbackAsPdf(capturedPages,fullPath,"Report Data and Learning Feedback",fullPdfText,92);

        foreach (Texture2D texture in capturedPages)
        {
            if (texture != null)
                Destroy(texture);
        }

        Debug.Log("PDF report saved at: " + fullPath);

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        Application.OpenURL("file:///" + folderPath.Replace("\\", "/"));
#else
    Application.OpenURL("file://" + fullPath);
#endif

        pdfExportCoroutine = null;
        SetPdfDownloadState(HasReportReadyForPdf(), "Select a report first");
    }

    private Texture2D CaptureRectTransform(RectTransform rectTransform)
    {
        if (rectTransform == null)
            return null;

        Texture2D screenTexture = ScreenCapture.CaptureScreenshotAsTexture();

        if (screenTexture == null)
            return null;

        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);

        Camera canvasCamera = GetCanvasCamera(rectTransform);

        Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(canvasCamera, corners[0]);
        Vector2 topRight = RectTransformUtility.WorldToScreenPoint(canvasCamera, corners[2]);

        int xMin = Mathf.RoundToInt(Mathf.Min(bottomLeft.x, topRight.x));
        int yMin = Mathf.RoundToInt(Mathf.Min(bottomLeft.y, topRight.y));
        int xMax = Mathf.RoundToInt(Mathf.Max(bottomLeft.x, topRight.x));
        int yMax = Mathf.RoundToInt(Mathf.Max(bottomLeft.y, topRight.y));

        xMin = Mathf.Clamp(xMin, 0, screenTexture.width - 1);
        yMin = Mathf.Clamp(yMin, 0, screenTexture.height - 1);
        xMax = Mathf.Clamp(xMax, 0, screenTexture.width);
        yMax = Mathf.Clamp(yMax, 0, screenTexture.height);

        int width = xMax - xMin;
        int height = yMax - yMin;

        if (width <= 0 || height <= 0)
        {
            Destroy(screenTexture);
            return null;
        }

        Texture2D croppedTexture = new Texture2D(width, height, TextureFormat.RGB24, false);
        croppedTexture.SetPixels(screenTexture.GetPixels(xMin, yMin, width, height));
        croppedTexture.Apply();

        Destroy(screenTexture);

        return croppedTexture;
    }

    private void ScrollToPdfCapturePage(RectTransform target)
    {
        if (reportScrollRect == null || target == null)
            return;

        RectTransform content = reportScrollRect.content;

        if (content == null)
            return;

        RectTransform viewport = reportScrollRect.viewport;

        if (viewport == null)
            viewport = reportScrollRect.GetComponent<RectTransform>();

        if (viewport == null)
            return;

        Canvas.ForceUpdateCanvases();

        Bounds targetBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(content, target);

        float contentHeight = content.rect.height;
        float viewportHeight = viewport.rect.height;

        if (contentHeight <= viewportHeight)
        {
            reportScrollRect.verticalNormalizedPosition = 1f;
            reportScrollRect.velocity = Vector2.zero;
            return;
        }

        float targetCenterFromTop = content.rect.yMax - targetBounds.center.y;
        float desiredScrollFromTop = targetCenterFromTop - viewportHeight * 0.5f;
        float scrollableHeight = contentHeight - viewportHeight;

        float normalizedPosition = 1f - Mathf.Clamp01(desiredScrollFromTop / scrollableHeight);

        reportScrollRect.verticalNormalizedPosition = normalizedPosition;
        reportScrollRect.horizontalNormalizedPosition = 0f;
        reportScrollRect.velocity = Vector2.zero;

        Canvas.ForceUpdateCanvases();
    }

    private Camera GetCanvasCamera(RectTransform rectTransform)
    {
        Canvas canvas = rectTransform.GetComponentInParent<Canvas>();

        if (canvas == null)
            return null;

        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        if (canvas.worldCamera != null)
            return canvas.worldCamera;

        return Camera.main;
    }

    private void RefreshDownloadReportButtonLabel()
    {
        if (downloadReportButtonText == null)
            return;

        if (!canDownloadPdf)
        {
            downloadReportButtonText.text = disabledPdfDownloadText;
            return;
        }

        if (isViewingSubjectSummary)
        {
            downloadReportButtonText.text = "Download comparison results";
            return;
        }

        if (isViewingCourseSummary)
        {
            TeacherCourseData selectedCourse = GetSelectedCourse();
            string courseCode = selectedCourse != null ? GetClassCode(selectedCourse) : "Course";
            downloadReportButtonText.text = "Download course " + courseCode + " results";
            return;
        }

        if (currentSelectedStudent != null)
        {
            downloadReportButtonText.text = "Download " + currentSelectedStudent.displayName + " results";
            return;
        }

        downloadReportButtonText.text = "Download report";
    }

    private void SetPdfDownloadState(bool canDownload, string disabledText = null)
    {
        canDownloadPdf = canDownload;

        if (!canDownload && !string.IsNullOrWhiteSpace(disabledText))
            disabledPdfDownloadText = disabledText;

        if (downloadReportButton != null)
            downloadReportButton.interactable = canDownload;

        RefreshDownloadReportButtonLabel();
    }

    private bool HasReportReadyForPdf()
    {
        if (isViewingSubjectSummary)
            return currentSubjectCourseSummaries.Count > 0 && currentSubjectResults.Count > 0;

        if (isViewingCourseSummary)
            return currentCourseStudentSummaries.Count > 0 && currentCourseResults.Count > 0;

        if (currentSelectedStudent != null)
            return GetResultsRelatedToSelectedCourse().Count > 0;

        return false;
    }

    private string GetCurrentReportTargetName()
    {
        if (isViewingSubjectSummary)
            return "Comparativa_" + GetComparedCourseCodesFileText();

        if (isViewingCourseSummary)
        {
            TeacherCourseData selectedCourse = GetSelectedCourse();
            string courseCode = selectedCourse != null ? GetClassCode(selectedCourse) : "Curso";
            return "Curso_" + courseCode;
        }

        if (currentSelectedStudent != null)
            return currentSelectedStudent.displayName;

        return "Reporte";
    }

    private string GetComparedCourseCodesFileText()
    {
        List<string> codes = new List<string>();

        foreach (SubjectCoursePerformanceData courseSummary in currentSubjectCourseSummaries)
        {
            if (courseSummary == null)
                continue;

            string code = GetCourseComparisonCode(courseSummary);

            if (!string.IsNullOrWhiteSpace(code) && !codes.Contains(code))
                codes.Add(code);
        }

        if (codes.Count == 0)
        {
            TeacherCourseData selectedCourse = GetSelectedCourse();
            return selectedCourse != null ? GetSubjectCode(selectedCourse) : "Comparativa";
        }

        return string.Join("_vs_", codes.ToArray());
    }

    private string SanitizeFileName(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
            return "Reporte";

        string cleanName = rawName.Trim();

        foreach (char invalidChar in Path.GetInvalidFileNameChars())
            cleanName = cleanName.Replace(invalidChar, '_');

        cleanName = cleanName.Replace(" ", "_");
        cleanName = cleanName.Replace("/", "_");
        cleanName = cleanName.Replace("\\", "_");

        return cleanName;
    }


    private string BuildCurrentReportFeedback()
    {
        if (isViewingSubjectSummary)
            return BuildSubjectComparisonFeedback();

        if (isViewingCourseSummary)
            return BuildCourseFeedback();

        if (currentSelectedStudent != null)
            return BuildStudentFeedback();

        return "Learning feedback:\nSelect a student, course summary, or course comparison before downloading the report.";
    }

    private string BuildCurrentPdfDataText()
    {
        StringBuilder builder = new StringBuilder();

        builder.AppendLine("Report Data");
        builder.AppendLine("");

        string reportTitle = selectedStudentText != null ? selectedStudentText.text : "General Report";
        builder.AppendLine("Report: " + reportTitle);

        TeacherCourseData selectedCourse = GetSelectedCourse();

        if (selectedCourse != null)
        {
            builder.AppendLine("Subject: " + GetCourseName(selectedCourse));
            builder.AppendLine("Subject code: " + GetSubjectCode(selectedCourse));
            builder.AppendLine("Selected class: " + GetClassCode(selectedCourse));
        }

        if (dayDropdown != null && dayDropdown.options.Count > 0)
            builder.AppendLine("Selected day filter: " + dayDropdown.options[dayDropdown.value].text);

        builder.AppendLine("");

        builder.AppendLine("Main results:");

        builder.AppendLine(
            GetSafeText(performanceLabelText) + ": " + GetSafeText(generalPerformanceText)
        );

        builder.AppendLine(
            GetSafeText(revenueLabelText) + ": " + GetSafeText(totalRevenueText)
        );

        builder.AppendLine(
            GetSafeText(errorsLabelText) + ": " + GetSafeText(totalErrorsText)
        );

        builder.AppendLine(
            GetSafeText(activitiesLabelText) + ": " + GetSafeText(activitiesText)
        );

        builder.AppendLine("");

        if (isViewingSubjectSummary)
        {
            builder.AppendLine("Comparison type: Courses from the same subject");
            builder.AppendLine("Compared courses: " + GetComparedCourseCodesText());
            builder.AppendLine("");

            List<int> days = GetDaysForCurrentChartView(currentSubjectResults);

            foreach (SubjectCoursePerformanceData courseSummary in currentSubjectCourseSummaries)
            {
                if (courseSummary == null)
                    continue;

                string courseCode = GetCourseComparisonCode(courseSummary);

                int score = CalculateCourseAverageScore(courseSummary, days);
                int revenue = CalculateCourseRevenue(courseSummary, days);
                int errors = CalculateCourseErrors(courseSummary, days);
                int activities = CalculateCourseActivities(courseSummary, days);

                builder.AppendLine(
                    "Course " + courseCode +
                    " - Score: " + score + "%" +
                    " - Revenue: $" + revenue +
                    " - Errors: " + errors +
                    " - Tasks: " + activities
                );
            }
        }
        else if (isViewingCourseSummary)
        {
            builder.AppendLine("Report type: Course summary");
            builder.AppendLine("Students included: " + currentCourseStudentSummaries.Count);
        }
        else if (currentSelectedStudent != null)
        {
            builder.AppendLine("Report type: Student results");
            builder.AppendLine("Student: " + currentSelectedStudent.displayName);
        }

        return builder.ToString();
    }

    private string GetSafeText(TMP_Text text)
    {
        if (text == null)
            return "-";

        if (string.IsNullOrWhiteSpace(text.text))
            return "-";

        return text.text.Replace("\n", " ");
    }

    private string BuildStudentFeedback()
    {
        int score = ExtractPercentValue(generalPerformanceText != null ? generalPerformanceText.text : "");

        return
            "Learning feedback:\n" +
            GetScoreFeedback(score) + "\n" +
            "Review the charts to identify low-scoring activities, days with 0 activity, repeated service errors, and areas that need more practice.";
    }

    private string BuildCourseFeedback()
    {
        int score = ExtractPercentValue(generalPerformanceText != null ? generalPerformanceText.text : "");

        return
            "Learning feedback:\n" +
            GetScoreFeedback(score) + "\n" +
            "This course report summarizes the learning performance of all students in the selected course. Use the days with 0 activity and the lowest activity scores as priority areas for reinforcement.";
    }

    private string BuildSubjectComparisonFeedback()
    {
        List<int> days = GetDaysForCurrentChartView(currentSubjectResults);

        int bestScore = -1;
        string bestScoreCourse = "-";

        int bestRevenue = -1;
        string bestRevenueCourse = "-";

        int lowestErrors = int.MaxValue;
        string lowestErrorsCourse = "-";
        bool foundErrorsCandidate = false;

        int bestActivities = -1;
        string bestActivitiesCourse = "-";

        foreach (SubjectCoursePerformanceData courseSummary in currentSubjectCourseSummaries)
        {
            if (courseSummary == null)
                continue;

            string courseCode = GetCourseComparisonCode(courseSummary);

            int score = CalculateCourseAverageScore(courseSummary, days);
            int revenue = CalculateCourseRevenue(courseSummary, days);
            int errors = CalculateCourseErrors(courseSummary, days);
            int activities = CalculateCourseActivities(courseSummary, days);

            if (score > bestScore)
            {
                bestScore = score;
                bestScoreCourse = courseCode;
            }

            if (revenue > bestRevenue)
            {
                bestRevenue = revenue;
                bestRevenueCourse = courseCode;
            }

            if (activities > 0 && errors < lowestErrors)
            {
                lowestErrors = errors;
                lowestErrorsCourse = courseCode;
                foundErrorsCandidate = true;
            }

            if (activities > bestActivities)
            {
                bestActivities = activities;
                bestActivitiesCourse = courseCode;
            }
        }

        if (!foundErrorsCandidate)
        {
            lowestErrors = 0;
            lowestErrorsCourse = bestScoreCourse;
        }

        return
            "Learning feedback:\n" +
            "Compared courses: " + GetComparedCourseCodesText() + ".\n" +
            "Best learning performance: " + bestScoreCourse + " (" + Mathf.Max(0, bestScore) + "%).\n" +
            "Highest revenue result: " + bestRevenueCourse + " ($" + Mathf.Max(0, bestRevenue) + ").\n" +
            "Best error control: " + lowestErrorsCourse + " (" + lowestErrors + " errors).\n" +
            "Most completed tasks: " + bestActivitiesCourse + " (" + Mathf.Max(0, bestActivities) + " tasks).\n" +
            "Use the lower-performing course as the main reference for reinforcement and compare its activity scores with the best-performing course.";
    }

    private string GetScoreFeedback(int score)
    {
        if (score < 0)
            return "The report does not have enough score data yet.";

        if (score >= 85)
            return "Excellent performance. The learning outcomes are being achieved consistently.";

        if (score >= 70)
            return "Good performance. The learning outcomes are mostly achieved, but some areas still need practice.";

        if (score >= 60)
            return "Acceptable performance. The student or course needs reinforcement in the weaker activities.";

        return "Low performance. The learning outcomes require immediate reinforcement and additional guided practice.";
    }

    private int ExtractPercentValue(string text)
    {
        if (string.IsNullOrEmpty(text))
            return -1;

        int percentIndex = text.IndexOf("%");

        if (percentIndex < 0)
            return -1;

        int startIndex = percentIndex - 1;

        while (startIndex >= 0 && char.IsDigit(text[startIndex]))
            startIndex--;

        startIndex++;

        if (startIndex >= percentIndex)
            return -1;

        string numberText = text.Substring(startIndex, percentIndex - startIndex);

        int value;
        if (int.TryParse(numberText, out value))
            return value;

        return -1;
    }


}

[System.Serializable]
public class StudentPerformanceRequestData
{
    public string studentPlayFabId;
}

[System.Serializable]
public class StudentPerformanceResponseData
{
    public bool success;
    public string message;
    public bool hasStartedGame;
    public int currentDay;
    public CloudDailyResultData[] results;
}

public class DayPerformanceData
{
    public int day;
    public int count;
    public int totalScore;
    public int totalRevenue;
    public int totalErrors;

    public int AverageScore
    {
        get
        {
            if (count <= 0) return 0;
            return Mathf.RoundToInt(totalScore / (float)count);
        }
    }
}

public class ActivityPerformanceData
{
    public string activityName;
    public int count;
    public int totalScore;

    public int AverageScore
    {
        get
        {
            if (count <= 0) return 0;
            return Mathf.RoundToInt(totalScore / (float)count);
        }
    }
}

public class CourseStudentPerformanceData
{
    public AssignedStudentData student;
    public int currentDay;
    public List<CloudDailyResultData> relatedResults;
}

public class SubjectCoursePerformanceData
{
    public TeacherCourseData course;
    public string displayName;
    public int currentDay;
    public List<CourseStudentPerformanceData> studentSummaries;
    public List<CloudDailyResultData> relatedResults;
}

public class SubjectKpiLabels
{
    public string performanceLabel;
    public string revenueLabel;
    public string errorsLabel;
    public string activitiesLabel;

    public SubjectKpiLabels(string performanceLabel, string revenueLabel, string errorsLabel, string activitiesLabel)
    {
        this.performanceLabel = performanceLabel;
        this.revenueLabel = revenueLabel;
        this.errorsLabel = errorsLabel;
        this.activitiesLabel = activitiesLabel;
    }
}