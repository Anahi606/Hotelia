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
    [SerializeField] private LineChart revenueLineChart;

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
                "Rendimiento general: " + averageScore + "%\n" +
                "Ingresos: $" + totalRevenue + "\n" +
                "Errores: " + totalErrors + "\n" +
                "Actividades: " + totalActivities;
        }

        if (stpText != null)
            stpText.text = stpSummary.TrimEnd();

        if (feedbackText != null)
            feedbackText.text = feedbackSummary.TrimEnd();
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

        bool hasSavedHistory =
            DailyResultsManager.Instance != null &&
            DailyResultsManager.Instance.GetSavedHistory() != null &&
            DailyResultsManager.Instance.GetSavedHistory().Count > 0;

        allDaysButton.SetActive(hasSavedHistory);
    }
    private void UpdateCharts(List<SummaryData> summary)
    {
        UpdateScoreBarChart(summary);
        UpdateQualityPieChart(summary);
        UpdateRevenueLineChart(summary);
    }

    private void UpdateScoreBarChart(List<SummaryData> summary)
    {
        if (scoreBarChart == null) return;

        ClearChartSeries(scoreBarChart);

        SetChartTitle(scoreBarChart, "Puntaje por actividad (%)");
        SetLegend(scoreBarChart, true);

        if (summary == null || summary.Count == 0)
        {
            scoreBarChart.RefreshChart();
            return;
        }

        foreach (SummaryData item in summary)
        {
            scoreBarChart.AddXAxisData(GetFullActivityName(item.name));
        }

        for (int i = 0; i < summary.Count; i++)
        {
            SummaryData item = summary[i];

            string activityName = GetFullActivityName(item.name);
            string serieName = activityName + " (" + item.AverageScore + "%)";

            scoreBarChart.AddSerie<Bar>(serieName);

            for (int j = 0; j < summary.Count; j++)
            {
                int value = i == j ? item.AverageScore : 0;
                scoreBarChart.AddData(i, value, activityName + ": " + item.AverageScore + "%");
            }
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

    private void UpdateRevenueLineChart(List<SummaryData> summary)
    {
        if (revenueLineChart == null) return;

        ClearChartSeries(revenueLineChart);

        SetChartTitle(revenueLineChart, "Ingresos por actividad ($)");
        SetLegend(revenueLineChart, true);

        if (summary == null || summary.Count == 0)
        {
            revenueLineChart.RefreshChart();
            return;
        }

        foreach (SummaryData item in summary)
        {
            revenueLineChart.AddXAxisData(GetFullActivityName(item.name));
        }

        for (int i = 0; i < summary.Count; i++)
        {
            SummaryData item = summary[i];

            string activityName = GetFullActivityName(item.name);
            string serieName = activityName + " ($" + item.totalRevenue + ")";

            revenueLineChart.AddSerie<Line>(serieName);

            for (int j = 0; j < summary.Count; j++)
            {
                int value = i == j ? item.totalRevenue : 0;
                revenueLineChart.AddData(i, value, activityName + ": $" + item.totalRevenue);
            }
        }

        revenueLineChart.RefreshChart();
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

        if (allResults == null)
            return new List<DaySummaryData>();

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
                allDaysGeneralText.text = "Sin datos históricos.";

            return;
        }

        int totalScore = 0;
        int totalRevenue = 0;
        int totalErrors = 0;
        int totalActivities = 0;

        foreach (DaySummaryData day in daySummary)
        {
            totalScore += day.AverageScore;
            totalRevenue += day.totalRevenue;
            totalErrors += day.totalErrors;
            totalActivities += day.count;
        }

        int averageScore = Mathf.RoundToInt(totalScore / (float)daySummary.Count);

        if (allDaysGeneralText != null)
        {
            allDaysGeneralText.text =
                "Días registrados: " + daySummary.Count + "\n" +
                "Rendimiento promedio: " + averageScore + "%\n" +
                "Ingresos acumulados: $" + totalRevenue + "\n" +
                "Errores acumulados: " + totalErrors + "\n" +
                "Actividades totales: " + totalActivities;
        }
    }

    private void UpdateAllDaysCharts(List<DaySummaryData> daySummary)
    {
        UpdateAllDaysKpiChart(daySummary);
        UpdateAllDaysRevenueChart(daySummary);
        UpdateAllDaysErrorsChart(daySummary);
    }

    private void UpdateAllDaysKpiChart(List<DaySummaryData> daySummary)
    {
        if (allDaysKpiLineChart == null) return;

        allDaysKpiLineChart.ClearData();

        SetChartTitle(allDaysKpiLineChart, "Evolución del rendimiento (%)");
        SetLegend(allDaysKpiLineChart, false);
        SetSerieName(allDaysKpiLineChart, 0, "Rendimiento");

        foreach (DaySummaryData day in daySummary)
        {
            string label = "Día " + day.day;
            allDaysKpiLineChart.AddXAxisData(label);
            allDaysKpiLineChart.AddData(0, day.AverageScore, label + ": " + day.AverageScore + "%");
        }

        allDaysKpiLineChart.RefreshChart();
    }

    private void UpdateAllDaysRevenueChart(List<DaySummaryData> daySummary)
    {
        if (allDaysRevenueBarChart == null) return;

        allDaysRevenueBarChart.ClearData();

        SetChartTitle(allDaysRevenueBarChart, "Ingresos por día ($)");
        SetLegend(allDaysRevenueBarChart, false);
        SetSerieName(allDaysRevenueBarChart, 0, "Ingresos");

        foreach (DaySummaryData day in daySummary)
        {
            string label = "Día " + day.day;
            allDaysRevenueBarChart.AddXAxisData(label);
            allDaysRevenueBarChart.AddData(0, day.totalRevenue, label + ": $" + day.totalRevenue);
        }

        allDaysRevenueBarChart.RefreshChart();
    }

    private void UpdateAllDaysErrorsChart(List<DaySummaryData> daySummary)
    {
        if (allDaysErrorsBarChart == null) return;

        allDaysErrorsBarChart.ClearData();

        SetChartTitle(allDaysErrorsBarChart, "Errores por día");
        SetLegend(allDaysErrorsBarChart, false);
        SetSerieName(allDaysErrorsBarChart, 0, "Errores");

        foreach (DaySummaryData day in daySummary)
        {
            string label = "Día " + day.day;
            allDaysErrorsBarChart.AddXAxisData(label);
            allDaysErrorsBarChart.AddData(0, day.totalErrors, label + ": " + day.totalErrors + " errores");
        }

        allDaysErrorsBarChart.RefreshChart();
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
    }
}