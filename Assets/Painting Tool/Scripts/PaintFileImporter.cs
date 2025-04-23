using System.Collections.Generic;
using UnityEditor.AssetImporters;
using UnityEngine;
using System.IO;

[ScriptedImporter(100, "Pain")]
public class PaintFileImporter : ScriptedImporter
{

    public override void OnImportAsset(AssetImportContext ctx)
    {
        string fileText = File.ReadAllText(ctx.assetPath);

        string[] tokens = fileText.Split('|', System.StringSplitOptions.RemoveEmptyEntries);

        PaintSO PaintFile = ScriptableObject.CreateInstance<PaintSO>();

        PaintFile.width = int.Parse(tokens[0]);
        PaintFile.height = int.Parse(tokens[1]);
        PaintFile.animationIndex = int.Parse(tokens[2]);
        PaintFile.LayerIndex = int.Parse(tokens[3]);
        PaintFile.PaintAnimations  = ParsePaintData(tokens[4]);
        PaintFile.UndoStack = ParseChangeData(tokens[5]);
          
        ctx.AddObjectToAsset("Unity Paint", PaintFile);
        ctx.SetMainObject(PaintFile);
    }

    private List<List<PaintingToolScript.PaintLayer>> ParsePaintData(string PaintData)
    {
        List<List<PaintingToolScript.PaintLayer>> Animations = new List<List<PaintingToolScript.PaintLayer>>();

        string[] tokens = PaintData.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);

        foreach (string token in tokens) 
        {
            Animations.Add(ParseAnimation(token));
        }

        return Animations;
    }
    private List<ChangesStack.Changes> ParseChangeData(string StackData)
    {
        List <ChangesStack.Changes> UndoStack = new List<ChangesStack.Changes>();

        string[] tokens = StackData.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);

        foreach (string token in tokens)
        {
            UndoStack.Add(ParseChange(token));
        }

        return UndoStack;
    }

    private List<PaintingToolScript.PaintLayer> ParseAnimation(string animationData)
    {
        List<PaintingToolScript.PaintLayer> Canvas = new List<PaintingToolScript.PaintLayer>();

        string[] tokens = animationData.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
        foreach (string token in tokens)
        {
            Canvas.Add(ParseLayer(token));
        }
        return Canvas;
    }
    private PaintingToolScript.PaintLayer ParseLayer(string layerData)
    {
        PaintingToolScript.PaintLayer Layer = new PaintingToolScript.PaintLayer();

        //handle Layer Parse

        return Layer;
    }

    private ChangesStack.Changes ParseChange(string ChangeData)
    {
        ChangesStack.Changes change = new ChangesStack.Changes();

        //handle Change Parse

        return change;
    }
}
