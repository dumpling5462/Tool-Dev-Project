using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewScriptableObjectScript", menuName = "Scriptable Objects/NewScriptableObjectScript")]
public class PaintSO : ScriptableObject
{
    public int width;
    public int height;

    public int animationIndex;
    public int LayerIndex;

    public List<List<PaintingToolScript.PaintLayer>> PaintAnimations;

    public List<ChangesStack.Changes> UndoStack;
}
