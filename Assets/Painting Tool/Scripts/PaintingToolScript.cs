using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

public class PaintingToolScript
{
    public event Action UpdateCavas;
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
    public List<PaintLayer> Canvas;

    public int SelectedLayer = 0;

    public Color SelectedColour;
    public BrushMode SelectedBrush = BrushMode.Paintbrush;
    public int BrushSize;

    private int Stacksize = 24;
    private UndoRedoChangeStack UndoRedoStack;

    public void initialize(int width, int height, string name)
    {
        UndoRedoStack = new UndoRedoChangeStack();
        UndoRedoStack.initialize(Stacksize);

        Canvas = new List<PaintLayer>();
        PaintLayer layer;
        CanvasHeight = height;
        CanvasWidth = width;
        layer.LayerImage = new Texture2D(width,height);
        layer.LayerName = name;
        layer.LayerVisible = true;
      
        Canvas.Add(layer);
        ChangeMade();
    }
    public void AddLayer(string name)
    {
        PaintLayer layer;
        layer.LayerImage = new Texture2D(CanvasWidth, CanvasHeight);
        layer.LayerName = name;
        layer.LayerVisible = true;
        Canvas.Add(layer);
    }
    public void RemoveLayer(int ID)
    {
        Canvas.RemoveAt(ID);
    }

    public void brush()
    {

    }
    public void UpdateDisplayImage()
    {
        UpdateCavas?.Invoke();
    }

    public Texture2D GetDisplayImage()
    {
        Texture2D DisplayImage = new Texture2D(CanvasWidth,CanvasHeight);
        if (Canvas.Count > 1)
        {
            List<PaintLayer> layers = new List<PaintLayer>(Canvas);
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
        else if (Canvas[0].LayerVisible)
        {
            for (int y = 0; y < CanvasHeight;y++)
            {
                for (int x = 0;x < CanvasWidth; x++)
                {
                    Color Pixel = Canvas[0].LayerImage.GetPixel(x, y);
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
        if (x < 0 || x >= CanvasWidth || y < 0 || y >= CanvasHeight)
            return;

        switch (SelectedBrush)
        {
            case BrushMode.Paintbrush:
                break;
            case BrushMode.PaintBucket:
                break;
            case BrushMode.Eraser:
                break;
            case BrushMode.Eyedropper:
                break;
        }

        ChangeMade();
        Canvas[SelectedLayer].LayerImage.Apply();
        UpdateDisplayImage();
    }

    private void paintPixel(int x,int y,Color selectedColour)
    {
        Canvas.ElementAt(SelectedLayer).LayerImage.SetPixel(x,y,selectedColour);
    }

    public void ChangeColour(Color ColourChange)
    {
        SelectedColour = ColourChange;
    }

    public void Undo()
    {
        ChangesStack.Changes? CurrentChange = UndoRedoStack.pop();
        if (CurrentChange != null)
        {
            ApplyChange((ChangesStack.Changes)CurrentChange);
        }
    }
    public void Redo()
    {
        ChangesStack.Changes? CurrentChange = UndoRedoStack.Redopop();
        if (CurrentChange != null)
        {
            ApplyChange((ChangesStack.Changes)CurrentChange);
        }
    }
    public void ChangeMade()
    {
        ChangesStack.Changes NewChange;
        NewChange.SelectedLayer = SelectedLayer;
        NewChange.SelectedAnim = 0;
        NewChange.layer = Canvas.ElementAt(SelectedLayer);
        UndoRedoStack.push(NewChange);
    }
    public void ApplyChange(ChangesStack.Changes change)
    {
        Canvas[change.SelectedLayer] = change.layer;
        UpdateDisplayImage();
    }
}
