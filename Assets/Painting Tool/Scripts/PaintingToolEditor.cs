using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
public class PaintingToolEditor : EditorWindow
{
    public enum WindowOptions
    {
        Menu,
        Paint
    }
    private static WindowOptions SelectedWindow;

    private static PaintingToolScript PainterScript;

    [MenuItem("Unity Paint/Menu")]
    public static void ShowMenuWindow()
    {
        SelectedWindow = WindowOptions.Menu;
        PaintingToolEditor window = GetWindow<PaintingToolEditor>();
        window.titleContent = new GUIContent("Paint Hub");
    }
    [MenuItem("Unity Paint/Paint")]
    public static void ShowPaintWindow()
    {
        SelectedWindow = WindowOptions.Paint;
        PaintingToolEditor window = GetWindow<PaintingToolEditor>();
        window.titleContent = new GUIContent("Le Painte");
    }

    public void CreateGUI()
    {
        if (SelectedWindow == WindowOptions.Paint)
        { 
        VisualElement root = rootVisualElement;
            
        VisualTreeAsset asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Painting Tool/UI/PaintCanvasUI.uxml");
        asset.CloneTree(root);
        StyleSheet sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Painting Tool/UI/PaintCanvasStyleSheet.uss");
        root.styleSheets.Add(sheet);
        }
        else if (SelectedWindow == WindowOptions.Menu)
        {
            VisualElement root = rootVisualElement;

            VisualTreeAsset asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Painting Tool/UI/PaintMenuUI.uxml");
            asset.CloneTree(root);
        }
        else
        {
            VisualElement root = rootVisualElement;
            root.Add(new Label("No Valid Window Selected"));
            
        }
    }

    public void OnGUI()
    {
        
    }
    public void Update()
    {
        
    }

}
