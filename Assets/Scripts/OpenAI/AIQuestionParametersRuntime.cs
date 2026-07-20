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
        CurrentParameters = parameters;

        if (parameters == null)
        {
            Debug.LogWarning("AIQuestionParametersRuntime received null parameters.");
            return;
        }

        Debug.Log(
            "AIQuestionParametersRuntime updated. " +
            "Subject=" + parameters.subjectName +
            ", ClassCode=" + parameters.classCode +
            ", Status=" + parameters.status
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
