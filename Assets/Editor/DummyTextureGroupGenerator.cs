using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

public class DummyTextureGroupGenerator
{
    const string DummyTexturesPath = "Assets/DummyAssets/Textures";
    const string OutputPath = "Assets/DummyAssets/Groups";
    const int TexturesPerGroup = 10;

    [MenuItem("Tools/Generate Dummy Texture Groups")]
    static void GenerateTextureGroups()
    {
        if (!Directory.Exists(DummyTexturesPath))
        {
            Debug.LogError($"Dummy textures directory not found: {DummyTexturesPath}");
            return;
        }

        if (!Directory.Exists(OutputPath))
            Directory.CreateDirectory(OutputPath);

        var textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { DummyTexturesPath });
        var textures = textureGuids
            .Select(guid => AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(texture => texture != null)
            .ToArray();

        var groupCount = Mathf.CeilToInt((float)textures.Length / TexturesPerGroup);

        for (var i = 0; i < groupCount; i++)
        {
            var group = ScriptableObject.CreateInstance<DummyTextureGroup>();
            var startIndex = i * TexturesPerGroup;
            var endIndex = Mathf.Min(startIndex + TexturesPerGroup, textures.Length);

            for (var j = startIndex; j < endIndex; j++)
            {
                var fieldIndex = j - startIndex;
                var field = typeof(DummyTextureGroup).GetField("textures", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var textureArray = (Texture2D[])field.GetValue(group);
                textureArray[fieldIndex] = textures[j];
            }

            var assetPath = Path.Combine(OutputPath, $"DummyTextureGroup_{i:D3}.asset");
            AssetDatabase.CreateAsset(group, assetPath);

            if (i % 5 == 0)
                EditorUtility.DisplayProgressBar("Generating Texture Groups",
                    $"Creating group {i + 1}/{groupCount}",
                    (float)(i + 1) / groupCount);
        }

        EditorUtility.ClearProgressBar();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Generated {groupCount} texture groups in {OutputPath}");
    }
}