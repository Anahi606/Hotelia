using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using PlayFab;
using PlayFab.ClientModels;
using XCharts.Runtime;

public class TeacherPerformancePanelUI : MonoBehaviour
{
    private const string TeacherCoursesKey = "Hotelia_TeacherCourses";
    private const string TeacherStudentsKey = "Hotelia_TeacherStudents";

    [Header("Filters")]
    [SerializeField] private TMP_Dropdown courseDropdown;
    [SerializeField] private TMP_Dropdown dayDropdown;

    [Header("Students List")]
    [SerializeField] private Transform studentsContainer;
    [SerializeField] private GameObject performanceStudentRowPrefab;

    [Header("Report Texts")]
    [SerializeField] private TMP_Text selectedStudentText;
    [SerializeField] private TMP_Text generalPerformanceText;
    [SerializeField] private TMP_Text totalRevenueText;
    [SerializeField] private TMP_Text totalErrorsText;
    [SerializeField] private TMP_Text activitiesText;

    [Header("Result Rows")]
    [SerializeField] private Transform resultsContainer;
    [SerializeField] private GameObject performanceResultRowPrefab;

    [Header("Charts")]
    [SerializeField] private LineChart performanceLineChart;
    [SerializeField] private BarChart activityScoreBarChart;
    [SerializeField] private BarChart revenueBarChart;
    [SerializeField] private BarChart errorsBarChart;

    [Header("Azure URLs")]
    [SerializeField] private string getStudentPerformanceUrl;

    private readonly List<TeacherCourseData> courses = new List<TeacherCourseData>();
    private readonly List<AssignedStudentData> assignedStudents = new List<AssignedStudentData>();
    private readonly List<CloudDailyResultData> currentStudentResults = new List<CloudDailyResultData>();

    private AssignedStudentData currentSelectedStudent;

