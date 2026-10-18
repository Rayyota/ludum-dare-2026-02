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
    public class RepairSelectionTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [UnityTest]
        public IEnumerator BothRepairTypesShareLivesCareAndTheSessionClock()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/TimingPrototype.unity");
            yield return new EnterPlayMode();
            yield return null;
            var game = Object.FindFirstObjectByType<TimingMinigame>();
            var contactPanel = Field<GameObject>(game, "contactRepairPanel");
            var matching = Field<ContactMatchingGame>(game, "contactMatching");
            var timer = Field<Text>(game, "sessionTimerText");
            var care = game.GetComponent<PetCareActions>();
            var status = game.GetComponent<PetStatus>();
            var feed = game.transform.Find("Canvas/Home/Pet/CareActions/FeedButton").GetComponent<Button>();
            Assert.That(Field<KeyHoldGame>(game, "keyHold"), Is.Not.Null);
            Assert.That(Field<BoardCleaningGame>(game, "boardCleaning"), Is.Not.Null);
            Assert.That(contactPanel.activeSelf, Is.False);
            Assert.That(Field<GameObject>(game, "sessionTimerFrame").activeSelf, Is.False);
            TickClock(game, 10);
            Assert.That(Field<float>(game, "sessionTimeRemaining"), Is.EqualTo(180));
            Field<Button>(game, "startButton").onClick.Invoke();
            status.SetDecayEnabled(false);
            Set(care, "wearPerAction", 0f);
            Assert.That(timer.text, Is.EqualTo("03:00"));
            Assert.That(Field<GameObject>(game, "sessionTimerFrame").activeSelf, Is.True);
            TickClock(game, 60);
            Assert.That(timer.text, Is.EqualTo("02:00"));
            ForceNext(game, RepairKind.Timing);
            for (int i = 0; i < 8; i++) feed.onClick.Invoke();
            Assert.That(Field<TimingRound>(game, "round"), Is.Not.Null);
            Assert.That(contactPanel.activeSelf, Is.False);
            Assert.That(status.GetValue(PetStat.Satiety), Is.EqualTo(56));
            Assert.That(feed.targetGraphic.color.a, Is.Zero, "The button hotspot stays invisible when broken.");
            TickClock(game, 2);
            game.AttemptHit();
            Assert.That(Field<int>(game, "lives"), Is.EqualTo(2));
            Assert.That(Field<GameObject>(game, "repairPanel").activeSelf, Is.False);
            ForceNext(game, RepairKind.Contacts);
            feed.onClick.Invoke();
            Assert.That(contactPanel.activeSelf, Is.True);
            Assert.That(Field<TimingRound>(game, "round"), Is.Null);
            feed.onClick.Invoke();
            game.AttemptHit();
            Assert.That(status.GetValue(PetStat.Satiety), Is.EqualTo(56));
            Assert.That(feed.targetGraphic.color.a, Is.Zero, "The button hotspot stays invisible when broken.");
            TickClock(game, 30);
            Assert.That(timer.text, Is.EqualTo("01:28"));
            WinContacts(matching);
            Assert.That(contactPanel.activeSelf, Is.False);
            Assert.That(status.GetValue(PetStat.Satiety), Is.EqualTo(64));
            Assert.That(care.HasBrokenButtons, Is.False);
            Assert.That(Field<int>(game, "lives"), Is.EqualTo(2));
            ForceNext(game, RepairKind.Contacts);
            for (int i = 0; i < 8; i++) feed.onClick.Invoke();
            Assert.That(contactPanel.activeSelf, Is.True);
            foreach (var contact in Field<ContactDrag[]>(matching, "contacts"))
                Assert.That(contact.GetComponent<Image>().raycastTarget, Is.True);
            LoseContacts(matching);
            Assert.That(Field<int>(game, "lives"), Is.EqualTo(1));
            Assert.That(contactPanel.activeSelf, Is.False);
            Assert.That(care.HasBrokenButtons, Is.True);
            ForceNext(game, RepairKind.Contacts);
            feed.onClick.Invoke();
            Assert.That(contactPanel.activeSelf, Is.True);
            LoseContacts(matching);
            Assert.That(Field<int>(game, "lives"), Is.Zero);
            Assert.That(Field<GameObject>(game, "gameOverScreen").activeSelf, Is.True);
            Field<Button>(game, "restartButton").onClick.Invoke();
            Assert.That(timer.text, Is.EqualTo("03:00"));
            Assert.That(Field<int>(game, "lives"), Is.EqualTo(3));
            ForceNext(game, RepairKind.Contacts);
            for (int i = 0; i < 8; i++) feed.onClick.Invoke();
            Assert.That(contactPanel.activeSelf, Is.True);
            TickClock(game, 181);
            Assert.That(timer.text, Is.EqualTo("00:00"));
            Assert.That(contactPanel.activeSelf, Is.False);
            Assert.That(matching.IsGameEnded, Is.True);
            Assert.That(Field<GameObject>(game, "gameOverScreen").activeSelf, Is.True);
            float value = status.GetValue(PetStat.Satiety);
            matching.ContactConnected();
            feed.onClick.Invoke();
            status.Tick(60);
            Assert.That(status.GetValue(PetStat.Satiety), Is.EqualTo(value));
            Assert.That(Field<int>(game, "lives"), Is.Zero);
            LogAssert.NoUnexpectedReceived();
            yield return new ExitPlayMode();
        }

        private static void ForceNext(TimingMinigame game, RepairKind kind)
        {
            var bag = Field<List<RepairKind>>(game, "repairBag");
            bag.Clear();
            bag.Add(kind);
        }

        private static void WinContacts(ContactMatchingGame matching)
        {
            for (int i = 1; i <= 4; i++)
            {
                LogAssert.Expect(LogType.Log, $"Соединено: {i}/4");
                if (i == 4) LogAssert.Expect(LogType.Log, "Мини-игра пройдена!");
                matching.ContactConnected();
            }
        }

        private static void LoseContacts(ContactMatchingGame matching)
        {
            LogAssert.Expect(LogType.Log, "Мини-игра проиграна!");
            Set(matching, "remainingTime", 0f);
            typeof(ContactMatchingGame).GetMethod("Update", Private).Invoke(matching, null);
        }

        private static void TickClock(TimingMinigame game, float seconds) =>
            typeof(TimingMinigame).GetMethod("TickSession", Private).Invoke(game, new object[] { seconds });
        private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Private).GetValue(owner);
        private static void Set(object owner, string name, object value) => owner.GetType().GetField(name, Private).SetValue(owner, value);
    }
}
