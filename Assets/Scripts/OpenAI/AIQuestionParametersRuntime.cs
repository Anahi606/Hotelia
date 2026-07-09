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
                   CurrentParameters.status == "ACTIVE";
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

        if (CurrentParameters != null)
        {
            Debug.Log(
                "AI parameters loaded: " +
                CurrentParameters.subjectName +
                " - Class " +
                CurrentParameters.classCode
            );
        }
    }

    public void ClearCurrentParameters()
    {
        CurrentParameters = null;
        Debug.Log("AI parameters cleared.");
    }
}