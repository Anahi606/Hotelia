using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MissionJournalInput : MonoBehaviour
{
    [Header("Journal Panel")]
    [SerializeField] private MissionJournalPanelUI journalPanel;

    [Header("Icon Button")]
    [SerializeField] private Button journalIconButton;

    [Header("Keyboard Input System")]
    [SerializeField] private InputActionReference journalAction;

    [Header("Fallback")]
    [SerializeField] private bool useJKeyFallback = true;

    private void OnEnable()
    {
        if (journalIconButton != null)
            journalIconButton.onClick.AddListener(ToggleJournal);

        if (journalAction != null && journalAction.action != null)
        {
            journalAction.action.performed += OnJournalPerformed;
            journalAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (journalIconButton != null)
            journalIconButton.onClick.RemoveListener(ToggleJournal);

        if (journalAction != null && journalAction.action != null)
        {
            journalAction.action.performed -= OnJournalPerformed;
            journalAction.action.Disable();
        }
    }

    private void Update()
    {
        if (!useJKeyFallback)
            return;

        if (journalAction != null && journalAction.action != null)
            return;

        if (Keyboard.current != null && Keyboard.current.jKey.wasPressedThisFrame)
        {
            ToggleJournal();
        }
    }

    private void OnJournalPerformed(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        ToggleJournal();
    }

    private void ToggleJournal()
    {
        if (journalPanel == null)
        {
            Debug.LogWarning("No asignaste MissionJournalPanelUI en MissionJournalInput.");
            return;
        }

        journalPanel.ToggleJournal();
    }
}