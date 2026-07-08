using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TeacherAIQuestionParameterCourseRowUI : MonoBehaviour
{
    [Header("Texts")]
    [SerializeField] private TMP_Text courseNameText;
    [SerializeField] private TMP_Text courseCodeText;
    [SerializeField] private TMP_Text assignedStatusText;

    [Header("Buttons")]
    [SerializeField] private Button editButton;
    [SerializeField] private Button deleteButton;

    private string courseId = "";
    private TeacherAIQuestionParametersPanelUI owner;

    public void Setup(
        AIQuestionParametersData parameterData,
        TeacherAIQuestionParametersPanelUI panelOwner
    )
    {
        owner = panelOwner;

        if (parameterData == null)
        {
            Debug.LogWarning("Parameter row received null data.");
            gameObject.SetActive(false);
            return;
        }

        courseId = parameterData.courseId;

        if (courseNameText != null)
        {
            if (!string.IsNullOrWhiteSpace(parameterData.subjectName))
                courseNameText.text = parameterData.subjectName;
            else
                courseNameText.text = "Unnamed course";
        }

        if (courseCodeText != null)
        {
            string subjectCode = !string.IsNullOrWhiteSpace(parameterData.subjectCode)
                ? parameterData.subjectCode
                : "No subject code";

            string classCode = !string.IsNullOrWhiteSpace(parameterData.classCode)
                ? parameterData.classCode
                : "No class code";

            courseCodeText.text = "Code: " + subjectCode + " / Class: " + classCode;
        }

        if (assignedStatusText != null)
            assignedStatusText.text = "Parameters assigned";

        if (editButton != null)
        {
            editButton.onClick.RemoveAllListeners();
            editButton.onClick.AddListener(EditButtonClicked);
            editButton.gameObject.SetActive(true);
        }

        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(DeleteButtonClicked);
            deleteButton.gameObject.SetActive(true);
        }
    }

    private void EditButtonClicked()
    {
        if (owner == null)
        {
            Debug.LogWarning("Cannot edit because owner panel is missing.");
            return;
        }

        if (string.IsNullOrEmpty(courseId))
        {
            Debug.LogWarning("Cannot edit because courseId is empty.");
            return;
        }

        owner.EditParametersForCourse(courseId);
    }

    private void DeleteButtonClicked()
    {
        if (owner == null)
        {
            Debug.LogWarning("Cannot delete because owner panel is missing.");
            return;
        }

        if (string.IsNullOrEmpty(courseId))
        {
            Debug.LogWarning("Cannot delete because courseId is empty.");
            return;
        }

        owner.DeleteParametersForCourse(courseId);
    }
}