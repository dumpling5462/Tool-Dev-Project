using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class PaintingToolScript
{
    public event Action UpdateCanvas;
    public event Action ColourChange;
    public event Action LayerSelected;
    public event Action AnimationSelected;
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

    private ChangesStack.Changes? StoredChange;

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
                Animation.Add(newLayer);
            }
        }
        CanvasImage.Add(Animation);
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
            SelectedLayer = 0;
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
            SelectedAnimation = 0;
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

        ChangesStack.Changes? CurrentChange = UndoRedoStack.pop(GetChange());
        StoredChange = CurrentChange;
        if (CurrentChange != null)
        {
            ApplyChange((ChangesStack.Changes)CurrentChange);
        }
    }
    public void Redo()
    {
        if (StoredChange == null)
        {
            UndoRedoStack.pushRedo(GetChange());
        }
        else
        {
            UndoRedoStack.pushRedo((ChangesStack.Changes)StoredChange);
        }
        ChangesStack.Changes? CurrentChange = UndoRedoStack.Redopop();
        if (CurrentChange != null)
        {
            ApplyChange((ChangesStack.Changes)CurrentChange);
        }
    }
    public void ChangeMade()
    {
        if (StoredChange != null)
        {
            StoredChange = null;
        }
        UndoRedoStack.push(GetChange());
    }
    private ChangesStack.Changes GetChange()
    {
        ChangesStack.Changes NewChange;
        NewChange = new ChangesStack.Changes();
        NewChange.SelectedLayer = SelectedLayer;
        NewChange.SelectedAnim = SelectedAnimation;
        PaintLayer ChangedLayer = new();
        Texture2D tex = new Texture2D(CanvasWidth, CanvasHeight);
        for (int y = 0; y < CanvasHeight; y++)
        {
            for (int x = 0; x < CanvasHeight; x++)
            {
                tex.SetPixel(x, y, CanvasImage[SelectedAnimation][SelectedLayer].LayerImage.GetPixel(x, y));
            }
        }
        ChangedLayer.LayerImage = tex;
        ChangedLayer.LayerName = CanvasImage[SelectedAnimation][SelectedLayer].LayerName;
        ChangedLayer.LayerVisible = CanvasImage[SelectedAnimation][SelectedLayer].LayerVisible;


        NewChange.layer = ChangedLayer;
        NewChange.Delete = false;

        return NewChange;
    }
    public void ApplyChange(ChangesStack.Changes change)
    {
        if (!change.Delete)
        {
            CanvasImage[change.SelectedAnim][change.SelectedLayer] = change.layer;
            if (change.SelectedLayer != SelectedLayer)
            UpdateSelectedLayer(change.SelectedLayer);
            if (change.SelectedAnim != SelectedAnimation)
            UpdateSelectedAnimation(change.SelectedAnim);
            UpdateDisplayImage();
        }
        else
        {
            if (change.Frame != null)
            {
                //load Animation
                CanvasImage.Insert(change.SelectedAnim,change.Frame);
            }
            else
            {
                //load layers
                foreach (List<PaintLayer> Frame in CanvasImage)
                {
                    Frame.Insert(change.SelectedLayer,change.Layers[0]);
                    change.Layers.RemoveAt(0);
                }
            }
        }
    }

    public void UpdateSelectedLayer(int NewLayerIndex)
    {
        if (NewLayerIndex >= CanvasImage[SelectedAnimation].Count || NewLayerIndex < 0 || NewLayerIndex == SelectedLayer)
            return;
        SelectedLayer = NewLayerIndex;
        LayerSelected?.Invoke();
    }

    public void UpdateSelectedAnimation(int NewAnimationIndex)
    {
        ChangeMade();
        if (NewAnimationIndex >= CanvasImage.Count || NewAnimationIndex < 0 || NewAnimationIndex == SelectedAnimation)
            return;
        SelectedAnimation = NewAnimationIndex;
        AnimationSelected?.Invoke();
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
        return true;
    }
    public bool DeleteAnimation(int AnimationIndex)
    {
        if (CanvasImage.Count <= 1)
            return false;
        DeleteMade(0,AnimationIndex);
        CanvasImage.RemoveAt(AnimationIndex);

        return true;
    }

    public void DeleteMade(int LayerIndex,int AnimationIndex)
    {
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
        
    }
}
