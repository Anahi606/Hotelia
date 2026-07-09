using UnityEngine;

public static class StudentClassRuntime
{
    private const string ClassCodePrefsKey = "Hotelia_CurrentStudentClassCode";

    public static string CurrentClassCode { get; private set; }

    public static void SetClassCode(string classCode)
    {
        CurrentClassCode = string.IsNullOrWhiteSpace(classCode)
            ? ""
            : classCode.Trim();

        PlayerPrefs.SetString(ClassCodePrefsKey, CurrentClassCode);
        PlayerPrefs.Save();

        Debug.Log("Student class code saved: " + CurrentClassCode);
    }

    public static string GetClassCode()
    {
        if (!string.IsNullOrWhiteSpace(CurrentClassCode))
            return CurrentClassCode;

        CurrentClassCode = PlayerPrefs.GetString(ClassCodePrefsKey, "").Trim();
        return CurrentClassCode;
    }

    public static void ClearClassCode()
    {
        CurrentClassCode = "";
        PlayerPrefs.DeleteKey(ClassCodePrefsKey);
        PlayerPrefs.Save();
    }
}