using Tamagotchi.Minigames;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class VictoryScreenSetup
{
    [MenuItem("Tools/Tamagotchi/Apply Victory Screen")]
    public static void Apply()
    {
        const string path = "Assets/Prefabs/Timing Prototype UI.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            Configure(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
    }

    public static void Configure(GameObject root)
    {
        const string art = "Assets/Art/Tamagotchi/win-screen.png";
        var importer = AssetImporter.GetAtPath(art) as TextureImporter;
        if (importer == null) return;
        importer.textureType = TextureImporterType.Sprite;
        importer.textureShape = TextureImporterShape.Texture2D;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.SaveAndReimport();
        var canvas = root.transform.Find("Canvas");
        var screen = canvas.Find("WinScreen") as RectTransform;
        if (screen == null)
        {
            screen = new GameObject("WinScreen", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<RectTransform>();
            screen.SetParent(canvas, false);
        }
        screen.anchorMin = Vector2.zero;
        screen.anchorMax = Vector2.one;
        screen.offsetMin = screen.offsetMax = Vector2.zero;
        screen.GetComponent<Image>().sprite = AssetDatabase.LoadAssetAtPath<Sprite>(art);
        screen.SetAsLastSibling();
        var retry = screen.Find("RetryButton") as RectTransform;
        if (retry == null)
        {
            retry = new GameObject("RetryButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button)).GetComponent<RectTransform>();
            retry.SetParent(screen, false);
        }
        retry.anchorMin = new Vector2(69f / 228f, 10f / 128f);
        retry.anchorMax = new Vector2(160f / 228f, 36f / 128f);
        retry.offsetMin = retry.offsetMax = Vector2.zero;
        var image = retry.GetComponent<Image>();
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Tamagotchi/UI/retry.png");
        var button = retry.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        var data = new SerializedObject(root.GetComponent<TimingMinigame>());
        data.FindProperty("winScreen").objectReferenceValue = screen.gameObject;
        data.FindProperty("winRetryButton").objectReferenceValue = button;
        data.ApplyModifiedPropertiesWithoutUndo();
        screen.gameObject.SetActive(false);
        PetAudioSetup.Configure(root);
    }
}
