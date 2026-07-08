using UnityEngine;
using UnityEngine.SceneManagement;

public class TeacherMainMenuButton : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string mainMenuSceneName = "Menu";

    public void GoToMainMenu()
    {
        // Por si el juego estaba pausado desde algún panel
        Time.timeScale = 1f;

        // Cargar escena del menú principal
        SceneManager.LoadScene(mainMenuSceneName);
    }
}