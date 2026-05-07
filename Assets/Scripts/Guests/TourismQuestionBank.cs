using UnityEngine;

public class TourismQuestionBank : MonoBehaviour
{
    public static TourismQuestionBank Instance { get; private set; }

    [Header("Preguntas de turismo")]
    public TourismQuestion[] questions;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public TourismQuestion GetRandomQuestion()
    {
        if (questions == null || questions.Length == 0)
        {
            Debug.LogWarning("No hay preguntas de turismo configuradas.");
            return null;
        }

        return questions[Random.Range(0, questions.Length)];
    }
}