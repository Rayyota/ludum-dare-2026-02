using Tamagotchi.Minigames;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class SessionTimerSetup
{
    [MenuItem("Tools/Tamagotchi/Apply Session Timer")]
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
        const string path = "Assets/Art/Tamagotchi/UI/session-timer.png";
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
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
        var frame = canvas.Find("SessionTimer") as RectTransform;
        if (frame == null)
        {
            frame = new GameObject("SessionTimer", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<RectTransform>();
            frame.SetParent(canvas, false);
        }
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        frame.anchorMin = frame.anchorMax = new Vector2(0, 1);
        frame.pivot = new Vector2(0, 1);
        frame.anchoredPosition = new Vector2(24, -20);
        frame.sizeDelta = sprite.rect.size * 2;
        frame.SetAsLastSibling();
        var image = frame.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.raycastTarget = false;
        var label = frame.Find("Time") as RectTransform;
        if (label == null)
        {
            label = new GameObject("Time", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)).GetComponent<RectTransform>();
            label.SetParent(frame, false);
        }
        label.anchorMin = Vector2.zero;
        label.anchorMax = Vector2.one;
        label.offsetMin = label.offsetMax = Vector2.zero;
        var text = label.GetComponent<Text>();
        text.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Tiny5-Regular.ttf");
        text.fontSize = 24;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color32(80, 47, 40, 255);
        text.raycastTarget = false;
        text.text = "03:00";
        var data = new SerializedObject(root.GetComponent<TimingMinigame>());
        data.FindProperty("sessionTimerFrame").objectReferenceValue = frame.gameObject;
        data.FindProperty("sessionTimerText").objectReferenceValue = text;
        data.ApplyModifiedPropertiesWithoutUndo();
        frame.gameObject.SetActive(false);
    }
}