    private void OnEnable()
    {
        LoadTeacherData();
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
            options.Add(course.courseName + " (" + course.courseCode + ")");

        if (options.Count == 0)
            options.Add("No courses available");

        courseDropdown.AddOptions(options);
        courseDropdown.value = 0;
        courseDropdown.RefreshShownValue();

        courseDropdown.onValueChanged.AddListener(delegate
        {
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

    public void LoadStudentPerformance(AssignedStudentData student)
    {
        if (student == null)
            return;

        currentSelectedStudent = student;
        StartCoroutine(GetStudentPerformanceRequest(student));
    }

    private IEnumerator GetStudentPerformanceRequest(AssignedStudentData student)
    {
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
            yield break;
        }

        StudentPerformanceResponseData response =
            JsonUtility.FromJson<StudentPerformanceResponseData>(request.downloadHandler.text);

        if (response == null || !response.success)
        {
            yield break;
        }

        currentStudentResults.Clear();

        if (response.results != null)
            currentStudentResults.AddRange(response.results);

        currentStudentResults.Sort((a, b) =>
        {
            int dayCompare = a.day.CompareTo(b.day);
            if (dayCompare != 0) return dayCompare;
            return string.Compare(a.minigameName, b.minigameName);
        });

        PopulateDayDropdown();
        UpdateReportWithSelectedDay();
    }

    private void PopulateDayDropdown()
    {
        if (dayDropdown == null)
            return;

        dayDropdown.onValueChanged.RemoveAllListeners();
        dayDropdown.ClearOptions();

        List<string> options = new List<string>();
        options.Add("All Days");

        HashSet<int> daysSet = new HashSet<int>();

        foreach (CloudDailyResultData result in currentStudentResults)
            daysSet.Add(result.day);

        List<int> days = new List<int>(daysSet);
        days.Sort();

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
        List<CloudDailyResultData> filtered = new List<CloudDailyResultData>();

        if (dayDropdown == null || dayDropdown.value == 0)
        {
            filtered.AddRange(currentStudentResults);
            return filtered;
        }

        string option = dayDropdown.options[dayDropdown.value].text;
        int selectedDay = ExtractDayNumber(option);

        foreach (CloudDailyResultData result in currentStudentResults)
        {
            if (result.day == selectedDay)
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

    private void UpdateReport(AssignedStudentData student, List<CloudDailyResultData> results)
    {
        ClearResultRows();

        if (selectedStudentText != null)
        {
            selectedStudentText.text = student.displayName + " results";
        }

        if (results == null || results.Count == 0)
        {
            if (generalPerformanceText != null)
                generalPerformanceText.text = "0%";

            if (totalRevenueText != null)
                totalRevenueText.text = "$0";

            if (totalErrorsText != null)
                totalErrorsText.text = "0";

            if (activitiesText != null)
                activitiesText.text = "0";

            ClearCharts();
            return;
        }

        int totalScore = 0;
        int totalRevenue = 0;
        int totalErrors = 0;

        foreach (CloudDailyResultData result in results)
        {
            totalScore += result.finalScore;
            totalRevenue += result.revenue;
            totalErrors += result.errors;

            GameObject rowObject = Instantiate(performanceResultRowPrefab, resultsContainer);
            PerformanceResultRowUI rowUI = rowObject.GetComponent<PerformanceResultRowUI>();

            if (rowUI != null)
                rowUI.Setup(result);
        }

        int averageScore = Mathf.RoundToInt(totalScore / (float)results.Count);

        if (generalPerformanceText != null)
            generalPerformanceText.text = averageScore + "%";

        if (totalRevenueText != null)
            totalRevenueText.text = "$" + totalRevenue;

        if (totalErrorsText != null)
            totalErrorsText.text = totalErrors.ToString();

        if (activitiesText != null)
            activitiesText.text = results.Count.ToString();

        UpdateCharts(results);
    }

    private void UpdateCharts(List<CloudDailyResultData> results)
    {
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

        SetChartTitle(performanceLineChart, "Performance Trend (%)");
        SetLegend(performanceLineChart, false);

        performanceLineChart.AddSerie<Line>("Performance");

        Dictionary<int, DayPerformanceData> dayData = BuildDayPerformance(results);

        List<int> days = new List<int>(dayData.Keys);
        days.Sort();

        foreach (int day in days)
        {
            performanceLineChart.AddXAxisData("Day " + day);
            performanceLineChart.AddData(0, dayData[day].AverageScore);
        }

        performanceLineChart.RefreshChart();
    }

    private void UpdateActivityScoreBarChart(List<CloudDailyResultData> results)
    {
        if (activityScoreBarChart == null)
            return;

        ClearChartSeries(activityScoreBarChart);

        SetChartTitle(activityScoreBarChart, "Score by Activity (%)");
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

        SetChartTitle(revenueBarChart, "Revenue by Day");
        SetLegend(revenueBarChart, false);

        revenueBarChart.AddSerie<Bar>("Revenue");

        Dictionary<int, DayPerformanceData> dayData = BuildDayPerformance(results);

        List<int> days = new List<int>(dayData.Keys);
        days.Sort();

        foreach (int day in days)
        {
            revenueBarChart.AddXAxisData("Day " + day);
            revenueBarChart.AddData(0, dayData[day].totalRevenue);
        }

        revenueBarChart.RefreshChart();
    }

    private void UpdateErrorsBarChart(List<CloudDailyResultData> results)
    {
        if (errorsBarChart == null)
            return;

        ClearChartSeries(errorsBarChart);

        SetChartTitle(errorsBarChart, "Errors by Day");
        SetLegend(errorsBarChart, false);

        errorsBarChart.AddSerie<Bar>("Errors");

        Dictionary<int, DayPerformanceData> dayData = BuildDayPerformance(results);

        List<int> days = new List<int>(dayData.Keys);
        days.Sort();

        foreach (int day in days)
        {
            errorsBarChart.AddXAxisData("Day " + day);
            errorsBarChart.AddData(0, dayData[day].totalErrors);
        }

        errorsBarChart.RefreshChart();
    }

    private Dictionary<int, DayPerformanceData> BuildDayPerformance(List<CloudDailyResultData> results)
    {
        Dictionary<int, DayPerformanceData> data = new Dictionary<int, DayPerformanceData>();

        foreach (CloudDailyResultData result in results)
        {
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
            data[result.day].totalScore += result.finalScore;
            data[result.day].totalRevenue += result.revenue;
            data[result.day].totalErrors += result.errors;
        }

        return data;
    }

    private Dictionary<string, ActivityPerformanceData> BuildActivityPerformance(List<CloudDailyResultData> results)
    {
        Dictionary<string, ActivityPerformanceData> data = new Dictionary<string, ActivityPerformanceData>();

        foreach (CloudDailyResultData result in results)
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
        }

        return data;
    }

    private void ClearReport()
    {
        currentSelectedStudent = null;
        currentStudentResults.Clear();

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