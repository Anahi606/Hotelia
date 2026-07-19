using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    [Header("Levels To Load")]
    public string _newGameLevel = "06 - Character";
    [SerializeField] private string teacherDashboardScene = "07 - TeacherDashboard";

    private string levelToLoad;

    [SerializeField] private GameObject noSavedGameDialog = null;

    public void NewGameDialogYes()
    {
        if (PlayfabManager.IsTeacher)
        {
            Debug.LogWarning("Teacher accounts cannot start a new game.");
            return;
        }

        HotelSaveSystem.DeleteSave();

        CheckInTutorialBookUI.ResetForNewGame();
        RestaurantTutorialBookUI.ResetForNewGame();
        RoomCleaningTutorialBookUI.ResetForNewGame();

        if (DayManager.Instance != null)
        {
            DayManager.Instance.ResetDays();
        }

        SceneManager.LoadScene(_newGameLevel);
    }

    public void LoadGameDialogYes()
    {
        if (PlayfabManager.IsTeacher)
        {
            Debug.LogWarning("Teacher accounts cannot continue a game.");
            return;
        }

        if (PlayfabManager.IsLoggedInWithEmail)
        {
            PlayfabCloudSaveManager.SyncBestSaveBetweenLocalAndPlayFab(saveData =>
            {
                if (saveData != null && saveData.hasStartedGame && !string.IsNullOrEmpty(saveData.savedSceneName))
                {
                    levelToLoad = saveData.savedSceneName;
                    SceneManager.LoadScene(levelToLoad);
                }
                else
                {
                    ShowNoSavedGameDialog();
                }
            });

            return;
        }

        HotelSaveData localSave = HotelSaveSystem.LoadGame();

        if (localSave != null && localSave.hasStartedGame && !string.IsNullOrEmpty(localSave.savedSceneName))
        {
            levelToLoad = localSave.savedSceneName;
            SceneManager.LoadScene(levelToLoad);
        }
        else
        {
            ShowNoSavedGameDialog();
        }
    }

    public void OpenTeacherDashboard()
    {
        if (!PlayfabManager.IsLoggedInWithEmail)
        {
            Debug.LogWarning("You must log in first.");
            return;
        }

        if (!PlayfabManager.IsTeacher)
        {
            Debug.LogWarning("Only teacher accounts can access the dashboard.");
            return;
        }

        SceneManager.LoadScene(teacherDashboardScene);
    }

    private void ShowNoSavedGameDialog()
    {
        if (noSavedGameDialog != null)
        {
            noSavedGameDialog.SetActive(true);
        }
        else
        {
            Debug.LogWarning("There is no save and noSavedGameDialog is not assigned.");
        }
    }

    public void ExitButton()
    {
        Application.Quit();
    }
}