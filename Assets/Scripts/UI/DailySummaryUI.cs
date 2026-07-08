using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XCharts.Runtime;

public class DailySummaryUI : MonoBehaviour
{
    public static DailySummaryUI Instance { get; private set; }

    [Header("Daily Summary Panel")]
    [SerializeField] private GameObject summaryPanel;

    [Header("Daily Texts")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text generalSummaryText;
    [SerializeField] private TMP_Text stpText;
    [SerializeField] private TMP_Text feedbackText;

    [Header("Daily Charts")]
    [SerializeField] private BarChart scoreBarChart;
    [SerializeField] private PieChart errorsPieChart;
    [SerializeField] private BarChart revenueBarChart;

    [Header("All Days Dashboard Panel")]
    [SerializeField] private GameObject allDaysDashboardPanel;
    [SerializeField] private TMP_Text allDaysTitleText;
    [SerializeField] private TMP_Text allDaysGeneralText;

    [Header("All Days Charts")]
    [SerializeField] private LineChart allDaysKpiLineChart;
    [SerializeField] private BarChart allDaysRevenueBarChart;
    [SerializeField] private BarChart allDaysErrorsBarChart;

    [Header("Buttons")]
    [SerializeField] private GameObject allDaysButton;

    [Header("Daily Scroll Content")]
    [SerializeField] private RectTransform dailySummaryContent;

    [Header("KPI Cards")]
    [SerializeField] private TMP_Text performanceValueText;
    [SerializeField] private TMP_Text performanceLabelText;

    [SerializeField] private TMP_Text revenueValueText;
    [SerializeField] private TMP_Text revenueLabelText;

    [SerializeField] private TMP_Text errorsValueText;
    [SerializeField] private TMP_Text errorsLabelText;

    [SerializeField] private TMP_Text focusValueText;
    [SerializeField] private TMP_Text focusLabelText;

    private class SummaryData
    {
        public string name;
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

    private class DaySummaryData
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

    private const int MaxDailyDashboardDays = 15;
    private const int DaysPerGameMonth = 30;

    private class PeriodSummaryData
    {
        public string label;
        public int daysCount;
        public int totalActivities;
        public int totalRevenue;
        public int totalErrors;
        public int totalDailyAverageScore;

        public int AverageScore
        {
            get
            {
                if (daysCount <= 0) return 0;
                return Mathf.RoundToInt(totalDailyAverageScore / (float)daysCount);
            }
        }
    }

    private void Awake()
    {
        Instance = this;

        if (summaryPanel != null)
            summaryPanel.SetActive(false);

        if (allDaysDashboardPanel != null)
            allDaysDashboardPanel.SetActive(false);
    }

    public void OpenSummary()
    {
        StartCoroutine(OpenSummaryRoutine());
    }

    private IEnumerator OpenSummaryRoutine()
    {
        if (Time.timeScale == 0f)
            Time.timeScale = 1f;

        if (summaryPanel != null)
            summaryPanel.SetActive(true);

        if (allDaysDashboardPanel != null)
            allDaysDashboardPanel.SetActive(false);

        List<MiniGameResultData> results = DailyResultsManager.Instance != null
            ? DailyResultsManager.Instance.GetTodayResults()
            : new List<MiniGameResultData>();

        List<SummaryData> summary = BuildSummary(results);

        UpdateTexts(summary);
        UpdateAllDaysButtonVisibility();

        Canvas.ForceUpdateCanvases();

        if (dailySummaryContent != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(dailySummaryContent);

        yield return null;
        yield return new WaitForEndOfFrame();

        Canvas.ForceUpdateCanvases();

        if (dailySummaryContent != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(dailySummaryContent);

        UpdateCharts(summary);

        yield return null;
        yield return new WaitForEndOfFrame();

        ForceRefreshDailyCharts();

        yield return new WaitForSecondsRealtime(1.5f);

        ForceRefreshDailyCharts();

        HotelGamePause.RequestPause();
    }

    private void ForceRefreshDailyCharts()
    {
        ForceRefreshChart(scoreBarChart);
        ForceRefreshChart(errorsPieChart);
        ForceRefreshChart(revenueBarChart);
    }

    private void ForceRefreshChart(BaseChart chart)
    {
        if (chart == null) return;

        RectTransform rt = chart.transform as RectTransform;

        if (rt != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);

            if (rt.rect.width < 20f || rt.rect.height < 20f)
            {
                Debug.LogWarning(
                    chart.name + " tiene tamaño muy pequeño: " +
                    rt.rect.width + " x " + rt.rect.height
                );
            }
        }

        chart.RefreshChart();
    }

    private List<SummaryData> BuildSummary(List<MiniGameResultData> results)
    {
        Dictionary<string, SummaryData> data = new Dictionary<string, SummaryData>();

        if (results == null)
            return new List<SummaryData>();

        foreach (MiniGameResultData result in results)
        {
            if (result == null) continue;

            string key = result.minigameName;

            if (!data.ContainsKey(key))
            {
                data[key] = new SummaryData
                {
                    name = key,
                    count = 0,
                    totalScore = 0,
                    totalRevenue = 0,
                    totalErrors = 0
                };
            }

            data[key].count++;
            data[key].totalScore += result.finalScore;
            data[key].totalRevenue += result.revenue;
            data[key].totalErrors += result.errors;
        }

        return new List<SummaryData>(data.Values);
    }

    private void UpdateKpiCards(int averageScore,int totalRevenue,int totalErrors,string weakestActivityName)
    {
        if (performanceValueText != null)
            performanceValueText.text = averageScore + "%";

        if (performanceLabelText != null)
            performanceLabelText.text = "Performance";

        if (revenueValueText != null)
            revenueValueText.text = "$" + totalRevenue;

        if (revenueLabelText != null)
            revenueLabelText.text = "Revenue";

        if (errorsValueText != null)
            errorsValueText.text = totalErrors.ToString();

        if (errorsLabelText != null)
            errorsLabelText.text = "Errors";

        if (focusValueText != null)
            focusValueText.text = weakestActivityName;

        if (focusLabelText != null)
            focusLabelText.text = "Needs Practice";
    }

    private void UpdateTexts(List<SummaryData> summary)
    {
        int currentDay = DayManager.Instance != null ? DayManager.Instance.CurrentDay : 1;

        if (titleText != null)
            titleText.text = "Day " + currentDay + " Summary";

        if (summary == null || summary.Count == 0)
        {
            if (generalSummaryText != null)
                generalSummaryText.text =
                    "<b>DAILY KPI</b>\n" +
                    "No results recorded today.";

            if (stpText != null)
                stpText.text =
                    "<b>APPLIED STP</b>\n" +
                    "No activity data available.";

            if (feedbackText != null)
                feedbackText.text =
                    "<b>FEEDBACK</b>\n" +
                    "Complete activities to generate feedback.";

            return;
        }

        int totalScore = 0;
        int totalRevenue = 0;
        int totalErrors = 0;
        int totalActivities = 0;

        SummaryData bestActivity = null;
        SummaryData weakestActivity = null;

        string stpSummary = "";
        string feedbackSummary = "";

        foreach (SummaryData item in summary)
        {
            if (item == null)
                continue;

            totalScore += item.AverageScore;
            totalRevenue += item.totalRevenue;
            totalErrors += item.totalErrors;
            totalActivities += item.count;

            if (bestActivity == null || item.AverageScore > bestActivity.AverageScore)
                bestActivity = item;

            if (weakestActivity == null || item.AverageScore < weakestActivity.AverageScore)
                weakestActivity = item;

            stpSummary += GetDetailedSTP(item) + "\n";
            feedbackSummary += GetDetailedFeedback(item) + "\n";
        }

        int averageScore = Mathf.RoundToInt(totalScore / (float)summary.Count);

        string bestActivityName = bestActivity != null ? GetFullActivityName(bestActivity.name) : "-";
        string weakestActivityName = weakestActivity != null ? GetFullActivityName(weakestActivity.name) : "-";
        UpdateKpiCards(averageScore,totalRevenue,totalErrors, weakestActivityName);

        if (generalSummaryText != null)
        {
            generalSummaryText.text =
                "<b>DAILY KPI</b>\n" +
                "Score: " + averageScore + "% (" + GetPerformanceLevel(averageScore) + ")\n" +
                "Revenue: $" + totalRevenue + "\n" +
                "Errors: " + totalErrors + "\n" +
                "Activities: " + totalActivities + "\n" +
                "Best: " + bestActivityName + "\n" +
                "Review: " + weakestActivityName;
        }

        if (stpText != null)
        {
            stpText.text =
                "<b>APPLIED STP</b>\n" +
                stpSummary.TrimEnd();
        }

        if (feedbackText != null)
        {
            feedbackText.text =
                "<b>FEEDBACK</b>\n" +
                feedbackSummary.TrimEnd() +
                "\n" +
                GetGeneralRecommendation(averageScore, totalErrors, weakestActivity);
        }
    }

    private string GetDetailedSTP(SummaryData item)
    {
        if (item == null) return "";

        string activityName = GetFullActivityName(item.name);

        switch (item.name)
        {
            case "Check-in":
                return "• Check-in: guest type, room fit, offer and budget.";

            case "Habitación":
            case "Room":
                return "• Room: status, cleaning, bed and trash.";

            case "Restaurante":
            case "Restaurant":
                return "• Restaurant: ticket priority and correct delivery.";

            case "Paquetes":
            case "Packages":
                return "• Packages: budget, menu, decoration and occasion.";

            default:
                return "• " + activityName + ": service decision and accuracy.";
        }
    }

    private string GetDetailedFeedback(SummaryData item)
    {
        if (item == null) return "";

        string activityName = GetFullActivityName(item.name);

        string baseText =
            "• " + activityName + ": " +
            item.AverageScore + "%, " +
            item.totalErrors + " errors. ";

        if (item.totalErrors == 0 && item.AverageScore >= 85)
            return baseText + "Excellent. Keep this strategy.";

        if (item.AverageScore >= 70)
            return baseText + "Good. Improve small details.";

        if (item.AverageScore >= 50)
            return baseText + "Review choices before confirming.";

        return baseText + "Needs practice. Read the task carefully.";
    }

    private string GetPerformanceLevel(int score)
    {
        if (score >= 85)
            return "Excellent";

        if (score >= 70)
            return "Good";

        if (score >= 60)
            return "Acceptable";

        if (score >= 40)
            return "At risk";

        return "Critical";
    }

    private string GetRiskLevel(int averageScore, int totalErrors)
    {
        if (averageScore >= 85 && totalErrors == 0)
            return "Low";

        if (averageScore >= 70 && totalErrors <= 2)
            return "Moderate";

        if (averageScore >= 50)
            return "High";

        return "Critical";
    }

    private string GetGeneralRecommendation(int averageScore, int totalErrors, SummaryData weakestActivity)
    {
        string weakestActivityName = weakestActivity != null
            ? GetFullActivityName(weakestActivity.name)
            : "the weakest activity";

        if (averageScore >= 85 && totalErrors == 0)
            return "<b>Next:</b> Continue. Keep high accuracy.";

        if (averageScore >= 70)
            return "<b>Next:</b> Review " + weakestActivityName + " to reduce errors.";

        if (averageScore >= 50)
            return "<b>Next:</b> Practice " + weakestActivityName + " again.";

        return "<b>Next:</b> Repeat " + weakestActivityName + " slowly.";
    }

    private void UpdateAllDaysButtonVisibility()
    {
        if (allDaysButton == null) return;

        int currentDay = DayManager.Instance != null ? DayManager.Instance.CurrentDay : 1;

        bool hasSavedHistory =
            DailyResultsManager.Instance != null &&
            DailyResultsManager.Instance.GetSavedHistory() != null &&
            DailyResultsManager.Instance.GetSavedHistory().Count > 0;

        bool shouldShow = hasSavedHistory || currentDay > 1;

        allDaysButton.SetActive(shouldShow);
    }
    private void UpdateCharts(List<SummaryData> summary)
    {
        UpdateScoreBarChart(summary);
        UpdateQualityPieChart(summary);
        UpdateRevenueBarChart(summary);
    }

    private void UpdateScoreBarChart(List<SummaryData> summary)
    {
        if (scoreBarChart == null) return;

        ClearChartSeries(scoreBarChart);

        SetChartTitle(scoreBarChart, "Score by Activity (%)");
        SetLegend(scoreBarChart, false);
        SetPercentageYAxis(scoreBarChart);

        scoreBarChart.AddSerie<Bar>("Score");

        if (summary == null || summary.Count == 0)
        {
            scoreBarChart.RefreshChart();
            UpdateKpiCards(0, 0, 0, "-");
            return;
        }

        foreach (SummaryData item in summary)
        {
            string activityName = GetFullActivityName(item.name);
            int score = Mathf.Clamp(item.AverageScore, 0, 100);

            scoreBarChart.AddXAxisData(activityName);

            scoreBarChart.AddData(
                0,
                score,
                activityName + ": " + score + "%"
            );

            Debug.Log("Daily Score Chart -> " + activityName + ": " + score + "%");
        }

        scoreBarChart.RefreshChart();
    }

    private void UpdateQualityPieChart(List<SummaryData> summary)
    {
        if (errorsPieChart == null) return;

        ClearChartSeries(errorsPieChart);

        SetChartTitle(errorsPieChart, "Daily Quality");
        SetLegend(errorsPieChart, true);

        errorsPieChart.AddSerie<Pie>("Result");
        SetSerieName(errorsPieChart, 0, "Result");

        if (summary == null || summary.Count == 0)
        {
            errorsPieChart.RefreshChart();
            return;
        }

        int totalScore = 0;

        foreach (SummaryData item in summary)
        {
            totalScore += item.AverageScore;
        }

        int averageScore = Mathf.RoundToInt(totalScore / (float)summary.Count);
        int improvement = Mathf.Clamp(100 - averageScore, 0, 100);

        errorsPieChart.AddData(0, averageScore, "Achieved " + averageScore + "%");
        errorsPieChart.AddData(0, improvement, "Needs improvement " + improvement + "%");

        errorsPieChart.RefreshChart();
    }

    private void UpdateRevenueBarChart(List<SummaryData> summary)
    {
        if (revenueBarChart == null) return;

        ClearChartSeries(revenueBarChart);

        SetChartTitle(revenueBarChart, "Revenue by Activity ($)");
        SetLegend(revenueBarChart, false);

        revenueBarChart.AddSerie<Bar>("Revenue");

        if (summary == null || summary.Count == 0)
        {
            revenueBarChart.RefreshChart();
            return;
        }

        foreach (SummaryData item in summary)
        {
            string activityName = GetFullActivityName(item.name);

            revenueBarChart.AddXAxisData(activityName);

            revenueBarChart.AddData(
                0,
                item.totalRevenue,
                activityName + ": $" + item.totalRevenue
            );
        }

        revenueBarChart.RefreshChart();
    }

    private void ClearChartSeries(BaseChart chart)
    {
        if (chart == null) return;

        chart.ClearData();

        while (chart.GetSerie(0) != null)
        {
            chart.RemoveSerie(0);
        }
    }

    public void OpenAllDaysDashboard()
    {
        StartCoroutine(OpenAllDaysDashboardRoutine());
    }

    private IEnumerator OpenAllDaysDashboardRoutine()
    {
        bool wasPaused = Time.timeScale == 0f;

        if (wasPaused)
            Time.timeScale = 1f;

        if (summaryPanel != null)
            summaryPanel.SetActive(false);

        if (allDaysDashboardPanel != null)
            allDaysDashboardPanel.SetActive(true);

        List<MiniGameResultData> allResults = DailyResultsManager.Instance != null
            ? DailyResultsManager.Instance.GetSavedHistory()
            : new List<MiniGameResultData>();

        List<DaySummaryData> daySummary = BuildAllDaysSummary(allResults);

        UpdateAllDaysTexts(daySummary);
        UpdateAllDaysCharts(daySummary);

        Canvas.ForceUpdateCanvases();

        yield return null;
        yield return null;

        Canvas.ForceUpdateCanvases();

        yield return new WaitForSecondsRealtime(1.2f);

        if (wasPaused)
            Time.timeScale = 0f;
    }

    public void BackToDailySummary()
    {
        if (allDaysDashboardPanel != null)
            allDaysDashboardPanel.SetActive(false);

        if (summaryPanel != null)
            summaryPanel.SetActive(true);
    }

    private List<DaySummaryData> BuildAllDaysSummary(List<MiniGameResultData> allResults)
    {
        Dictionary<int, DaySummaryData> data = new Dictionary<int, DaySummaryData>();

        int currentDay = DayManager.Instance != null ? DayManager.Instance.CurrentDay : 1;

        int lastFinishedDay = Mathf.Max(0, currentDay - 1);

        int maxDay = lastFinishedDay;

        if (allResults != null)
        {
            foreach (MiniGameResultData result in allResults)
            {
                if (result == null) continue;

                if (result.day > maxDay)
                    maxDay = result.day;
            }
        }

        if (maxDay <= 0)
            return new List<DaySummaryData>();

        for (int day = 1; day <= maxDay; day++)
        {
            data[day] = new DaySummaryData
            {
                day = day,
                count = 0,
                totalScore = 0,
                totalRevenue = 0,
                totalErrors = 0
            };
        }

        if (allResults != null)
        {
            foreach (MiniGameResultData result in allResults)
            {
                if (result == null) continue;

                int day = result.day;

                if (!data.ContainsKey(day))
                {
                    data[day] = new DaySummaryData
                    {
                        day = day,
                        count = 0,
                        totalScore = 0,
                        totalRevenue = 0,
                        totalErrors = 0
                    };
                }

                data[day].count++;
                data[day].totalScore += result.finalScore;
                data[day].totalRevenue += result.revenue;
                data[day].totalErrors += result.errors;
            }
        }

        List<DaySummaryData> list = new List<DaySummaryData>(data.Values);
        list.Sort((a, b) => a.day.CompareTo(b.day));

        return list;
    }

    private void UpdateAllDaysTexts(List<DaySummaryData> daySummary)
    {
        if (allDaysTitleText != null)
            allDaysTitleText.text = "Historical Dashboard";

        if (daySummary == null || daySummary.Count == 0)
        {
            if (allDaysGeneralText != null)
                allDaysGeneralText.text = "No days recorded.";

            return;
        }

        List<PeriodSummaryData> periodSummary = BuildDashboardPeriodSummary(daySummary);

        bool showingByMonths = daySummary.Count > MaxDailyDashboardDays;

        int totalScore = 0;
        int totalRevenue = 0;
        int totalErrors = 0;
        int totalActivities = 0;

        foreach (PeriodSummaryData period in periodSummary)
        {
            totalScore += period.AverageScore;
            totalRevenue += period.totalRevenue;
            totalErrors += period.totalErrors;
            totalActivities += period.totalActivities;
        }

        int averageScore = Mathf.RoundToInt(totalScore / (float)periodSummary.Count);

        string modeText = showingByMonths
            ? "Grouped by game months."
            : "Daily view.";

        if (allDaysGeneralText != null)
        {
            allDaysGeneralText.text =
                "<b>HISTORICAL SUMMARY</b>\n" +
                modeText + "\n" +
                "Recorded days: " + daySummary.Count + "\n" +
                "Average performance: " + averageScore + "%\n" +
                "Total revenue: $" + totalRevenue + "\n" +
                "Total errors: " + totalErrors + "\n" +
                "Total activities: " + totalActivities;
        }
    }

    private void UpdateAllDaysCharts(List<DaySummaryData> daySummary)
    {
        List<PeriodSummaryData> periodSummary = BuildDashboardPeriodSummary(daySummary);

        UpdateAllDaysKpiChart(periodSummary);
        UpdateAllDaysRevenueChart(periodSummary);
        UpdateAllDaysErrorsChart(periodSummary);
    }

    private void UpdateAllDaysKpiChart(List<PeriodSummaryData> periodSummary)
    {
        if (allDaysKpiLineChart == null) return;

        ClearChartSeries(allDaysKpiLineChart);

        SetChartTitle(allDaysKpiLineChart, "Performance Trend (%)");
        SetLegend(allDaysKpiLineChart, false);
        SetPercentageYAxis(allDaysKpiLineChart);

        allDaysKpiLineChart.AddSerie<Line>("Performance");

        if (periodSummary == null || periodSummary.Count == 0)
        {
            allDaysKpiLineChart.RefreshChart();
            return;
        }

        foreach (PeriodSummaryData period in periodSummary)
        {
            allDaysKpiLineChart.AddXAxisData(period.label);

            allDaysKpiLineChart.AddData(
                0,
                period.AverageScore,
                period.label + ": " + period.AverageScore + "%"
            );
        }

        allDaysKpiLineChart.RefreshChart();
    }

    private void UpdateAllDaysRevenueChart(List<PeriodSummaryData> periodSummary)
    {
        if (allDaysRevenueBarChart == null) return;

        ClearChartSeries(allDaysRevenueBarChart);

        SetChartTitle(allDaysRevenueBarChart, "Revenue");
        SetLegend(allDaysRevenueBarChart, false);

        allDaysRevenueBarChart.AddSerie<Bar>("Revenue");

        if (periodSummary == null || periodSummary.Count == 0)
        {
            allDaysRevenueBarChart.RefreshChart();
            return;
        }

        foreach (PeriodSummaryData period in periodSummary)
        {
            allDaysRevenueBarChart.AddXAxisData(period.label);

            allDaysRevenueBarChart.AddData(
                0,
                period.totalRevenue,
                period.label + " - Revenue: $" + period.totalRevenue
            );
        }

        allDaysRevenueBarChart.RefreshChart();
    }

    private void UpdateAllDaysErrorsChart(List<PeriodSummaryData> periodSummary)
    {
        if (allDaysErrorsBarChart == null) return;

        ClearChartSeries(allDaysErrorsBarChart);

        SetChartTitle(allDaysErrorsBarChart, "Errors");
        SetLegend(allDaysErrorsBarChart, false);

        allDaysErrorsBarChart.AddSerie<Bar>("Errors");

        if (periodSummary == null || periodSummary.Count == 0)
        {
            allDaysErrorsBarChart.RefreshChart();
            return;
        }

        foreach (PeriodSummaryData period in periodSummary)
        {
            allDaysErrorsBarChart.AddXAxisData(period.label);

            allDaysErrorsBarChart.AddData(
                0,
                period.totalErrors,
                period.label + " - Errors: " + period.totalErrors
            );
        }

        allDaysErrorsBarChart.RefreshChart();
    }


    private List<PeriodSummaryData> BuildDashboardPeriodSummary(List<DaySummaryData> daySummary)
    {
        List<PeriodSummaryData> periodSummary = new List<PeriodSummaryData>();

        if (daySummary == null || daySummary.Count == 0)
            return periodSummary;

        bool showByMonths = daySummary.Count > MaxDailyDashboardDays;

        if (!showByMonths)
        {
            foreach (DaySummaryData day in daySummary)
            {
                periodSummary.Add(new PeriodSummaryData
                {
                    label = "Day " + day.day,
                    daysCount = 1,
                    totalActivities = day.count,
                    totalRevenue = day.totalRevenue,
                    totalErrors = day.totalErrors,
                    totalDailyAverageScore = day.AverageScore
                });
            }

            return periodSummary;
        }

        Dictionary<int, PeriodSummaryData> months = new Dictionary<int, PeriodSummaryData>();

        foreach (DaySummaryData day in daySummary)
        {
            int monthNumber = ((day.day - 1) / DaysPerGameMonth) + 1;

            if (!months.ContainsKey(monthNumber))
            {
                int startDay = ((monthNumber - 1) * DaysPerGameMonth) + 1;
                int endDay = monthNumber * DaysPerGameMonth;

                months[monthNumber] = new PeriodSummaryData
                {
                    label = "Month " + monthNumber + "\nDay " + startDay + "-" + endDay,
                    daysCount = 0,
                    totalActivities = 0,
                    totalRevenue = 0,
                    totalErrors = 0,
                    totalDailyAverageScore = 0
                };
            }

            months[monthNumber].daysCount++;
            months[monthNumber].totalActivities += day.count;
            months[monthNumber].totalRevenue += day.totalRevenue;
            months[monthNumber].totalErrors += day.totalErrors;
            months[monthNumber].totalDailyAverageScore += day.AverageScore;
        }

        List<int> keys = new List<int>(months.Keys);
        keys.Sort();

        foreach (int key in keys)
        {
            periodSummary.Add(months[key]);
        }

        return periodSummary;
    }

    private string GetFullActivityName(string minigameName)
    {
        switch (minigameName)
        {
            case "Check-in":
                return "Check-in";

            case "Habitación":
            case "Room":
                return "Room";

            case "Restaurante":
            case "Restaurant":
                return "Restaurant";

            default:
                return minigameName;
        }
    }

    private void SetChartTitle(BaseChart chart, string text)
    {
        if (chart == null) return;

        Title title = chart.EnsureChartComponent<Title>();

        if (title != null)
        {
            title.show = true;
            title.text = text;
        }
    }

    private void SetLegend(BaseChart chart, bool show)
    {
        if (chart == null) return;

        Legend legend = chart.EnsureChartComponent<Legend>();

        if (legend != null)
            legend.show = show;
    }

    private void SetPercentageYAxis(BaseChart chart)
    {
        if (chart == null)
            return;

        YAxis yAxis = chart.EnsureChartComponent<YAxis>();

        if (yAxis != null)
        {
            yAxis.minMaxType = Axis.AxisMinMaxType.Custom;
            yAxis.min = 0;
            yAxis.max = 100;
            yAxis.splitNumber = 5;
        }
    }

    private void SetSerieName(BaseChart chart, int index, string serieName)
    {
        if (chart == null) return;

        Serie serie = chart.GetSerie(index);

        if (serie != null)
            serie.serieName = serieName;
    }

    public void ContinueToNextDay()
    {
        if (summaryPanel != null)
            summaryPanel.SetActive(false);

        if (allDaysDashboardPanel != null)
            allDaysDashboardPanel.SetActive(false);

        if (DayManager.Instance != null)
            DayManager.Instance.EndDay();

        HotelGamePause.ReleasePause();
    }

    private void OnDisable()
    {
        if ((summaryPanel != null && summaryPanel.activeSelf) ||
            (allDaysDashboardPanel != null && allDaysDashboardPanel.activeSelf))
        {
            HotelGamePause.ReleasePause();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if ((summaryPanel != null && summaryPanel.activeSelf) ||
            (allDaysDashboardPanel != null && allDaysDashboardPanel.activeSelf))
        {
            HotelGamePause.ReleasePause();
        }
    }
}