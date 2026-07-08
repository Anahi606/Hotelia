using UnityEngine;
using UnityEngine.UI;
using UnityEngine.U2D.Animation;

public class CheckInPlayerImageUI : MonoBehaviour
{
    [Header("UI Image")]
    [SerializeField] private Image playerImage;

    [Header("Sprite Libraries")]
    [SerializeField] private SpriteLibraryAsset maleLibrary;
    [SerializeField] private SpriteLibraryAsset femaleLibrary;

    [Header("Sprite State")]
    [SerializeField] private string category = "Idle";
    [SerializeField] private string label = "sprite_0";

    private void Awake()
    {
        if (playerImage == null)
            playerImage = GetComponent<Image>();
    }

    private void OnEnable()
    {
        ApplySelectedCharacterImage();
    }

    public void ApplySelectedCharacterImage()
    {
        if (playerImage == null)
        {
            Debug.LogWarning("No hay Image asignado para mostrar el sprite del jugador.");
            return;
        }

        if (HotelGameData.Instance == null)
        {
            Debug.LogWarning("No existe HotelGameData. No se puede cargar la imagen del jugador.");
            return;
        }

        PlayerCharacterType selectedCharacter = HotelGameData.Instance.selectedCharacter;

        SpriteLibraryAsset selectedLibrary = null;

        switch (selectedCharacter)
        {
            case PlayerCharacterType.Male:
                selectedLibrary = maleLibrary;
                break;

            case PlayerCharacterType.Female:
                selectedLibrary = femaleLibrary;
                break;

            default:
                Debug.LogWarning("No se ha elegido personaje. Se usará Female por defecto.");
                selectedLibrary = femaleLibrary;
                break;
        }

        if (selectedLibrary == null)
        {
            Debug.LogWarning("No hay SpriteLibraryAsset asignado para " + selectedCharacter);
            return;
        }

        Sprite selectedSprite = selectedLibrary.GetSprite(category, label);

        if (selectedSprite == null)
        {
            Debug.LogWarning(
                "No se encontró sprite en la librería. Category: " +
                category +
                " / Label: " +
                label
            );
            return;
        }

        playerImage.sprite = selectedSprite;
        playerImage.preserveAspect = true;

        Debug.Log("Imagen UI del jugador aplicada: " + selectedCharacter);
    }
}