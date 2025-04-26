using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.ShortcutManagement;
using UnityEditor.UIElements;
public class PaintingToolEditor : EditorWindow
{
    private PaintingToolScript PainterScript;

    public Texture2D DisplayImage;
    public int height = 64;
    public int width = 64;

    private ScrollView LayerList;
    private ScrollView AnimationList;

    public Color PrimaryColor;
    public Color SecondaryColor;

    bool paint=false;

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
        StyleSheet sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Painting Tool/UI/PaintMenuStyleSheet.uss");
        root.styleSheets.Remove(sheet);
        root.Clear();
        VisualTreeAsset asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Painting Tool/UI/PaintCanvasUI.uxml");
        asset.CloneTree(root);
        sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Painting Tool/UI/PaintCanvasStyleSheet.uss");
        root.styleSheets.Add(sheet);

        InitializePainter();
        InitializePaintBindings();

        DisplayImage = PainterScript.GetDisplayImage();
        VisualElement Image = root.Q<VisualElement>("DisplayTexture");
        Image.style.backgroundImage = new StyleBackground(DisplayImage);

    }

    private void InitializePainter()
    {
        PainterScript = new PaintingToolScript();
        PainterScript.Initialize(width, height);
    }

    private void InitializePaintBindings()
    {
        if (PainterScript == null)
        {
            return;
        }
        VisualElement root = rootVisualElement;
        root.Q<Button>("BrushButton").clicked += Brush;
        root.Q<Button>("EraserButton").clicked += Eraser;
        root.Q<Button>("EyeDropButton").clicked += EyeDropper;
        root.Q<Button>("PaintBucketButton").clicked += PaintBucket;

        ColorField color = root.Q<ColorField>("PrimaryColour");
        color.RegisterValueChangedCallback(ColorChange => { PrimaryColor = ColorChange.newValue; ChangeColour(); });
        PrimaryColor = color.value;
        PainterScript.SelectedColour = PrimaryColor;
        ColorField color2 = root.Q<ColorField>("SecondaryColour");
        color2.RegisterValueChangedCallback(ColorChange => { SecondaryColor = ColorChange.newValue; });
        SecondaryColor = color2.value;

        root.Q<Button>("SwapButton").clicked += SwapColour;

        root.Q<Button>("SaveButton").clicked += SaveImage;
        root.Q<Button>("ExportButton").clicked += ExportImage;

        AnimationList = root.Q<ScrollView>("AnimationList");
        LayerList = root.Q<ScrollView>("LayerList");

        root.Q<Button>("AddLayerButton").clicked+=AddLayer;
        root.Q<Button>("AddAnimationButton").clicked += AddAnimation;

        root.Q<Button>("UndoButton").clicked+= UndoChange;
        root.Q<Button>("RedoButton").clicked+= RedoChange;

        VisualElement DisplayTex = root.Q<VisualElement>("DisplayTexture");
        DisplayTex.RegisterCallback<ClickEvent>(Paint);
        DisplayTex.RegisterCallback<MouseEnterEvent>(MouseOver);
        DisplayTex.RegisterCallback<MouseLeaveEvent>(MouseOut);

        PainterScript.UpdateCanvas += UpdateDisplayImage;
        PainterScript.ColourChange += UpdateColour;
        PainterScript.LayerSelected += UpdateLayerButtons;
        PainterScript.AnimationSelected += UpdateAnimationButtons;

        PainterScript.AnimationAdded += AddAnimationAtIndex;
        PainterScript.AnimationRemoved += RemoveAnimationAtIndex;
        PainterScript.LayerAdded += AddLayerAtIndex;
        PainterScript.LayerRemoved += RemoveLayerAtIndex;

        PainterScript.NameChange += LayerNameChange;
        PainterScript.VisibiltyChange += LayerVisibiltyChange;

        PainterScript.LayerMoved += MovedLayer;
        AddAnimation();
        AddLayer();
        UpdateAnimationButtons();
        UpdateLayerButtons();
    }
    private void MovedLayer(int OldIndex,int newIndex)
    {
        VisualElement Layer = LayerList.Query<VisualElement>("Layer").ToList()[OldIndex];
        LayerList.RemoveAt(OldIndex);
        LayerList.Insert(newIndex,Layer);
        Debug.Log(OldIndex + " " + newIndex + " " + LayerList.IndexOf(Layer));
        UpdateLayerButtons();

    }
    private void LayerVisibiltyChange(int index, bool visibilty)
    {
        LayerList[index].Q<Toggle>("Toggle").value = visibilty;
    }
    private void LayerNameChange(int index, string name)
    {
        LayerList[index].Q<Button>("LayerButton").text = name;
    }

    private void UpdateDisplayImage()
    {
        VisualElement root = rootVisualElement;

        DisplayImage = PainterScript.GetDisplayImage();
        VisualElement Image = root.Q<VisualElement>("DisplayTexture");
        DestroyImmediate(Image.style.backgroundImage.value.texture);
        DisplayImage.filterMode = FilterMode.Point;
        Image.style.backgroundImage = new StyleBackground(DisplayImage);
    }

    [Shortcut("b")]
    private void Brush()
    {
        if (PainterScript == null)
        {
            return;
        }
        PainterScript.SelectedBrush = PaintingToolScript.BrushMode.Paintbrush;
        UpdateVisual();
    }
    [Shortcut("e")]
    private void Eraser()
    {
        if (PainterScript == null)
        {
            return;
        }
        PainterScript.SelectedBrush = PaintingToolScript.BrushMode.Eraser;
        UpdateVisual();
    }
    [Shortcut("g")]
    private void PaintBucket()
    {
        if (PainterScript == null)
        {
            return;
        }
        PainterScript.SelectedBrush = PaintingToolScript.BrushMode.PaintBucket;
        UpdateVisual();
    }
    [Shortcut("i")]
    private void EyeDropper()
    {
        if (PainterScript == null)
        {
            return;
        }
        PainterScript.SelectedBrush = PaintingToolScript.BrushMode.Eyedropper;
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (PainterScript == null)
        {
            return;
        }
        switch (PainterScript.SelectedBrush)
        {
            case PaintingToolScript.BrushMode.Paintbrush:
                break;
            case PaintingToolScript.BrushMode.Eraser:
                break;
            case PaintingToolScript.BrushMode.PaintBucket:
                break;
            case PaintingToolScript.BrushMode.Eyedropper:
                break;
        }
    }
    [Shortcut("+")]
    private void IncreaseBrushSize()
    {
        if (PainterScript == null)
        {
            return;
        }
    }
    [Shortcut("-")]
    private void DecreaseBrushSize() 
    {
        if (PainterScript == null)
        {  
            return; 
        }
    }
    private void UndoChange()
    {
        if (PainterScript == null)
        {
            return;
        }
        PainterScript.Undo();
    }
    private void RedoChange()
    {
        if (PainterScript == null)
        {
            return;
        }
        PainterScript.Redo();
    }
    private void AddAnimation()
    {
        if (PainterScript == null)
        {
            return;
        }
        VisualTreeAsset AnimationInfo = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Painting Tool/UI/PaintAnimationItem.uxml");
        VisualElement Frame = AnimationInfo.CloneTree();
        AnimationList.Insert(AnimationList.childCount-1,Frame);

        Button Animationbutton = Frame.Q<Button>("AnimationButton");
        Animationbutton.text = (AnimationList.IndexOf(Frame)+1).ToString();
        Animationbutton.RegisterCallback<ClickEvent, Button>(SelectAnimation,Animationbutton);
        Button DeleteButton = Frame.Q<Button>("DeleteButton");
        DeleteButton.RegisterCallback<ClickEvent, Button>(DeleteAnimation, DeleteButton);
        Button LeftButton = Frame.Q<Button>("LeftButton");
        LeftButton.RegisterCallback<ClickEvent, Button>(MoveAnimation,LeftButton);
        Button RightButton = Frame.Q<Button>("RightButton");
        RightButton.RegisterCallback<ClickEvent, Button>(MoveAnimation,RightButton);
        UpdateAnimationLayers();
        PainterScript.AddAnimation();
    }

    private void AddAnimationAtIndex(int index)
    {
        VisualTreeAsset AnimationInfo = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Painting Tool/UI/PaintAnimationItem.uxml");
        VisualElement Frame = AnimationInfo.CloneTree();
        AnimationList.Insert(AnimationList.childCount - 1, Frame);

        Button Animationbutton = Frame.Q<Button>("AnimationButton");
        Animationbutton.text = (AnimationList.IndexOf(Frame) + 1).ToString();
        Animationbutton.RegisterCallback<ClickEvent, Button>(SelectAnimation, Animationbutton);
        Button DeleteButton = Frame.Q<Button>("DeleteButton");
        DeleteButton.RegisterCallback<ClickEvent, Button>(DeleteAnimation, DeleteButton);
        UpdateAnimationLayers();
    }

    private void AddLayerAtIndex(int index,PaintingToolScript.PaintLayer LayerData)
    {
        VisualTreeAsset LayerInfo = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Painting Tool/UI/PaintLayerItem.uxml");
        VisualElement Layer = LayerInfo.CloneTree();
        LayerList.Insert(index, Layer);
        Button LayerButton = Layer.Q<Button>("LayerButton");
        LayerButton.text = (LayerData.LayerName);
        LayerButton.RegisterCallback<ClickEvent, Button>(SelectLayer, LayerButton);
        UpdateAnimationLayers();

        Toggle ToggleButton = Layer.Q<Toggle>("Toggle");
        ToggleButton.RegisterCallback<ClickEvent, Toggle>(ToggleVisibilty, ToggleButton);
        Button DeleteButton = Layer.Q<Button>("DeleteButton");
        DeleteButton.RegisterCallback<ClickEvent, Button>(DeleteLayer, DeleteButton);
        if (!LayerData.LayerVisible)
        {
            ToggleButton.value = false;
            ToggleVisibilty(null,ToggleButton);
        }
    }
    private void RemoveLayerAtIndex(int index)
    {
        if (PainterScript.DeleteLayer(index))
        {
            LayerList.RemoveAt(index);
            foreach (ScrollView FrameData in AnimationList.Query<ScrollView>("Layers").ToList())
            {
                FrameData.RemoveAt(FrameData.childCount-1);
            }
        }
    }
    private void RemoveAnimationAtIndex(int index)
    {
        if (PainterScript.DeleteAnimation(index))
        {
            AnimationList.RemoveAt(AnimationList.childCount-2);
        }
    }

    private void DeleteAnimation(ClickEvent click,Button AnimationToDelete)
    {
        Debug.Log("Delete " + AnimationList.IndexOf(AnimationToDelete.parent.parent.parent));
        RemoveAnimationAtIndex(AnimationList.IndexOf(AnimationToDelete.parent.parent.parent));
    }
    private void DeleteLayer(ClickEvent click,Button LayerToDelete)
    {
        RemoveLayerAtIndex(LayerList.IndexOf(LayerToDelete.parent.parent.parent.parent));
    }
    private void SelectAnimationAndFrame(ClickEvent click,Button button)
    {
        foreach (ScrollView LayerFrame in AnimationList.Query<ScrollView>("Layers").ToList())
        {
            if (LayerFrame.Contains(button))
            {
                PainterScript.UpdateSelectedLayer(LayerFrame.IndexOf(button),false);
                PainterScript.UpdateSelectedAnimation(AnimationList.IndexOf(LayerFrame.parent.parent.parent),false);
                break;
            }
        }
    }
    private void UpdateAnimationLayers()
    {
        foreach(ScrollView LayerData in AnimationList.Query<ScrollView>("Layers").ToList())
        { 
             while (LayerData.childCount < LayerList.childCount-1)
             {
                Button NewButton = new Button();
                NewButton.name = "AnimationLayer";
                NewButton.AddToClassList("layerButton");
                LayerData.Add(NewButton);
                NewButton.RegisterCallback<ClickEvent,Button>(SelectAnimationAndFrame,NewButton);
                NewButton.text = (LayerData.IndexOf(NewButton)+1).ToString();
             }
        }
    }

    private void ToggleVisibilty(ClickEvent clicked, Toggle ToggleButton)
    {
        int index = LayerList.IndexOf(ToggleButton.parent.parent);
        UnityEngine.Debug.Log(index);
        PainterScript.UpdateVisibilty(index,ToggleButton.value);
        

    }
    private void SelectLayer(ClickEvent Clicked, Button LayerButton)
    {
        int index = LayerList.IndexOf(LayerButton.parent.parent);
        if (index >= 0 && index < PainterScript.CanvasImage[0].Count)
        {
            PainterScript.UpdateSelectedLayer(index, false);
        }
    }
    private void SelectAnimation(ClickEvent Clicked,Button AnimationButton)
    {
        int index = AnimationList.IndexOf(AnimationButton.parent.parent.parent);

        if (index >= 0)
        {
            PainterScript.UpdateSelectedAnimation(index,false);
        }
    }
    private void AddLayer()
    {
        if (PainterScript == null)
        {
            return;
        }
        VisualTreeAsset LayerInfo = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Painting Tool/UI/PaintLayerItem.uxml");
        VisualElement Layer = LayerInfo.CloneTree();
        LayerList.Insert(LayerList.childCount-1,Layer);
        Button LayerButton = Layer.Q<Button>("LayerButton");
        LayerButton.text = ("Layer " + (LayerList.IndexOf(Layer)+1));
        LayerButton.RegisterCallback<ClickEvent, Button>(SelectLayer, LayerButton);
        UpdateAnimationLayers();
        PainterScript.AddLayer("Layer " + (LayerList.IndexOf(Layer) + 1));

        Toggle ToggleButton = Layer.Q<Toggle>("Toggle");
        ToggleButton.RegisterCallback<ClickEvent, Toggle>(ToggleVisibilty, ToggleButton);

        Button DeleteButton = Layer.Q<Button>("DeleteButton");
        DeleteButton.RegisterCallback<ClickEvent, Button>(DeleteLayer,DeleteButton);

        Button MoveUpButton = Layer.Q<Button>("UpButton");
        MoveUpButton.RegisterCallback<ClickEvent, Button>(MoveLayer,MoveUpButton);

        Button MoveDownButton = Layer.Q<Button>("DownButton");
        MoveDownButton.RegisterCallback<ClickEvent, Button>(MoveLayer, MoveDownButton);
    }
    private void ChangeColour()
    {
        if (PainterScript == null)
        {
            return;
        }
        PainterScript.ChangeColour(PrimaryColor);
    }

    private void Paint(ClickEvent Clicked)
    {
        if (PainterScript == null)
        {
            return;
        }
        if (!paint)
            return;

        VisualElement root = rootVisualElement;
        VisualElement Image = root.Q<VisualElement>("DisplayTexture");

        Vector2 position = Clicked.position;

        float x = position.x;
        //////x -= Image.worldBound.xMin;
        float y = position.y;
        //////y -= Image.worldBound.yMin;

        Vector2 maxBound = new Vector2(Image.worldBound.xMax,Image.worldBound.yMax);
        Vector2 minBound = new Vector2(Image.worldBound.xMin, Image.worldBound.yMin);
        float distance = maxBound.x - minBound.x;
        float distance2 = maxBound.y - minBound.y;
        x %= width; y %= height;
        x *= distance/width;
        y *= distance2/height;
        


        //Debug.Log(Image.worldBound.xMin + "  " + Image.worldBound.yMin + "   " + position);

        //if (x >= width)
        //    x /= width;
        //if (y >= height)
        //   y /= height;

        // This is relative to the VisualElement


        PainterScript.PressedPixel((int)x,(int)y);
    }
    private void SwapColour()
    {
        if (PainterScript == null)
        {
            return;
        }
        Color temp = PrimaryColor;
        PrimaryColor = SecondaryColor;
        SecondaryColor = temp;

        VisualElement root = rootVisualElement;
        root.Q<ColorField>("PrimaryColour").value = PrimaryColor;
        root.Q<ColorField>("SecondaryColour").value = SecondaryColor;

        ChangeColour();
    }

    private void UpdateColour()
    {
        if (PainterScript == null)
        {
            return;
        }
        PrimaryColor = PainterScript.SelectedColour;
        VisualElement root = rootVisualElement;
        root.Q<ColorField>("PrimaryColour").value = PrimaryColor;
    }

    private void UpdateLayerButtons()
    {
        foreach (Button button in LayerList.Query<Button>("LayerButton").ToList())
        {
            if (LayerList.IndexOf(button.parent.parent) == PainterScript.SelectedLayer)
            {
                button.AddToClassList("layerButtonSelected");
                button.RemoveFromClassList("layerButton");
            }
            else if (button.ClassListContains("layerButtonSelected"))
            {
                button.AddToClassList("layerButton");
                button.RemoveFromClassList("layerButtonSelected");
            }
        }
        UpdateFrameLayers();

    }
    private void UpdateFrameLayers()
    {
        foreach (ScrollView Layers in AnimationList.Query<ScrollView>("Layers").ToList())
        {
            foreach (Button LayerButton in Layers.Query<Button>("AnimationLayer").ToList())
            {
                if (Layers.IndexOf(LayerButton) == PainterScript.SelectedLayer && AnimationList.IndexOf(Layers.parent.parent.parent) == PainterScript.SelectedAnimation)
                {
                    LayerButton.AddToClassList("layerButtonSelected");
                    LayerButton.RemoveFromClassList("layerButton");
                }
                else if (LayerButton.ClassListContains("layerButtonSelected"))
                {
                    LayerButton.AddToClassList("layerButton");
                    LayerButton.RemoveFromClassList("layerButtonSelected");
                }
            }
        }
    }
    private void UpdateAnimationButtons()
    {
        UpdateFrameLayers();
        foreach (Button AnimationButton in AnimationList.Query<Button>("AnimationButton").ToList())
        {
            if (AnimationList.IndexOf(AnimationButton.parent.parent.parent) == PainterScript.SelectedAnimation)
            {
                AnimationButton.AddToClassList("layerButtonSelected");
                AnimationButton.RemoveFromClassList("layerButton");
            }
            else if (AnimationButton.ClassListContains("layerButtonSelected"))
            {
                AnimationButton.AddToClassList("layerButton");
                AnimationButton.RemoveFromClassList("layerButtonSelected");
            }
        }
    }
    private void MoveLayer(ClickEvent click, Button button)
    {
        Debug.Log("Movement " + LayerList.IndexOf(button.parent.parent.parent.parent));
        if (button.name == "UpButton")
        {
            if (PainterScript.MoveLayerUp(LayerList.IndexOf(button.parent.parent.parent.parent)))
            {
                VisualElement LayerToMove = button.parent.parent.parent.parent;
                int LayerIndex = LayerList.IndexOf(LayerToMove);
                LayerList.Remove(LayerToMove);
                LayerList.Insert(LayerIndex-1, LayerToMove);
            }
        }
        else if (button.name == "DownButton")
        {
            if (PainterScript.MoveLayerDown(LayerList.IndexOf(button.parent.parent.parent.parent)))
            {
                VisualElement LayerToMove = button.parent.parent.parent.parent;
                int LayerIndex = LayerList.IndexOf(LayerToMove);
                LayerList.Remove(LayerToMove);
                LayerList.Insert(LayerIndex + 1, LayerToMove);
            }
        }
        UpdateLayerButtons();
    }
    private void MoveAnimation(ClickEvent click, Button button)
    {
        if (button.name == "LeftButton")
        {
            if (PainterScript.MoveAnimationUp(AnimationList.IndexOf(button.parent.parent.parent)))
            {
                VisualElement AnimationToMove = button.parent.parent.parent;
                int AnimationIndex = AnimationList.IndexOf(AnimationToMove);
                AnimationList.Remove(AnimationToMove);
                AnimationList.Insert(AnimationIndex - 1, AnimationToMove);
            }
        }
        else if (button.name == "RightButton")
        {
            if (PainterScript.MoveAnimationDown(AnimationList.IndexOf(button.parent.parent.parent)))
            {
                VisualElement AnimationToMove = button.parent.parent.parent;
                int AnimationIndex = LayerList.IndexOf(AnimationToMove);
                AnimationList.Remove(AnimationToMove);
                AnimationList.Insert(AnimationIndex + 1, AnimationToMove);
            }
        }
        UpdateAnimationButtons();
    }
    public void ExportImage()
    {
        UpdateDisplayImage();
        DisplayImage.EncodeToPNG();
    }

    public void LoadImage()
    {

    }
    public void SaveImage() { }

    private void MouseOver(MouseEnterEvent mouse)
    {
        paint = true;
    }
    private void MouseOut(MouseLeaveEvent mouse)
    {
        paint = false;
    }

}
