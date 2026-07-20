using UnityEngine;

public class CoverPlacedDebugger : MonoBehaviour
{
    private void OnDisable()
    {
        string reason = gameObject.activeSelf
            ? "El objeto sigue activo, pero uno de sus padres fue desactivado."
            : "Se ejecutó SetActive(false) directamente sobre CoverPlaced.";

        Debug.LogError(
            "COVERPLACED FUE DESACTIVADO\n" +
            "Motivo probable: " + reason + "\n" +
            "Objeto: " + name + "\n" +
            "Jerarquía: " + GetHierarchyPath() + "\n\n" +
            StackTraceUtility.ExtractStackTrace(),
            this
        );
    }

    private void OnDestroy()
    {
        Debug.LogError(
            "COVERPLACED FUE DESTRUIDO\n" +
            "Jerarquía: " + GetHierarchyPath() + "\n\n" +
            StackTraceUtility.ExtractStackTrace(),
            this
        );
    }

    private string GetHierarchyPath()
    {
        string path = name;
        Transform current = transform.parent;

        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }
}