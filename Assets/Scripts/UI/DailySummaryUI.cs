using System.Collections.Generic;
using TMPro;
using UnityEngine;
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
        HotelGamePause.RequestPause();

        if (summaryPanel != null)
            summaryPanel.SetActive(true);

        if (allDaysDashboardPanel != null)
            allDaysDashboardPanel.SetActive(false);

        List<MiniGameResultData> results = DailyResultsManager.Instance != null
            ? DailyResultsManager.Instance.GetTodayResults()
            : new List<MiniGameResultData>();

        List<SummaryData> summary = BuildSummary(results);

        UpdateTexts(summary);
        UpdateCharts(summary);
        UpdateAllDaysButtonVisibility();
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

    private void UpdateTexts(List<SummaryData> summary)
    {
        int currentDay = DayManager.Instance != null ? DayManager.Instance.CurrentDay : 1;

        if (titleText != null)
            titleText.text = "Resumen del día " + currentDay;

        if (summary == null || summary.Count == 0)
        {
            if (generalSummaryText != null)
                generalSummaryText.text = "Sin resultados registrados.";

            if (stpText != null)
                stpText.text = "STP: sin datos.";

            if (feedbackText != null)
                feedbackText.text = "Realiza actividades para generar métricas.";

            return;
        }

        int totalScore = 0;
        int totalRevenue = 0;
        int totalErrors = 0;
        int totalActivities = 0;

        string stpSummary = "";
        string feedbackSummary = "";

        foreach (SummaryData item in summary)
        {
            totalScore += item.AverageScore;
            totalRevenue += item.totalRevenue;
            totalErrors += item.totalErrors;
            totalActivities += item.count;

            stpSummary += GetShortSTP(item) + "\n";
            feedbackSummary += GetShortFeedback(item) + "\n";
        }

        int averageScore = Mathf.RoundToInt(totalScore / (float)summary.Count);

        if (generalSummaryText != null)
        {
            generalSummaryText.text =
                "<b>KPI DEL DÍA</b>\n" +
                "Rendimiento general: " + averageScore + "%\n" +
                "Ingresos: $" + totalRevenue + "\n" +
                "Errores: " + totalErrors + "\n" +
                "Actividades: " + totalActivities;
        }

        if (stpText != null)
        {
            stpText.text =
                "<b>STP APLICADO</b>\n" +
                stpSummary.TrimEnd();
        }

        if (feedbackText != null)
        {
            feedbackText.text =
                "<b>FEEDBACK</b>\n" +
                feedbackSummary.TrimEnd();
        }
    }

    private string GetShortSTP(SummaryData item)
    {
        if (item == null) return "";

        switch (item.name)
        {
            case "Check-in":
                return "• Check-in: huésped + oferta.";

            case "Habitación":
                return "• Habitación: limpieza + orden.";

            case "Restaurante":
                return "• Restaurante: prioridad + servicio.";

            default:
                return "• " + item.name + ": gestión.";
        }
    }

    private string GetShortFeedback(SummaryData item)
    {
        if (item == null) return "";

        if (item.totalErrors == 0 && item.AverageScore >= 85)
            return "• " + item.name + ": excelente.";

        if (item.AverageScore >= 70)
            return "• " + item.name + ": bien, mejorar detalles.";

        if (item.AverageScore >= 50)
            return "• " + item.name + ": revisar decisiones.";

        return "• " + item.name + ": requiere mejora.";
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

        SetChartTitle(scoreBarChart, "Puntaje por actividad (%)");
        SetLegend(scoreBarChart, false);

        scoreBarChart.AddSerie<Bar>("Puntaje");

        if (summary == null || summary.Count == 0)
        {
            scoreBarChart.RefreshChart();
            return;
        }

        foreach (SummaryData item in summary)
        {
            string activityName = GetFullActivityName(item.name);

            scoreBarChart.AddXAxisData(activityName);

            scoreBarChart.AddData(
                0,
                item.AverageScore,
                activityName + ": " + item.AverageScore + "%"
            );
        }

        scoreBarChart.RefreshChart();
    }

    private void UpdateQualityPieChart(List<SummaryData> summary)
    {
        if (errorsPieChart == null) return;

        errorsPieChart.ClearData();

        SetChartTitle(errorsPieChart, "Calidad general del día");
        SetLegend(errorsPieChart, true);
        SetSerieName(errorsPieChart, 0, "Resultado");

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

        errorsPieChart.AddData(0, averageScore, "Logrado " + averageScore + "%");
        errorsPieChart.AddData(0, improvement, "Por mejorar " + improvement + "%");

        errorsPieChart.RefreshChart();
    }

    private void UpdateRevenueBarChart(List<SummaryData> summary)
    {
        if (revenueBarChart == null) return;

        ClearChartSeries(revenueBarChart);

        SetChartTitle(revenueBarChart, "Ingresos por actividad ($)");
        SetLegend(revenueBarChart, false);

        revenueBarChart.AddSerie<Bar>("Ingresos");

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
            allDaysTitleText.text = "Dashboard histórico";

        if (daySummary == null || daySummary.Count == 0)
        {
            if (allDaysGeneralText != null)
                allDaysGeneralText.text = "Sin días registrados.";

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
            ? "Vista agrupada por meses del juego."
            : "Vista diaria.";

        if (allDaysGeneralText != null)
        {
            allDaysGeneralText.text =
                "<b>RESUMEN HISTÓRICO</b>\n" +
                modeText + "\n" +
                "Días registrados: " + daySummary.Count + "\n" +
                "Rendimiento promedio: " + averageScore + "%\n" +
                "Ingresos acumulados: $" + totalRevenue + "\n" +
                "Errores acumulados: " + totalErrors + "\n" +
                "Actividades totales: " + totalActivities;
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

        SetChartTitle(allDaysKpiLineChart, "Evolución del rendimiento (%)");
        SetLegend(allDaysKpiLineChart, false);

        allDaysKpiLineChart.AddSerie<Line>("Rendimiento");

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

        SetChartTitle(allDaysRevenueBarChart, "Ingresos");
        SetLegend(allDaysRevenueBarChart, false);

        allDaysRevenueBarChart.AddSerie<Bar>("Ingresos");

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
                period.label + " - Ingresos: $" + period.totalRevenue
            );
        }

        allDaysRevenueBarChart.RefreshChart();
    }

    private void UpdateAllDaysErrorsChart(List<PeriodSummaryData> periodSummary)
    {
        if (allDaysErrorsBarChart == null) return;

        ClearChartSeries(allDaysErrorsBarChart);

        SetChartTitle(allDaysErrorsBarChart, "Errores");
        SetLegend(allDaysErrorsBarChart, false);

        allDaysErrorsBarChart.AddSerie<Bar>("Errores");

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
                period.label + " - Errores: " + period.totalErrors
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
                    label = "Día " + day.day,
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
                    label = "Mes " + monthNumber + "\nDía " + startDay + "-" + endDay,
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
                return "Habitación";

            case "Restaurante":
                return "Restaurante";

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

    private void SetSerieName(BaseChart chart, int index, string serieName)
    {
        if (chart == null) return;

        Serie serie = chart.GetSerie(index);

        if (serie != null)
            serie.serieName = serieName;
    }

    public void ContinueToNextDay()
    {
        if (DailyResultsManager.Instance != null)
        {
            DailyResultsManager.Instance.CommitTodayResults();
        }

        if (summaryPanel != null)
            summaryPanel.SetActive(false);

        if (allDaysDashboardPanel != null)
            allDaysDashboardPanel.SetActive(false);

        if (DayManager.Instance != null)
            DayManager.Instance.EndDay();

        if (DailyResultsManager.Instance != null)
            DailyResultsManager.Instance.ClearTodayResults();

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