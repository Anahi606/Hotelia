using System;
using UnityEngine;

[Serializable]
public class TutorialPageData
{
    [Header("Content")]
    public string title;

    [TextArea(3, 8)]
    public string description;

    public Sprite image;
}