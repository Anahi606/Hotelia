using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SearchStudentRowUI : MonoBehaviour
{
    [Header("Texts")]
    [SerializeField] private TMP_Text usernameText;
    [SerializeField] private TMP_Text emailText;

    [Header("Button")]
    [SerializeField] private Button selectButton;

    private StudentProfileData student;
    private TeacherStudentsPanelUI panel;

    public void Setup(StudentProfileData studentData, TeacherStudentsPanelUI studentsPanel)
    {
        student = studentData;
        panel = studentsPanel;

        if (usernameText != null)
            usernameText.text = student.displayName;

        if (emailText != null)
            emailText.text = student.email;

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(() =>
            {
                panel.SelectStudentFromSearch(student);
            });
        }
    }
}