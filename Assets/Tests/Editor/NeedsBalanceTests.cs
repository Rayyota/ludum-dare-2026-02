using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Tamagotchi.Minigames;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Tamagotchi.Tests
{
    public class NeedsBalanceTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [UnityTest]
        public IEnumerator EmptyNeedsAllowRecoveryAndShareOneDamageCountdown()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/TimingPrototype.unity");
            yield return new EnterPlayMode();
            yield return null;
            var game = Object.FindFirstObjectByType<TimingMinigame>();
            var status = game.GetComponent<PetStatus>();
            Tick(game, 1000);
            Assert.That(Field<int>(game, "lives"), Is.EqualTo(3));
            Assert.That(Field<float>(game, "openingGraceRemaining"), Is.EqualTo(10));
            Field<Button>(game, "startButton").onClick.Invoke();
            status.SetDecayEnabled(false);
            Tick(game, 10);
            Tick(game, 9.9f);
            Assert.That(Field<int>(game, "lives"), Is.EqualTo(3));
            for (int i = 0; i < 3; i++) status.SetValue((PetStat)i, 20);
            Tick(game, 0.2f);
            Assert.That(Field<float>(game, "neglectedFor"), Is.Zero);
            status.SetValue(PetStat.Satiety, 0);
            Tick(game, 9.9f);
            status.SetValue(PetStat.Cleanliness, 0);
            Tick(game, 0.2f);
            Assert.That(Field<int>(game, "lives"), Is.EqualTo(2), "Several empty needs cost one shared heart.");
            Tick(game, 0.1f);
            Assert.That(Field<int>(game, "lives"), Is.EqualTo(2));
            for (int i = 0; i < 3; i++) status.SetValue((PetStat)i, 20);
            Tick(game, 0.1f);
            status.SetValue(PetStat.Joy, 0);
            var bag = Field<List<RepairKind>>(game, "repairBag");
            bag.Clear(); bag.Add(RepairKind.Keys);
            game.RequestRepair();
            var keys = Field<KeyHoldGame>(game, "keyHold");
            Assert.That(keys.IsRunning, Is.True);
            Tick(game, 10);
            Assert.That(Field<int>(game, "lives"), Is.EqualTo(1));
            Assert.That(keys.IsRunning, Is.True);
            Tick(game, 10);
            Assert.That(Field<int>(game, "lives"), Is.Zero);
            Assert.That(keys.IsRunning, Is.False);
            Assert.That(Field<GameObject>(game, "gameOverScreen").activeSelf, Is.True);
            Tick(game, 1000);
            Assert.That(Field<int>(game, "lives"), Is.Zero);
            Field<Button>(game, "restartButton").onClick.Invoke();
            Assert.That(Field<int>(game, "lives"), Is.EqualTo(3));
            Assert.That(Field<float>(game, "neglectedFor"), Is.Zero);
            Assert.That(Field<float>(game, "openingGraceRemaining"), Is.EqualTo(10));
            LogAssert.NoUnexpectedReceived();
            yield return new ExitPlayMode();
        }

        private static void Tick(TimingMinigame game, float seconds) =>
            typeof(TimingMinigame).GetMethod("TickNeedsRisk", Private).Invoke(game, new object[] { seconds });
        private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Private).GetValue(owner);
    }
}
