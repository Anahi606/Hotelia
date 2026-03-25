using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DialogueCharacter
{
    //public string name;
    public Sprite icon;
}

[System.Serializable]
public class DialogueLine
{
    public DialogueCharacter character;

    [TextArea(2, 6)]
    public string line;
}

[System.Serializable]
public class DialogueSequence
{
    public List<DialogueLine> lines = new();
}