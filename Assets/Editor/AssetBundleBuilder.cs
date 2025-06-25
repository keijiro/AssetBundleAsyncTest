using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class AssetBundleBuilder
{
    const string DummyTexturesPath = "Assets/DummyAssets/Textures";
    const string DummyGroupsPath = "Assets/DummyAssets/Groups";
    const string BundleName = "dummy-textures";
    const string TempBuildPath = "AssetBundles";
    const string StreamingAssetsPath = "Assets/StreamingAssets";

    [MenuItem("Tools/Build Asset Bundle")]
    static void BuildAssetBundle()
    {
        if (!Directory.Exists(DummyTexturesPath))
        {
            Debug.LogError($"Dummy textures directory not found: {DummyTexturesPath}");
            return;
        }

        AssignAssetBundleNames();
        BuildBundlesWithAllCompressions();
        CopyToStreamingAssets();
        
        Debug.Log("Asset bundle build completed successfully");
    }

    static void AssignAssetBundleNames()
    {
        var textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { DummyTexturesPath });
        var groupGuids = AssetDatabase.FindAssets("t:DummyTextureGroup", new[] { DummyGroupsPath });
        
        foreach (var guid in textureGuids)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(assetPath);
            if (importer != null)
                importer.assetBundleName = BundleName;
        }

        foreach (var guid in groupGuids)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(assetPath);
            if (importer != null)
                importer.assetBundleName = BundleName;
        }
        
        Debug.Log($"Assigned {textureGuids.Length} textures and {groupGuids.Length} groups to bundle: {BundleName}");
    }

    static void BuildBundlesWithAllCompressions()
    {
        if (Directory.Exists(TempBuildPath))
            Directory.Delete(TempBuildPath, true);
        
        Directory.CreateDirectory(TempBuildPath);

        var compressionOptions = new Dictionary<string, BuildAssetBundleOptions>
        {
            { "none", BuildAssetBundleOptions.UncompressedAssetBundle },
            { "lzma", BuildAssetBundleOptions.None },
            { "lz4", BuildAssetBundleOptions.ChunkBasedCompression }
        };

        foreach (var compression in compressionOptions)
        {
            var compressionPath = Path.Combine(TempBuildPath, compression.Key);
            Directory.CreateDirectory(compressionPath);
            
            BuildPipeline.BuildAssetBundles(
                compressionPath,
                compression.Value,
                BuildTarget.StandaloneOSX
            );
            
            Debug.Log($"Asset bundles built with {compression.Key} compression to: {compressionPath}");
        }
    }

    static void CopyToStreamingAssets()
    {
        if (!Directory.Exists(StreamingAssetsPath))
            Directory.CreateDirectory(StreamingAssetsPath);

        var compressionTypes = new[] { "none", "lzma", "lz4" };
        
        foreach (var compression in compressionTypes)
        {
            var sourcePath = Path.Combine(TempBuildPath, compression, BundleName);
            var destPath = Path.Combine(StreamingAssetsPath, $"{BundleName}-{compression}");

            if (File.Exists(sourcePath))
            {
                if (File.Exists(destPath))
                    File.Delete(destPath);
                
                File.Copy(sourcePath, destPath);
                Debug.Log($"Asset bundle ({compression}) copied to: {destPath}");
            }
            else
            {
                Debug.LogError($"Built asset bundle not found: {sourcePath}");
            }
        }

        AssetDatabase.Refresh();
    }
}