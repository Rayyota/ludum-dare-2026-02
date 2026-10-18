using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Tamagotchi.Minigames;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Tamagotchi.Tests
{
    public class KeyCombinationTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [UnityTest]
        public IEnumerator PhysicalKeysWinImmediatelyAndCombinationsChangeOnEveryAttempt()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/TimingPrototype.unity");
            yield return new EnterPlayMode();
            yield return null;
            var game = Object.FindFirstObjectByType<TimingMinigame>();
            var keys = Field<KeyHoldGame>(game, "keyHold");
            var care = game.GetComponent<PetCareActions>();
            var feed = game.transform.Find("Canvas/Home/Pet/CareActions/FeedButton").GetComponent<Button>();
            Field<Button>(game, "startButton").onClick.Invoke();
            Set(care, "wearPerAction", 0f);
            var keyboard = InputSystem.AddDevice<Keyboard>();
            keyboard.MakeCurrent();
            try
            {
                int previous = 0;
                var seen = new HashSet<int>();
                for (int attempt = 0; attempt < 14; attempt++)
                {
                    var bag = Field<List<RepairKind>>(game, "repairBag");
                    bag.Clear(); bag.Add(RepairKind.Keys);
                    for (int i = 0; i < 8; i++) feed.onClick.Invoke();
                    int mask = Field<int>(keys, "requiredMask");
                    if (attempt % 7 == 0) seen.Clear();
                    Assert.That(seen.Add(mask), Is.True, "All seven combinations appear before repeating.");
                    Assert.That(mask, Is.Not.EqualTo(previous));
                    Assert.That(mask & 5, Is.Not.EqualTo(5), "W and S are never required together.");
                    var images = Field<Image[]>(keys, "keyImages");
                    int count = 0;
                    for (int i = 0; i < images.Length; i++)
                    {
                        bool required = (mask & (1 << i)) != 0;
                        Assert.That(images[i].gameObject.activeSelf, Is.EqualTo(required));
                        if (required) count++;
                    }
                    Assert.That(count, Is.InRange(2, 3));
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState(ToKeys(mask & (mask - 1))));
                    InputSystem.Update();
                    typeof(KeyHoldGame).GetMethod("Step", Private).Invoke(keys, new object[] { 0f });
                    Assert.That(keys.IsRunning, Is.True, "Partial combinations do not win.");
                    typeof(RepairChallenge).GetField("remaining", Private).SetValue(keys, 0.0001f);
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState(ToKeys(mask)));
                    InputSystem.Update();
                    typeof(RepairChallenge).GetMethod("Update", Private).Invoke(keys, null);
                    Assert.That(keys.IsRunning, Is.False, "Matching input wins without any holding delay.");
                    Assert.That(care.HasBrokenButtons, Is.False);
                    Assert.That(Field<int>(game, "lives"), Is.EqualTo(3));
                    Assert.That(keys.gameObject.activeSelf, Is.False);
                    previous = mask;
                }
            }
            finally { InputSystem.RemoveDevice(keyboard); }
            LogAssert.NoUnexpectedReceived();
            yield return new ExitPlayMode();
        }

        private static Key[] ToKeys(int mask)
        {
            var result = new List<Key>();
            Key[] keys = { Key.W, Key.A, Key.S, Key.D };
            for (int i = 0; i < keys.Length; i++) if ((mask & (1 << i)) != 0) result.Add(keys[i]);
            return result.ToArray();
        }

        private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Private).GetValue(owner);
        private static void Set(object owner, string name, object value) => owner.GetType().GetField(name, Private).SetValue(owner, value);
    }
}
