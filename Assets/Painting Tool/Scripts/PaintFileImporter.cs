using System.Collections.Generic;
using UnityEditor.AssetImporters;
using UnityEngine;
using System.IO;

[ScriptedImporter(100, "Pain")]
public class PaintFileImporter : ScriptedImporter
{
    int width;
    int height;
    public override void OnImportAsset(AssetImportContext ctx)
    {
        string fileText = File.ReadAllText(ctx.assetPath);

        string[] tokens = fileText.Split('|', System.StringSplitOptions.RemoveEmptyEntries);

        PaintSO PaintFile = ScriptableObject.CreateInstance<PaintSO>();

        PaintFile.width = int.Parse(tokens[0]);
        PaintFile.height = int.Parse(tokens[1]);
        width = PaintFile.width;
        height = PaintFile.height;
        PaintFile.animationIndex = int.Parse(tokens[2]);
        PaintFile.LayerIndex = int.Parse(tokens[3]);
        PaintFile.PaintData  = ParsePaintData(tokens[4]);
          
        ctx.AddObjectToAsset("Unity Paint", PaintFile);
        ctx.SetMainObject(PaintFile);
    }

    private List<List<PaintingToolScript.PaintLayer>> ParsePaintData(string PaintData)
    {
        List<List<PaintingToolScript.PaintLayer>> Animations = new List<List<PaintingToolScript.PaintLayer>>();

        string[] tokens = PaintData.Split(';', System.StringSplitOptions.RemoveEmptyEntries);

        foreach (string token in tokens) 
        {
            Animations.Add(ParseFrameData(token));
        }

        return Animations;
    }
    private List<PaintingToolScript.PaintLayer> ParseFrameData(string animationData)
    {
        List<PaintingToolScript.PaintLayer> Canvas = new List<PaintingToolScript.PaintLayer>();

        string[] tokens = animationData.Split(',', System.StringSplitOptions.RemoveEmptyEntries);
        foreach (string token in tokens)
        {
            Canvas.Add(ParseLayerData(token));
        }
        return Canvas;
    }
    private PaintingToolScript.PaintLayer ParseLayerData(string layerData)
    {
        PaintingToolScript.PaintLayer Layer = new PaintingToolScript.PaintLayer();
        Layer.LayerImage = new Texture2D(width,height);
        //handle Layer Parse
        byte[] data = ConvertLayerDataToByteArray(layerData);
        Layer.LayerImage.LoadRawTextureData(data);
        Layer.LayerImage.Compress(true);
        Layer.LayerImage.Apply(updateMipmaps:false,makeNoLongerReadable: true);

        return Layer;
    }

    // A helper function that will split the data properly
    private byte[] ConvertLayerDataToByteArray(string layerData)
    {
        // Assuming layerData is a string of byte values separated by '!' 
        // You might need to adjust the split logic depending on your actual data format
        string[] tokens = layerData.Split('!', System.StringSplitOptions.RemoveEmptyEntries);
        byte[] byteArray = new byte[tokens.Length];

        for (int i = 0; i < tokens.Length; i++)
        {
            byteArray[i] = byte.Parse(tokens[i]);
        }

        return byteArray;
    }

    private List<ChangesStack.Changes> ParseUndoStackData(string StackData)
    {
        List <ChangesStack.Changes> UndoStack = new List<ChangesStack.Changes>();

        string[] tokens = StackData.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);

        foreach (string token in tokens)
        {
            UndoStack.Add(ParseChangeData(token));
        }

        return UndoStack;
    }

    private ChangesStack.Changes ParseChangeData(string ChangeData)
    {
        ChangesStack.Changes change = new ChangesStack.Changes();

        //handle Change Parse

        return change;
    }
}
