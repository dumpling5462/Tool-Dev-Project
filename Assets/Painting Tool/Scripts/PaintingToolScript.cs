using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class PaintingToolScript
{
    public event Action UpdateCanvas;
    public event Action ColourChange;
    public event Action LayerSelected;
    public event Action AnimationSelected;

    public event Action<int,PaintLayer> LayerAdded;
    public event Action<int> LayerRemoved;
    public event Action<int> AnimationAdded;
    public event Action<int> AnimationRemoved;

    public event Action<int,bool> VisibiltyChange;
    public event Action<int, string> NameChange;

    public event Action<int, int> LayerMoved;
    public enum BrushMode
    {
        Paintbrush,
        Eraser,
        PaintBucket,
        Eyedropper
    }
    public struct PaintLayer
    {
        public Texture2D LayerImage;
        public string LayerName;
        public bool LayerVisible;
    }

    public int CanvasWidth;
    public int CanvasHeight;
    public List<List<PaintLayer>> CanvasImage = new List<List<PaintLayer>>();

    public int SelectedLayer = 0;
    public int SelectedAnimation = 0;

    public Color SelectedColour = new Color(1,1,1,1);
    public BrushMode SelectedBrush = BrushMode.Paintbrush;
    public int BrushSize;

    private readonly int Stacksize = 24;
    private UndoRedoChangeStack UndoRedoStack;

    private bool firstUndo = true;
    private ChangesStack.Changes? StoredUndoChange;

    private bool IsPerformingUndoRedo = false;

    public void Initialize(int width, int height)
    {
        UndoRedoStack = new UndoRedoChangeStack();
        UndoRedoStack.initialize(Stacksize);
        CanvasImage = new List<List<PaintLayer>>();
        CanvasHeight = height;
        CanvasWidth = width;
    }
    public void AddAnimation()
    {
        List<PaintLayer> Animation = new List<PaintLayer>();
        if (CanvasImage.Count >= 1)
        {
            UnityEngine.Debug.Log("Adding layer info");
            foreach (PaintLayer layer in CanvasImage[0])
            {
                PaintLayer newLayer = new PaintLayer();
                newLayer.LayerImage = new Texture2D(CanvasWidth,CanvasHeight);
                newLayer.LayerImage.filterMode = FilterMode.Point;
                newLayer.LayerName = layer.LayerName;
                newLayer.LayerVisible = true;
                FillTexture2D(newLayer.LayerImage);
                Animation.Add(newLayer);
            }
        }
        CanvasImage.Add(Animation);
        if (!IsPerformingUndoRedo && CanvasImage.Count > 1)
        {
            AddedMade(-1, CanvasImage.Count - 1);
        }
    }
    public void AddLayer(string name)
    {
        foreach(List<PaintLayer> Frame in CanvasImage)
        {
            PaintLayer layer = new PaintLayer();
            layer.LayerImage = new Texture2D(CanvasWidth, CanvasHeight);
            layer.LayerName = name;
            layer.LayerVisible = true;
            layer.LayerImage.filterMode = FilterMode.Point;
            FillTexture2D(layer.LayerImage);
            Frame.Add(layer);
        }
        if (!IsPerformingUndoRedo && CanvasImage[SelectedAnimation].Count > 1)
        {
            AddedMade(CanvasImage[0].Count-1,-1);
        }
    }
    public void RemoveLayer(int ID)
    {
        if (ID >= CanvasImage[0].Count)
        {
            return;
        }
        for (int i = 0; i < CanvasImage.Count; i++)
        {
            CanvasImage[i].RemoveAt(ID);
        }
        if (SelectedLayer == ID)
        {
            UpdateSelectedLayer(0,true);
        }
        UpdateDisplayImage();
    }
    public void RemoveAnimation(int ID)
    {
        if (ID >= CanvasImage.Count)
        {
            return;
        }
        CanvasImage.RemoveAt(ID);
        if (SelectedAnimation == ID)
        {
            UpdateSelectedAnimation(0, true);
            UpdateSelectedLayer(0,true);
        }
        UpdateDisplayImage();
    }

    public void Brush()
    {

    }
    public void UpdateDisplayImage()
    {
        UpdateCanvas?.Invoke();
    }

    public Texture2D GetDisplayImage()
    {
        Texture2D DisplayImage = new Texture2D(CanvasWidth,CanvasHeight);
        DisplayImage.filterMode = FilterMode.Point;
        if (CanvasImage[SelectedAnimation].Count > 1)
        {
            List<PaintLayer> layers = new List<PaintLayer>(CanvasImage[SelectedAnimation]);
            layers.Reverse();

            foreach (PaintLayer layer in layers)
            {
                if (!layer.LayerVisible)
                    continue;
                for (int y = 0; y < CanvasHeight; y++)
                {
                    for (int x = 0; x < CanvasWidth; x++)
                    {
                        Color Pixel = layer.LayerImage.GetPixel(x, y);
                        if (Pixel.a > 0)
                        {
                            DisplayImage.SetPixel(x, y, Pixel);
                        }
                    }
                }
            }
        }
        else if (CanvasImage[SelectedAnimation][0].LayerVisible)
        {
            for (int y = 0; y < CanvasHeight; y++)
            {
                for (int x = 0; x < CanvasWidth; x++)
                {
                    Color Pixel = CanvasImage[SelectedAnimation][0].LayerImage.GetPixel(x, y);
                    if (Pixel.a > 0)
                    {
                        DisplayImage.SetPixel(x, y, Pixel);
                    }
                }
            }
        }
        DisplayImage.Apply();
        return DisplayImage;
    }

    public void SwitchBrushMode(BrushMode newBrushMode)
    {
        SelectedBrush = newBrushMode;
    }

    public void PressedPixel(int x,int y)
    {
        //if (x < 0 || x >= CanvasWidth || y < 0 || y >= CanvasHeight)
        //    return;
        ChangeMade();
        UnityEngine.Debug.Log("Paint " + SelectedAnimation + " " + SelectedLayer);
        switch (SelectedBrush)
        {
            case BrushMode.Paintbrush:
                PaintPixel(x,y, SelectedColour);
                break;
            case BrushMode.PaintBucket:
                Fill(x,y,GetPixel(x,y));
                break;
            case BrushMode.Eraser:
                PaintPixel(x, y, new Color(0,0,0,0));
                break;
            case BrushMode.Eyedropper:
                EyeDropper(x,y);
                break;
        }

        CanvasImage[SelectedAnimation][SelectedLayer].LayerImage.Apply();
        UpdateDisplayImage();
    }

    private void PaintPixel(int x,int y,Color selectedColour)
    {
        CanvasImage[SelectedAnimation][SelectedLayer].LayerImage.SetPixel(x,y,selectedColour);
    }
    private void EyeDropper(int x, int y)
    {
        Color newColor = GetPixel(x,y);
        if (newColor.a > 0)
        {
            SelectedColour = newColor;
            ColourChange?.Invoke();
        }
    }
    private void Fill(int x, int y, Color ColorToFill)
    {

    }
    private Color GetPixel(int x, int y)
    {
       return CanvasImage[SelectedAnimation][SelectedLayer].LayerImage.GetPixel(x,y);
    }
    public void ChangeColour(Color ColourChange)
    {
        SelectedColour = ColourChange;
    }

    public void Undo()
    {
        if (!UndoRedoStack.isEmpty())
        {
        IsPerformingUndoRedo = true;
        ChangesStack.Changes? CurrentChange = null;
            if (firstUndo)
            {
                ChangesStack.Changes FirstChange = GetChange();
                if (!FirstChange.Delete && !FirstChange.Added)
                {
                    firstUndo = false;
                    UndoRedoStack.Redopush(FirstChange);
                }
            }
        if (StoredUndoChange != null)
        {
            CurrentChange = UndoRedoStack.pop(StoredUndoChange);
        }
        else
        {
            CurrentChange = UndoRedoStack.pop(GetChange());
        }
        StoredUndoChange = CurrentChange;
        if (CurrentChange != null)
        {
            ApplyChange((ChangesStack.Changes)CurrentChange,false);
        }
        IsPerformingUndoRedo = false;
        }
    }
    public void Redo()
    {
        if (!UndoRedoStack.Redo.isEmpty())
        {
        IsPerformingUndoRedo = true;
            ChangesStack.Changes? CurrentChange = UndoRedoStack.Redopop();
            UndoRedoStack.Redopush(CurrentChange.Value);
        if (CurrentChange != null)
        {
            ApplyChange((ChangesStack.Changes)CurrentChange,true);
        }
        IsPerformingUndoRedo = false;
        }
    }
    public void ChangeMade()
    {
        UnityEngine.Debug.Log("ChangeMade");
        if (IsPerformingUndoRedo)
        {
            return;
        }
        StoredUndoChange = null;
        UndoRedoStack.push(GetChange());
    }
    private ChangesStack.Changes GetChange()
    {
        ChangesStack.Changes NewChange;
        NewChange = new ChangesStack.Changes();
        NewChange.SelectedLayer = SelectedLayer;
        NewChange.SelectedAnim = SelectedAnimation;
        PaintLayer ChangedLayer;
        Color[] pixels = CanvasImage[SelectedAnimation][SelectedLayer].LayerImage.GetPixels();
        Texture2D tex = new Texture2D(CanvasWidth, CanvasHeight);
        tex.SetPixels(pixels);
        tex.Apply();
        ChangedLayer.LayerImage = tex;
        ChangedLayer.LayerName = CanvasImage[SelectedAnimation][SelectedLayer].LayerName;
        ChangedLayer.LayerVisible = CanvasImage[SelectedAnimation][SelectedLayer].LayerVisible;


        NewChange.layer = ChangedLayer;
        NewChange.Delete = false;

        return NewChange;
    }
    public void ApplyChange(ChangesStack.Changes change,bool redo)
    {
        if (!change.Delete && !change.Added && !change.Move)
        {
            if (change.layer.LayerVisible != CanvasImage[0][change.SelectedLayer].LayerVisible)
            {
                foreach(List<PaintLayer> Layers in CanvasImage)
                {
                    PaintLayer layer = Layers[change.SelectedLayer];
                    layer.LayerVisible = change.layer.LayerVisible;
                    Layers[change.SelectedLayer] = layer;
                }
                VisibiltyChange?.Invoke(change.SelectedLayer,change.layer.LayerVisible);
            }
            if (change.layer.LayerName != CanvasImage[0][change.SelectedLayer].LayerName)
            {
                foreach (List<PaintLayer> Layers in CanvasImage)
                {
                    PaintLayer layer = Layers[change.SelectedLayer];
                    layer.LayerName = change.layer.LayerName;
                    Layers[change.SelectedLayer] = layer;
                }
                NameChange?.Invoke(change.SelectedLayer, change.layer.LayerName);
            }
            CanvasImage[change.SelectedAnim][change.SelectedLayer] = change.layer;
            if (change.SelectedLayer != SelectedLayer)
                UpdateSelectedLayer(change.SelectedLayer, true);
            if (change.SelectedAnim != SelectedAnimation)
                UpdateSelectedAnimation(change.SelectedAnim, true);
            UpdateDisplayImage();
        }
        else if (change.Added)
        {
                UnityEngine.Debug.Log("Add");
            if (redo)
            {
                if (change.SelectedLayer == -1)
                {
                    AddedAnimation(change.SelectedAnim);
                    AddAnimation();
                }
                else
                {
                    AddedLayer(change.SelectedLayer, change.layer);
                    AddLayer(change.layer.LayerName);
                }
            }
            else
            {
                if (change.SelectedLayer == -1)
                {
                    RemovedAnimation(change.SelectedAnim);
                }
                else
                {
                    RemovedLayer(change.SelectedLayer);
                }
            }
        }
        else if (change.Delete)
        {
            if (!redo)
            {
                if (change.Frame != null)
                {
                    //load Animation
                    if (change.SelectedAnim < CanvasImage.Count)
                        CanvasImage.Insert(change.SelectedAnim, change.Frame);
                    else
                        CanvasImage.Add(change.Frame);
                    AddedAnimation(change.SelectedAnim);
                }
                else
                {
                    //load layers
                    if (change.Layers.Count > 0)
                    {
                        AddedLayer(change.SelectedLayer, change.Layers[0]);
                        int num = 0;
                        foreach (List<PaintLayer> Frame in CanvasImage)
                        {
                            Frame.Insert(change.SelectedLayer, change.Layers[num]);
                            num++;
                        }
                    }
                }
            }
            else if (redo)
            {
                if (change.Frame != null)
                {
                    //Delete Animation
                    RemovedAnimation(change.SelectedAnim);
                    if (CanvasImage.Count >  change.SelectedAnim)
                        CanvasImage.RemoveAt(change.SelectedAnim);
                }
                else
                {
                    //Delete layers
                    RemovedLayer(change.SelectedLayer);
                    foreach (List<PaintLayer> Frame in CanvasImage)
                    {
                        if (Frame.Count > change.SelectedLayer)
                        Frame.RemoveAt(change.SelectedLayer);
                    }
                }
            }
        }
        else if (change.Move)
        {
            if (redo)
            {
                if (change.SelectedLayer == -1)
                {
                    //move animation
                    List<PaintLayer> AnimationToMove = CanvasImage[change.NewIndex];
                    CanvasImage.RemoveAt(change.NewIndex);
                    CanvasImage.Insert(change.OldIndex, AnimationToMove);
                }
                else
                {
                    //move layer
                    foreach (List<PaintLayer> Frame in CanvasImage)
                    {
                        PaintLayer LayerToMove = Frame[change.NewIndex];
                        Frame.RemoveAt(change.NewIndex);
                        Frame.Insert(change.OldIndex,LayerToMove);

                        LayerMoved?.Invoke(change.NewIndex,change.OldIndex);
                    }
                }
            }
            else
            {
                if (change.SelectedLayer == -1)
                {
                    //move animation
                    List<PaintLayer> AnimationToMove = CanvasImage[change.OldIndex];
                    CanvasImage.RemoveAt(change.OldIndex);
                    CanvasImage.Insert(change.NewIndex, AnimationToMove);
                }
                else
                {
                    //move layer
                    foreach (List<PaintLayer> Frame in CanvasImage)
                    {
                        PaintLayer LayerToMove = Frame[change.OldIndex];
                        Frame.RemoveAt(change.OldIndex);
                        Frame.Insert(change.NewIndex, LayerToMove);

                        LayerMoved?.Invoke(change.OldIndex, change.NewIndex);
                    }
                }
            }
        }

    }
    public void AddedAnimation(int index)
    {
        AnimationAdded?.Invoke(index);
    }
    public void AddedLayer(int index, PaintLayer layer)
    {
        LayerAdded?.Invoke(index,layer);
    }
    public void RemovedAnimation(int index)
    {
        AnimationRemoved?.Invoke(index);
    }
    public void RemovedLayer(int index)
    {
        LayerRemoved?.Invoke(index);
    }

    public void UpdateSelectedLayer(int NewLayerIndex, bool? dochange)
    {
        if(IsPerformingUndoRedo)
                ChangeMade();
        if (NewLayerIndex >= CanvasImage[0].Count || NewLayerIndex < 0 || NewLayerIndex == SelectedLayer)
            return;
        SelectedLayer = NewLayerIndex;
        LayerSelected?.Invoke();
        UnityEngine.Debug.Log("Layer :" + SelectedLayer);
    }

    public void UpdateSelectedAnimation(int NewAnimationIndex,bool? dochange)
    {
        if(IsPerformingUndoRedo)
            ChangeMade();
        if (NewAnimationIndex >= CanvasImage.Count || NewAnimationIndex < 0 || NewAnimationIndex == SelectedAnimation)
            return;
        SelectedAnimation = NewAnimationIndex;
        AnimationSelected?.Invoke();
        UnityEngine.Debug.Log("Animation: " + SelectedAnimation);
        UpdateDisplayImage();
    }

    private void FillTexture2D(Texture2D layer)
    {
        Color32[] colors = new Color32[CanvasWidth * CanvasHeight];
        for (int i = 0; i < colors.Length; i++)
            colors[i] = new Color(0, 0, 0, 0);
        layer.SetPixels32(colors);
        layer.Apply();
    }

    public void UpdateVisibilty(int LayerIndex, bool Visibility)
    {
        ChangeMade();
        foreach (List<PaintLayer> Frame in CanvasImage) 
        {
            PaintLayer layer = Frame[LayerIndex];
            layer.LayerVisible = Visibility;
            Frame[LayerIndex] = layer;
        }
        UpdateDisplayImage();
    }
    public void UpdateName(int LayerIndex, string name)
    {
        ChangeMade();
        foreach (List<PaintLayer> Frame in CanvasImage)
        {
            PaintLayer layer = Frame[LayerIndex];
            layer.LayerName = name;
            Frame[LayerIndex] = layer;
        }
    }

    public bool DeleteLayer(int LayerIndex)
    {
        if (CanvasImage[SelectedAnimation].Count <= 1)
        {
            return false;
        }
        DeleteMade(LayerIndex,-1);
        foreach (List<PaintLayer>Frame in CanvasImage)
        {
            Frame.RemoveAt(LayerIndex);
        }
        
            UpdateSelectedLayer(0,true);
        
        UpdateDisplayImage();
        return true;
    }
    public bool DeleteAnimation(int AnimationIndex)
    {
        if (CanvasImage.Count <= 1)
            return false;
        DeleteMade(0,AnimationIndex);
        CanvasImage.RemoveAt(AnimationIndex);
      
        UpdateSelectedAnimation(0,true);
        UpdateSelectedLayer(0,true);
        
        UpdateDisplayImage();

        return true;
    }

    public void DeleteMade(int LayerIndex,int AnimationIndex)
    {
        if (IsPerformingUndoRedo)
            return;
            ChangesStack.Changes DeleteChange;
        DeleteChange = new ChangesStack.Changes();
        DeleteChange.Delete = true;
        if (AnimationIndex > -1)
        {
            DeleteChange.Frame = CanvasImage[AnimationIndex];
            DeleteChange.SelectedAnim = AnimationIndex;
        }
        else
        {
            DeleteChange.Layers = new List<PaintLayer>();
            DeleteChange.SelectedLayer = LayerIndex;
            foreach(List<PaintLayer> layerData in CanvasImage)
            {
                DeleteChange.Layers.Add(layerData[LayerIndex]);
            }
        }

        UndoRedoStack.push(DeleteChange);
    }
    public void AddedMade(int LayerIndex, int AnimationIndex)
    {
        if (IsPerformingUndoRedo)
            return;
        ChangesStack.Changes AddedChange;
        AddedChange = new ChangesStack.Changes();
        AddedChange.Added = true;
        if (AnimationIndex > -1)
        {
            AddedChange.SelectedAnim = AnimationIndex;
            AddedChange.SelectedLayer = -1;
        }
        else
        {
            AddedChange.layer = CanvasImage[0][LayerIndex];
            AddedChange.SelectedLayer = LayerIndex;
        }

        UndoRedoStack.push(AddedChange);
    }

    public bool MoveLayerUp(int index)
    {
        if (index < 1 ||index>=CanvasImage[0].Count|| CanvasImage[0].Count <= 1)
        {
        UnityEngine.Debug.Log("up " + index);
            return false;
        }
        foreach (List<PaintLayer> Layers in CanvasImage)
        {
            PaintLayer LayerToMove = Layers[index];
            Layers.RemoveAt(index);
            Layers.Insert(index-1, LayerToMove);
        }

        LayerChange(index, index - 1, true);
        SelectedLayer = index - 1;
        UpdateDisplayImage();
        return true;
    }
    public bool MoveLayerDown(int index)
    {
        if (index > 0||index >= CanvasImage[0].Count-1 || CanvasImage[0].Count <= 1)
        {
            UnityEngine.Debug.Log("down " + index);
            return false;
        }
        foreach (List<PaintLayer> Layers in CanvasImage)
        {
            PaintLayer LayerToMove = Layers[index];
            Layers.RemoveAt(index);
            Layers.Insert(index + 1, LayerToMove);
        }

        LayerChange(index, index + 1, true);
        SelectedLayer = index + 1;
        UpdateDisplayImage();
        return true;
    }
    public bool MoveAnimationUp(int index)
    {
        if (index < 1 ||index >= CanvasImage.Count|| CanvasImage.Count <= 1)
        {
            return false;
        }
        
        List<PaintLayer> FrameToMove = CanvasImage[index];
        CanvasImage.RemoveAt(index);
        CanvasImage.Insert(index-1, FrameToMove);
        LayerChange(index, index - 1, false);
        SelectedAnimation = index - 1;
        UpdateDisplayImage();
        return true;
    }
    public bool MoveAnimationDown(int index)
    {
        if (index < 0 ||index >= CanvasImage.Count - 1 || CanvasImage.Count <= 1)
        {
            return false;
        }

        List<PaintLayer> FrameToMove = CanvasImage[index];
        CanvasImage.RemoveAt(index);
        CanvasImage.Insert(index + 1, FrameToMove);
        LayerChange(index,index+1,false);
        SelectedAnimation = index + 1;
        UpdateDisplayImage();
        return true;
    }

    public void LayerChange(int NewIndex, int OldIndex,bool Layer)
    {
        if (IsPerformingUndoRedo)
            return;
        ChangesStack.Changes LayerChange = new ChangesStack.Changes();

        LayerChange.Move = true;
        if (Layer)
        {
            LayerChange.SelectedAnim = -1;
        }
        else
        {
            LayerChange.SelectedLayer = -1;
        }
            LayerChange.NewIndex = NewIndex;
            LayerChange.OldIndex = OldIndex;

        UndoRedoStack.push(LayerChange);
    }
}
