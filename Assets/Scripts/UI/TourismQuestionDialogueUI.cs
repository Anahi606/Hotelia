using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TourismQuestionDialogueUI : MonoBehaviour
{
    public static TourismQuestionDialogueUI Instance { get; private set; }

    [Header("Panel que se prende/apaga")]
    public GameObject panel;

    [Header("NPC Portrait")]
    public Image npcPortrait;

    [Header("Texts")]
    public TMP_Text questionText;
    public TMP_Text feedbackText;

    [Header("Buttons")]
    public Button optionAButton;
    public Button optionBButton;
    public Button optionCButton;

    public TMP_Text optionAText;
    public TMP_Text optionBText;
    public TMP_Text optionCText;

    [Header("Typewriter")]
    public float charsPerSecond = 40f;

    [Header("Auto Close")]
    public float closeDelay = 2f;

    [Header("Player")]
    public PlayerMovement playerMovement;

    public bool IsOpen { get; private set; }

    private TourismQuestion currentQuestion;
    private Action<bool> onFinished;
    private Coroutine typingCoroutine;
    private Coroutine closeCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (panel != null)
            panel.SetActive(false);
        else
            Debug.LogWarning("Falta asignar QuestionPanel en TourismQuestionDialogueUI.");

        if (playerMovement == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

            if (playerObj != null)
                playerMovement = playerObj.GetComponent<PlayerMovement>();
        }
    }

    public void ShowQuestion(TourismQuestion question, Sprite npcSprite, Action<bool> finishedCallback)
    {
        if (question == null)
            return;

        if (!ValidateReferences())
        {
            Debug.LogWarning("Faltan referencias en TourismQuestionDialogueUI. Revisa el Inspector.");
            return;
        }

        currentQuestion = question;
        onFinished = finishedCallback;
        IsOpen = true;

        panel.SetActive(true);

        if (playerMovement != null)
            playerMovement.SetMovementEnabled(false);

        if (npcPortrait != null)
            npcPortrait.sprite = npcSprite;

        questionText.text = "";
        feedbackText.text = "";

        optionAText.text = question.optionA;
        optionBText.text = question.optionB;
        optionCText.text = question.optionC;

        SetButtonsVisible(false);
        SetButtonsInteractable(true);

        optionAButton.onClick.RemoveAllListeners();
        optionBButton.onClick.RemoveAllListeners();
        optionCButton.onClick.RemoveAllListeners();

        optionAButton.onClick.AddListener(() => SelectAnswer(0));
        optionBButton.onClick.AddListener(() => SelectAnswer(1));
        optionCButton.onClick.AddListener(() => SelectAnswer(2));

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeQuestion(question.question));
    }

    private IEnumerator TypeQuestion(string text)
    {
        questionText.text = "";

        float delay = 1f / Mathf.Max(1f, charsPerSecond);

        foreach (char c in text)
        {
            if (!ValidateReferences())
                yield break;

            questionText.text += c;
            yield return new WaitForSeconds(delay);
        }

        typingCoroutine = null;

        SetButtonsVisible(true);
    }

    private void SelectAnswer(int selectedIndex)
    {
        if (currentQuestion == null)
            return;

        bool isCorrect = selectedIndex == currentQuestion.correctIndex;

        SetButtonsInteractable(false);

        feedbackText.text = isCorrect
            ? currentQuestion.correctFeedback
            : currentQuestion.wrongFeedback;

        onFinished?.Invoke(isCorrect);

        if (closeCoroutine != null)
            StopCoroutine(closeCoroutine);

        closeCoroutine = StartCoroutine(CloseAfterDelay());
    }

    private IEnumerator CloseAfterDelay()
    {
        yield return new WaitForSeconds(closeDelay);
        ClosePanel();
    }

    private void ClosePanel()
    {
        IsOpen = false;

        if (panel != null)
            panel.SetActive(false);

        if (playerMovement != null)
            playerMovement.SetMovementEnabled(true);

        currentQuestion = null;
        onFinished = null;
    }

    private void SetButtonsVisible(bool visible)
    {
        if (optionAButton != null)
            optionAButton.gameObject.SetActive(visible);

        if (optionBButton != null)
            optionBButton.gameObject.SetActive(visible);

        if (optionCButton != null)
            optionCButton.gameObject.SetActive(visible);
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (optionAButton != null)
            optionAButton.interactable = interactable;

        if (optionBButton != null)
            optionBButton.interactable = interactable;

        if (optionCButton != null)
            optionCButton.interactable = interactable;
    }

    private bool ValidateReferences()
    {
        return panel != null &&
               questionText != null &&
               feedbackText != null &&
               optionAButton != null &&
               optionBButton != null &&
               optionCButton != null &&
               optionAText != null &&
               optionBText != null &&
               optionCText != null;
    }

    private void OnDisable()
    {
        if (playerMovement != null)
            playerMovement.SetMovementEnabled(true);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (playerMovement != null)
            playerMovement.SetMovementEnabled(true);
    }
}