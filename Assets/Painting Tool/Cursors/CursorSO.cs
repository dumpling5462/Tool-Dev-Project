using UnityEngine;

[CreateAssetMenu(fileName = "CursorSO", menuName = "Scriptable Objects/CursorSO")]
public class CursorSO : ScriptableObject
{
    public PaintingToolScript.BrushMode BrushTool;
    public Texture2D CursorIcon;
}
