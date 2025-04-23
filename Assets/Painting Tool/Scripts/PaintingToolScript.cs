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
    public List<List<PaintLayer>> Canvas;

    public int SelectedLayer = 0;
    public int SelectedAnimation = 0;

    public Color SelectedColour = new Color(1,1,1,1);
    public BrushMode SelectedBrush = BrushMode.Paintbrush;
    public int BrushSize;

    private int Stacksize = 24;
    private UndoRedoChangeStack UndoRedoStack;

    public void initialize(int width, int height)
    {
        UndoRedoStack = new UndoRedoChangeStack();
        UndoRedoStack.initialize(Stacksize);
        Canvas = new List<List<PaintLayer>>();
        CanvasHeight = height;
        CanvasWidth = width;
    }
    public void AddAnimation()
    {
        List<PaintLayer> Animation = new List<PaintLayer>();
        if (Canvas.Count > 1)
        {
            foreach (PaintLayer layer in Canvas[0])
            {
                PaintLayer newLayer = new PaintLayer();
                newLayer.LayerImage = new Texture2D(CanvasWidth,CanvasHeight);
                newLayer.LayerName = layer.LayerName;
                newLayer.LayerVisible = true;
                Animation.Add(newLayer);
            }
        }
        Canvas.Add(Animation);
    }
    public void AddLayer(string name)
    {
        PaintLayer layer;
        layer.LayerImage = new Texture2D(CanvasWidth, CanvasHeight);
        layer.LayerName = name;
        layer.LayerVisible = true;
        Canvas[SelectedAnimation].Add(layer);
    }
    public void RemoveLayer(int ID)
    {
        if (ID >= Canvas[0].Count)
        {
            return;
        }
        for (int i = 0; i < Canvas.Count; i++)
        {
            Canvas[i].RemoveAt(ID);
        }
        if (SelectedLayer == ID)
        {
            SelectedLayer = 0;
        }
        UpdateDisplayImage();
    }
    public void RemoveAnimation(int ID)
    {
        if (ID >= Canvas.Count)
        {
            return;
        }
        Canvas.RemoveAt(ID);
        if (SelectedAnimation == ID)
        {
            SelectedAnimation = 0;
        }
        UpdateDisplayImage();
    }

    public void brush()
    {

    }
    public void UpdateDisplayImage()
    {
        UpdateCanvas?.Invoke();
    }

    public Texture2D GetDisplayImage()
    {
        Texture2D DisplayImage = new Texture2D(CanvasWidth,CanvasHeight);
        if (Canvas[SelectedAnimation].Count > 1)
        {
            List<PaintLayer> layers = new List<PaintLayer>(Canvas[SelectedAnimation]);
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
        else if (Canvas[SelectedAnimation][0].LayerVisible)
        {
            for (int y = 0; y < CanvasHeight;y++)
            {
                for (int x = 0;x < CanvasWidth; x++)
                {
                    Color Pixel = Canvas[SelectedAnimation][0].LayerImage.GetPixel(x, y);
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

        switch (SelectedBrush)
        {
            case BrushMode.Paintbrush:
                paintPixel(x,y, SelectedColour);
                break;
            case BrushMode.PaintBucket:
                break;
            case BrushMode.Eraser:
                break;
            case BrushMode.Eyedropper:
                break;
        }

        ChangeMade();
        Canvas[SelectedAnimation][SelectedLayer].LayerImage.Apply();
        UpdateDisplayImage();
    }

    private void paintPixel(int x,int y,Color selectedColour)
    {
        Canvas[SelectedAnimation].ElementAt(SelectedLayer).LayerImage.SetPixel(x,y,selectedColour);
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
        NewChange.SelectedAnim = SelectedAnimation;
        NewChange.layer = Canvas[SelectedAnimation].ElementAt(SelectedLayer);
        UndoRedoStack.push(NewChange);
    }
    public void ApplyChange(ChangesStack.Changes change)
    {
        Canvas[change.SelectedAnim][change.SelectedLayer] = change.layer;
        UpdateDisplayImage();
    }
}
