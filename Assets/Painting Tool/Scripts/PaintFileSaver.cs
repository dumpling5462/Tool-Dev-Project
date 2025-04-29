using System.Collections.Generic;
using System.IO;

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
                //foreach (Color PixelColor in Layer.LayerImage.GetPixels())
                //{
                //    SaveWriter.Write(PixelColor.ToString());
                //    SaveWriter.Write("-");
                //}
                byte[] rawData = Layer.LayerImage.GetRawTextureData(); // Much faster
                string base64 = System.Convert.ToBase64String(rawData);
                SaveWriter.Write(base64);
                SaveWriter.Write('_');
            }
            SaveWriter.Write(';');
        }
    }

    public void CreateBinarySave(string filePath, int width, int height, int AnimationIndex, int LayerIndex, List<List<PaintingToolScript.PaintLayer>> AnimationData)
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

        BinaryWriter SaveWriter = new BinaryWriter(File.Open(filePath,FileMode.Create));

        SaveWriter.Write(width);
        SaveWriter.Write(height);
        SaveWriter.Write(AnimationIndex);
        SaveWriter.Write(LayerIndex);

        SaveWriter.Write(AnimationData.Count);
        foreach (List<PaintingToolScript.PaintLayer> Frame in AnimationData)
        {
            SaveWriter.Write(Frame.Count);
            foreach (PaintingToolScript.PaintLayer layer in Frame)
            {
                SaveWriter.Write(layer.LayerName);
                SaveWriter.Write(layer.LayerVisible);

                byte[] rawData = layer.LayerImage.GetRawTextureData();
                SaveWriter.Write(rawData.Length);
                SaveWriter.Write(rawData);
            }
        }
        SaveWriter.Close();
    }
}
