using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
public class PaintingToolEditor : EditorWindow
{
    private PaintingToolScript PainterScript;

    public Texture2D DisplayImage;
    public int height = 64;
    public int width = 64;

    public Color PrimaryColor;
    public Color SecondaryColor;

    [MenuItem("Unity Paint/Menu")]
    public static void ShowMenuWindow()
    {
        PaintingToolEditor window = GetWindow<PaintingToolEditor>();
        window.titleContent = new GUIContent("Paint Hub");
    }

    public void CreateGUI()
    { 
        VisualElement root = rootVisualElement;
        VisualTreeAsset asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Painting Tool/UI/PaintMenuUI.uxml");
        asset.CloneTree(root);
        StyleSheet sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Painting Tool/UI/PaintMenuStyleSheet.uss");
        root.styleSheets.Add(sheet);
        root.Q<Button>("NewButton").clicked +=OpenCanvasSizeMenu;

    }

    private void OpenCanvasSizeMenu()
    {
        VisualElement root = rootVisualElement;
        root.Clear();
        VisualTreeAsset asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Painting Tool/UI/PaintCanvasSize.uxml");
        asset.CloneTree(root);
        root.Q<Button>("CreateCanvasButton").clicked += OpenPainter;
    }
    private void OpenPainter()
    {
        VisualElement root = rootVisualElement;
        width = root.Q<IntegerField>("WidthField").value;
        height = root.Q<IntegerField>("HeightField").value;
        root.Clear();
        VisualTreeAsset asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Painting Tool/UI/PaintCanvasUI.uxml");
        asset.CloneTree(root);
        StyleSheet sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Painting Tool/UI/PaintCanvasStyleSheet.uss");
        root.styleSheets.Add(sheet);

        PainterScript = new PaintingToolScript();
        PainterScript.initialize(width,height,"Layer1");
        DisplayImage = PainterScript.GetDisplayImage();
        VisualElement Image = root.Q<VisualElement>("DisplayTexture");
        Image.style.backgroundImage = new StyleBackground(DisplayImage);

        PainterScript.UpdateCavas += UpdateDisplayImage;
    }

    private void UpdateDisplayImage()
    {
        VisualElement root = rootVisualElement;

        DisplayImage = PainterScript.GetDisplayImage();
        VisualElement Image = root.Q<VisualElement>("DisplayTexture");
        Image.style.backgroundImage = new StyleBackground(DisplayImage);
    }

    public void OnGUI()
    {
        
    }
    public void Update()
    {
        
    }

    public void ExportImage()
    {
        UpdateDisplayImage();
        DisplayImage.EncodeToPNG();
    }
}
