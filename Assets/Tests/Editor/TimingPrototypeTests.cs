using System.Collections;
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
    public class TimingPrototypeTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private const string ScenePath = "Assets/Scenes/TimingPrototype.unity";

        [Test]
        public void InterfaceExistsInSceneBeforePlayAndUsesTiny5()
        {
            EditorSceneManager.OpenScene(ScenePath);
            var game = Object.FindFirstObjectByType<TimingMinigame>();
            Assert.That(game, Is.Not.Null);
            Assert.That(game.GetComponentInChildren<Canvas>(), Is.Not.Null);
            Assert.That(game.transform.Find("Canvas/Home/Pet"), Is.Not.Null);
            Assert.That(game.transform.Find("Canvas/Home/Stats").childCount, Is.EqualTo(4));
            Assert.That(game.transform.Find("Canvas/Home/Lives").childCount, Is.EqualTo(3));
            Assert.That(game.transform.Find("Canvas/Home/Pet/CareActions").childCount, Is.EqualTo(3));
            Assert.That(Field<Image>(game, "petImage").sprite.texture.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(Field<Sprite>(game, "healthyPetSprite"), Is.Not.Null);
            Assert.That(Field<Sprite>(game, "brokenPetSprite"), Is.Not.Null);
            foreach (var button in game.GetComponentsInChildren<Button>(true))
            {
                Assert.That(button.transition, Is.EqualTo(Selectable.Transition.None), button.name);
                Assert.That(button.GetComponent<ButtonClickSound>(), Is.Not.Null, button.name);
            }
            foreach (var heart in game.GetComponentsInChildren<HeartIcon>())
                Assert.That(heart.GetComponent<CanvasRenderer>(), Is.Not.Null);
            foreach (var label in game.GetComponentsInChildren<Text>(true))
            {
                Assert.That(label.font, Is.Not.Null, label.name);
                Assert.That(label.font.name, Does.Contain("Tiny5"));
            }
            var font = game.GetComponentInChildren<Text>(true).font;
            foreach (char letter in "СытостьЧистотаРадостьПоломкаПойматьконтактЁё0123456789%")
                Assert.That(font.HasCharacter(letter), Is.True, "Missing font character: " + letter);
        }

        [Test]
        public void NeedsDecayGraduallyButBreakdownDoesNot()
        {
            var host = new GameObject("Need decay test");
            try
            {
                var status = host.AddComponent<PetStatus>();
                status.ResetState();
                for (int i = 0; i < 4; i++) status.SetValue((PetStat)i, i == 3 ? 100 : 90);
                status.Tick(1);
                Assert.That(status.GetValue(PetStat.Satiety), Is.EqualTo(88.2f).Within(0.001f));
                Assert.That(status.GetValue(PetStat.Cleanliness), Is.EqualTo(88.8f).Within(0.001f));
                Assert.That(status.GetValue(PetStat.Joy), Is.EqualTo(88.5f).Within(0.001f));
                Assert.That(status.GetValue(PetStat.Breakdown), Is.EqualTo(100));
                status.SetDecayEnabled(false);
                status.Tick(60);
                Assert.That(status.GetValue(PetStat.Satiety), Is.EqualTo(88.2f).Within(0.001f));
                status.SetDecayEnabled(true);
                status.Tick(100000);
                for (int i = 0; i < 3; i++) Assert.That(status.GetValue((PetStat)i), Is.Zero);
            }
            finally { Object.DestroyImmediate(host); }
        }

        [UnityTest]
        public IEnumerator DeviceButtonsFollowArtworkAndReceiveClicks()
        {
            EditorSceneManager.OpenScene(ScenePath);
            yield return new EnterPlayMode();
            var game = Object.FindFirstObjectByType<TimingMinigame>();
            Field<Button>(game, "startButton").onClick.Invoke();
            var status = game.GetComponent<PetStatus>();
            status.SetDecayEnabled(false);
            yield return null;
            Assert.That(game.transform.Find("Canvas/Home/CareActions"), Is.Null);
            var controls = game.transform.Find("Canvas/Home/Pet/CareActions");
            string[] names = { "FeedButton", "WashButton", "PlayButton" };
            Vector2[] centers = { new Vector2(63, 104), new Vector2(90, 100), new Vector2(37, 102) };
            for (int i = 0; i < names.Length; i++)
            {
                var button = controls.Find(names[i]).GetComponent<Button>();
                Assert.That(button.GetComponentInChildren<Text>(true), Is.Null);
                ClickDeviceButton(game, button, centers[i]);
                Assert.That(status.GetValue((PetStat)i), Is.EqualTo(8));
                var rect = button.GetComponent<RectTransform>();
                Vector2 corner = RectTransformUtility.WorldToScreenPoint(null,
                    rect.TransformPoint(rect.rect.max - Vector2.one * 0.1f));
                Assert.That(button.GetComponent<CircularButtonHitArea>().IsRaycastLocationValid(corner, null), Is.False);
            }
            var pet = Field<Image>(game, "petImage").rectTransform;
            pet.anchoredPosition += new Vector2(30, 10);
            pet.sizeDelta = new Vector2(500, 320);
            Canvas.ForceUpdateCanvases();
            yield return null;
            ClickDeviceButton(game, controls.Find("FeedButton").GetComponent<Button>(), centers[0]);
            Assert.That(status.GetValue(PetStat.Satiety), Is.EqualTo(16));
            LogAssert.NoUnexpectedReceived();
            yield return new ExitPlayMode();
        }

        private static void ClickDeviceButton(TimingMinigame game, Button expected, Vector2 sourcePixel)
        {
            var pet = Field<Image>(game, "petImage").rectTransform;
            var size = pet.rect.size;
            const float aspect = 114f / 124f;
            if (size.x / size.y > aspect) size.x = size.y * aspect;
            else size.y = size.x / aspect;
            Vector2 local = pet.rect.center + new Vector2((sourcePixel.x / 114f - 0.5f) * size.x,
                (0.5f - sourcePixel.y / 124f) * size.y);
            var point = RectTransformUtility.WorldToScreenPoint(null, pet.TransformPoint(local));
            var pointer = new PointerEventData(EventSystem.current)
                { position = point, button = PointerEventData.InputButton.Left };
            var results = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, results);
            Assert.That(results.Count, Is.GreaterThan(0));
            Assert.That(results[0].gameObject, Is.EqualTo(expected.gameObject));
            ExecuteEvents.Execute(results[0].gameObject, pointer, ExecuteEvents.pointerClickHandler);
        }

        [UnityTest]
        public IEnumerator ButtonWearRepairsLivesAndRestartWorkTogether()
        {
            EditorSceneManager.OpenScene(ScenePath);
            yield return new EnterPlayMode();
            var game = Object.FindFirstObjectByType<TimingMinigame>();
            Set(game, "contactMatching", null);
            Set(game, "keyHold", null);
            Set(game, "boardCleaning", null);
            yield return null;
            Assert.That(Field<GameObject>(game, "startScreen").activeSelf, Is.True);
            Field<Button>(game, "startButton").onClick.Invoke();
            Assert.That(Field<GameObject>(game, "startScreen").activeSelf, Is.False);
            var status = game.GetComponent<PetStatus>();
            var care = game.GetComponent<PetCareActions>();
            status.SetDecayEnabled(false);
            var careRoot = game.transform.Find("Canvas/Home/Pet/CareActions");
            var feed = careRoot.Find("FeedButton").GetComponent<Button>();
            var wash = careRoot.Find("WashButton").GetComponent<Button>();
            var play = careRoot.Find("PlayButton").GetComponent<Button>();
            var bars = Field<Slider[]>(game, "statBars");
            AssertLives(game, 3);
            Assert.That(Field<TimingRound>(game, "round"), Is.Null);
            Assert.That(game.transform.Find("Canvas/RepairPanel/Puzzle").GetComponentsInChildren<Text>(true).Length,
                Is.EqualTo(1), "The modal only has the contact button label.");
            Assert.That(game.transform.Find("Canvas/RepairPanel/Puzzle/StageDots").childCount, Is.EqualTo(3));

            feed.onClick.Invoke(); wash.onClick.Invoke(); play.onClick.Invoke();
            for (int i = 0; i < 3; i++) Assert.That(bars[i].value, Is.EqualTo(8));
            Assert.That(bars[3].value, Is.EqualTo(3.75f), "Completed care gradually increases wear.");

            game.ResetGame(); status.SetDecayEnabled(false);
            Set(care, "wearPerAction", 0f);
            wash.onClick.Invoke(); play.onClick.Invoke();
            for (int i = 0; i < 7; i++) feed.onClick.Invoke();
            Assert.That(Field<TimingRound>(game, "round"), Is.Null, "Each button has its own counter.");
            Assert.That(status.GetValue(PetStat.Satiety), Is.EqualTo(56));
            feed.onClick.Invoke();
            Assert.That(Field<TimingRound>(game, "round"), Is.Not.Null, "Eighth healthy press breaks the button.");
            status.SetValue(PetStat.Satiety, 10);
            feed.onClick.Invoke();
            Assert.That(status.GetValue(PetStat.Satiety), Is.EqualTo(10), "No care during a repair.");
            Win(game);
            Assert.That(status.GetValue(PetStat.Satiety), Is.EqualTo(18), "Repair completes interrupted care once.");
            Assert.That(Field<GameObject>(game, "repairPanel").activeSelf, Is.False);
            AssertLives(game, 3);
            Assert.That(Field<Image>(game, "petImage").sprite, Is.EqualTo(Field<Sprite>(game, "healthyPetSprite")));

            for (int i = 0; i < 7; i++) feed.onClick.Invoke();
            Assert.That(Field<TimingRound>(game, "round"), Is.Null, "Success resets the button counter.");
            feed.onClick.Invoke();
            status.SetValue(PetStat.Satiety, 10);
            Lose(game);
            AssertLives(game, 2);
            Assert.That(Field<Image>(game, "petImage").sprite, Is.EqualTo(Field<Sprite>(game, "brokenPetSprite")));
            Assert.That(status.GetValue(PetStat.Satiety), Is.EqualTo(10));
            wash.onClick.Invoke();
            Assert.That(status.GetValue(PetStat.Cleanliness), Is.EqualTo(16), "Other buttons remain usable.");
            for (int remaining = 1; remaining >= 0; remaining--)
            {
                feed.onClick.Invoke();
                Assert.That(Field<TimingRound>(game, "round"), Is.Not.Null, "Broken button starts a retry.");
                Lose(game);
                AssertLives(game, remaining);
            }
            feed.onClick.Invoke(); status.Tick(60);
            Assert.That(status.GetValue(PetStat.Satiety), Is.EqualTo(10), "Game over stops care and decay.");
            Assert.That(Field<GameObject>(game, "gameOverScreen").activeSelf, Is.True);
            Field<Button>(game, "restartButton").onClick.Invoke();
            Assert.That(Field<GameObject>(game, "gameOverScreen").activeSelf, Is.False);
            AssertLives(game, 3);
            Assert.That(Field<TimingRound>(game, "round"), Is.Null);
            for (int i = 0; i < 4; i++) Assert.That(bars[i].value, Is.Zero);

            feed.onClick.Invoke(); feed.onClick.Invoke(); feed.onClick.Invoke();
            status.SetValue(PetStat.Breakdown, 100);
            Assert.That(care.CurrentPressLimit, Is.EqualTo(4));
            feed.onClick.Invoke();
            Assert.That(Field<TimingRound>(game, "round"), Is.Not.Null,
                "Increasing wear shortens the limit even for an already used button.");
            Field<TimingRound>(game, "round").Tick(5);
            typeof(TimingMinigame).GetMethod("Update", Private).Invoke(game, null);
            AssertLives(game, 2);
            Assert.That(Field<GameObject>(game, "repairPanel").activeSelf, Is.False);
            feed.onClick.Invoke();
            Win(game);
            Assert.That(status.GetValue(PetStat.Satiety), Is.EqualTo(32).Within(0.1));
            Assert.That(Field<GameObject>(game, "repairPanel").activeSelf, Is.False);
            LogAssert.NoUnexpectedReceived();
            yield return new ExitPlayMode();
        }

        private static void Win(TimingMinigame game)
        {
            var round = Field<TimingRound>(game, "round");
            round.Start(0.5f);
            for (int stage = 0; stage < 3; stage++)
            {
                round.Tick(round.TargetCenter / Field<float>(game, "cursorSpeed"));
                Click(Field<RectTransform>(game, "cursor").parent.gameObject);
                if (stage < 2) round.NextStage(0.5f);
            }
            Assert.That(round.State, Is.EqualTo(TimingState.Won));
        }

        private static void Lose(TimingMinigame game)
        {
            var round = Field<TimingRound>(game, "round");
            round.Start(0.5f);
            Click(Field<Button>(game, "hitButton").gameObject);
            Click(Field<RectTransform>(game, "cursor").parent.gameObject);
            Assert.That(Field<TimingRound>(game, "round"), Is.Null);
        }

        private static void Set(object target, string name, object value) =>
            target.GetType().GetField(name, Private).SetValue(target, value);

        private static void AssertLives(TimingMinigame game, int expected)
        {
            var hearts = Field<HeartIcon[]>(game, "hearts");
            Assert.That(hearts.Length, Is.EqualTo(3));
            Assert.That(System.Array.FindAll(hearts, heart => heart.Filled).Length, Is.EqualTo(expected));
        }

        private static T Field<T>(TimingMinigame game, string name) =>
            (T)typeof(TimingMinigame).GetField(name, Private).GetValue(game);

        private static void Click(GameObject target)
        {
            ExecuteEvents.Execute(target, new PointerEventData(EventSystem.current)
                { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerDownHandler);
        }
    }
}
