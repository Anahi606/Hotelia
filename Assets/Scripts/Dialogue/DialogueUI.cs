using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueUI : MonoBehaviour
{
    [Header("UI Refs")]
    public GameObject dialogueBox;
    public TMP_Text dialogueText;
    public Image portraitImage;
    public GameObject continueIcon;

    [Header("Typing")]
    public float charsPerSecond = 40f;

    public bool IsOpen { get; private set; }
    public bool IsTyping => typingCoroutine != null;

    Coroutine typingCoroutine;

    public void Open()
    {
        IsOpen = true;
        if (dialogueBox) dialogueBox.SetActive(true);
        if (continueIcon) continueIcon.SetActive(false);
    }

    public void Close()
    {
        IsOpen = false;
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = null;

        if (dialogueBox) dialogueBox.SetActive(false);
    }

    public void SetLine(DialogueCharacter character, string line)
    {
        if (portraitImage) portraitImage.sprite = character != null ? character.icon : null;

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeLine(line));
    }

    public void SkipTyping(string fullLine)
    {
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = null;

        if (dialogueText) dialogueText.text = fullLine;
        if (continueIcon) continueIcon.SetActive(true);
    }

    IEnumerator TypeLine(string line)
    {
        if (dialogueText) dialogueText.text = "";
        if (continueIcon) continueIcon.SetActive(false);

        float delay = 1f / Mathf.Max(1f, charsPerSecond);

        foreach (char c in line)
        {
            if (dialogueText) dialogueText.text += c;
            yield return new WaitForSeconds(delay);
        }

        typingCoroutine = null;
        if (continueIcon) continueIcon.SetActive(true);
    }
}