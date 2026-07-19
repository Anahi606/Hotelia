using TMPro;
using UnityEngine;
using Hotelia.Core;

public class RoomCleaningKPIManager : MonoBehaviour
{
    public static RoomCleaningKPIManager Instance { get; private set; }
    private bool setupAlreadyExecuted;

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

    public void SetupCleaningRoom(
    bool isDirty,
    TrashSpawner spawner,
    BedSpot[] activeBeds
)
    {
        if (setupAlreadyExecuted)
        {
            Debug.LogWarning(
                "SetupCleaningRoom intentó ejecutarse más de una vez. " +
                "Se ignoró la nueva inicialización."
            );

            return;
        }

        setupAlreadyExecuted = true;

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

        minigameActive =
            CleaningRules.CanStartCleaning(roomIsDirty);

        if (!roomIsDirty)
        {
            if (timerText != null)
                timerText.text = "";
        }
        else
        {
            UpdateTimerUI();
        }

        Debug.Log(
            "Minijuego limpieza configurado. Sucia: " +
            roomIsDirty +
            " | Camas: " + totalBeds +
            " | Basura: " + totalTrash
        );
    }

    private void SetupBeds()
    {
        if (beds == null) return;

        foreach (BedSpot bed in beds)
        {
            if (bed == null) continue;

            if (roomIsDirty)
            {
                bed.SetBedCompletedVisual(false);
            }
            else
            {
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
        if (!minigameActive || resultsShown)
            return;

        cleanedTrash++;

        if (cleanedTrash > totalTrash)
            cleanedTrash = totalTrash;

        RestoreCompletedBedVisuals();

        CheckIfRoomFinished();
    }

    private void RestoreCompletedBedVisuals()
    {
        if (beds == null)
            return;

        foreach (BedSpot bed in beds)
        {
            if (bed != null)
                bed.EnsureCompletedVisuals();
        }
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
        if (resultsShown)
            return;

        minigameActive = false;
        const bool completedEverything = false;

        roomIsDirty = true;
        RoomCleaningSession.selectedNeedsCleaning = true;

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
            Debug.LogWarning(
                "No existe HotelGameData. No se pudo actualizar la habitación."
            );

            return;
        }

        RoomRuntimeData room =
            HotelGameData.Instance.GetRoomById(
                RoomCleaningSession.selectedRoomId
            );

        if (room == null)
        {
            Debug.LogWarning(
                "No se encontró la habitación " +
                RoomCleaningSession.selectedRoomId +
                " en HotelGameData."
            );

            return;
        }

        CleaningRoomOutcome outcome =
            CleaningRules.ResolveRoomOutcome(
                completedEverything,
                RoomCleaningSession.selectedReservationStillActive
            );

        switch (outcome)
        {
            case CleaningRoomOutcome.Available:
                room.needsCleaning = false;
                room.state = RoomState.Available;

                Debug.Log(
                    "Habitación " + room.roomId +
                    " quedó limpia y libre."
                );
                break;

            case CleaningRoomOutcome.Occupied:
                room.needsCleaning = false;
                room.state = RoomState.Occupied;

                Debug.Log(
                    "Habitación " + room.roomId +
                    " quedó limpia, pero sigue ocupada."
                );
                break;

            case CleaningRoomOutcome.OccupiedNeedsCleaning:
                room.needsCleaning = true;
                room.state = RoomState.Occupied;

                Debug.Log(
                    "Habitación " + room.roomId +
                    " sigue ocupada y con limpieza pendiente."
                );
                break;

            case CleaningRoomOutcome.Dirty:
                room.needsCleaning = true;
                room.state = RoomState.Dirty;

                Debug.Log(
                    "Habitación " + room.roomId +
                    " sigue sucia."
                );
                break;

            default:
                Debug.LogWarning(
                    "Resultado de limpieza no reconocido para la habitación " +
                    room.roomId
                );
                break;
        }
    }

    private void ShowResults(bool completedEverything)
    {
        if (resultsShown) return;

        resultsShown = true;
        minigameActive = false;

        int remainingTrash =
            trashSpawner != null
                ? trashSpawner.GetRemainingTrash()
                : 0;

        CleaningProgressResult evaluation =
            CleaningRules.EvaluateProgress(
                totalTrash: totalTrash,
                remainingTrash: remainingTrash,
                totalBeds: totalBeds,
                madeBeds: madeBeds,
                currentTime: currentTime,
                totalTime: totalTime
            );

        cleanedTrash = evaluation.CleanedTrash;

        int remainingBeds = evaluation.RemainingBeds;
        int trashErrors = evaluation.RemainingTrash;
        int bedErrors = evaluation.RemainingBeds;
        int totalErrors = evaluation.TotalErrors;

        int trashScore = evaluation.TrashScore;
        int bedScore = evaluation.BedScore;
        int timeScore = evaluation.TimeScore;
        int finalScore = evaluation.FinalScore;

        RegisterDailyRoomCleaningResult(
            completedEverything,
            totalErrors,
            timeScore,
            finalScore
        );

        RoomCleaningTutorialBookUI.MarkAsCompleted();

        if (resultPanel != null)
            resultPanel.SetActive(true);

        if (titleText != null)
            titleText.text = completedEverything ? "Room cleaned" : "Incomplete cleaning";

        if (trashText != null)
            trashText.text = "Trash collected: " + cleanedTrash + "/" + totalTrash + " (" + trashScore + "%)";

        if (bedsText != null)
            bedsText.text = "Beds made: " + madeBeds + "/" + totalBeds + " (" + bedScore + "%)";

        if (errorsText != null)
        {
            errorsText.text =
                "Errors: " + totalErrors +
                "\nPending trash: " + trashErrors +
                "\nUnmade beds: " + bedErrors;
        }

        if (scoreText != null)
            scoreText.text = "Cleaning score: " + finalScore + "%";

        if (feedbackText != null)
        {
            if (completedEverything)
            {
                feedbackText.text = "Excellent. The room is ready for the next guest.";
            }
            else if (finalScore >= 70)
            {
                feedbackText.text = "Good attempt, but some details are still pending.";
            }
            else
            {
                feedbackText.text = "The room is not ready. You still need to collect trash or make the beds.";
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
        result.minigameName = "Room";

        result.satisfaction = finalScore;
        result.revenue = 0;
        result.errors = totalErrors;
        result.timeScore = timeScore;
        result.finalScore = finalScore;

        result.stpSummary =
            "Post-stay service: cleaning, order, and room preparation for the next guest.";

        result.feedback = completedEverything
            ? "The room is ready for the next guest."
            : "Some cleaning or bed-making tasks are still pending.";

        DailyResultsManager.Instance.RegisterResult(result);
    }

    private void UpdateTimerUI()
    {
        if (timerText == null) return;

        int seconds = Mathf.CeilToInt(currentTime);
        timerText.text = seconds.ToString() + "s";
    }

    public void CloseResultPanel()
    {
        if (resultPanel != null)
            resultPanel.SetActive(false);
    }
}