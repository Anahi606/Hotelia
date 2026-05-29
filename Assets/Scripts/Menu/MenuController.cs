using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    [Header("Levels To Load")]
    public string _newGameLevel = "06 - Character";

    private string levelToLoad;

    [SerializeField] private GameObject noSavedGameDialog = null;

    public void NewGameDialogYes()
    {
        if (HotelSaveSystem.HasSave())
        {
            HotelSaveSystem.DeleteSave();
        }

        SceneManager.LoadScene(_newGameLevel);
    }

    public void LoadGameDialogYes()
    {
        HotelSaveData saveData = HotelSaveSystem.LoadGame();

        if (saveData != null && saveData.hasStartedGame && !string.IsNullOrEmpty(saveData.savedSceneName))
        {
            levelToLoad = saveData.savedSceneName;
            SceneManager.LoadScene(levelToLoad);
        }
        else
        {
            if (noSavedGameDialog != null)
            {
                noSavedGameDialog.SetActive(true);
            }
            else
            {
                Debug.LogWarning("No hay partida guardada y noSavedGameDialog no está asignado.");
            }
        }
    }

    public void ExitButton()
    {
        Application.Quit();
    }
}