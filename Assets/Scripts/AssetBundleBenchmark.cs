using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

public class AssetBundleBenchmark : MonoBehaviour
{
    const string BundleName = "dummy-textures";
    
    UIDocument uiDocument;
    Label awakeCounterLabel;
    Label benchmarkResultLabel;
    Button startBenchmarkButton;
    DropdownField priorityDropdown;
    DropdownField compressionDropdown;
    
    AssetBundle loadedBundle;
    bool isBenchmarkRunning;
    int[] awakeCountPerFrame = new int[1000];
    int frameCount;
    float loadFromFileTime;
    float totalLoadTime;
    long currentBundleSize;
    long noneCompressionSize;

    void Start()
    {
        SetupUI();
        DummyTextureGroup.ResetCounter();
        UpdateUI();
    }

    void SetupUI()
    {
        uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
            uiDocument = gameObject.AddComponent<UIDocument>();

        var root = uiDocument.rootVisualElement;
        awakeCounterLabel = root.Q<Label>("awake-counter");
        benchmarkResultLabel = root.Q<Label>("benchmark-result");
        startBenchmarkButton = root.Q<Button>("start-button");
        priorityDropdown = root.Q<DropdownField>("priority-dropdown");
        compressionDropdown = root.Q<DropdownField>("compression-dropdown");

        if (startBenchmarkButton != null)
            startBenchmarkButton.clicked += StartBenchmark;
            
        if (priorityDropdown != null)
        {
            priorityDropdown.choices = new List<string> { "Low", "BelowNormal", "Normal", "High" };
            priorityDropdown.value = "Low";
        }
        
        if (compressionDropdown != null)
        {
            compressionDropdown.choices = new List<string> { "None", "LZMA", "LZ4" };
            compressionDropdown.value = "LZMA";
        }
    }

    void Update()
    {
        if (!isBenchmarkRunning)
            UpdateUI();
    }

    void UpdateUI()
    {
        if (awakeCounterLabel != null)
            awakeCounterLabel.text = $"Awake Counter: {DummyTextureGroup.AwakeCounter}";
    }

    void StartBenchmark()
    {
        if (isBenchmarkRunning)
            return;

        DummyTextureGroup.ResetCounter();
        if (benchmarkResultLabel != null)
            benchmarkResultLabel.text = "Loading...";
        StartCoroutine(BenchmarkCoroutine());
    }

    IEnumerator BenchmarkCoroutine()
    {
        isBenchmarkRunning = true;
        frameCount = 0;
        
        var selectedPriority = priorityDropdown?.value ?? "Low";
        Application.backgroundLoadingPriority = selectedPriority switch
        {
            "Low" => ThreadPriority.Low,
            "BelowNormal" => ThreadPriority.BelowNormal,
            "Normal" => ThreadPriority.Normal,
            "High" => ThreadPriority.High,
            _ => ThreadPriority.Low
        };
        
        var totalStopwatch = Stopwatch.StartNew();
        var loadFromFileStopwatch = Stopwatch.StartNew();

        var selectedCompression = compressionDropdown?.value ?? "LZMA";
        var compressionSuffix = selectedCompression.ToLower() switch
        {
            "none" => "none",
            "lzma" => "lzma",
            "lz4" => "lz4",
            _ => "lzma"
        };
        var bundlePath = Path.Combine(Application.streamingAssetsPath, $"{BundleName}-{compressionSuffix}");
        
        // Get file sizes for compression ratio calculation
        currentBundleSize = GetFileSize(bundlePath);
        var noneCompressionPath = Path.Combine(Application.streamingAssetsPath, $"{BundleName}-none");
        noneCompressionSize = GetFileSize(noneCompressionPath);
        
        var bundleRequest = AssetBundle.LoadFromFileAsync(bundlePath);
        yield return bundleRequest;
        
        loadFromFileStopwatch.Stop();
        loadFromFileTime = loadFromFileStopwatch.ElapsedMilliseconds;

        if (bundleRequest.assetBundle == null)
        {
            if (benchmarkResultLabel != null)
                benchmarkResultLabel.text = "Failed to load asset bundle";
            isBenchmarkRunning = false;
            yield break;
        }

        loadedBundle = bundleRequest.assetBundle;

        var assetNames = loadedBundle.GetAllAssetNames();
        var groupAssetNames = new List<string>();

        foreach (var assetName in assetNames)
        {
            if (assetName.Contains("dummytexturegroup") && assetName.EndsWith(".asset"))
                groupAssetNames.Add(assetName);
        }

        UnityEngine.Debug.Log($"Found {groupAssetNames.Count} ScriptableObject assets");

        var loadRequests = new List<AssetBundleRequest>();
        foreach (var assetName in groupAssetNames)
        {
            var request = loadedBundle.LoadAssetAsync<DummyTextureGroup>(assetName);
            loadRequests.Add(request);
        }

        var allCompleted = false;
        while (!allCompleted)
        {
            allCompleted = true;
            foreach (var request in loadRequests)
            {
                if (!request.isDone)
                {
                    allCompleted = false;
                    break;
                }
            }
            
            if (frameCount < awakeCountPerFrame.Length)
                awakeCountPerFrame[frameCount] = DummyTextureGroup.AwakeCounter;
                
            UnityEngine.Debug.Log($"Frame {frameCount}: Awake Counter = {DummyTextureGroup.AwakeCounter}");
            frameCount++;
            
            yield return null;
        }

        totalStopwatch.Stop();
        totalLoadTime = totalStopwatch.ElapsedMilliseconds;

        var maxAwakePerFrame = 0;
        for (var i = 0; i < frameCount && i < awakeCountPerFrame.Length; i++)
        {
            var awakeThisFrame = i == 0 ? awakeCountPerFrame[i] : awakeCountPerFrame[i] - awakeCountPerFrame[i-1];
            if (awakeThisFrame > maxAwakePerFrame)
                maxAwakePerFrame = awakeThisFrame;
        }

        var fileSizeKB = currentBundleSize / 1024f;
        var compressionRatio = noneCompressionSize > 0 ? (float)currentBundleSize / noneCompressionSize : 1.0f;
        var compressionPercent = (1.0f - compressionRatio) * 100f;

        if (benchmarkResultLabel != null)
        {
            benchmarkResultLabel.text = $"Compression: {selectedCompression}\n" +
                                      $"File Size: {fileSizeKB:F1} KB\n" +
                                      $"Compression Ratio: {compressionRatio:F2} ({compressionPercent:F1}% reduction)\n" +
                                      $"LoadFromFileAsync: {loadFromFileTime}ms\n" +
                                      $"Total Load Time: {totalLoadTime}ms\n" +
                                      $"Loaded {groupAssetNames.Count} ScriptableObjects\n" +
                                      $"Max Awake/Frame: {maxAwakePerFrame}\n" +
                                      $"Total Frames: {frameCount}";
        }
        
        UnityEngine.Debug.Log($"Benchmark completed - Compression: {selectedCompression}, File Size: {fileSizeKB:F1}KB, Ratio: {compressionRatio:F2}, LoadFromFile: {loadFromFileTime}ms, Total: {totalLoadTime}ms, Max Awake/Frame: {maxAwakePerFrame}, Awake Counter: {DummyTextureGroup.AwakeCounter}");

        isBenchmarkRunning = false;
    }

    long GetFileSize(string filePath)
    {
        if (File.Exists(filePath))
            return new FileInfo(filePath).Length;
        return 0;
    }

    void OnDestroy()
    {
        if (loadedBundle != null)
            loadedBundle.Unload(true);
    }
}