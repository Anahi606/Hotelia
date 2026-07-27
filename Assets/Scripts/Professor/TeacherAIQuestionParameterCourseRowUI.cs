using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TeacherAIQuestionParameterCourseRowUI :
    MonoBehaviour
{
    [Header("Texts")]
    [SerializeField]
    private TMP_Text courseNameText;

    [SerializeField]
    private TMP_Text courseCodeText;

    [SerializeField]
    private TMP_Text assignedStatusText;

    [Header("Buttons")]
    [SerializeField]
    private Button editButton;

    [SerializeField]
    private Button deleteButton;

    private string parameterId = "";

    private TeacherAIQuestionParametersPanelUI owner;

    public void Setup(
        AIQuestionParametersData parameterData,
        TeacherAIQuestionParametersPanelUI panelOwner,
        bool courseStillExists
    )
    {
        owner = panelOwner;

        if (parameterData == null)
        {
            Debug.LogWarning(
                "Parameter row received null data."
            );

            gameObject.SetActive(false);
            return;
        }

        /*
         * Editar y eliminar se hacen por parameterId.
         */
        parameterId =
            parameterData.parameterId;

        if (courseNameText != null)
        {
            courseNameText.text =
                !string.IsNullOrWhiteSpace(
                    parameterData.subjectName
                )
                    ? parameterData.subjectName
                    : "Unnamed course";
        }

        if (courseCodeText != null)
        {

            courseCodeText.text =
                courseStillExists &&
                !string.IsNullOrWhiteSpace(
                    parameterData.classCode
                )
                    ? parameterData.classCode
                    : "N/A";
        }

        if (assignedStatusText != null)
        {
            assignedStatusText.text =
                courseStillExists
                    ? "Active"
                    : "Pending";
        }

        if (editButton != null)
        {
            editButton.onClick.RemoveAllListeners();

            editButton.onClick.AddListener(
                EditButtonClicked
            );

            editButton.gameObject.SetActive(true);
        }

        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveAllListeners();

            deleteButton.onClick.AddListener(
                DeleteButtonClicked
            );

            deleteButton.gameObject.SetActive(true);
        }
    }

    private void EditButtonClicked()
    {
        if (owner == null)
        {
            Debug.LogWarning(
                "Cannot edit because owner panel is missing."
            );

            return;
        }

        if (string.IsNullOrWhiteSpace(parameterId))
        {
            Debug.LogWarning(
                "Cannot edit because parameterId is empty."
            );

            return;
        }

        owner.EditParameters(parameterId);
    }

    private void DeleteButtonClicked()
    {
        if (owner == null)
        {
            Debug.LogWarning(
                "Cannot delete because owner panel is missing."
            );

            return;
        }

        if (string.IsNullOrWhiteSpace(parameterId))
        {
            Debug.LogWarning(
                "Cannot delete because parameterId is empty."
            );

            return;
        }

        owner.DeleteParameters(parameterId);
    }
}