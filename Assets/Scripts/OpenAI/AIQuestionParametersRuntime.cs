using UnityEngine;

public class AIQuestionParametersRuntime : MonoBehaviour
{
    public static AIQuestionParametersRuntime Instance { get; private set; }

    public AIQuestionParametersData CurrentParameters { get; private set; }

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

        if (parameters != null)
        {
            Debug.Log(
                "Current AI parameters set: " +
                parameters.subjectName +
                " / Class " +
                parameters.classCode
            );
        }
        else
        {
            Debug.LogWarning("Current AI parameters were cleared.");
        }
    }

    public void ClearParameters()
    {
        CurrentParameters = null;
    }
}