using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AssignedStudentRowUI : MonoBehaviour
{
    [Header("Texts")]
    [SerializeField] private TMP_Text usernameText;
    [SerializeField] private TMP_Text emailText;
    [SerializeField] private TMP_Text courseText;
    [SerializeField] private TMP_Text statusText;

    [Header("Buttons")]
    [SerializeField] private Button editButton;
    [SerializeField] private Button removeButton;

    private AssignedStudentData student;
    private TeacherStudentsPanelUI panel;

    public void Setup(AssignedStudentData studentData, TeacherStudentsPanelUI studentsPanel)
    {
        student = studentData;
        panel = studentsPanel;

        if (usernameText != null)
            usernameText.text = student.displayName;

        if (emailText != null)
            emailText.text = student.email;

        if (courseText != null)
            courseText.text = student.courseName;

        if (statusText != null)
            statusText.text = student.status;

        if (editButton != null)
        {
            editButton.onClick.RemoveAllListeners();
            editButton.onClick.AddListener(() =>
            {
                panel.EditStudentAssignment(student.playFabId);
            });
        }

        if (removeButton != null)
        {
            removeButton.onClick.RemoveAllListeners();
            removeButton.onClick.AddListener(() =>
            {
                panel.RemoveStudentFromCourse(student.playFabId);
            });
        }
    }
}