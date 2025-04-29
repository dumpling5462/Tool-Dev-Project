using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.ShortcutManagement;
using UnityEditor.UIElements;
using System.IO;
using System;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
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
    bool isMouseDown = false;

    int brushSize = 1;

    [MenuItem("Unity Paint/Menu")]
    public static void ShowMenuWindow()
    {
        PaintingToolEditor window = GetWindow<PaintingToolEditor>();
        window.titleContent = new GUIContent("Pain Hub");
    }
    [MenuItem("Unity Paint/Help")]
    public static void ShowHelpWindow()
    {
        PaintingToolEditor window = GetWindow<PaintingToolEditor>();
        window.titleContent = new GUIContent("Pls end my life");
    }

    public void CreateGUI()
    { 
        VisualElement root = rootVisualElement;
        VisualTreeAsset asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Painting Tool/UI/PaintMenuUI.uxml");
        asset.CloneTree(root);
        StyleSheet sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Painting Tool/UI/PaintMenuStyleSheet.uss");
        root.styleSheets.Add(sheet);
        root.Q<Button>("NewButton").clicked +=OpenCanvasSizeMenu;
        root.Q<Button>("LoadButton").clicked += loadFileData;
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
        if (width < 1 || width > 512 || height < 1 || height > 512)
        {
            width = 16; height = 16;
            Debug.LogError("Size not valid");
            return;
        }
        StyleSheet sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Painting Tool/UI/PaintMenuStyleSheet.uss");
        root.styleSheets.Remove(sheet);
        root.Clear();
        VisualTreeAsset asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Painting Tool/UI/PaintCanvasUI.uxml");
        asset.CloneTree(root);
        sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Painting Tool/UI/PaintCanvasStyleSheet.uss");
        root.styleSheets.Add(sheet);

        InitializePainter();
        InitializePaintBindings();

        AddAnimation();
        AddLayer();
        UpdateAnimationButtons();
        UpdateLayerButtons();
        Brush();

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
        root.Q<Button>("ExportButton").clicked += OpenExportPopup;

        AnimationList = root.Q<ScrollView>("AnimationList");
        LayerList = root.Q<ScrollView>("LayerList");

        root.Q<Button>("AddLayerButton").clicked+=AddLayer;
        root.Q<Button>("AddAnimationButton").clicked += AddAnimation;

        root.Q<Button>("UndoButton").clicked+= UndoChange;
        root.Q<Button>("RedoButton").clicked+= RedoChange;

        root.Q<Button>("IncreaseBrush").clicked += IncreaseBrushSize;
        root.Q<Label>("BrushText").text = brushSize.ToString();
        root.Q<Button>("DecreaseBrush").clicked += DecreaseBrushSize;

        VisualElement DisplayTex = root.Q<VisualElement>("DisplayTexture");
        DisplayTex.RegisterCallback<ClickEvent>(Paint);
        DisplayTex.RegisterCallback<PointerEnterEvent>(MouseOver);
        DisplayTex.RegisterCallback<PointerLeaveEvent>(MouseOut);
        DisplayTex.RegisterCallback<PointerDownEvent>(MouseDown);
        DisplayTex.RegisterCallback<PointerUpEvent>(MouseUp);
        DisplayTex.RegisterCallback<PointerMoveEvent>(MouseMove);

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

    private static PaintingToolEditor EditorReference;
    private static void SetEditorReference()
    {
        if (EditorReference == null || EditorReference.Equals(null))
        {
            EditorReference = GetWindow<PaintingToolEditor>();
        }
    }
    [Shortcut("Painting Tool/Brush", KeyCode.B)]
    private static void BrushShortcut()
    {
        SetEditorReference();
        EditorReference.Brush();
    }
    private void Brush()
    {
        if (PainterScript == null)
        {
            return;
        }
        PainterScript.SelectedBrush = PaintingToolScript.BrushMode.Paintbrush;
        UpdateVisual();
    }
    [Shortcut("Painting Tool/Eraser", KeyCode.H)]
    private static void EraserShortcut()
    {
        SetEditorReference();
        EditorReference.Eraser();
    }
    private void Eraser()
    {
        if (PainterScript == null)
        {
            return;
        }
        PainterScript.SelectedBrush = PaintingToolScript.BrushMode.Eraser;
        UpdateVisual();
    }
    [Shortcut("Painting Tool/Fill", KeyCode.G)]
    private static void PaintBucketShortcut()
    {
       SetEditorReference();
       EditorReference.PaintBucket();
    }
    public void PaintBucket()
    {
        if (PainterScript == null)
        {
            return;
        }
        PainterScript.SelectedBrush = PaintingToolScript.BrushMode.PaintBucket;
        UpdateVisual();
    }
    [Shortcut("Painting Tool/EyeDropper", KeyCode.I)]
    private static void EyeDropperShortcut()
    {
        SetEditorReference();
        EditorReference.EyeDropper();
    }
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

        VisualElement root = rootVisualElement;
        Button BrushButton = root.Q<Button>("BrushButton");
        Button EraserButton = root.Q<Button>("EraserButton");
        Button EyeDropperButton = root.Q<Button>("EyeDropButton");
        Button PaintBucketButton = root.Q<Button>("PaintBucketButton");

        SwapButtonStyle(BrushButton,false);
        SwapButtonStyle(EraserButton,false);
        SwapButtonStyle(EyeDropperButton,false);
        SwapButtonStyle(PaintBucketButton,false);
        
        switch (PainterScript.SelectedBrush)
        {
            case PaintingToolScript.BrushMode.Paintbrush:
                SwapButtonStyle(BrushButton,true);
                break;
            case PaintingToolScript.BrushMode.Eraser:
                SwapButtonStyle(EraserButton,true);
                break;
            case PaintingToolScript.BrushMode.PaintBucket:
                SwapButtonStyle(PaintBucketButton,true);
                break;
            case PaintingToolScript.BrushMode.Eyedropper:
                SwapButtonStyle(EyeDropperButton,true);
                break;
        }
    }

    private void SwapButtonStyle(Button Button, bool Swap)
    {
        if (Swap)
        { 
            if (Button.ClassListContains("PaintToolbutton"))
            {
                Button.RemoveFromClassList("PaintToolbutton");
                Button.AddToClassList("PaintToolButtonSelected");
            }
        }
        else
        {
            if (Button.ClassListContains("PaintToolButtonSelected"))
            {
                Button.RemoveFromClassList("PaintToolButtonSelected");
                Button.AddToClassList("PaintToolbutton");
            }
        }
    }


    [Shortcut("Painting Tool/IncreaseBrush", KeyCode.Equals)]
    [Shortcut("Painting Tool/IncreaseBrush 2", KeyCode.RightBracket)]
    private static void IncreaseBrushSizeShortcut()
    {
        Debug.Log("Increase");
        SetEditorReference();
        EditorReference.IncreaseBrushSize();
    }
    private void IncreaseBrushSize()
    {
        if (PainterScript == null)
        {
            return;
        }
        if (brushSize >= 1 && (brushSize < width || brushSize < height))
        {
            brushSize++;
            PainterScript.BrushSize = brushSize;
            rootVisualElement.Q<Label>("BrushText").text = brushSize.ToString();
        }
    }
    [Shortcut("Painting Tool/DecreaseBrush", KeyCode.Minus)]
    [Shortcut("Painting Tool/DecreaseBrush 2", KeyCode.LeftBracket)]
    private static void DecreaseBrushSizeShortcut()
    {
        SetEditorReference();
        EditorReference.DecreaseBrushSize();
    }
    private void DecreaseBrushSize() 
    {
        Debug.Log("Decrease");
        if (PainterScript == null)
        {  
            return; 
        }
        if (brushSize > 1)
        {
            brushSize--;
            PainterScript.BrushSize = brushSize;
            rootVisualElement.Q<Label>("BrushText").text = brushSize.ToString();
        }
    }
    [Shortcut("painting Tool/Undo Change",KeyCode.Z,ShortcutModifiers.Alt | ShortcutModifiers.Shift)]
    private static void UndoChangeShortcut()
    {
        SetEditorReference();
        EditorReference.UndoChange();
    }
    private void UndoChange()
    {
        if (PainterScript == null)
        {
            return;
        }
        PainterScript.Undo();
    }
    [Shortcut("painting Tool/Redo Change", KeyCode.Y, ShortcutModifiers.Alt | ShortcutModifiers.Shift)]
    private static void RedoChangeShortcut()
    {
        SetEditorReference();
        EditorReference.RedoChange();
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
        Button LeftButton = Frame.Q<Button>("LeftButton");
        LeftButton.RegisterCallback<ClickEvent, Button>(MoveAnimation, LeftButton);
        Button RightButton = Frame.Q<Button>("RightButton");
        RightButton.RegisterCallback<ClickEvent, Button>(MoveAnimation, RightButton);
        UpdateAnimationLayers();
        UpdateAnimationButtons();
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

        Button MoveUpButton = Layer.Q<Button>("UpButton");
        MoveUpButton.RegisterCallback<ClickEvent, Button>(MoveLayer, MoveUpButton);

        Button MoveDownButton = Layer.Q<Button>("DownButton");
        MoveDownButton.RegisterCallback<ClickEvent, Button>(MoveLayer, MoveDownButton);

        Button RenameButton = Layer.Q<Button>("RenameButton");
        RenameButton.RegisterCallback<ClickEvent, Button>(OpenRenameWindow, RenameButton);
        if (!LayerData.LayerVisible)
        {
            ToggleButton.value = false;
            ToggleVisibilty(null,ToggleButton);
        }
        UpdateLayerButtons();
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
        UpdateLayerButtons();
    }
    private void RemoveAnimationAtIndex(int index)
    {
        Debug.Log("delete anim");
        if (PainterScript.DeleteAnimation(index))
        {
            AnimationList.RemoveAt(AnimationList.childCount-2);
        }
        UpdateAnimationButtons();
    }

    private void DeleteAnimation(ClickEvent click,Button AnimationToDelete)
    {
        RemoveAnimationAtIndex(AnimationList.IndexOf(FindChild(AnimationList,AnimationToDelete)));
        UpdateAnimationButtons();
    }
    private void DeleteLayer(ClickEvent click,Button LayerToDelete)
    {
        RemoveLayerAtIndex(LayerList.IndexOf(FindChild(LayerList,LayerToDelete)));
        UpdateLayerButtons();
    }
    private void SelectAnimationAndFrame(ClickEvent click,Button button)
    {
        foreach (ScrollView LayerFrame in AnimationList.Query<ScrollView>("Layers").ToList())
        {
            if (LayerFrame.Contains(button))
            {
                PainterScript.UpdateSelectedLayer(LayerFrame.IndexOf(button),false);
                PainterScript.UpdateSelectedAnimation(AnimationList.IndexOf(FindChild(AnimationList,LayerFrame)),false);
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
        int index = LayerList.IndexOf(FindChild(LayerList, ToggleButton));
        UnityEngine.Debug.Log(index);
        PainterScript.UpdateVisibilty(index,ToggleButton.value);
        

    }
    private void OpenRenameWindow(ClickEvent clicked,Button button)
    {
        int index = LayerList.IndexOf(FindChild(LayerList,button));

        VisualElement root = rootVisualElement;

        if (root.Q<VisualElement>("RenamePopupWindow") != null) 
        {
            return;
        }

        VisualTreeAsset RenamePopup = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Painting Tool/UI/RenamePopupWindow.uxml");
        //if ( root.ClassListContains(RenamePopup))
        //{
        //    return;
        //}
        VisualElement RenamePopupWindow = RenamePopup.CloneTree();
        RenamePopupWindow.style.position = Position.Absolute;
        RenamePopupWindow.style.top = 0;
        RenamePopupWindow.style.left = 0;
        RenamePopupWindow.style.right = 0;
        RenamePopupWindow.style.bottom = 0;

        RenamePopupWindow.style.flexDirection = FlexDirection.Row;
        RenamePopupWindow.style.justifyContent = Justify.Center;
        RenamePopupWindow.style.alignItems = Align.Center;
        root.Add(RenamePopupWindow);

        RenamePopupWindow.Q<Button>("ExitButton").clicked+= () => { root.Remove(RenamePopupWindow); };
        TextField textField = RenamePopupWindow.Q<TextField>("NameField");
        textField.name = index.ToString();
        textField.value = PainterScript.CanvasImage[0][index].LayerName;
        RenamePopupWindow.Q<Button>("ConfirmButton").RegisterCallback<ClickEvent,VisualElement>(RenamedLayer,RenamePopupWindow);

    }

    private void RenamedLayer(ClickEvent clicked, VisualElement PopupWindow)
    {
        TextField textfield = PopupWindow.Q<TextField>();
        int index = int.Parse(textfield.name);
        textfield.label = index.ToString();
        if (textfield.value != "")
        {
            PainterScript.UpdateName(index,textfield.value);
            LayerList[index].Q<Button>("LayerButton").text = textfield.value;
        }
        rootVisualElement.Remove(PopupWindow);
    }

    private void SelectLayer(ClickEvent Clicked, Button LayerButton)
    {
        int index = LayerList.IndexOf(FindChild(LayerList,LayerButton));
        if (index >= 0 && index < PainterScript.CanvasImage[0].Count)
        {
            PainterScript.UpdateSelectedLayer(index, false);
        }
    }
    private void SelectAnimation(ClickEvent Clicked,Button AnimationButton)
    {
        int index = AnimationList.IndexOf(FindChild(AnimationList,AnimationButton));

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

        Button RenameButton = Layer.Q<Button>("RenameButton");
        RenameButton.RegisterCallback<ClickEvent, Button>(OpenRenameWindow,RenameButton);
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
        if (isPainting)
            return;
        Vector2 mousePosition = Clicked.position;
        Vector2 PaintPosition = CalculateOffset(mousePosition.x,mousePosition.y);
        PainterScript.PressedPixel((int)PaintPosition.x,(int)PaintPosition.y);

    }

    private Vector2 CalculateOffset(float x, float y)
    {
        VisualElement root = rootVisualElement;
        VisualElement Display = root.Q<VisualElement>("DisplayTexture");

        Vector2 maxBound = new Vector2(Display.worldBound.xMax, Display.worldBound.yMax);
        Vector2 minBound = new Vector2(Display.worldBound.xMin, Display.worldBound.yMin);

        x = x - minBound.x;
        y = y - minBound.y;

        // Element size
        float elementWidth = maxBound.x - minBound.x;
        float elementHeight = maxBound.y - minBound.y;


        // How the texture is scaled to fit
        float scaleX = elementWidth / width;
        float scaleY = elementHeight / height;

        float scale = Mathf.Min(scaleX, scaleY);

        float textureDisplayWidth = width * scale;
        float textureDisplayHeight = height * scale;

        // Offsets if the texture is centered
        float offsetX = (elementWidth - textureDisplayWidth) / 2;
        float offsetY = (elementHeight - textureDisplayHeight) / 2;

        // Mouse position relative to texture
        float textureX = (x - offsetX) / scale;
        float textureY = (y - offsetY) / scale;
        textureY = height - textureY;

        return (new Vector2(textureX,textureY));
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
            if (LayerList.IndexOf(FindChild(LayerList,button)) == PainterScript.SelectedLayer)
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
                if (Layers.IndexOf(LayerButton) == PainterScript.SelectedLayer && AnimationList.IndexOf(FindChild(AnimationList,LayerButton)) == PainterScript.SelectedAnimation)
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
            if (AnimationList.IndexOf(FindChild(AnimationList,AnimationButton)) == PainterScript.SelectedAnimation)
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
        VisualElement OuterLayerElement = FindChild(LayerList,button);
        if (button.name == "UpButton")
        {
            if (PainterScript.MoveLayerUp(LayerList.IndexOf(OuterLayerElement)))
            {
                VisualElement LayerToMove = OuterLayerElement;
                int LayerIndex = LayerList.IndexOf(LayerToMove);
                LayerList.Remove(LayerToMove);
                LayerList.Insert(LayerIndex-1, LayerToMove);
            }
        }
        else if (button.name == "DownButton")
        {
            if (PainterScript.MoveLayerDown(LayerList.IndexOf(OuterLayerElement)))
            {
                VisualElement LayerToMove = OuterLayerElement;
                int LayerIndex = LayerList.IndexOf(LayerToMove);
                LayerList.Remove(LayerToMove);
                LayerList.Insert(LayerIndex + 1, LayerToMove);
            }
        }
        UpdateLayerButtons();
    }

    private VisualElement FindChild(VisualElement parent, VisualElement childobject)
    {
        while (parent.IndexOf(childobject) == -1)
        {
            childobject = childobject.parent;
        }
        return childobject;
    }
    private void MoveAnimation(ClickEvent click, Button button)
    {
        VisualElement OuterAnimationElement = FindChild(AnimationList,button);
        Debug.Log("move " + AnimationList.IndexOf(OuterAnimationElement));
        if (button.name == "LeftButton")
        {
            if (PainterScript.MoveAnimationUp(AnimationList.IndexOf(OuterAnimationElement)))
            {
                //VisualElement AnimationToMove = OuterAnimationElement;
                //int AnimationIndex = AnimationList.IndexOf(AnimationToMove);
                //AnimationList.Remove(AnimationToMove);
                //AnimationList.Insert(AnimationIndex - 1, AnimationToMove);
            }
        }
        else if (button.name == "RightButton")
        {
            if (PainterScript.MoveAnimationDown(AnimationList.IndexOf(OuterAnimationElement)))
            {
                //VisualElement AnimationToMove = OuterAnimationElement;
                //int AnimationIndex = LayerList.IndexOf(AnimationToMove);
                //AnimationList.Remove(AnimationToMove);
                //AnimationList.Insert(AnimationIndex + 1, AnimationToMove);
            }
        }
        UpdateAnimationButtons();
    }

    public void OpenExportPopup()
    {
        VisualElement root = rootVisualElement;

        if (root.Q<VisualElement>("ExportPopupWindow") != null)
        {
            return;
        }

        VisualTreeAsset ExportPopup = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Painting Tool/UI/ExportPopup.uxml");
        VisualElement ExportPopupWindow = ExportPopup.CloneTree();
        ExportPopupWindow.style.position = Position.Absolute;
        ExportPopupWindow.style.top = 0;
        ExportPopupWindow.style.left = 0;
        ExportPopupWindow.style.right = 0;
        ExportPopupWindow.style.bottom = 0;

        ExportPopupWindow.style.flexDirection = FlexDirection.Row;
        ExportPopupWindow.style.justifyContent = Justify.Center;
        ExportPopupWindow.style.alignItems = Align.Center;
        root.Add(ExportPopupWindow);

        ExportPopupWindow.Q<Button>("CancelButton").clicked += () => { root.Remove(ExportPopupWindow); };
        ExportPopupWindow.Q<Button>("ExportImageButton").RegisterCallback<ClickEvent,VisualElement>(ExportImage,ExportPopupWindow);
        ExportPopupWindow.Q<Button>("ExportSpriteSheetButton").RegisterCallback<ClickEvent, VisualElement>(ExportSpriteSheet, ExportPopupWindow);
    }
    public void ExportImage(ClickEvent clicked, VisualElement Popupwindow)
    {
        rootVisualElement.Remove(Popupwindow);
        Texture2D ExportImage;
        ExportImage = PainterScript.GetExportImage();
        if (ExportImage == null)
            return;
        byte[] pngImage = ExportImage.EncodeToPNG();
        DestroyImmediate(ExportImage);
        if (pngImage == null)
        {
            Debug.Log("Can't export Image");
            return;
        }
        string path = EditorUtility.SaveFilePanel("save Image as PNG","","CoolPaint!","png");
        if (string.IsNullOrEmpty(path))
        {
            Debug.Log("Canceled save");
            return;
        }
        File.WriteAllBytes(path, pngImage);
    }
    public void ExportSpriteSheet(ClickEvent clicked, VisualElement Popupwindow)
    {
        rootVisualElement.Remove(Popupwindow);
        Texture2D ExportImage;
        ExportImage = PainterScript.GetExportImages();
        if (ExportImage == null)
            return;
        byte[] pngImage = ExportImage.EncodeToPNG();
        DestroyImmediate(ExportImage);
        if (pngImage == null)
        {
            Debug.Log("Can't export Image");
            return;
        }
        string path = EditorUtility.SaveFilePanel("save Image as PNG", "", "CoolPaint!", "png");
        if (string.IsNullOrEmpty(path))
        {
            Debug.Log("Canceled save");
            return;
        }
        File.WriteAllBytes(path, pngImage);
    }
    public void SaveImage() 
    {
        string path = EditorUtility.SaveFilePanel("Save Image","","UnityPaint","pain");
        //string path = EditorUtility.SaveFolderPanel("Save File", "Saves", "UnityPaint");
        Debug.Log(path);
        if (string.IsNullOrEmpty(path))
            return;
        PaintFileSaver fileSaver = new PaintFileSaver();
        fileSaver.CreateSave(path,width,height,PainterScript.SelectedAnimation,PainterScript.SelectedLayer,PainterScript.CanvasImage);
    }

    private void MouseOver(PointerEnterEvent mouse)
    {
        paint = true;
    }
    private void MouseOut(PointerLeaveEvent mouse)
    {
        paint = false;
    }
    private void MouseUp(PointerUpEvent mouse)
    {
        if (isPainting)
        {
            Vector2 position = mouse.position;
            Vector2 PaintPosition = CalculateOffset(position.x, position.y);
            PainterScript.PressedPixel((int)PaintPosition.x, (int)PaintPosition.y,true);
            isPainting = false;
        }
        isMouseDown = false;
    }
    private void MouseDown(PointerDownEvent mouse)
    {
        isMouseDown = true;
    }
    private bool isPainting;
    private void MouseMove(PointerMoveEvent mouse)
    {
        if (PainterScript == null)
        {
            return;
        }
        if (!paint)
            return;
        if (!isMouseDown)
        {
            return;
        }
        if (PainterScript.SelectedBrush == PaintingToolScript.BrushMode.Paintbrush || PainterScript.SelectedBrush == PaintingToolScript.BrushMode.Eraser)
        {
            if (!isPainting)
                PainterScript.RegisterChange();
        isPainting = true;
        Vector2 position = mouse.position;
        Vector2 PaintPosition = CalculateOffset(position.x, position.y);
        PainterScript.PressedPixel((int)PaintPosition.x, (int)PaintPosition.y,true);
        }
    }

    Texture2D CopiedLayer;

    [Shortcut("Painting Tool/Copy Layer", KeyCode.C,ShortcutModifiers.Shift | ShortcutModifiers.Alt)]
    private static void CopyShortcut()
    {
        Debug.Log("Copied Layer");
        SetEditorReference();
        EditorReference.CopyLayer();
    }
    private void CopyLayer()
    {
        if (PainterScript == null)
            return;

        Color[] LayerImage = PainterScript.CanvasImage[PainterScript.SelectedAnimation][PainterScript.SelectedLayer].LayerImage.GetPixels();
        if (CopiedLayer != null)
            DestroyImmediate(CopiedLayer);
        CopiedLayer = new Texture2D(width,height);
        CopiedLayer.SetPixels(LayerImage);
    }
    [Shortcut("Painting Tool/Paste Layer", KeyCode.V, ShortcutModifiers.Shift | ShortcutModifiers.Alt)]
    private static void PasteShortcut()
    {
        SetEditorReference();
        EditorReference.PasteLayer();
    }
    private void PasteLayer()
    {
        if (PainterScript == null)
            return;

        if (CopiedLayer!=null)
        {
            PainterScript.PasteLayer(CopiedLayer);
        }
    }

    private void loadFileData()
    {
        string path = EditorUtility.OpenFilePanel("Load Image","","png,jpg,jpeg,pain,asset");

        if (string.IsNullOrEmpty(path))
        {
            Debug.Log("Load canceled.");
            return;
        }
        string extension = Path.GetExtension(path).ToLower();

        if (extension == ".asset" ||extension == ".pain")
        {
            if (extension == ".pain")
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                path = path.Replace(".pain", ".asset");
            }
            PaintSO PaintFile = AssetDatabase.LoadAssetAtPath<PaintSO>(path);
            if (PaintFile != null)
            {
                if (PaintFile is PaintSO)
                {
                    width = PaintFile.width;
                    height = PaintFile.height;
                    LoadPainter(PaintFile);
                }
                else
                {
                    Debug.LogError("The file isn't a .pain file");
                }
            }
            else
            {
                Debug.LogError("Could not load the file at the path");
            }
        }
        else if (extension == ".png" || extension == ".jpg" || extension == ".jpeg")
        {
            byte[] fileData = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(1, 1);
            tex.LoadImage(fileData);
            width = tex.width;
            height = tex.height;
            LoadPainter(tex);
        }
        else
        {
            Debug.LogError("wrong file type selected");
            return;
        }
    }
    private void LoadPainter(Texture2D TextureToLoad)
    {
        VisualElement root = rootVisualElement;
        StyleSheet sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Painting Tool/UI/PaintMenuStyleSheet.uss");
        root.styleSheets.Remove(sheet);
        root.Clear();
        VisualTreeAsset asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Painting Tool/UI/PaintCanvasUI.uxml");
        asset.CloneTree(root);
        sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Painting Tool/UI/PaintCanvasStyleSheet.uss");
        root.styleSheets.Add(sheet);

        InitializePainter();
        InitializePaintBindings();

        AddAnimation();
        LoadLayer(TextureToLoad);
        UpdateAnimationButtons();
        UpdateLayerButtons();
        Brush();

        DisplayImage = PainterScript.GetDisplayImage();
        VisualElement Image = root.Q<VisualElement>("DisplayTexture");
        Image.style.backgroundImage = new StyleBackground(DisplayImage);
    }

    public void LoadLayer(Texture2D TextureToLoad)
    {
        if (PainterScript == null)
        {
            return;
        }
        VisualTreeAsset LayerInfo = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Painting Tool/UI/PaintLayerItem.uxml");
        VisualElement Layer = LayerInfo.CloneTree();
        LayerList.Insert(LayerList.childCount - 1, Layer);
        Button LayerButton = Layer.Q<Button>("LayerButton");
        LayerButton.text = ("Layer " + (LayerList.IndexOf(Layer) + 1));
        LayerButton.RegisterCallback<ClickEvent, Button>(SelectLayer, LayerButton);
        UpdateAnimationLayers();
        PainterScript.LoadLayer(TextureToLoad,"Layer " + (LayerList.IndexOf(Layer) + 1));
        DestroyImmediate(TextureToLoad);

        Toggle ToggleButton = Layer.Q<Toggle>("Toggle");
        ToggleButton.RegisterCallback<ClickEvent, Toggle>(ToggleVisibilty, ToggleButton);

        Button DeleteButton = Layer.Q<Button>("DeleteButton");
        DeleteButton.RegisterCallback<ClickEvent, Button>(DeleteLayer, DeleteButton);

        Button MoveUpButton = Layer.Q<Button>("UpButton");
        MoveUpButton.RegisterCallback<ClickEvent, Button>(MoveLayer, MoveUpButton);

        Button MoveDownButton = Layer.Q<Button>("DownButton");
        MoveDownButton.RegisterCallback<ClickEvent, Button>(MoveLayer, MoveDownButton);

        Button RenameButton = Layer.Q<Button>("RenameButton");
        RenameButton.RegisterCallback<ClickEvent, Button>(OpenRenameWindow, RenameButton);
    }

    private void LoadPainter(PaintSO PaintFile)
    {
        VisualElement root = rootVisualElement;
        StyleSheet sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Painting Tool/UI/PaintMenuStyleSheet.uss");
        root.styleSheets.Remove(sheet);
        root.Clear();
        VisualTreeAsset asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Painting Tool/UI/PaintCanvasUI.uxml");
        asset.CloneTree(root);
        sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Painting Tool/UI/PaintCanvasStyleSheet.uss");
        root.styleSheets.Add(sheet);

        InitializePainter();
        InitializePaintBindings();



        UpdateAnimationButtons();
        UpdateLayerButtons();
        Brush();

        DisplayImage = PainterScript.GetDisplayImage();
        VisualElement Image = root.Q<VisualElement>("DisplayTexture");
        Image.style.backgroundImage = new StyleBackground(DisplayImage);
    }
}
