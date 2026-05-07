using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.Animation;

public class NPCSpriteLibraryApplier : MonoBehaviour
{
    [System.Serializable]
    public class SpriteLibraryEntry
    {
        public string customerName;
        public SpriteLibraryAsset spriteLibraryAsset;
    }

    [Header("Sprite Library Target")]
    [SerializeField] private SpriteLibrary spriteLibrary;

    [Header("Sprite Resolver Target")]
    [SerializeField] private SpriteResolver spriteResolver;

    [Header("Customer Sprite Libraries")]
    [SerializeField] private SpriteLibraryEntry[] libraries;

    [Header("Spawn Default")]
    [SerializeField] private string spawnIdleCategory = "Idle";
    [SerializeField] private string spawnIdleLabel = "sprite_0";

    private void Awake()
    {
        if (spriteLibrary == null)
            spriteLibrary = GetComponent<SpriteLibrary>();

        if (spriteLibrary == null)
            spriteLibrary = GetComponentInChildren<SpriteLibrary>(true);

        if (spriteResolver == null)
            spriteResolver = GetComponent<SpriteResolver>();

        if (spriteResolver == null)
            spriteResolver = GetComponentInChildren<SpriteResolver>(true);
    }

    public void ApplyFromSpriteName(string selectedSpriteName)
    {
        if (spriteLibrary == null)
        {
            Debug.LogWarning("No hay SpriteLibrary asignado.");
            return;
        }

        if (spriteResolver == null)
        {
            Debug.LogWarning("No hay SpriteResolver asignado.");
            return;
        }

        if (string.IsNullOrEmpty(selectedSpriteName))
        {
            Debug.LogWarning("selectedSpriteName está vacío.");
            return;
        }

        string cleanName = GetCleanLibraryName(selectedSpriteName);

        SpriteLibraryAsset selectedLibrary = FindLibrary(cleanName);

        if (selectedLibrary == null)
        {
            Debug.LogWarning(
                "No se encontró SpriteLibrary para: " + cleanName +
                ". Revisa el array Libraries en el prefab del NPC."
            );
            return;
        }

        spriteLibrary.spriteLibraryAsset = selectedLibrary;

        ForceSpawnIdle(selectedLibrary);

        Debug.Log("SpriteLibrary cambiada a: " + selectedLibrary.name);
    }

    private void ForceSpawnIdle(SpriteLibraryAsset libraryAsset)
    {
        if (libraryAsset == null || spriteResolver == null)
            return;

        string categoryToUse = spawnIdleCategory;
        string labelToUse = spawnIdleLabel;

        if (!LibraryHasCategoryAndLabel(libraryAsset, categoryToUse, labelToUse))
        {
            GetFirstValidCategoryAndLabel(
                libraryAsset,
                out categoryToUse,
                out labelToUse
            );
        }

        if (string.IsNullOrEmpty(categoryToUse) || string.IsNullOrEmpty(labelToUse))
        {
            Debug.LogWarning("La SpriteLibrary no tiene categorías válidas.");
            return;
        }

        spriteResolver.SetCategoryAndLabel(categoryToUse, labelToUse);
        spriteResolver.ResolveSpriteToSpriteRenderer();

        Debug.Log(
            "NPC spawneado en Category: " +
            categoryToUse +
            " / Label: " +
            labelToUse
        );
    }

    private bool LibraryHasCategoryAndLabel(
        SpriteLibraryAsset libraryAsset,
        string category,
        string label
    )
    {
        IEnumerable<string> categoryNames = libraryAsset.GetCategoryNames();

        foreach (string categoryName in categoryNames)
        {
            if (categoryName != category)
                continue;

            IEnumerable<string> labelNames =
                libraryAsset.GetCategoryLabelNames(categoryName);

            foreach (string labelName in labelNames)
            {
                if (labelName == label)
                    return true;
            }
        }

        return false;
    }

    private void GetFirstValidCategoryAndLabel(
        SpriteLibraryAsset libraryAsset,
        out string category,
        out string label
    )
    {
        category = "";
        label = "";

        IEnumerable<string> categoryNames = libraryAsset.GetCategoryNames();

        foreach (string categoryName in categoryNames)
        {
            IEnumerable<string> labelNames =
                libraryAsset.GetCategoryLabelNames(categoryName);

            foreach (string labelName in labelNames)
            {
                category = categoryName;
                label = labelName;
                return;
            }
        }
    }

    private SpriteLibraryAsset FindLibrary(string cleanName)
    {
        if (libraries == null)
            return null;

        foreach (SpriteLibraryEntry entry in libraries)
        {
            if (entry == null)
                continue;

            if (entry.spriteLibraryAsset == null)
                continue;

            if (entry.customerName == cleanName)
                return entry.spriteLibraryAsset;

            if (entry.spriteLibraryAsset.name == cleanName)
                return entry.spriteLibraryAsset;
        }

        return null;
    }

    private string GetCleanLibraryName(string spriteName)
    {
        string cleanName = spriteName;

        if (cleanName.EndsWith("_0"))
            cleanName = cleanName.Substring(0, cleanName.Length - 2);

        return cleanName;
    }
}