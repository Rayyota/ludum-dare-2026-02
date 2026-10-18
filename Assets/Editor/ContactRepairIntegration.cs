using Tamagotchi.Minigames;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class ContactRepairIntegration
{
    private const string MatchingPrefab = "Assets/Prefabs/Contact Matching UI.prefab";
    private const string MainPrefab = "Assets/Prefabs/Timing Prototype UI.prefab";

    [MenuItem("Tools/Tamagotchi/Integrate Contact Repairs")]
    public static void Apply()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Minigames/ContactMatching.unity");
        var canvas = Object.FindFirstObjectByType<Canvas>();
        var manager = Object.FindFirstObjectByType<ContactMatchingGame>();
        manager.transform.SetParent(canvas.transform, false);
        var data = new SerializedObject(manager);
        data.FindProperty("playOnStart").boolValue = false;
        data.FindProperty("showResultPanel").boolValue = false;
        data.ApplyModifiedPropertiesWithoutUndo();
        var panel = canvas.gameObject;
        Object.DestroyImmediate(panel.GetComponent<GraphicRaycaster>());
        Object.DestroyImmediate(panel.GetComponent<CanvasScaler>());
        Object.DestroyImmediate(canvas);
        panel.name = "ContactRepairPanel";
        Stretch(panel.GetComponent<RectTransform>());
        panel.transform.Find("Background").GetComponent<Image>().raycastTarget = true;
        panel.SetActive(false);
        PrefabUtility.SaveAsPrefabAsset(panel, MatchingPrefab);
        var root = PrefabUtility.LoadPrefabContents(MainPrefab);
        try
        {
            Configure(root);
            AdditionalRepairSetup.Configure(root);
            SessionTimerSetup.Configure(root);
            VictoryScreenSetup.Configure(root);
            PrefabUtility.SaveAsPrefabAsset(root, MainPrefab);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    public static void Configure(GameObject root)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(MatchingPrefab);
        if (asset == null) return;
        var canvas = root.transform.Find("Canvas");
        var previous = canvas.Find("ContactRepairPanel") ?? canvas.Find("Contact Matching UI");
        if (previous != null) Object.DestroyImmediate(previous.gameObject);
        var panel = (GameObject)PrefabUtility.InstantiatePrefab(asset, root.scene);
        panel.name = "ContactRepairPanel";
        panel.transform.SetParent(canvas, false);
        Stretch(panel.GetComponent<RectTransform>());
        panel.SetActive(false);
        var data = new SerializedObject(root.GetComponent<TimingMinigame>());
        data.FindProperty("contactRepairPanel").objectReferenceValue = panel;
        data.FindProperty("contactMatching").objectReferenceValue = panel.GetComponentInChildren<ContactMatchingGame>(true);
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Stretch(RectTransform rect)
    {
        rect.localScale = Vector3.one;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
