using TMPro;
using UnityEngine;

public class RoomCleaningKPIManager : MonoBehaviour
{
    public static RoomCleaningKPIManager Instance { get; private set; }

    [Header("Timer")]
    [SerializeField] private float totalTime = 60f;
    [SerializeField] private TMP_Text timerText;

    [Header("Result Panel")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text trashText;
    [SerializeField] private TMP_Text bedsText;
    [SerializeField] private TMP_Text errorsText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text feedbackText;

    private TrashSpawner trashSpawner;
    private BedSpot[] beds;

    private bool roomIsDirty;
    private bool minigameActive;
    private bool resultsShown;
    private bool resultRegistered;

    private float currentTime;

    private int totalTrash;
    private int cleanedTrash;
    private int totalBeds;
    private int madeBeds;

    public bool IsRoomDirty => roomIsDirty;
    public bool IsMinigameActive => minigameActive;

    private void Awake()
    {
        Instance = this;

        if (resultPanel != null)
            resultPanel.SetActive(false);
    }

    private void Update()
    {
        if (!minigameActive || resultsShown) return;

        currentTime -= Time.deltaTime;

        if (currentTime < 0f)
            currentTime = 0f;

        UpdateTimerUI();

        if (currentTime <= 0f)
        {
            FinishByTime();
        }
    }

    public void SetupCleaningRoom(bool isDirty, TrashSpawner spawner, BedSpot[] activeBeds)
    {
        roomIsDirty = isDirty;
        trashSpawner = spawner;
        beds = activeBeds;

        minigameActive = false;
        resultsShown = false;
        resultRegistered = false;

        totalTrash = 0;
        cleanedTrash = 0;

        totalBeds = beds != null ? beds.Length : 0;
        madeBeds = 0;

        currentTime = totalTime;

        if (resultPanel != null)
            resultPanel.SetActive(false);

        SetupBeds();
        SetupTrash();

        minigameActive = roomIsDirty;

        if (!roomIsDirty)
        {
            if (timerText != null)
                timerText.text = "";
        }
        else
        {
            UpdateTimerUI();
        }

        Debug.Log("Minijuego limpieza configurado. Sucia: " + roomIsDirty +
                  " | Camas: " + totalBeds +
                  " | Basura: " + totalTrash);
    }

    private void SetupBeds()
    {
        if (beds == null) return;

        foreach (BedSpot bed in beds)
        {
            if (bed == null) continue;

            if (roomIsDirty)
            {
                // Habitación sucia: cama sin piezas y minijuego disponible.
                bed.SetBedCompletedVisual(false);
            }
            else
            {
                // Habitación ocupada/libre: cama ya tendida y sin minijuego.
                bed.SetBedCompletedVisual(true);
                madeBeds++;
            }
        }
    }

    private void SetupTrash()
    {
        if (trashSpawner == null) return;

        if (roomIsDirty)
        {
            trashSpawner.SpawnTrash();
            totalTrash = trashSpawner.TotalSpawnedTrash;
            cleanedTrash = 0;
        }
        else
        {
            trashSpawner.ClearTrash();
            totalTrash = 0;
            cleanedTrash = 0;
        }
    }

    public void RegisterTrashCleaned()
    {
        if (!minigameActive || resultsShown) return;

        cleanedTrash++;

        if (cleanedTrash > totalTrash)
            cleanedTrash = totalTrash;

        CheckIfRoomFinished();
    }

    public void RegisterBedMade()
    {
        if (!minigameActive || resultsShown) return;

        madeBeds++;

        if (madeBeds > totalBeds)
            madeBeds = totalBeds;

        CheckIfRoomFinished();
    }

    private void CheckIfRoomFinished()
    {
        bool allTrashCleaned = trashSpawner == null || trashSpawner.GetRemainingTrash() <= 0;
        bool allBedsMade = madeBeds >= totalBeds;

        if (allTrashCleaned && allBedsMade)
        {
            FinishSuccessfully();
        }
    }

    private void FinishSuccessfully()
    {
        minigameActive = false;
        roomIsDirty = false;

        RoomCleaningSession.selectedNeedsCleaning = false;

        UpdateRoomRuntimeData(true);

        ShowResults(true);
    }

    private void FinishByTime()
    {
        if (resultsShown) return;

        minigameActive = false;

        bool allTrashCleaned = trashSpawner == null || trashSpawner.GetRemainingTrash() <= 0;
        bool allBedsMade = madeBeds >= totalBeds;

        bool completedEverything = allTrashCleaned && allBedsMade;

        roomIsDirty = !completedEverything;
        RoomCleaningSession.selectedNeedsCleaning = !completedEverything;

        UpdateRoomRuntimeData(completedEverything);

        ShowResults(completedEverything);
    }

    public void FinishCleaningManually()
    {
        if (resultsShown) return;

        minigameActive = false;

        bool allTrashCleaned = trashSpawner == null || trashSpawner.GetRemainingTrash() <= 0;
        bool allBedsMade = madeBeds >= totalBeds;

        bool completedEverything = allTrashCleaned && allBedsMade;

        roomIsDirty = !completedEverything;
        RoomCleaningSession.selectedNeedsCleaning = !completedEverything;

        UpdateRoomRuntimeData(completedEverything);

        ShowResults(completedEverything);
    }

    private void UpdateRoomRuntimeData(bool completedEverything)
    {
        if (HotelGameData.Instance == null)
        {
            Debug.LogWarning("No existe HotelGameData. No se pudo actualizar la habitación.");
            return;
        }

        RoomRuntimeData room = HotelGameData.Instance.GetRoomById(RoomCleaningSession.selectedRoomId);

        if (room == null)
        {
            Debug.LogWarning("No se encontró la habitación " + RoomCleaningSession.selectedRoomId + " en HotelGameData.");
            return;
        }

        if (completedEverything)
        {
            room.state = RoomState.Libre;
            room.needsCleaning = false;

            Debug.Log("Habitación " + room.roomId + " quedó limpia y libre.");
        }
        else
        {
            room.state = RoomState.Sucia;
            room.needsCleaning = true;

            Debug.Log("Habitación " + room.roomId + " sigue sucia.");
        }
    }

    private void ShowResults(bool completedEverything)
    {
        if (resultsShown) return;

        resultsShown = true;
        minigameActive = false;

        int remainingTrash = trashSpawner != null ? trashSpawner.GetRemainingTrash() : 0;
        int remainingBeds = Mathf.Max(0, totalBeds - madeBeds);

        int trashErrors = remainingTrash;
        int bedErrors = remainingBeds;
        int totalErrors = trashErrors + bedErrors;

        int trashScore = totalTrash == 0 ? 100 : Mathf.RoundToInt((cleanedTrash / (float)totalTrash) * 100f);
        int bedScore = totalBeds == 0 ? 100 : Mathf.RoundToInt((madeBeds / (float)totalBeds) * 100f);
        int timeScore = totalTime <= 0 ? 0 : Mathf.RoundToInt((currentTime / totalTime) * 100f);

        trashScore = Mathf.Clamp(trashScore, 0, 100);
        bedScore = Mathf.Clamp(bedScore, 0, 100);
        timeScore = Mathf.Clamp(timeScore, 0, 100);

        int finalScore = Mathf.RoundToInt(
            (trashScore * 0.4f) +
            (bedScore * 0.4f) +
            (timeScore * 0.2f)
        );

        finalScore = Mathf.Clamp(finalScore, 0, 100);

        RegisterDailyRoomCleaningResult(
            completedEverything,
            totalErrors,
            timeScore,
            finalScore
        );

        if (resultPanel != null)
            resultPanel.SetActive(true);

        if (titleText != null)
            titleText.text = completedEverything ? "Habitación limpia" : "Limpieza incompleta";

        if (trashText != null)
            trashText.text = "Basura recogida: " + cleanedTrash + "/" + totalTrash + " (" + trashScore + "%)";

        if (bedsText != null)
            bedsText.text = "Camas tendidas: " + madeBeds + "/" + totalBeds + " (" + bedScore + "%)";

        if (errorsText != null)
        {
            errorsText.text =
                "Errores: " + totalErrors +
                "\nBasura pendiente: " + trashErrors +
                "\nCamas sin tender: " + bedErrors;
        }

        if (scoreText != null)
            scoreText.text = "Puntaje de limpieza: " + finalScore + "%";

        if (feedbackText != null)
        {
            if (completedEverything)
            {
                feedbackText.text = "Excelente. La habitación quedó lista para el siguiente huésped.";
            }
            else if (finalScore >= 70)
            {
                feedbackText.text = "Buen intento, pero quedaron detalles pendientes.";
            }
            else
            {
                feedbackText.text = "La habitación no quedó lista. Faltó recoger basura o tender camas.";
            }
        }
    }

    private void RegisterDailyRoomCleaningResult(bool completedEverything, int totalErrors, int timeScore, int finalScore)
    {
        if (resultRegistered) return;

        resultRegistered = true;

        if (DailyResultsManager.Instance == null)
        {
            Debug.LogWarning("No existe DailyResultsManager. No se pudo guardar el resultado de Habitación.");
            return;
        }

        MiniGameResultData result = new MiniGameResultData();

        result.day = DayManager.Instance != null ? DayManager.Instance.CurrentDay : 1;
        result.minigameName = "Habitación";

        result.satisfaction = finalScore;
        result.revenue = 0;
        result.errors = totalErrors;
        result.timeScore = timeScore;
        result.finalScore = finalScore;

        result.stpSummary =
            "Servicio post-estadía: limpieza, orden y preparación de la habitación para el siguiente huésped.";

        result.feedback = completedEverything
            ? "La habitación quedó lista para el siguiente huésped."
            : "Quedaron tareas pendientes de limpieza o cama.";

        DailyResultsManager.Instance.RegisterResult(result);
    }

    private void UpdateTimerUI()
    {
        if (timerText == null) return;

        int seconds = Mathf.CeilToInt(currentTime);
        timerText.text = seconds.ToString() + "s";
    }
}