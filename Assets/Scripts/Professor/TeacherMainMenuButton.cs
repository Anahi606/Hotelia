using UnityEngine;
using UnityEngine.SceneManagement;

public class TeacherMainMenuButton : MonoBehaviour
{
    private const string MainMenuSceneName = "01-Menu";

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;

        if (Application.CanStreamedLevelBeLoaded(MainMenuSceneName))
        {
            SceneManager.LoadScene(MainMenuSceneName);
        }
        else
        {
            Debug.LogError(
                $"No se encontró la escena '{MainMenuSceneName}'. " +
                "Verifica que esté incluida en Build Profiles > Scene List."
            );
        }
    }
}