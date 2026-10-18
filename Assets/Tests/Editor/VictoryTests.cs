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
    public class VictoryTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [Test]
        public void FullNeedsStayFullBrieflyThenResumeDecay()
        {
            var host = new GameObject("Full need test");
            try
            {
                var status = host.AddComponent<PetStatus>();
                status.SetValue(PetStat.Satiety, 100);
                status.Tick(2);
                Assert.That(status.GetValue(PetStat.Satiety), Is.EqualTo(100));
                status.Tick(1);
                Assert.That(status.GetValue(PetStat.Satiety), Is.EqualTo(100));
                status.Tick(1);
                Assert.That(status.GetValue(PetStat.Satiety), Is.EqualTo(98.2f).Within(0.001));
                status.ResetState();
                status.SetValue(PetStat.Satiety, 30);
                status.Tick(1);
                Assert.That(status.GetValue(PetStat.Satiety), Is.EqualTo(28.2f).Within(0.001));
            }
            finally { Object.DestroyImmediate(host); }
        }

        [UnityTest]
        public IEnumerator FillingAllNeedsWinsStopsTheGameAndAllowsRetry()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/TimingPrototype.unity");
            yield return new EnterPlayMode();
            yield return null;
            var game = Object.FindFirstObjectByType<TimingMinigame>();
            var status = game.GetComponent<PetStatus>();
            var care = game.GetComponent<PetCareActions>();
            var victory = Field<GameObject>(game, "winScreen");
            Assert.That(victory.activeSelf, Is.False);
            Field<Button>(game, "startButton").onClick.Invoke();
            status.SetValue(PetStat.Satiety, 100);
            status.Tick(1);
            status.SetValue(PetStat.Cleanliness, 100);
            status.Tick(1);
            Assert.That(victory.activeSelf, Is.False);
            status.SetValue(PetStat.Joy, 100);
            Assert.That(victory.activeSelf, Is.True);
            Assert.That(Field<bool>(game, "sessionWon"), Is.True);
            Assert.That(Field<int>(game, "lives"), Is.EqualTo(3));
            Assert.That(Field<GameObject>(game, "gameOverScreen").activeSelf, Is.False);
            float time = Field<float>(game, "sessionTimeRemaining");
            typeof(TimingMinigame).GetMethod("TickSession", Private).Invoke(game, new object[] { 1000f });
            typeof(TimingMinigame).GetMethod("TickNeedsRisk", Private).Invoke(game, new object[] { 1000f });
            status.Tick(1000);
            game.RequestRepair();
            Assert.That(Field<float>(game, "sessionTimeRemaining"), Is.EqualTo(time));
            for (int i = 0; i < 3; i++) Assert.That(status.GetValue((PetStat)i), Is.EqualTo(100));
            Assert.That(Field<GameObject>(care, "controls").activeSelf, Is.False);
            Field<Button>(game, "winRetryButton").onClick.Invoke();
            Assert.That(victory.activeSelf, Is.False);
            Assert.That(Field<bool>(game, "sessionWon"), Is.False);
            Assert.That(Field<Text>(game, "sessionTimerText").text, Is.EqualTo("03:00"));
            for (int i = 0; i < 3; i++) Assert.That(status.GetValue((PetStat)i), Is.Zero);
            var bag = Field<List<RepairKind>>(game, "repairBag");
            bag.Clear(); bag.Add(RepairKind.Keys);
            game.RequestRepair();
            var keys = Field<KeyHoldGame>(game, "keyHold");
            Assert.That(keys.IsRunning, Is.True);
            for (int i = 0; i < 3; i++) status.SetValue((PetStat)i, 100);
            Assert.That(victory.activeSelf, Is.True);
            Assert.That(keys.IsRunning, Is.False);
            Assert.That(keys.gameObject.activeSelf, Is.False);
            keys.AdvanceTime(1000);
            Assert.That(Field<int>(game, "lives"), Is.EqualTo(3));
            Field<Button>(game, "winRetryButton").onClick.Invoke();
            typeof(TimingMinigame).GetMethod("TickSession", Private).Invoke(game, new object[] { 181f });
            Assert.That(Field<GameObject>(game, "gameOverScreen").activeSelf, Is.True);
            Assert.That(victory.activeSelf, Is.False);
            LogAssert.NoUnexpectedReceived();
            yield return new ExitPlayMode();
        }

        private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Private).GetValue(owner);
    }
}
