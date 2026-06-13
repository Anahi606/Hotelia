using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PerformanceStudentRowUI : MonoBehaviour
{
    [Header("Texts")]
    [SerializeField] private TMP_Text usernameText;
    [SerializeField] private TMP_Text emailText;

    [Header("Button")]
    [SerializeField] private Button studentButton;

    private AssignedStudentData student;
    private TeacherPerformancePanelUI panel;

    public void Setup(AssignedStudentData studentData, TeacherPerformancePanelUI performancePanel)
    {
        student = studentData;
        panel = performancePanel;

        if (usernameText != null)
            usernameText.text = student.displayName;

        if (emailText != null)
            emailText.text = student.email;

        if (studentButton != null)
        {
            studentButton.onClick.RemoveAllListeners();
            studentButton.onClick.AddListener(() =>
            {
                panel.LoadStudentPerformance(student);
            });
        }
    }
}