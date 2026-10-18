using Tamagotchi;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class PetAudioSetup
{
    [MenuItem("Tools/Tamagotchi/Apply Supplied Audio")]
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
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/pet-meow.mp3");
        if (clip == null) return;
        var voice = root.transform.Find("Pet Voice");
        if (voice == null)
        {
            voice = new GameObject("Pet Voice", typeof(AudioSource)).transform;
            voice.SetParent(root.transform, false);
        }
        var source = voice.GetComponent<AudioSource>();
        source.clip = clip;
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0;
        source.volume = 0.65f;
        var care = new SerializedObject(root.GetComponent<PetCareActions>());
        care.FindProperty("petVoice").objectReferenceValue = source;
        care.ApplyModifiedPropertiesWithoutUndo();
        ConfigureButtonSounds(root);
        var musicClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/track-05.mp3");
        if (musicClip == null) return;
        var music = root.transform.Find("Background Music");
        if (music == null)
        {
            music = new GameObject("Background Music", typeof(AudioSource)).transform;
            music.SetParent(root.transform, false);
        }
        var musicSource = music.GetComponent<AudioSource>();
        musicSource.clip = musicClip;
        musicSource.playOnAwake = true;
        musicSource.loop = true;
        musicSource.spatialBlend = 0;
        musicSource.volume = 0.22f;
    }

    private static void ConfigureButtonSounds(GameObject root)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/button-click.mp3");
        if (clip == null) return;
        var audio = root.transform.Find("Button Click");
        if (audio == null)
        {
            audio = new GameObject("Button Click", typeof(AudioSource)).transform;
            audio.SetParent(root.transform, false);
        }
        var source = audio.GetComponent<AudioSource>();
        source.clip = clip;
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0;
        source.volume = 0.6f;
        foreach (var button in root.GetComponentsInChildren<Button>(true))
        {
            var sound = button.GetComponent<ButtonClickSound>();
            if (sound == null) sound = button.gameObject.AddComponent<ButtonClickSound>();
            var data = new SerializedObject(sound);
            data.FindProperty("source").objectReferenceValue = source;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }

}
