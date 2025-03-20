using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
public class PaintingToolEditor : EditorWindow
{
    [MenuItem("Unity Paint/MainWindow")]
    public static void ShowWindow()
    {
        PaintingToolEditor window = GetWindow<PaintingToolEditor>();
        window.titleContent = new GUIContent("Unity Paint!!");
    }

    public void CreateGUI()
    {
        VisualElement root = rootVisualElement;
            
        VisualTreeAsset asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Painting Tool/UI/PaintCanvasUI.uxml");
        asset.CloneTree(root);
        StyleSheet sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Painting Tool/UI/PaintCanvasStyleSheet.uss");
        root.styleSheets.Add(sheet);

    }

    public void OnGUI()
    {
      
    }

}
