using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Tamagotchi.Minigames;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Tamagotchi.Tests
{
    public class AdditionalRepairTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [UnityTest]
        public IEnumerator FourBreakdownsLaunchAllFourGamesAndRestoreCare()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/TimingPrototype.unity");
            yield return new EnterPlayMode();
            yield return null;
            var game = Object.FindFirstObjectByType<TimingMinigame>();
            var care = game.GetComponent<PetCareActions>();
            var status = game.GetComponent<PetStatus>();
            var feed = game.transform.Find("Canvas/Home/Pet/CareActions/FeedButton").GetComponent<Button>();
            Field<Button>(game, "startButton").onClick.Invoke();
            status.SetDecayEnabled(false);
            Set(care, "wearPerAction", 0f);
            var seen = new HashSet<RepairKind>();
            for (int cycle = 0; cycle < 4; cycle++)
            {
                for (int press = 0; press < 8; press++) feed.onClick.Invoke();
                Canvas.ForceUpdateCanvases();
                var kind = Field<RepairKind?>(game, "lastRepair").Value;
                Assert.That(seen.Add(kind), Is.True, "Every game must appear once per four breakdowns.");
                switch (kind)
                {
                    case RepairKind.Timing: WinTiming(game); break;
                    case RepairKind.Contacts: WinContacts(Field<ContactMatchingGame>(game, "contactMatching")); break;
                    case RepairKind.Keys:
                        var keys = Field<KeyHoldGame>(game, "keyHold");
                        Assert.That(keys.IsRunning, Is.True);
                        int required = Field<int>(keys, "requiredMask");
                        PressKeys(keys, required & (required - 1));
                        Assert.That(keys.IsRunning, Is.True);
                        PressKeys(keys, required);
                        Assert.That(keys.IsRunning, Is.False);
                        break;
                    case RepairKind.Cleaning: CleanBoard(Field<BoardCleaningGame>(game, "boardCleaning")); break;
                }
                Assert.That(care.HasBrokenButtons, Is.False);
                Assert.That(Field<int>(game, "lives"), Is.EqualTo(3));
                Assert.That(Field<GameObject>(care, "controls").activeSelf, Is.True);
            }
            Assert.That(seen.Count, Is.EqualTo(4));
            RepairKind previous = Field<RepairKind?>(game, "lastRepair").Value;
            var draw = typeof(TimingMinigame).GetMethod("DrawRepair", Private);
            for (int cycle = 0; cycle < 8; cycle++)
            {
                seen.Clear();
                for (int i = 0; i < 4; i++)
                {
                    var kind = (RepairKind)draw.Invoke(game, null);
                    Assert.That(kind, Is.Not.EqualTo(previous));
                    Assert.That(seen.Add(kind), Is.True);
                    previous = kind;
                }
            }
            LogAssert.NoUnexpectedReceived();
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator NewGameTimeoutsRetryResetAndStopAtGameOver()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/TimingPrototype.unity");
            yield return new EnterPlayMode();
            yield return null;
            var game = Object.FindFirstObjectByType<TimingMinigame>();
            var care = game.GetComponent<PetCareActions>();
            var keys = Field<KeyHoldGame>(game, "keyHold");
            var cleaning = Field<BoardCleaningGame>(game, "boardCleaning");
            var feed = game.transform.Find("Canvas/Home/Pet/CareActions/FeedButton").GetComponent<Button>();
            Assert.That(Field<bool>(care, "voiceActive"), Is.False);
            Field<Button>(game, "startButton").onClick.Invoke();
            Assert.That(Field<bool>(care, "voiceActive"), Is.True);
            Set(care, "meowDelay", 0f);
            typeof(PetCareActions).GetMethod("Update", Private).Invoke(care, null);
            Assert.That(Field<float>(care, "meowDelay"), Is.InRange(3f, 5f));
            Set(care, "wearPerAction", 0f);
            ForceNext(game, RepairKind.Keys);
            for (int i = 0; i < 8; i++) feed.onClick.Invoke();
            keys.AdvanceTime(6);
            Assert.That(Field<int>(game, "lives"), Is.EqualTo(2));
            Assert.That(keys.gameObject.activeSelf, Is.False);
            ForceNext(game, RepairKind.Cleaning);
            feed.onClick.Invoke();
            Assert.That(cleaning.IsRunning, Is.True);
            cleaning.AdvanceTime(13);
            cleaning.AdvanceTime(13);
            Assert.That(Field<int>(game, "lives"), Is.EqualTo(1));
            ForceNext(game, RepairKind.Keys);
            feed.onClick.Invoke();
            PressKeys(keys, 0);
            typeof(TimingMinigame).GetMethod("TickSession", Private).Invoke(game, new object[] { 181f });
            Assert.That(keys.IsRunning, Is.False);
            Assert.That(cleaning.IsRunning, Is.False);
            Assert.That(Field<GameObject>(game, "gameOverScreen").activeSelf, Is.True);
            Assert.That(Field<bool>(care, "voiceActive"), Is.False);
            PressKeys(keys, 15);
            Assert.That(Field<int>(game, "lives"), Is.Zero);
            Field<Button>(game, "restartButton").onClick.Invoke();
            ForceNext(game, RepairKind.Keys);
            for (int i = 0; i < 8; i++) feed.onClick.Invoke();
            Assert.That(keys.IsRunning, Is.True);
            PressKeys(keys, Field<int>(keys, "requiredMask"));
            Assert.That(care.HasBrokenButtons, Is.False);
            LogAssert.NoUnexpectedReceived();
            yield return new ExitPlayMode();
        }

        private static void CleanBoard(BoardCleaningGame game)
        {
            var patches = Field<Image[]>(game, "dirt");
            foreach (var patch in patches)
            {
                if (!game.IsRunning) break;
                Vector3 center = patch.rectTransform.position;
                var pointer = new PointerEventData(EventSystem.current) {
                    button = PointerEventData.InputButton.Left,
                    position = RectTransformUtility.WorldToScreenPoint(null, center)
                };
                game.OnPointerDown(pointer);
                game.OnPointerUp(pointer);
                Assert.That(patch.color.a, Is.GreaterThan(0), "A stationary click does not remove dirt.");
                game.OnPointerDown(pointer);
                for (int stroke = 0; stroke < 6 && game.IsRunning; stroke++)
                {
                    var world = patch.rectTransform.TransformPoint(new Vector2(stroke % 2 == 0 ? -30 : 30, 0));
                    pointer.position = RectTransformUtility.WorldToScreenPoint(null, world);
                    game.OnDrag(pointer);
                }
                game.OnPointerUp(pointer);
            }
            Assert.That(game.IsRunning, Is.False);
            foreach (var patch in patches) Assert.That(patch.enabled, Is.False);
        }

        private static void WinTiming(TimingMinigame game)
        {
            var round = Field<TimingRound>(game, "round");
            round.Start(0.5f);
            for (int stage = 0; stage < 3; stage++)
            {
                round.Tick(round.TargetCenter / Field<float>(game, "cursorSpeed"));
                game.AttemptHit();
                if (stage < 2) round.NextStage(0.5f);
            }
        }

        private static void WinContacts(ContactMatchingGame game)
        {
            for (int i = 1; i <= 4; i++)
            {
                LogAssert.Expect(LogType.Log, $"Соединено: {i}/4");
                if (i == 4) LogAssert.Expect(LogType.Log, "Мини-игра пройдена!");
                game.ContactConnected();
            }
        }

        private static void PressKeys(KeyHoldGame game, int held) =>
            typeof(KeyHoldGame).GetMethod("CheckKeys", Private).Invoke(game, new object[] { held });
        private static void ForceNext(TimingMinigame game, RepairKind kind)
        {
            var bag = Field<List<RepairKind>>(game, "repairBag");
            bag.Clear(); bag.Add(kind);
        }
        private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Private).GetValue(owner);
        private static void Set(object owner, string name, object value) => owner.GetType().GetField(name, Private).SetValue(owner, value);
    }
}
