using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MissionJournalAttentionButton : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Botón que abre el panel de tareas.")]
    [SerializeField] private Button journalButton;

    [Tooltip("Panel del diario de misiones.")]
    [SerializeField] private MissionJournalPanelUI journalPanel;

    [Tooltip(
        "Imagen UI utilizada como brillo. " +
        "Se recomienda colocarla como hija del botón."
    )]
    [SerializeField] private Image glowImage;

    [Header("Task Detection")]
    [Tooltip("Cada cuánto tiempo se comprueba si existen tareas.")]
    [Min(0.1f)]
    [SerializeField] private float refreshInterval = 0.5f;

    [Tooltip(
        "Oculta el brillo mientras el panel de tareas está abierto."
    )]
    [SerializeField] private bool hideGlowWhileJournalIsOpen = true;

    [Header("Glow Animation")]
    [Tooltip("Velocidad de la animación del brillo.")]
    [Min(0.1f)]
    [SerializeField] private float pulseSpeed = 3f;

    [Tooltip("Opacidad mínima del brillo.")]
    [Range(0f, 1f)]
    [SerializeField] private float minimumAlpha = 0.15f;

    [Tooltip("Opacidad máxima del brillo.")]
    [Range(0f, 1f)]
    [SerializeField] private float maximumAlpha = 0.9f;

    [Tooltip("Escala mínima del brillo.")]
    [Min(0.1f)]
    [SerializeField] private float minimumScale = 1f;

    [Tooltip("Escala máxima del brillo.")]
    [Min(0.1f)]
    [SerializeField] private float maximumScale = 1.30f;

    [Tooltip(
        "Desactiva completamente la imagen de brillo " +
        "cuando no existen tareas."
    )]
    [SerializeField] private bool disableGlowWhenInactive = true;

    private RectTransform glowRectTransform;

    private Color originalGlowColor;
    private Vector3 originalGlowScale;

    private bool hasPendingTasks;
    private float nextRefreshTime;

    public bool HasPendingTasks
    {
        get
        {
            return hasPendingTasks;
        }
    }

    private void Awake()
    {
        if (journalButton == null)
        {
            journalButton = GetComponent<Button>();
        }

        if (glowImage != null)
        {
            glowRectTransform =
                glowImage.GetComponent<RectTransform>();

            originalGlowColor = glowImage.color;

            if (glowRectTransform != null)
            {
                originalGlowScale =
                    glowRectTransform.localScale;
            }

            glowImage.raycastTarget = false;
        }
        else
        {
            Debug.LogWarning(
                "MissionJournalAttentionButton: " +
                "Glow Image no está asignada."
            );
        }

        StopGlow();
    }

    private void OnEnable()
    {
        nextRefreshTime = 0f;
        RefreshTaskState();
    }

    private void Update()
    {
        if (Time.unscaledTime >= nextRefreshTime)
        {
            nextRefreshTime =
                Time.unscaledTime + refreshInterval;

            RefreshTaskState();
        }

        if (ShouldGlow())
        {
            AnimateGlow();
        }
        else
        {
            StopGlow();
        }
    }

    private bool ShouldGlow()
    {
        if (!hasPendingTasks)
            return false;

        if (!hideGlowWhileJournalIsOpen)
            return true;

        if (journalPanel == null)
            return true;

        return !journalPanel.IsOpen;
    }

    public void RefreshTaskState()
    {
        List<HotelMissionData> missions =
            HotelMissionTracker.BuildMissions();

        hasPendingTasks =
            missions != null &&
            missions.Count > 0;

        if (!hasPendingTasks)
        {
            StopGlow();
        }
    }

    public void RefreshNow()
    {
        nextRefreshTime = 0f;
        RefreshTaskState();
    }

    private void AnimateGlow()
    {
        if (glowImage == null)
            return;

        if (!glowImage.gameObject.activeSelf)
        {
            glowImage.gameObject.SetActive(true);
        }
        float pulse =
            (Mathf.Sin(
                Time.unscaledTime * pulseSpeed
            ) + 1f) * 0.5f;

        float currentAlpha = Mathf.Lerp(
            minimumAlpha,
            maximumAlpha,
            pulse
        );

        Color glowColor = originalGlowColor;
        glowColor.a = currentAlpha;

        glowImage.color = glowColor;

        if (glowRectTransform != null)
        {
            float currentScale = Mathf.Lerp(
                minimumScale,
                maximumScale,
                pulse
            );

            glowRectTransform.localScale =
                originalGlowScale * currentScale;
        }
    }

    private void StopGlow()
    {
        if (glowImage == null)
            return;

        glowImage.color = originalGlowColor;

        if (glowRectTransform != null)
        {
            glowRectTransform.localScale =
                originalGlowScale;
        }

        bool glowIsButtonObject =
            journalButton != null &&
            glowImage.gameObject ==
            journalButton.gameObject;

        if (disableGlowWhenInactive &&
            !glowIsButtonObject &&
            glowImage.gameObject.activeSelf)
        {
            glowImage.gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        StopGlow();
    }

    private void OnDestroy()
    {
        StopGlow();
    }
}