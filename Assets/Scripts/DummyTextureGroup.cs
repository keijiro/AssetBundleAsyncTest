using UnityEngine;

[CreateAssetMenu(fileName = "DummyTextureGroup", menuName = "Test/Dummy Texture Group")]
public class DummyTextureGroup : ScriptableObject
{
    public static int AwakeCounter { get; private set; }

    [SerializeField] Texture2D[] textures = new Texture2D[10];

    public Texture2D[] Textures => textures;

    void Awake()
        => AwakeCounter++;

    public static void ResetCounter()
        => AwakeCounter = 0;
}