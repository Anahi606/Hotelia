using UnityEngine;
using UnityEngine.U2D.Animation;

public class PlayerCharacterVisual : MonoBehaviour
{
    [Header("Sprite Libraries")]
    [SerializeField] private SpriteLibraryAsset maleLibrary;
    [SerializeField] private SpriteLibraryAsset femaleLibrary;

    [Header("Default Sprite Resolver State")]
    [SerializeField] private string defaultCategory = "Idle";
    [SerializeField] private string defaultLabel = "sprite_0";

    private SpriteLibrary spriteLibrary;
    private SpriteResolver spriteResolver;

    private void Awake()
    {
        spriteLibrary = GetComponent<SpriteLibrary>();
        spriteResolver = GetComponent<SpriteResolver>();
    }

    private void Start()
    {
        ApplySelectedCharacter();
    }

    public void ApplySelectedCharacter()
    {
        if (HotelGameData.Instance == null)
        {
            Debug.LogWarning("No existe HotelGameData. No se puede cargar personaje.");
            return;
        }

        PlayerCharacterType selectedCharacter = HotelGameData.Instance.selectedCharacter;

        if (spriteLibrary == null)
        {
            Debug.LogWarning("El Player no tiene SpriteLibrary.");
            return;
        }

        switch (selectedCharacter)
        {
            case PlayerCharacterType.Male:
                spriteLibrary.spriteLibraryAsset = maleLibrary;
                break;

            case PlayerCharacterType.Female:
                spriteLibrary.spriteLibraryAsset = femaleLibrary;
                break;

            default:
                Debug.LogWarning("No se ha elegido personaje. Se usará Female por defecto.");
                spriteLibrary.spriteLibraryAsset = femaleLibrary;
                break;
        }

        if (spriteResolver != null)
        {
            spriteResolver.SetCategoryAndLabel(defaultCategory, defaultLabel);
            spriteResolver.ResolveSpriteToSpriteRenderer();
        }

        Debug.Log("Personaje visual aplicado: " + selectedCharacter);
    }
}