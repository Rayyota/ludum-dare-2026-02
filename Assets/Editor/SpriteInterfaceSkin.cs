using System;
using System.IO;
using Tamagotchi;
using Tamagotchi.Minigames;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class SpriteInterfaceSkin
{
    private const string Art = "Assets/Art/Tamagotchi/";
    private const string PrefabPath = "Assets/Prefabs/Timing Prototype UI.prefab";

    [MenuItem("Tools/Tamagotchi/Apply Supplied Sprites")]
    public static void Apply()
    {
        ImportTextures();
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            ApplyArtwork(root);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
        Debug.Log("Supplied sprites applied to the editable UI prefab.");
    }

    private static void ImportTextures()
    {
        foreach (string path in Directory.GetFiles(Art, "*.png", SearchOption.AllDirectories))
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;
            importer.textureType = TextureImporterType.Sprite;
            importer.textureShape = TextureImporterShape.Texture2D;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            if (path.Replace('\\', '/').EndsWith("UI/frames.png"))
            {
                importer.spriteImportMode = SpriteImportMode.Multiple;
#pragma warning disable CS0618
                importer.spritesheet = new[]
                {
                    new SpriteMetaData { name = "Header", rect = new Rect(0, 88, 91, 26),
                        alignment = 0, pivot = Vector2.one * 0.5f, border = new Vector4(6, 6, 6, 6) },
                    new SpriteMetaData { name = "Board", rect = new Rect(0, 0, 91, 86),
                        alignment = 0, pivot = Vector2.one * 0.5f, border = new Vector4(6, 6, 6, 6) }
                };
#pragma warning restore CS0618
            }
            importer.SaveAndReimport();
        }
    }

    private static void ApplyArtwork(GameObject root)
    {
        var canvas = root.transform.Find("Canvas");
        canvas.GetComponent<CanvasScaler>().referenceResolution = new Vector2(912, 512);
        canvas.GetComponent<Canvas>().pixelPerfect = true;
        var home = canvas.Find("Home");
        var background = home.GetComponent<Image>();
        SetSprite(background, Sprite("background.png"));
        var header = Frame("Header");
        var board = Frame("Board");
        if (header == null || board == null) throw new InvalidOperationException("UI frame slices are missing.");

        var pet = home.Find("Pet").GetComponent<Image>();
        Place(pet.rectTransform, new Vector2(66f / 228f, 62f / 128f), Vector2.one * 0.5f,
            Vector2.zero, new Vector2(456, 496));
        SetSprite(pet, Sprite("device.png"));
        pet.preserveAspect = true;
        pet.enabled = true;
        pet.transform.Find("Placeholder").gameObject.SetActive(false);
        MapDeviceButtons(root);
        PetCharacterSetup.Configure(root);

        var lives = home.Find("Lives") as RectTransform;
        Place(lives, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-32, -32), new Vector2(364, 104));
        SetSprite(EnsureImage(lives), header, true);
        var hearts = lives.GetComponentsInChildren<HeartIcon>();
        for (int i = 0; i < hearts.Length; i++)
        {
            Place(hearts[i].rectTransform, new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(88 + i * 60, -24), new Vector2(52, 48));
            var data = new SerializedObject(hearts[i]);
            Set(data, "fullSprite", Sprite("UI/heart-full.png"));
            Set(data, "emptySprite", Sprite("UI/heart-empty.png"));
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        var stats = home.Find("Stats") as RectTransform;
        Place(stats, Vector2.one, Vector2.one, new Vector2(-32, -144), new Vector2(364, 344));
        SetSprite(EnsureImage(stats), board, true);
        string[] nodes = { "Joy", "Satiety", "Cleanliness", "Breakdown" };
        string[] icons = { "icon-joy.png", "icon-food.png", "icon-cleanliness.png", "icon-breakdown.png" };
        for (int i = 0; i < nodes.Length; i++)
        {
            var row = stats.Find(nodes[i]) as RectTransform;
            Place(row, new Vector2(0, 1), new Vector2(0, 1), new Vector2(36, -24 - i * 68), new Vector2(288, 64));
            row.Find("Label").gameObject.SetActive(false);
            var bar = row.Find("Bar").GetComponent<Image>();
            Place(bar.rectTransform, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(288, 64));
            SetSprite(bar, Sprite("UI/bar.png"));
            var slider = bar.GetComponent<Slider>();
            var fill = slider.fillRect;
            var area = bar.transform.Find("FillArea") as RectTransform;
            if (area == null) area = CreateRect(bar.transform, "FillArea");
            area.anchorMin = Vector2.zero; area.anchorMax = Vector2.one;
            area.offsetMin = new Vector2(64, 16); area.offsetMax = new Vector2(-12, -16);
            fill.SetParent(area, false);
            Stretch(fill);
            fill.GetComponent<Image>().color = i == 3 ? new Color32(202, 68, 52, 255) : new Color32(104, 195, 48, 255);
            slider.fillRect = fill;
            float value = slider.value;
            slider.SetValueWithoutNotify(100);
            slider.SetValueWithoutNotify(value);
            var icon = ChildImage(bar.transform, "Icon");
            var sprite = Sprite("UI/" + icons[i]);
            SetSprite(icon, sprite);
            Place(icon.rectTransform, new Vector2(0, 0.5f), Vector2.one * 0.5f,
                new Vector2(32, 0), sprite.rect.size * 4);
            icon.preserveAspect = true;
        }

        LayoutHome(root);
        PetAudioSetup.Configure(root);

        var start = ChildImage(canvas, "StartScreen");
        Stretch(start.rectTransform);
        SetSprite(start, Sprite("start-screen.png"));
        var startButton = MenuButton(start.transform, "StartButton", "start.png", "start-pressed.png");
        var death = ChildImage(canvas, "GameOverScreen");
        Stretch(death.rectTransform);
        SetSprite(death, Sprite("game-over-screen.png"));
        var restart = root.GetComponent<TimingMinigame>();
        var config = new SerializedObject(restart);
        var retry = config.FindProperty("restartButton").objectReferenceValue as Button;
        retry.transform.SetParent(death.transform, false);
        StyleMenuButton(retry, "retry.png", "retry-pressed.png");
        var label = retry.GetComponentInChildren<Text>(true);
        if (label != null) label.gameObject.SetActive(false);
        retry.gameObject.SetActive(true);
        home.Find("Status").gameObject.SetActive(false);
        start.transform.SetAsLastSibling();
        death.transform.SetAsLastSibling();
        var repair = canvas.Find("RepairPanel");
        repair.SetAsLastSibling();
        repair.GetComponent<Image>().color = new Color(0.02f, 0.06f, 0.03f, 0.78f);
        var puzzle = repair.Find("Puzzle").GetComponent<Image>();
        SetSprite(puzzle, board, true);
        StyleWoodButton(puzzle.transform.Find("HitButton").GetComponent<Button>(), header);
        start.gameObject.SetActive(true);
        death.gameObject.SetActive(false);
        repair.gameObject.SetActive(false);
        Set(config, "healthyPetSprite", Sprite("device.png"));
        Set(config, "brokenPetSprite", Sprite("device-broken.png"));
        Set(config, "startScreen", start.gameObject);
        Set(config, "gameOverScreen", death.gameObject);
        Set(config, "startButton", startButton);
        config.ApplyModifiedPropertiesWithoutUndo();
        ContactRepairIntegration.Configure(root);
        AdditionalRepairSetup.Configure(root);
        SessionTimerSetup.Configure(root);
        VictoryScreenSetup.Configure(root);
    }

    [MenuItem("Tools/Tamagotchi/Apply Home Spacing")]
    public static void ApplyHomeSpacing()
    {
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            LayoutHome(root);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
    }

    private static void LayoutHome(GameObject root)
    {
        const float scale = 0.86f;
        var home = root.transform.Find("Canvas/Home");
        var pet = (RectTransform)home.Find("Pet");
        Place(pet, Vector2.one * 0.5f, Vector2.one * 0.5f,
            new Vector2(-206, -8) * scale, new Vector2(456, 496));
        pet.localScale = Vector3.one * scale;
        var lives = (RectTransform)home.Find("Lives");
        var stats = (RectTransform)home.Find("Stats");
        var edgeInset = new Vector2(456, 256) * (1 - scale);
        Place(lives, Vector2.one, Vector2.one,
            -edgeInset + new Vector2(-18, -32) * scale, new Vector2(364, 104));
        Place(stats, Vector2.one, Vector2.one,
            -edgeInset + new Vector2(-18, -144) * scale, new Vector2(364, 344));
        lives.localScale = stats.localScale = Vector3.one * scale;
    }

    public static void BindDeviceButtons()
    {
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            MapDeviceButtons(root);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
    }

    private static void MapDeviceButtons(GameObject root)
    {
        var home = root.transform.Find("Canvas/Home");
        var pet = home.Find("Pet");
        var actions = (home.Find("CareActions") ?? pet.Find("CareActions")) as RectTransform;
        actions.SetParent(pet, false);
        Stretch(actions);
        var fitter = actions.GetComponent<AspectRatioFitter>();
        if (fitter == null) fitter = actions.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = 114f / 124f;
        string[] names = { "FeedButton", "WashButton", "PlayButton" };
        Vector2[] centers = { new Vector2(63, 104), new Vector2(90, 100), new Vector2(37, 102) };
        for (int i = 0; i < names.Length; i++)
        {
            var button = actions.Find(names[i]).GetComponent<Button>();
            var rect = button.GetComponent<RectTransform>();
            var center = new Vector2(centers[i].x / 114f, 1f - centers[i].y / 124f);
            var half = new Vector2(12f / 114f, 12f / 124f);
            rect.anchorMin = center - half;
            rect.anchorMax = center + half;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = button.GetComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            image.type = Image.Type.Simple;
            image.color = new Color(1, 1, 1, 0);
            image.raycastTarget = true;
            button.transition = Selectable.Transition.None;
            var label = button.transform.Find("Label");
            if (label != null) UnityEngine.Object.DestroyImmediate(label.gameObject);
            if (button.GetComponent<CircularButtonHitArea>() == null)
                button.gameObject.AddComponent<CircularButtonHitArea>();

        }

    }

    private static Sprite Sprite(string name)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + name);
        if (sprite == null) throw new InvalidOperationException("Missing sprite: " + name);
        return sprite;
    }

    private static Sprite Frame(string name) => Array.Find(AssetDatabase.LoadAllAssetsAtPath(Art + "UI/frames.png"),
        asset => asset is Sprite && asset.name == name) as Sprite;

    private static void Set(SerializedObject data, string field, UnityEngine.Object value) =>
        data.FindProperty(field).objectReferenceValue = value;

    private static Image EnsureImage(RectTransform rect)
    {
        var image = rect.GetComponent<Image>();
        if (image == null) image = rect.gameObject.AddComponent<Image>();
        image.raycastTarget = false;
        return image;
    }

    private static RectTransform CreateRect(Transform parent, string name)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj.GetComponent<RectTransform>();
    }

    private static Image ChildImage(Transform parent, string name)
    {
        var child = parent.Find(name) as RectTransform;
        if (child == null) child = CreateRect(parent, name);
        return EnsureImage(child);
    }

    private static void SetSprite(Image image, Sprite sprite, bool sliced = false)
    {
        image.sprite = sprite;
        image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
        image.pixelsPerUnitMultiplier = sliced ? 0.25f : 1f;
        image.color = Color.white;
        image.raycastTarget = false;
    }

    private static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static void StyleWoodButton(Button button, Sprite frame)
    {
        SetSprite(button.GetComponent<Image>(), frame);
        var rect = button.GetComponent<RectTransform>();
        Place(rect, new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(0, 16), frame.rect.size * 2);
        button.GetComponent<Image>().raycastTarget = true;
        button.transition = Selectable.Transition.None;
        var label = button.GetComponentInChildren<Text>(true);
        if (label != null) label.color = new Color32(80, 47, 40, 255);
    }

    private static Button MenuButton(Transform parent, string name, string normal, string pressed)
    {
        var image = ChildImage(parent, name);
        var button = image.GetComponent<Button>();
        if (button == null) button = image.gameObject.AddComponent<Button>();
        StyleMenuButton(button, normal, pressed);
        return button;
    }

    private static void StyleMenuButton(Button button, string normal, string pressed)
    {
        var image = button.GetComponent<Image>();
        SetSprite(image, Sprite("UI/" + normal));
        image.raycastTarget = true;
        var rect = image.rectTransform;
        rect.anchorMin = new Vector2(69f / 228f, 10f / 128f);
        rect.anchorMax = new Vector2(160f / 228f, 36f / 128f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;
        button.spriteState = default;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
    }
}
