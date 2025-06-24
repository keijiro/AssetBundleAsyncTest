using UnityEngine;
using UnityEditor;
using System.IO;

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
        BuildBundles();
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

    static void BuildBundles()
    {
        if (Directory.Exists(TempBuildPath))
            Directory.Delete(TempBuildPath, true);
        
        Directory.CreateDirectory(TempBuildPath);

        BuildPipeline.BuildAssetBundles(
            TempBuildPath,
            BuildAssetBundleOptions.None,
            BuildTarget.StandaloneOSX
        );
        
        Debug.Log($"Asset bundles built to: {TempBuildPath}");
    }

    static void CopyToStreamingAssets()
    {
        if (!Directory.Exists(StreamingAssetsPath))
            Directory.CreateDirectory(StreamingAssetsPath);

        var sourcePath = Path.Combine(TempBuildPath, BundleName);
        var destPath = Path.Combine(StreamingAssetsPath, BundleName);

        if (File.Exists(sourcePath))
        {
            if (File.Exists(destPath))
                File.Delete(destPath);
            
            File.Copy(sourcePath, destPath);
            Debug.Log($"Asset bundle copied to: {destPath}");
        }
        else
        {
            Debug.LogError($"Built asset bundle not found: {sourcePath}");
        }

        AssetDatabase.Refresh();
    }
}