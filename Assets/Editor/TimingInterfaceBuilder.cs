using Tamagotchi;
using Tamagotchi.Minigames;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class TimingInterfaceBuilder
{
    private static readonly Color Background = new Color32(23, 31, 35, 255);
    private static readonly Color Ink = new Color32(234, 235, 218, 255);
    private static readonly Color Green = new Color32(185, 208, 155, 255);
    private static Font font;

    [MenuItem("Tools/Tamagotchi/Rebuild Prototype UI Scene")]
    public static void BuildFromMenu()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) Build();
    }

    public static void Build()
    {
        font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Tiny5-Regular.ttf");
        if (font == null) throw new System.InvalidOperationException("Tiny5-Regular.ttf is missing.");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camera.tag = "MainCamera";
        camera.transform.position = new Vector3(0, 0, -10);
        camera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
        camera.GetComponent<Camera>().backgroundColor = Background;
        var root = new GameObject("Timing Prototype");
        var petStatus = root.AddComponent<PetStatus>();
        var controller = root.AddComponent<TimingMinigame>();
        var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(root.transform, false);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(960, 600);
        scaler.matchWidthOrHeight = 0.5f;
        var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        events.transform.SetParent(root.transform, false);
        events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();

        var home = Panel(canvasObject.transform, "Home", Background);
        Stretch(home.rectTransform);
        var lives = Rect(home.transform, "Lives", new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(48, -24), new Vector2(120, 36));
        var hearts = new HeartIcon[3];
        for (int i = 0; i < hearts.Length; i++)
        {
            var heart = Rect(lives, "Heart " + (i + 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(i * 40, 0), new Vector2(34, 34));
            hearts[i] = heart.gameObject.AddComponent<HeartIcon>();
            hearts[i].raycastTarget = false;
        }

        var petRect = Rect(home.transform, "Pet", new Vector2(0.33f, 0.48f), Vector2.one * 0.5f,
            Vector2.zero, new Vector2(360, 320));
        var petGroup = petRect.gameObject.AddComponent<CanvasGroup>();
        var petImage = petRect.gameObject.AddComponent<Image>();
        petImage.preserveAspect = true;
        petImage.raycastTarget = false;
        petImage.enabled = false;
        var placeholder = Rect(petRect, "Placeholder", Vector2.one * 0.5f, Vector2.one * 0.5f,
            Vector2.zero, new Vector2(260, 240));
        var body = Panel(placeholder, "Body", Green);
        Stretch(body.rectTransform);
        body.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        body.type = Image.Type.Sliced;
        body.raycastTarget = false;
        for (int i = 0; i < 2; i++)
        {
            var eye = Panel(placeholder, i == 0 ? "Left Eye" : "Right Eye", Background);
            Position(eye.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f,
                new Vector2(i == 0 ? -42 : 42, 15), new Vector2(20, 30));
            eye.raycastTarget = false;
        }
        Label(placeholder, "Mouth", "v", 40, Background, TextAnchor.MiddleCenter,
            Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0, -35), new Vector2(80, 50));
        var status = Label(home.transform, "Status", "", 24, Ink, TextAnchor.MiddleCenter,
            new Vector2(0.33f, 0), new Vector2(0.5f, 0), new Vector2(0, 100), new Vector2(520, 70));
        var restart = MakeButton(home.transform, "RestartButton", "Начать заново", Green, Background,
            new Vector2(0.33f, 0), new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(300, 46));
        restart.gameObject.SetActive(false);

        var stats = Rect(home.transform, "Stats", Vector2.one, Vector2.one,
            new Vector2(-48, -72), new Vector2(220, 200));
        string[] names = { "Satiety", "Cleanliness", "Joy", "Breakdown" };
        string[] titles = { "Сытость", "Чистота", "Радость", "Поломка" };
        var bars = new Slider[4];
        var labels = new Text[4];
        for (int i = 0; i < bars.Length; i++)
        {
            var row = Rect(stats, names[i], new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(0, -i * 46), new Vector2(220, 40));
            labels[i] = Label(row, "Label", titles[i] + " 0%", 16, Ink,
                TextAnchor.MiddleLeft, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, new Vector2(220, 22));
            var bar = Panel(row, "Bar", new Color32(15, 24, 28, 255));
            Position(bar.rectTransform, new Vector2(0, 0), new Vector2(0, 0), Vector2.zero, new Vector2(220, 12));
            bar.raycastTarget = false;
            var fill = Panel(bar.transform, "Fill", i == 3 ? new Color32(172, 92, 73, 255) : new Color32(107, 158, 96, 255));
            Stretch(fill.rectTransform);
            fill.raycastTarget = false;
            var slider = bar.gameObject.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.minValue = 0;
            slider.maxValue = 100;
            slider.value = 0;
            slider.interactable = false;
            slider.transition = Selectable.Transition.None;
            slider.navigation = new Navigation { mode = Navigation.Mode.None };
            bars[i] = slider;
        }

        var modal = Panel(canvasObject.transform, "RepairPanel", new Color(0.03f, 0.06f, 0.07f, 0.96f));
        Stretch(modal.rectTransform);
        var puzzle = Panel(modal.transform, "Puzzle", new Color32(36, 47, 50, 255));
        Position(puzzle.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(650, 310));
        Label(puzzle.transform, "Title", "Восстанови контакт", 32, Ink, TextAnchor.MiddleLeft,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, -22), new Vector2(590, 44));
        var stage = Label(puzzle.transform, "Stage", "Контакты: 0 / 3", 16, Ink, TextAnchor.MiddleLeft,
            new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(30, 54), new Vector2(270, 28));
        var time = Label(puzzle.transform, "Time", "Время: 5.0 с", 16, Ink, TextAnchor.MiddleRight,
            new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-30, 54), new Vector2(270, 28));
        var track = Panel(puzzle.transform, "Track", new Color32(15, 24, 28, 255));
        Position(track.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(570, 54));
        WireHit(track.gameObject, controller);
        var target = Panel(track.transform, "Target", new Color32(107, 158, 96, 255));
        target.rectTransform.anchorMin = new Vector2(0.27f, 0);
        target.rectTransform.anchorMax = new Vector2(0.73f, 1);
        target.rectTransform.offsetMin = new Vector2(0, 4);
        target.rectTransform.offsetMax = new Vector2(0, -4);
        target.raycastTarget = false;
        var cursor = Panel(track.transform, "Cursor", new Color32(255, 246, 218, 255));
        cursor.rectTransform.anchorMin = Vector2.zero;
        cursor.rectTransform.anchorMax = new Vector2(0, 1);
        cursor.rectTransform.sizeDelta = new Vector2(4, 16);
        cursor.rectTransform.anchoredPosition = Vector2.zero;
        cursor.raycastTarget = false;
        var hint = Label(puzzle.transform, "Feedback", "", 16, Ink, TextAnchor.MiddleCenter,
            Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0, -52), new Vector2(570, 30));
        var hit = MakeButton(puzzle.transform, "HitButton", "Поймать контакт", Green, Background,
            new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 24), new Vector2(570, 46));
        WireHit(hit.gameObject, controller);

        var config = new SerializedObject(controller);
        Set(config, "petStatus", petStatus);
        Set(config, "repairPanel", modal.gameObject);
        Set(config, "target", target.rectTransform);
        Set(config, "cursor", cursor.rectTransform);
        Set(config, "hearts", hearts);
        Set(config, "statBars", bars);
        Set(config, "statLabels", labels);
        Set(config, "statusLabel", status);
        Set(config, "hitButton", hit);
        Set(config, "restartButton", restart);
        Set(config, "petImage", petImage);
        Set(config, "placeholder", placeholder.gameObject);
        Set(config, "petGroup", petGroup);
        config.ApplyModifiedPropertiesWithoutUndo();
        AddCareButtons(root);
        SimplifyRepairPanel(root);
        SyncInitialStats(root);
        modal.gameObject.SetActive(false);
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
        PrefabUtility.SaveAsPrefabAssetAndConnect(root, "Assets/Prefabs/Timing Prototype UI.prefab", InteractionMode.AutomatedAction);
        EditorSceneManager.SaveScene(root.scene, "Assets/Scenes/TimingPrototype.unity");
        AssetDatabase.SaveAssets();
        if (AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Tamagotchi/device.png") != null)
            SpriteInterfaceSkin.Apply();
        Debug.Log("Editable Canvas scene and Timing Prototype UI prefab saved.");
    }

    public static void UpgradeCareButtons()
    {
        const string path = "Assets/Prefabs/Timing Prototype UI.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            AddCareButtons(root);
            SimplifyRepairPanel(root);
            SyncInitialStats(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
    }

    private static void AddCareButtons(GameObject root)
    {
        font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Tiny5-Regular.ttf");
        var care = root.GetComponent<PetCareActions>();
        if (care == null) care = root.AddComponent<PetCareActions>();
        var home = root.transform.Find("Canvas/Home");
        var controls = (home.Find("CareActions") ?? home.Find("Pet/CareActions")) as RectTransform;
        if (controls == null)
            controls = Rect(home, "CareActions", new Vector2(0.33f, 0), new Vector2(0.5f, 0),
                new Vector2(0, 80), new Vector2(528, 50));
        string[] names = { "FeedButton", "WashButton", "PlayButton" };
        string[] titles = { "Покормить", "Помыть", "Поиграть" };
        UnityEngine.Events.UnityAction[] actions = { care.Feed, care.Wash, care.Play };
        var buttons = new Button[3];
        for (int i = 0; i < buttons.Length; i++)
        {
            var existing = controls.Find(names[i]);
            if (existing != null) buttons[i] = existing.GetComponent<Button>();
            else
            {
                buttons[i] = MakeButton(controls, names[i], titles[i], Green, Background,
                    Vector2.zero, Vector2.zero, new Vector2(i * 180, 0), new Vector2(168, 46));
                UnityEventTools.AddPersistentListener(buttons[i].onClick, actions[i]);
            }
        }
        var data = new SerializedObject(care);
        Set(data, "petStatus", root.GetComponent<PetStatus>());
        Set(data, "repairGame", root.GetComponent<TimingMinigame>());
        Set(data, "controls", controls.gameObject);
        Set(data, "feedButton", buttons[0]);
        Set(data, "washButton", buttons[1]);
        Set(data, "playButton", buttons[2]);
        data.ApplyModifiedPropertiesWithoutUndo();
        var config = new SerializedObject(root.GetComponent<TimingMinigame>());
        Set(config, "careActions", care);
        config.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SimplifyRepairPanel(GameObject root)
    {
        var puzzle = root.transform.Find("Canvas/RepairPanel/Puzzle") as RectTransform;
        puzzle.sizeDelta = new Vector2(620, 220);
        foreach (string name in new[] { "Title", "Stage", "Time", "Feedback" })
        {
            var child = puzzle.Find(name);
            if (child != null) Object.DestroyImmediate(child.gameObject);
        }
        var track = puzzle.Find("Track") as RectTransform;
        track.anchoredPosition = new Vector2(0, 12);
        track.sizeDelta = new Vector2(560, 48);
        var hit = puzzle.Find("HitButton").GetComponent<Button>();
        Position(hit.GetComponent<RectTransform>(), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(0, 20), new Vector2(560, 44));
        hit.GetComponentInChildren<Text>().text = "Контакт";
        var dots = puzzle.Find("StageDots") as RectTransform;
        if (dots == null)
            dots = Rect(puzzle, "StageDots", new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -24), new Vector2(68, 12));
        var indicators = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            var existing = dots.Find("Dot " + (i + 1));
            indicators[i] = existing == null ? Panel(dots, "Dot " + (i + 1), new Color32(79, 97, 92, 255)) : existing.GetComponent<Image>();
            Position(indicators[i].rectTransform, Vector2.zero, Vector2.zero, new Vector2(i * 28, 0), new Vector2(12, 12));
            indicators[i].sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            indicators[i].raycastTarget = false;
        }
        var time = puzzle.Find("TimeBar");
        var timeBar = time == null ? Panel(puzzle, "TimeBar", new Color32(15, 24, 28, 255)) : time.GetComponent<Image>();
        Position(timeBar.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f,
            new Vector2(0, -24), new Vector2(560, 4));
        timeBar.raycastTarget = false;
        var oldFill = timeBar.transform.Find("Fill");
        var fill = oldFill == null ? Panel(timeBar.transform, "Fill", Green) : oldFill.GetComponent<Image>();
        Stretch(fill.rectTransform);
        fill.raycastTarget = false;
        var config = new SerializedObject(root.GetComponent<TimingMinigame>());
        Set(config, "stageDots", indicators);
        Set(config, "timeFill", fill.rectTransform);
        config.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SyncInitialStats(GameObject root)
    {
        var data = new SerializedObject(root.GetComponent<PetStatus>());
        string[] nodes = { "Satiety", "Cleanliness", "Joy", "Breakdown" };
        string[] fields = { "startingSatiety", "startingCleanliness", "startingJoy", "startingBreakdown" };
        string[] labels = { "Сытость", "Чистота", "Радость", "Поломка" };
        for (int i = 0; i < nodes.Length; i++)
        {
            float value = data.FindProperty(fields[i]).floatValue;
            var row = root.transform.Find("Canvas/Home/Stats/" + nodes[i]);
            row.Find("Bar").GetComponent<Slider>().value = value;
            row.Find("Label").GetComponent<Text>().text = $"{labels[i]} {value:0}%";
        }
    }

    private static void WireHit(GameObject target, TimingMinigame game)
    {
        var component = target.AddComponent<TimingHitArea>();
        var data = new SerializedObject(component);
        Set(data, "game", game);
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Set(SerializedObject data, string name, Object value) =>
        data.FindProperty(name).objectReferenceValue = value;

    private static void Set<T>(SerializedObject data, string name, T[] values) where T : Object
    {
        var property = data.FindProperty(name);
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }

    private static RectTransform Rect(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        var rect = obj.GetComponent<RectTransform>();
        Position(rect, anchor, pivot, position, size);
        return rect;
    }

    private static void Position(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static Image Panel(Transform parent, string name, Color color)
    {
        var rect = Rect(parent, name, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, Vector2.zero);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private static Text Label(Transform parent, string name, string text, int size, Color color, TextAnchor alignment,
        Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 dimensions)
    {
        var rect = Rect(parent, name, anchor, pivot, position, dimensions);
        var label = rect.gameObject.AddComponent<Text>();
        label.font = font;
        label.fontSize = size;
        label.fontStyle = FontStyle.Normal;
        label.color = color;
        label.alignment = alignment;
        label.text = text;
        label.raycastTarget = false;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        return label;
    }

    private static Button MakeButton(Transform parent, string name, string title, Color background, Color ink,
        Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        var image = Panel(parent, name, background);
        Position(image.rectTransform, anchor, pivot, position, size);
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        var label = Label(image.transform, "Label", title, 24, ink, TextAnchor.MiddleCenter,
            Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, size);
        Stretch(label.rectTransform);
        return button;
    }
}
