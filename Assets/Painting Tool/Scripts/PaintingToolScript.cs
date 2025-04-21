using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

public class PaintingToolScript
{
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
    public List<PaintLayer> Canvas = new List<PaintLayer>();

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
        layer.LayerImage = new Texture2D(width,height);
        layer.LayerName = name;
        layer.LayerVisible = true;
      
        Canvas.Add(layer);
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
                for (int i = 0; i < CanvasHeight; i++)
                {
                    for (int j = 0; j < CanvasWidth; j++)
                    {
                        if (layer.LayerImage.GetPixel(i, j) != new Color(0, 0, 0, 0))
                        {
                            DisplayImage.SetPixel(i, j, layer.LayerImage.GetPixel(i, j));
                        }
                    }
                }
            }
        }
        else if (Canvas.ElementAt(0).LayerVisible)
        {
            DisplayImage = (Canvas.ElementAt(0).LayerImage);
        }
        DisplayImage.Apply();
        return DisplayImage;
    }

    public void SwitchBrushMode(BrushMode newBrushMode)
    {
        SelectedBrush = newBrushMode;
    }

    public void PressedPixel(int width,int height)
    {
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
    }

    private void paintPixel(int width,int height,Color selectedColour)
    {
        Canvas.ElementAt(SelectedLayer).LayerImage.SetPixel(width,height,selectedColour);
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
