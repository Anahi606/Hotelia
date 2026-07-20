using System;
using UnityEngine;

[Serializable]
public class TourismQuestion
{
    [TextArea(2, 4)]
    public string question;

    public string optionA;
    public string optionB;
    public string optionC;

    [Range(0, 2)]
    public int correctIndex;

    [TextArea(2, 4)]
    public string correctFeedback;

    [TextArea(2, 4)]
    public string wrongFeedback;
}