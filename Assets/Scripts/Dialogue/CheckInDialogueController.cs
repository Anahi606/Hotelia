using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class CheckInDialogueController : MonoBehaviour
{
    [Header("Screen")]
    public GameObject checkInScreen;

    [Header("Customer UI")]
    public Image customerImage;
    public Image customerPortrait;

    [Header("Dialogue UI")]
    public GameObject dialogueBox;
    public TMP_Text dialogueText;
    public float charsPerSecond = 40f;

    [Header("Customer Sprites Folder (Resources)")]
    public string customersResourcesPath = "Customers";

    public bool IsDialogueActive { get; private set; }

    public Action OnDialogueFinished;

    private string[] lines;
    private int index;
    private Coroutine typingCo;
    private string currentLineFull;

    void Awake()
    {
        if (checkInScreen == null) checkInScreen = gameObject;
    }

    void Update()
    {
        if (!IsDialogueActive) return;
        if (Keyboard.current == null) return;

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            AdvanceDialogue();
        }
    }

    public void StartCheckIn(string[] dialogueLines)
    {
        checkInScreen.SetActive(true);

        Sprite sprite = GetRandomCustomerSprite();
        if (sprite != null)
        {
            if (customerImage != null) customerImage.sprite = sprite;
            if (customerPortrait != null) customerPortrait.sprite = sprite;
        }

        lines = dialogueLines;
        dialogueBox.SetActive(true);
        IsDialogueActive = true;
        index = 0;
        ShowLine();
    }

    public void AdvanceDialogue()
    {
        Debug.Log("AdvanceDialogue llamado. Index actual: " + index);

        if (!IsDialogueActive) return;

        if (typingCo != null)
        {
            StopCoroutine(typingCo);
            typingCo = null;
            dialogueText.text = currentLineFull;
            return;
        }

        index++;

        if (index >= lines.Length)
        {
            EndDialogue();
            return;
        }

        ShowLine();
    }

    private void ShowLine()
    {
        if (lines == null || lines.Length == 0) return;

        dialogueText.text = "";
        currentLineFull = lines[index];

        Debug.Log("Mostrando línea: " + currentLineFull);

        if (typingCo != null) StopCoroutine(typingCo);
        typingCo = StartCoroutine(TypeLine(currentLineFull));
    }

    private IEnumerator TypeLine(string line)
    {
        float delay = 1f / Mathf.Max(1f, charsPerSecond);

        foreach (char c in line)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(delay);
        }

        typingCo = null;
    }

    private void EndDialogue()
    {
        Debug.Log("Diálogo terminado.");

        IsDialogueActive = false;
        dialogueBox.SetActive(false);
        OnDialogueFinished?.Invoke();
    }

    private Sprite GetRandomCustomerSprite()
    {
        Sprite[] allSprites = Resources.LoadAll<Sprite>(customersResourcesPath);

        if (allSprites == null || allSprites.Length == 0)
        {
            Debug.LogWarning($"No encontré sprites en Resources/{customersResourcesPath}");
            return null;
        }

        List<Sprite> firstFrameSprites = new List<Sprite>();

        foreach (Sprite s in allSprites)
        {
            if (s.name.EndsWith("_0"))
            {
                firstFrameSprites.Add(s);
            }
        }

        if (firstFrameSprites.Count == 0)
        {
            Debug.LogWarning("No encontré sprites terminados en _0. Usaré el primero disponible.");
            return allSprites[0];
        }

        return firstFrameSprites[UnityEngine.Random.Range(0, firstFrameSprites.Count)];
    }
}