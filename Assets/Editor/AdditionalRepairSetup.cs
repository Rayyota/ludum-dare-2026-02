using System.IO;
using Tamagotchi.Minigames;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class AdditionalRepairSetup
{
    private const string Art = "Assets/Art/AdditionalRepairs/";

    [MenuItem("Tools/Tamagotchi/Apply Additional Repairs")]
    public static void Apply()
    {
        const string path = "Assets/Prefabs/Timing Prototype UI.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            Configure(root);
            SessionTimerSetup.Configure(root);
            VictoryScreenSetup.Configure(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
    }

    public static void Configure(GameObject root)
    {
        if (!Directory.Exists(Art)) return;
        foreach (string path in Directory.GetFiles(Art, "*.png"))
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.textureShape = TextureImporterShape.Texture2D;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.SaveAndReimport();
        }
        var canvas = root.transform.Find("Canvas");
        var keys = Panel(canvas, "KeyHoldPanel");
        var keyGame = keys.gameObject.AddComponent<KeyHoldGame>();
        var header = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/ContactMatching/windows.png");
        Sprite timer = null, board = null;
        foreach (var asset in header)
        {
            if (asset is Sprite sprite && sprite.name == "Timer") timer = sprite;
            if (asset is Sprite frame && frame.name == "Board") board = frame;
        }
        Picture(keys, "Window", board, new Vector2(0, -62), new Vector2(628, 316));
        Picture(keys, "TimerFrame", timer, new Vector2(-8, 160), new Vector2(220, 92));
        var keyTimer = Label(keys, "Timer", "00:05", new Vector2(-8, 160), new Vector2(180, 64), 32);
        Label(keys, "Title", "Зажимай клавиши!", new Vector2(0, 12), new Vector2(520, 48), 32);
        string letters = "wasd";
        var keyImages = new Image[letters.Length];
        for (int i = 0; i < letters.Length; i++)
        {
            var sprite = Sprite(Art + "key-" + letters[i] + ".png");
            keyImages[i] = Picture(keys, "Key " + char.ToUpperInvariant(letters[i]), sprite,
                new Vector2(-174 + i * 116, -84), sprite.rect.size * 4);
        }
        var keyData = new SerializedObject(keyGame);
        Set(keyData, "timerText", keyTimer);
        var icons = keyData.FindProperty("keyImages");
        icons.arraySize = keyImages.Length;
        for (int i = 0; i < keyImages.Length; i++) icons.GetArrayElementAtIndex(i).objectReferenceValue = keyImages[i];
        keyData.FindProperty("durationSeconds").floatValue = 5;
        keyData.ApplyModifiedPropertiesWithoutUndo();
        keys.gameObject.SetActive(false);

        var cleaning = Panel(canvas, "CleaningPanel");
        var cleanGame = cleaning.gameObject.AddComponent<BoardCleaningGame>();
        Picture(cleaning, "Window", Sprite(Art + "cleaning-window.png"), new Vector2(6, 4), new Vector2(628, 440));
        Label(cleaning, "Title", "Зачисти контакты", new Vector2(-96, 164), new Vector2(380, 44), 28);
        var cleanTimer = Label(cleaning, "Timer", "00:12", new Vector2(208, 164), new Vector2(108, 44), 28);
        var circuit = Picture(cleaning, "Circuit Board", Sprite(Art + "circuit-board.png"),
            new Vector2(10, -18), new Vector2(516, 276));
        circuit.raycastTarget = true;
        circuit.gameObject.AddComponent<RectMask2D>();
        Vector2[] positions = { new Vector2(-160, 76), new Vector2(132, 68), new Vector2(10, -64),
            new Vector2(174, -68), new Vector2(-168, -74) };
        var dirt = new Image[positions.Length];
        for (int i = 0; i < dirt.Length; i++)
        {
            var sprite = Sprite(Art + "dirt-" + (i + 1) + ".png");
            dirt[i] = Picture(circuit.transform, "Dirt " + (i + 1), sprite, positions[i], sprite.rect.size * 4);
        }
        var cleanData = new SerializedObject(cleanGame);
        Set(cleanData, "timerText", cleanTimer);
        Set(cleanData, "board", circuit.rectTransform);
        cleanData.FindProperty("durationSeconds").floatValue = 12;
        var patches = cleanData.FindProperty("dirt");
        patches.arraySize = dirt.Length;
        for (int i = 0; i < dirt.Length; i++) patches.GetArrayElementAtIndex(i).objectReferenceValue = dirt[i];
        cleanData.ApplyModifiedPropertiesWithoutUndo();
        cleaning.gameObject.SetActive(false);
        var game = new SerializedObject(root.GetComponent<TimingMinigame>());
        Set(game, "keyHold", keyGame);
        Set(game, "boardCleaning", cleanGame);
        game.ApplyModifiedPropertiesWithoutUndo();
    }

    private static RectTransform Panel(Transform canvas, string name)
    {
        var previous = canvas.Find(name);
        if (previous != null) Object.DestroyImmediate(previous.gameObject);
        var panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<RectTransform>();
        panel.SetParent(canvas, false);
        Stretch(panel);
        var image = panel.GetComponent<Image>();
        image.sprite = Sprite("Assets/Art/Tamagotchi/background.png");
        image.raycastTarget = true;
        return panel;
    }

    private static Image Picture(Transform parent, string name, Sprite sprite, Vector2 position, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = Vector2.one * 0.5f;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        var image = rect.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = sprite != null;
        image.raycastTarget = false;
        return image;
    }

    private static Text Label(Transform parent, string name, string title, Vector2 position, Vector2 size, int fontSize)
    {
        var rect = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = Vector2.one * 0.5f;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        var text = rect.GetComponent<Text>();
        text.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Tiny5-Regular.ttf");
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color32(80, 47, 40, 255);
        text.text = title;
        text.raycastTarget = false;
        return text;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static Sprite Sprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);
    private static void Set(SerializedObject data, string name, Object value) => data.FindProperty(name).objectReferenceValue = value;
}
