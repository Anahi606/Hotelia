using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TeacherCourseRowUI : MonoBehaviour
{
    [Header("Texts")]
    [SerializeField] private TMP_Text courseNameText;
    [SerializeField] private TMP_Text courseCodeText;
    [SerializeField] private TMP_Text statusText;

    [Header("Buttons")]
    [SerializeField] private Button courseNameButton;
    [SerializeField] private Button editButton;
    [SerializeField] private Button deleteButton;

    private TeacherCourseData course;
    private TeacherCoursesPanelUI coursesPanel;

    public void Setup(TeacherCourseData courseData, TeacherCoursesPanelUI panel)
    {
        course = courseData;
        coursesPanel = panel;

        if (courseNameText != null)
            courseNameText.text = course.courseName;

        if (courseCodeText != null)
            courseCodeText.text = course.courseCode;

        if (statusText != null)
            statusText.text = course.status;

        if (courseNameButton != null)
        {
            courseNameButton.onClick.RemoveAllListeners();
            courseNameButton.onClick.AddListener(() =>
            {
                coursesPanel.SelectCourse(course.courseId);
            });
        }

        if (editButton != null)
        {
            editButton.onClick.RemoveAllListeners();
            editButton.onClick.AddListener(() =>
            {
                coursesPanel.EditCourse(course.courseId);
            });
        }

        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(() =>
            {
                coursesPanel.DeleteCourse(course.courseId);
            });
        }
    }
}