
using System.Collections.Generic;
using System.Drawing;

public class UndoRedoChangeStack : ChangesStack
{
    public ChangesStack Redo;

    public override void initialize(int Size)
    {
        base.initialize(Size);
        Redo = new ChangesStack();
        Redo.initialize(Size);
    }

    public override void push(Changes change)
    {
        Redo.nullStack();
        base.push(change);    
    }
    public void pushRedo(Changes change)
    {
        Redo.push(change);
    }

    public override Changes? pop(Changes? oldState)
    {
        Changes? change = base.pop(null);
        if (change != null)
        {
            pushRedo(AssignChangeReferences(change.Value));
        }
        //else if (oldState != null)
        //{
        //    pushRedo(AssignChangeReferences(oldState.Value));
        //}
        return change;
    }

    public void Redopush(Changes change)
    {
        base.push(AssignChangeReferences(change));
    }

    //returns the top item of the stack and decrements the top pointer
    public Changes? Redopop()
    {
        return Redo.pop(null);
    }

    //checks the top item of the stack
    public Changes? Redopeek()
    {
        return Redo.peek();
    }

    public Changes AssignChangeReferences(Changes change)
    {
        Changes RedoChange = new Changes();
        //    public int SelectedAnim;
        //public int SelectedLayer;
        //public PaintingToolScript.PaintLayer layer;
        //public List<PaintingToolScript.PaintLayer>? Frame;
        //public List<PaintingToolScript.PaintLayer>? Layers;
        //public bool Delete;
        //public bool Added;
        //public bool Move;
        //public int NewIndex;
        //public int OldIndex;
        RedoChange.SelectedAnim = change.SelectedAnim;
        RedoChange.SelectedLayer = change.SelectedLayer;
        RedoChange.Delete = change.Delete;
        RedoChange.Added = change.Added;
        RedoChange.Move = change.Move;
        if (!change.Delete && !change.Move)
        {
            if (!change.layer.Equals(null) && change.layer.LayerImage != null)
            {
                PaintingToolScript.PaintLayer newLayer;
                newLayer.LayerName = change.layer.LayerName;
                newLayer.LayerVisible = change.layer.LayerVisible;
                UnityEngine.Color[] colors = change.layer.LayerImage.GetPixels();
                newLayer.LayerImage = new UnityEngine.Texture2D(change.layer.LayerImage.width,change.layer.LayerImage.height);
                newLayer.LayerImage.SetPixels(colors); 
                RedoChange.layer = newLayer;
            }
        }
        if (change.Delete)
        {
            if (change.Frame != null)
            {
                List<PaintingToolScript.PaintLayer> Frame = new List<PaintingToolScript.PaintLayer>();
                foreach (PaintingToolScript.PaintLayer layer in change.Frame)
                {
                    if (!layer.Equals(null) && layer.LayerImage != null )
                    {
                        PaintingToolScript.PaintLayer newLayer;
                        newLayer.LayerName= layer.LayerName;
                        newLayer.LayerVisible= layer.LayerVisible;
                        UnityEngine.Color[] colors = layer.LayerImage.GetPixels();
                        newLayer.LayerImage = new UnityEngine.Texture2D(layer.LayerImage.width,layer.LayerImage.height);
                        newLayer.LayerImage.SetPixels(colors);
                        Frame.Add(newLayer);
                    }
                }
                RedoChange.Frame = Frame;
            }
            if (change.Layers != null)
            {
                List<PaintingToolScript.PaintLayer> Layers = new List<PaintingToolScript.PaintLayer>();
                foreach (PaintingToolScript.PaintLayer layer in change.Layers)
                {
                    if (!layer.Equals(null) && layer.LayerImage != null)
                    {
                        PaintingToolScript.PaintLayer newLayer;
                        newLayer.LayerName = layer.LayerName;
                        newLayer.LayerVisible = layer.LayerVisible;
                        UnityEngine.Color[] colors = layer.LayerImage.GetPixels();
                        newLayer.LayerImage = new UnityEngine.Texture2D(layer.LayerImage.width, layer.LayerImage.height);
                        newLayer.LayerImage.SetPixels(colors);
                        Layers.Add(newLayer);
                    }
                }
                RedoChange.Layers = Layers;
            }
        }
        if (change.Move)
        {
            RedoChange.NewIndex = change.NewIndex;
            RedoChange.OldIndex = change.OldIndex;
        }

        return RedoChange;
    }
}
