using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class PaintingToolScript
{
    public event Action UpdateCanvas;
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
                break;
            case BrushMode.Eraser:
                PaintPixel(x, y, new Color(0,0,0,0));
                break;
            case BrushMode.Eyedropper:
                break;
        }

        CanvasImage[SelectedAnimation][SelectedLayer].LayerImage.Apply();
        UpdateDisplayImage();
    }

    private void PaintPixel(int x,int y,Color selectedColour)
    {
        CanvasImage[SelectedAnimation][SelectedLayer].LayerImage.SetPixel(x,y,selectedColour);
    }

    public void ChangeColour(Color ColourChange)
    {
        SelectedColour = ColourChange;
    }

    public void Undo()
    {
        ChangesStack.Changes? CurrentChange = UndoRedoStack.pop(GetChange());
        if (CurrentChange != null)
        {
            ApplyChange((ChangesStack.Changes)CurrentChange);
        }
    }
    public void Redo()
    {
        UndoRedoStack.pushRedo(GetChange());
        ChangesStack.Changes? CurrentChange = UndoRedoStack.Redopop();
        if (CurrentChange != null)
        {
            ApplyChange((ChangesStack.Changes)CurrentChange);
        }
    }
    public void ChangeMade()
    {
        UndoRedoStack.push(GetChange());
    }
    private ChangesStack.Changes GetChange()
    {
        ChangesStack.Changes NewChange;
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

        return NewChange;
    }
    public void ApplyChange(ChangesStack.Changes change)
    {
        CanvasImage[change.SelectedAnim][change.SelectedLayer] = change.layer;
        UpdateDisplayImage();
    }

    public void UpdateSelectedLayer(int NewLayerIndex)
    {
        if (NewLayerIndex >= CanvasImage[SelectedAnimation].Count)
            return;
        SelectedLayer = NewLayerIndex;
    }

    public void UpdateSelectedAnimation(int NewAnimationIndex)
    {
        ChangeMade();
        if (NewAnimationIndex >= CanvasImage.Count)
            return;
        SelectedAnimation = NewAnimationIndex;
        UnityEngine.Debug.Log(NewAnimationIndex);
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
}
