using System;
using UnityEngine;

public class AIQuestionParametersRuntime : MonoBehaviour
{
    public static AIQuestionParametersRuntime Instance { get; private set; }

    public AIQuestionParametersData CurrentParameters { get; private set; }

    public bool HasActiveParameters
    {
        get
        {
            return CurrentParameters != null &&
                   string.Equals(
                       CurrentParameters.status,
                       "ACTIVE",
                       StringComparison.OrdinalIgnoreCase
                   );
        }
    }

    public bool HasParametersForClass(string classCode)
    {
        if (!HasActiveParameters)
            return false;

        if (string.IsNullOrWhiteSpace(classCode))
            return false;

        if (string.IsNullOrWhiteSpace(CurrentParameters.classCode))
            return false;

        return string.Equals(
            CurrentParameters.classCode.Trim(),
            classCode.Trim(),
            StringComparison.OrdinalIgnoreCase
        );
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetCurrentParameters(AIQuestionParametersData parameters)
    {
        if (parameters == null)
        {
            CurrentParameters = null;

            Debug.LogWarning(
                "AIQuestionParametersRuntime received null parameters."
            );

            return;
        }

        if (!string.IsNullOrWhiteSpace(parameters.classCode))
        {
            parameters.classCode =
                parameters.classCode.Trim();

            StudentClassRuntime.SetClassCode(
                parameters.classCode
            );

            Debug.Log(
                "AIQuestionParametersRuntime: NRC received " +
                "from Azure and saved: " +
                parameters.classCode
            );
        }
        else
        {
            Debug.LogWarning(
                "AIQuestionParametersRuntime: the parameters " +
                "do not contain a classCode/NRC."
            );
        }

        if (!string.IsNullOrWhiteSpace(parameters.status))
        {
            parameters.status =
                parameters.status.Trim().ToUpperInvariant();
        }

        CurrentParameters = parameters;

        Debug.Log(
            "AIQuestionParametersRuntime updated." +
            "\nParameterId=" + parameters.parameterId +
            "\nSubject=" + parameters.subjectName +
            "\nClassCode=" + parameters.classCode +
            "\nCourseId=" + parameters.courseId +
            "\nStatus=" + parameters.status +
            "\nGoal=" + parameters.questionGoal
        );
    }

    public void ClearCurrentParameters()
    {
        CurrentParameters = null;
        Debug.Log("AIQuestionParametersRuntime parameters cleared.");
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
