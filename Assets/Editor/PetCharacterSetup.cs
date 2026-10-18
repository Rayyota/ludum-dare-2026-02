using Tamagotchi;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class PetCharacterSetup
{
    [MenuItem("Tools/Tamagotchi/Apply Pet Character")]
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
        var frames = new Sprite[4];
        for (int i = 0; i < frames.Length; i++)
        {
            string path = $"Assets/Art/Tamagotchi/Character/idle-{i}.png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.textureShape = TextureImporterShape.Texture2D;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.SaveAndReimport();
            frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        var pet = root.transform.Find("Canvas/Home/Pet");
        var screen = pet.Find("Screen") as RectTransform;
        if (screen == null)
        {
            screen = new GameObject("Screen", typeof(RectTransform), typeof(AspectRatioFitter)).GetComponent<RectTransform>();
            screen.SetParent(pet, false);
        }
        screen.anchorMin = Vector2.zero;
        screen.anchorMax = Vector2.one;
        screen.offsetMin = screen.offsetMax = Vector2.zero;
        var fitter = screen.GetComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = 114f / 124f;
        var character = screen.Find("Character") as RectTransform;
        if (character == null)
        {
            character = new GameObject("Character", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(PetSpriteAnimation)).GetComponent<RectTransform>();
            character.SetParent(screen, false);
        }
        var center = new Vector2(60f / 114f, 1f - 55f / 124f);
        var half = new Vector2(50f / 114f, 40f / 124f);
        character.anchorMin = center - half;
        character.anchorMax = center + half;
        character.offsetMin = character.offsetMax = Vector2.zero;
        var image = character.GetComponent<Image>();
        image.sprite = frames[0];
        image.preserveAspect = true;
        image.raycastTarget = false;
        var data = new SerializedObject(character.GetComponent<PetSpriteAnimation>());
        var sprites = data.FindProperty("frames");
        var durations = data.FindProperty("frameDurations");
        sprites.arraySize = durations.arraySize = frames.Length;
        float[] seconds = { 0.1f, 0.1f, 0.3f, 0.1f };
        for (int i = 0; i < frames.Length; i++)
        {
            sprites.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
            durations.GetArrayElementAtIndex(i).floatValue = seconds[i];
        }
        data.ApplyModifiedPropertiesWithoutUndo();
        pet.Find("CareActions").SetAsLastSibling();
    }
}
