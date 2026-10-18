using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

public static class ContactMatchingSkin
{
    private const string Art = "Assets/Art/ContactMatching/";
    private const string Scene = "Assets/Scenes/Minigames/ContactMatching.unity";

    [MenuItem("Tools/Tamagotchi/Apply Contact Matching Sprites")]
    public static void Apply()
    {
        foreach (string path in Directory.GetFiles(Art, "*.png")) Import(path);
        var scene = EditorSceneManager.OpenScene(Scene);
        var canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(912, 512);
        scaler.matchWidthOrHeight = 0.5f;
        canvas.pixelPerfect = true;
        var parent = canvas.transform;
        var background = Image(parent, "Background", Sprite("Assets/Art/Tamagotchi/background.png"));
        background.rectTransform.anchorMin = Vector2.zero;
        background.rectTransform.anchorMax = Vector2.one;
        background.rectTransform.offsetMin = background.rectTransform.offsetMax = Vector2.zero;
        background.transform.SetAsFirstSibling();
        var board = Image(parent, "Board", Frame("Board"));
        Place(board.rectTransform, new Vector2(0, -62), new Vector2(628, 316));
        board.transform.SetSiblingIndex(1);
        var timer = Image(parent, "TimerFrame", Frame("Timer"));
        Place(timer.rectTransform, new Vector2(-8, 160), new Vector2(220, 92));
        timer.transform.SetSiblingIndex(2);
        var cables = parent.Find("Cables") as RectTransform;
        Stretch(cables);
        var anchors = parent.Find("CableAnchors") as RectTransform;
        Stretch(anchors);
        string[] colors = { "red", "blue", "green", "yellow" };
        Color32[] tints = { new Color32(172, 48, 45, 255), new Color32(105, 180, 193, 255),
            new Color32(133, 173, 39, 255), new Color32(255, 247, 62, 255) };
        for (int i = 0; i < colors.Length; i++)
        {
            float y = 28 - i * 52;
            var moving = parent.Find("MovingContact" + (i + 1));
            var fixedContact = parent.Find("FixedContact" + (i + 1));
            SetSprite(moving.GetComponent<Image>(), Sprite(Art + colors[i] + "-moving.png"), true);
            SetSprite(fixedContact.GetComponent<Image>(), Sprite(Art + colors[i] + "-fixed.png"));
            Place((RectTransform)moving, new Vector2(-192, y), new Vector2(80, 40));
            Place((RectTransform)fixedContact, new Vector2(212, y), new Vector2(64, 40));
            moving.Find("PlugVisual").gameObject.SetActive(false);
            fixedContact.Find("SocketVisual").gameObject.SetActive(false);
            fixedContact.Find("RightWire").gameObject.SetActive(false);
            Place((RectTransform)moving.Find("PlugSnapPoint"), new Vector2(40, 0), Vector2.zero);
            Place((RectTransform)moving.Find("CableAttachPoint"), new Vector2(-40, 0), Vector2.zero);
            Place((RectTransform)fixedContact.Find("SocketSnapPoint"), new Vector2(-32, 0), Vector2.zero);
            Place((RectTransform)anchors.Find("WireAnchor" + (i + 1)), new Vector2(-304, y), Vector2.zero);
            var cable = cables.Find("Cable" + (i + 1));
            Place((RectTransform)cable, new Vector2(-268, y), new Vector2(72, 8));
            cable.localRotation = Quaternion.identity;
            cable.GetComponent<Image>().color = tints[i];
            cable.GetComponent<Image>().raycastTarget = false;
            var cableData = new SerializedObject(cable.GetComponent<DynamicCable>());
            cableData.FindProperty("thickness").floatValue = 8;
            cableData.ApplyModifiedPropertiesWithoutUndo();
            var contactData = new SerializedObject(moving.GetComponent<ContactDrag>());
            contactData.FindProperty("snapDistance").floatValue = 32;
            contactData.ApplyModifiedPropertiesWithoutUndo();
        }
        var font = PixelFont();
        var timerText = parent.Find("TimerText").GetComponent<TMP_Text>();
        Place(timerText.rectTransform, new Vector2(-8, 160), new Vector2(180, 64));
        StyleText(timerText, font, 32);
        timerText.text = "00:12";
        var result = parent.Find("ResultPanel");
        SetSprite(result.GetComponent<Image>(), Frame("Timer"));
        Place((RectTransform)result, Vector2.zero, new Vector2(440, 184));
        result.SetAsLastSibling();
        var resultText = result.GetComponentInChildren<TMP_Text>(true);
        Place(resultText.rectTransform, Vector2.zero, new Vector2(400, 120));
        StyleText(resultText, font, 32);
        result.gameObject.SetActive(false);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(scene);
    }

    private static void Import(string path)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.textureShape = TextureImporterShape.Texture2D;
        importer.spriteImportMode = path.EndsWith("windows.png") ? SpriteImportMode.Multiple : SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.npotScale = TextureImporterNPOTScale.None;
        if (path.EndsWith("windows.png"))
        {
#pragma warning disable CS0618
            importer.spritesheet = new[] {
                new SpriteMetaData { name = "Timer", rect = new Rect(49, 83, 55, 23), pivot = Vector2.one * 0.5f },
                new SpriteMetaData { name = "Board", rect = new Rect(0, 0, 157, 79), pivot = Vector2.one * 0.5f }
            };
#pragma warning restore CS0618
        }
        importer.SaveAndReimport();
    }

    private static TMP_FontAsset PixelFont()
    {
        const string path = "Assets/Fonts/Tiny5-Pixel.asset";
        var asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if (asset != null) return asset;
        asset = TMP_FontAsset.CreateFontAsset(AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Tiny5-Regular.ttf"),
            32, 1, GlyphRenderMode.RASTER_HINTED, 512, 512);
        asset.name = "Tiny5 Pixel";
        asset.atlasTexture.filterMode = FilterMode.Point;
        asset.TryAddCharacters("0123456789:ПОБЕДА!ВРЕМЯЫШЛО");
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.AddObjectToAsset(asset.material, asset);
        foreach (var atlas in asset.atlasTextures) AssetDatabase.AddObjectToAsset(atlas, asset);
        return asset;
    }

    private static void StyleText(TMP_Text text, TMP_FontAsset font, int size)
    {
        text.font = font;
        text.fontSize = size;
        text.enableAutoSizing = false;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color32(80, 47, 40, 255);
        text.raycastTarget = false;
    }

    private static Sprite Frame(string name) => Array.Find(AssetDatabase.LoadAllAssetsAtPath(Art + "windows.png"),
        asset => asset is Sprite && asset.name == name) as Sprite;
    private static Sprite Sprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

    private static Image Image(Transform parent, string name, Sprite sprite)
    {
        var child = parent.Find(name);
        if (child == null)
        {
            child = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).transform;
            child.SetParent(parent, false);
        }
        var image = child.GetComponent<Image>();
        SetSprite(image, sprite);
        return image;
    }

    private static void SetSprite(Image image, Sprite sprite, bool interactive = false)
    {
        image.sprite = sprite;
        image.type = UnityEngine.UI.Image.Type.Simple;
        image.color = Color.white;
        image.preserveAspect = true;
        image.raycastTarget = interactive;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = Vector2.one * 0.5f;
        rect.pivot = Vector2.one * 0.5f;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }
}
