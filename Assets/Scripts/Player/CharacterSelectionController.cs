using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class CharacterSelectionController : MonoBehaviour
{
    [Header("Scene To Load After Selection")]
    [SerializeField] private string firstGameSceneName = "02 - Hotel";

    [Header("UI Buttons")]
    [SerializeField] private Button maleButton;
    [SerializeField] private Button femaleButton;
    [SerializeField] private Button confirmButton;

    /*[Header("Optional Selection Visual")]
    [SerializeField] private GameObject maleSelectedFrame;
    [SerializeField] private GameObject femaleSelectedFrame;

    [Header("Optional Text")]
    [SerializeField] private TMP_Text selectedText;*/

    private PlayerCharacterType selectedCharacter = PlayerCharacterType.None;

    private void Start()
    {
        UpdateSelectionUI();
    }

    public void SelectMale()
    {
        selectedCharacter = PlayerCharacterType.Male;
        UpdateSelectionUI();
    }

    public void SelectFemale()
    {
        selectedCharacter = PlayerCharacterType.Female;
        UpdateSelectionUI();
    }

    public void ConfirmSelection()
    {
        if (selectedCharacter == PlayerCharacterType.None)
        {
            Debug.LogWarning("Debes seleccionar un personaje antes de continuar.");

            /*if (selectedText != null)
                selectedText.text = "Selecciona un personaje primero.";*/

            return;
        }

        EnsureHotelGameDataExists();

        HotelGameData.Instance.selectedCharacter = selectedCharacter;

        HotelSaveSystem.SaveNewCharacter(selectedCharacter, firstGameSceneName);

        SceneManager.LoadScene(firstGameSceneName);
    }

    private void UpdateSelectionUI()
    {
        /*if (maleSelectedFrame != null)
            maleSelectedFrame.SetActive(selectedCharacter == PlayerCharacterType.Male);

        if (femaleSelectedFrame != null)
            femaleSelectedFrame.SetActive(selectedCharacter == PlayerCharacterType.Female);*/

        if (confirmButton != null)
            confirmButton.interactable = selectedCharacter != PlayerCharacterType.None;

        /*if (selectedText != null)
        {
            switch (selectedCharacter)
            {
                case PlayerCharacterType.Male:
                    selectedText.text = "Personaje seleccionado: Hombre";
                    break;

                case PlayerCharacterType.Female:
                    selectedText.text = "Personaje seleccionado: Mujer";
                    break;

                default:
                    selectedText.text = "Selecciona tu personaje.";
                    break;
            }
        }*/
    }

    private void EnsureHotelGameDataExists()
    {
        if (HotelGameData.Instance != null)
            return;

        GameObject dataObject = new GameObject("HotelGameData");
        dataObject.AddComponent<HotelGameData>();
    }
}