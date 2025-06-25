using UnityEngine;
using UnityEditor;
using System.IO;

public class DummyTextureGenerator
{
    const int TextureCount = 1000;
    const int TextureWidth = 256;
    const int TextureHeight = 256;
    const string OutputPath = "Assets/DummyAssets/Textures";

    [MenuItem("Tools/Generate Dummy Textures")]
    static void GenerateTextures()
    {
        if (!Directory.Exists(OutputPath))
            Directory.CreateDirectory(OutputPath);

        for (var i = 0; i < TextureCount; i++)
        {
            var texture = CreateDummyTexture(TextureWidth, TextureHeight, i);
            var filePath = Path.Combine(OutputPath, $"DummyTexture_{i:D4}.png");
            File.WriteAllBytes(filePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            
            if (i % 10 == 0)
                EditorUtility.DisplayProgressBar
                  ("Generating Textures", 
                   $"Creating texture {i + 1}/{TextureCount}", 
                   (float)(i + 1) / TextureCount);
        }
        
        EditorUtility.ClearProgressBar();
        AssetDatabase.Refresh();
        
        Debug.Log($"Generated {TextureCount} dummy textures in {OutputPath}");
    }

    static Texture2D CreateDummyTexture(int width, int height, int seed)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        
        Random.InitState(seed);
        
        var pixels = new Color[width * height];
        for (var i = 0; i < pixels.Length; i++)
            pixels[i] = new Color(Random.value, Random.value, Random.value, 1.0f);
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        return texture;
    }
}