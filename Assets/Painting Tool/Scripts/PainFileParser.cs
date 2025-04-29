using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;

public class PainFileParser
{
    int width;
    int height;
    public PaintSO ImportFileAtPath(string FilePath)
    {
        PaintSO PaintFile = new PaintSO();
        string fileText = File.ReadAllText(FilePath);

        string[] tokens = fileText.Split('|', System.StringSplitOptions.RemoveEmptyEntries);


        PaintFile.width = int.Parse(tokens[0]);
        PaintFile.height = int.Parse(tokens[1]);
        width = PaintFile.width;
        height = PaintFile.height;
        PaintFile.animationIndex = int.Parse(tokens[2]);
        PaintFile.LayerIndex = int.Parse(tokens[3]);
        PaintFile.PaintData = ParsePaintData(tokens[4]);

        return PaintFile;
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

        string[] tokens = animationData.Split('_', System.StringSplitOptions.RemoveEmptyEntries);
        foreach (string token in tokens)
        {
            Canvas.Add(ParseLayerData(token));
        }
        return Canvas;
    }
    private PaintingToolScript.PaintLayer ParseLayerData(string layerData)
    {
        PaintingToolScript.PaintLayer Layer;

        string[] tokens = layerData.Split('*', System.StringSplitOptions.RemoveEmptyEntries);
        Layer.LayerName = tokens[0];
        Layer.LayerVisible = bool.Parse(tokens[1]);
        List<Color32> colors = new List<Color32>();

        //string[] PixelData = tokens[2].Split('-', System.StringSplitOptions.RemoveEmptyEntries);
        //Debug.Log(tokens[2]);
        //foreach (string token in PixelData)
        //{
        //    if (token.Contains("RGBA"))
        //    {
        //        string ColorData = token.Substring(token.IndexOf('(') + 1, token.IndexOf(')') - (token.IndexOf('(') + 1));
        //        UnityEngine.Debug.Log(ColorData);
        //        string[] ColorValues = ColorData.Split(',', System.StringSplitOptions.RemoveEmptyEntries);
        //        Color32 color = new Color(float.Parse(ColorValues[0]), float.Parse(ColorValues[1]), float.Parse(ColorValues[2]), float.Parse(ColorValues[3]));
        //        colors.Add(color);
        //    }
        //}
        byte[] raw = Convert.FromBase64String(tokens[2]);
        Layer.LayerImage = new Texture2D(width, height, TextureFormat.RGBA32, mipChain: false);
        //Layer.LayerImage.SetPixels32(colors.ToArray());
        Layer.LayerImage.LoadRawTextureData(raw);
        //Layer.LayerImage.Apply(updateMipmaps: false, makeNoLongerReadable: true);
        Layer.LayerImage.Apply();

        return Layer;
    }

    public PaintSO ImportBinaryFileAtPath(string FilePath)
    {
        PaintSO PaintFile = new PaintSO();
        using (FileStream fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read))
        using (BinaryReader reader = new BinaryReader(fs))
        {
            PaintFile.width = reader.ReadInt32();     
            width = PaintFile.width;
            PaintFile.height = reader.ReadInt32();   
            height = PaintFile.height;
            PaintFile.animationIndex = reader.ReadInt32();
            PaintFile.LayerIndex = reader.ReadInt32();

            PaintFile.PaintData = ReadAnimationData(reader);
        }
        return PaintFile;
    }

    private List<List<PaintingToolScript.PaintLayer>> ReadAnimationData(BinaryReader reader)
    {
        List<List<PaintingToolScript.PaintLayer>> animationData = new List<List<PaintingToolScript.PaintLayer>>();

        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            List<PaintingToolScript.PaintLayer> frame = new List<PaintingToolScript.PaintLayer>();

            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                string layerName = reader.ReadString();
                bool layerVisible = reader.ReadBoolean();

                int rawDataLength = reader.ReadInt32();
                byte[] rawData = reader.ReadBytes(rawDataLength);

                PaintingToolScript.PaintLayer layer;
                layer.LayerName = layerName;
                layer.LayerVisible = layerVisible;
                layer.LayerImage = new Texture2D(width, height, TextureFormat.RGBA32, mipChain: false);


                layer.LayerImage.LoadRawTextureData(rawData);
                layer.LayerImage.Apply();

                frame.Add(layer);
            }

            animationData.Add(frame);
        }

        return animationData;
    }

}
