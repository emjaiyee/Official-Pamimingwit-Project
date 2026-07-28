using UnityEngine;
using System;

[Serializable]
public struct CutsceneStep
{
    public string speakerName;
    [TextArea(3, 10)]
    public string dialogue;
    public Sprite background;
    public Sprite characterSprite;
}

[CreateAssetMenu(fileName = "NewCutscene", menuName = "Cutscene/Cutscene Data")]
public class CutsceneData : ScriptableObject
{
    public CutsceneStep[] steps;
}