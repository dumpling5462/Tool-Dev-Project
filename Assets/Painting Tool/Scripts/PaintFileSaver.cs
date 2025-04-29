using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor.U2D.Aseprite;
using UnityEngine;

public class PaintFileSaver
{
    public void CreateSave(string filePath,int width,int height,int AnimationIndex,int LayerIndex,List<List<PaintingToolScript.PaintLayer>> AnimationData)
    {
        FileStream SaveFile;

        if (File.Exists(filePath))
        {
            UnityEngine.Debug.Log("Open File");
            SaveFile = File.OpenWrite(filePath);
        }
        else
        {
            UnityEngine.Debug.Log("Create File");
            SaveFile = File.Create(filePath);
        }
        SaveFile.Close();

        StreamWriter SaveWriter = File.CreateText(filePath);
        SaveWriter.Write(width);
        SaveWriter.Write('|');
        SaveWriter.Write(height);
        SaveWriter.Write('|');
        SaveWriter.Write(AnimationIndex);
        SaveWriter.Write('|');
        SaveWriter.Write(LayerIndex);
        SaveWriter.Write('|');
        WriteAnimationData(SaveWriter,AnimationData);

        SaveWriter.Close();
    }

    private void WriteAnimationData(StreamWriter SaveWriter, List<List<PaintingToolScript.PaintLayer>> AnimationData)
    {
        foreach (List<PaintingToolScript.PaintLayer> Frame in AnimationData)
        {
            foreach(PaintingToolScript.PaintLayer Layer in Frame)
            {
                SaveWriter.Write(Layer.LayerName);
                SaveWriter.Write('*');
                SaveWriter.Write(Layer.LayerVisible.ToString().ToLower());
                SaveWriter.Write("*");
                foreach (Color PixelColor in Layer.LayerImage.GetPixels())
                {
                    SaveWriter.Write(PixelColor.ToString());
                    SaveWriter.Write("-");
                }
                SaveWriter.Write('_');
            }
            SaveWriter.Write(';');
        }
    }
}
