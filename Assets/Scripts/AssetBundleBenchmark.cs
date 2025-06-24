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
    
    AssetBundle loadedBundle;
    bool isBenchmarkRunning;

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

        if (startBenchmarkButton != null)
            startBenchmarkButton.clicked += StartBenchmark;
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
        
        Application.backgroundLoadingPriority = ThreadPriority.Low;
        
        var stopwatch = Stopwatch.StartNew();

        var bundlePath = Path.Combine(Application.streamingAssetsPath, BundleName);
        var bundleRequest = AssetBundle.LoadFromFileAsync(bundlePath);
        yield return bundleRequest;

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
        var frameCount = 0;
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
            
            UnityEngine.Debug.Log($"Frame {frameCount++}: Awake Counter = {DummyTextureGroup.AwakeCounter}");
            
            yield return null;
        }

        stopwatch.Stop();

        var elapsedMs = stopwatch.ElapsedMilliseconds;
        if (benchmarkResultLabel != null)
            benchmarkResultLabel.text = $"Loaded {groupAssetNames.Count} ScriptableObjects in {elapsedMs}ms";
        
        UnityEngine.Debug.Log($"Benchmark completed: {elapsedMs}ms, Awake Counter: {DummyTextureGroup.AwakeCounter}");

        isBenchmarkRunning = false;
    }

    void OnDestroy()
    {
        if (loadedBundle != null)
            loadedBundle.Unload(true);
    }
}