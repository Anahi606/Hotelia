using UnityEngine;

public static class StudentClassRuntime
{
    private const string ClassCodeKey = "Hotelia_CurrentStudentClassCode";

    private static string currentClassCode = "";

    public static void SetClassCode(string classCode)
    {
        currentClassCode = string.IsNullOrWhiteSpace(classCode)
            ? ""
            : classCode.Trim();

        if (string.IsNullOrWhiteSpace(currentClassCode))
        {
            Debug.LogWarning(
                "StudentClassRuntime: se intentó guardar un NRC vacío."
            );

            return;
        }

        PlayerPrefs.SetString(ClassCodeKey, currentClassCode);
        PlayerPrefs.Save();

        Debug.Log(
            "StudentClassRuntime: NRC guardado correctamente: " +
            currentClassCode
        );
    }

    public static string GetClassCode()
    {
        if (!string.IsNullOrWhiteSpace(currentClassCode))
            return currentClassCode;

        currentClassCode =
            PlayerPrefs.GetString(ClassCodeKey, "").Trim();

        return currentClassCode;
    }

    public static bool HasClassCode()
    {
        return !string.IsNullOrWhiteSpace(GetClassCode());
    }

    public static void ClearClassCode()
    {
        currentClassCode = "";

        PlayerPrefs.DeleteKey(ClassCodeKey);
        PlayerPrefs.Save();

        Debug.Log("StudentClassRuntime: NRC eliminado.");
    }
}