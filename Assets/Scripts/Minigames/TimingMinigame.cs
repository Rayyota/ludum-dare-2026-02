using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Tamagotchi.Minigames
{
    public sealed class TimingMinigame : MonoBehaviour
    {
        [Header("Timing puzzle")]
        [SerializeField, Range(0.1f, 0.8f)] private float firstTargetWidth = 0.48f;
        [SerializeField, Range(0.05f, 0.5f)] private float secondTargetWidth = 0.30f;
        [SerializeField, Range(0.03f, 0.3f)] private float thirdTargetWidth = 0.18f;
        [SerializeField, Min(0.1f)] private float cursorSpeed = 0.9f;
        [SerializeField, Min(1f)] private float secondsPerStage = 3f;
        [SerializeField, Min(0.1f)] private float hitFeedbackSeconds = 0.18f;

        [Header("Repair selection")]
        [SerializeField] private GameObject contactRepairPanel;
        [SerializeField] private ContactMatchingGame contactMatching;
        [SerializeField] private KeyHoldGame keyHold;
        [SerializeField] private BoardCleaningGame boardCleaning;

        [Header("Session timer")]
        [SerializeField, Min(1)] private float sessionDuration = 180;
        [SerializeField] private GameObject sessionTimerFrame;
        [SerializeField] private Text sessionTimerText;

        [Header("Empty needs")]
        [SerializeField, Min(1)] private float neglectSeconds = 10;
        [SerializeField, Min(0)] private float openingGraceSeconds = 10;

        [Header("Prototype loop")]
        [SerializeField, Min(1)] private int startingLives = 3;

        [Header("Scene UI references")]
        [SerializeField] private PetStatus petStatus;
        [SerializeField] private PetCareActions careActions;
        [SerializeField] private GameObject repairPanel;
        [SerializeField] private RectTransform target;
        [SerializeField] private RectTransform cursor;
        [SerializeField] private HeartIcon[] hearts;
        [SerializeField] private Slider[] statBars;
        [SerializeField] private Text[] statLabels;
        [SerializeField] private Text statusLabel;
        [SerializeField] private Image[] stageDots;
        [SerializeField] private RectTransform timeFill;
        [SerializeField] private Button hitButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Image petImage;
        [SerializeField] private GameObject placeholder;
        [SerializeField] private CanvasGroup petGroup;
        [Header("Supplied artwork")]
        [SerializeField] private Sprite healthyPetSprite;
        [SerializeField] private Sprite brokenPetSprite;
        [SerializeField] private GameObject startScreen;
        [SerializeField] private GameObject gameOverScreen;
        [SerializeField] private Button startButton;

        [Header("Victory")]
        [SerializeField] private GameObject winScreen;
        [SerializeField] private Button winRetryButton;

        [Header("Integration hooks")]
        [SerializeField] private UnityEvent onRepairSucceeded = new UnityEvent();
        [SerializeField] private UnityEvent onRepairFailed = new UnityEvent();
        [SerializeField] private UnityEvent onGameOver = new UnityEvent();
        [SerializeField] private UnityEvent onGameWon = new UnityEvent();

        private static readonly string[] StatNames = { "Сытость", "Чистота", "Радость", "Поломка" };
        private TimingRound round;
        private int lives;
        private float feedbackRemaining;
        private bool resultShown;
        private bool started;
        private bool sessionWon;
        private float sessionTimeRemaining;
        private float neglectedFor;
        private float openingGraceRemaining;
        private bool contactRepairActive;
        private RepairChallenge activeChallenge;
        private readonly List<RepairKind> repairBag = new List<RepairKind>();
        private RepairKind? lastRepair;
        private bool HasRound => round != null || contactRepairActive || activeChallenge != null;

        private void Awake()
        {
            if (petStatus == null || careActions == null || repairPanel == null || target == null || cursor == null ||
                hearts == null || hearts.Length == 0 || statBars == null || statBars.Length != 4 ||
                statLabels == null || statLabels.Length != 4 || hitButton == null || restartButton == null ||
                statusLabel == null || stageDots == null || stageDots.Length != 3 || timeFill == null ||
                petImage == null || placeholder == null || petGroup == null ||
                startScreen == null || gameOverScreen == null || startButton == null)
            {
                Debug.LogError("Timing Prototype UI references are missing. Open the TimingPrototype scene or use the Timing Prototype UI prefab.", this);
                enabled = false;
                return;
            }
            petStatus.Changed += RefreshStats;
            if (contactMatching != null) contactMatching.OnGameFinished += OnContactRepairFinished;
            if (keyHold != null) keyHold.OnGameFinished += OnChallengeFinished;
            if (boardCleaning != null) boardCleaning.OnGameFinished += OnChallengeFinished;
            restartButton.onClick.AddListener(ResetGame);
            startButton.onClick.AddListener(ResetGame);
            if (winRetryButton != null) winRetryButton.onClick.AddListener(ResetGame);
            petImage.enabled = petImage.sprite != null;
            placeholder.SetActive(petImage.sprite == null);
            ResetGame();
            started = false;
            RefreshHome();
        }

        private void Update()
        {
            if (!started || sessionWon || lives <= 0) return;
            TickSession(Time.deltaTime);
            TickNeedsRisk(Time.deltaTime);
            if (lives <= 0) return;
            if (round != null)
            {
                if (round.State == TimingState.BetweenStages)
                {
                    feedbackRemaining -= Time.deltaTime;
                    if (feedbackRemaining <= 0)
                    {
                        round.NextStage(Random.Range(0.25f, 0.75f));
                        hitButton.interactable = true;
                    }
                }
                else if (round.State == TimingState.Playing)
                    round.Tick(Time.deltaTime);
                RefreshRound();
                if (!resultShown && (round.State == TimingState.Won || round.State == TimingState.Lost))
                    FinishRepair();
                return;
            }
        }

        private void TickSession(float seconds)
        {
            if (!started || sessionWon || lives <= 0 || seconds <= 0) return;
            sessionTimeRemaining = Mathf.Max(0, sessionTimeRemaining - seconds);
            RefreshSessionTimer();
            if (sessionTimeRemaining > 0) return;
            lives = 0;
            resultShown = true;
            HideRepairs();
            RefreshHome();
            onGameOver.Invoke();
        }

        private void TickNeedsRisk(float seconds)
        {
            if (!started || sessionWon || lives <= 0 || seconds <= 0) return;
            float grace = Mathf.Min(openingGraceRemaining, seconds);
            openingGraceRemaining -= grace;
            seconds -= grace;
            if (seconds <= 0) return;
            bool empty = petStatus.GetValue(PetStat.Satiety) <= 0 ||
                petStatus.GetValue(PetStat.Cleanliness) <= 0 || petStatus.GetValue(PetStat.Joy) <= 0;
            if (!empty) { neglectedFor = 0; return; }
            neglectedFor += seconds;
            if (neglectedFor < neglectSeconds) return;
            neglectedFor = 0;
            lives = Mathf.Max(0, lives - 1);
            if (lives == 0)
            {
                resultShown = true;
                HideRepairs();
            }
            RefreshHome();
            if (lives == 0) onGameOver.Invoke();
        }

        private void RefreshSessionTimer()
        {
            if (sessionTimerText == null) return;
            int seconds = Mathf.CeilToInt(sessionTimeRemaining);
            sessionTimerText.text = $"{seconds / 60:00}:{seconds % 60:00}";
        }

        public void RequestRepair()
        {
            if (!enabled || !started || sessionWon || lives <= 0 || HasRound) return;
            var kind = DrawRepair();
            if (kind == RepairKind.Keys || kind == RepairKind.Cleaning)
            {
                activeChallenge = kind == RepairKind.Keys ? (RepairChallenge)keyHold : boardCleaning;
                resultShown = false;
                activeChallenge.gameObject.SetActive(true);
                RefreshHome();
                activeChallenge.BeginGame();
                return;
            }
            if (kind == RepairKind.Contacts)
            {
                contactRepairActive = true;
                resultShown = false;
                contactRepairPanel.SetActive(true);
                RefreshHome();
                if (contactMatching.BeginGame()) return;
                contactRepairActive = false;
                contactRepairPanel.SetActive(false);
            }
            StartRepair();
        }

        private RepairKind DrawRepair()
        {
            if (repairBag.Count == 0)
            {
                repairBag.Add(RepairKind.Timing);
                if (contactMatching != null && contactRepairPanel != null) repairBag.Add(RepairKind.Contacts);
                if (keyHold != null) repairBag.Add(RepairKind.Keys);
                if (boardCleaning != null) repairBag.Add(RepairKind.Cleaning);
                for (int i = repairBag.Count - 1; i > 0; i--)
                {
                    int index = Random.Range(0, i + 1);
                    (repairBag[i], repairBag[index]) = (repairBag[index], repairBag[i]);
                }
                if (repairBag.Count > 1 && repairBag[repairBag.Count - 1] == lastRepair)
                    (repairBag[0], repairBag[repairBag.Count - 1]) = (repairBag[repairBag.Count - 1], repairBag[0]);
            }
            int last = repairBag.Count - 1;
            var kind = repairBag[last];
            repairBag.RemoveAt(last);
            lastRepair = kind;
            return kind;
        }

        private void HideRepairs()
        {
            round = null;
            contactRepairActive = false;
            activeChallenge = null;
            if (contactMatching != null) contactMatching.CancelGame();
            if (contactRepairPanel != null) contactRepairPanel.SetActive(false);
            if (keyHold != null) { keyHold.CancelGame(); keyHold.gameObject.SetActive(false); }
            if (boardCleaning != null) { boardCleaning.CancelGame(); boardCleaning.gameObject.SetActive(false); }
            repairPanel.SetActive(false);
        }

        private void OnChallengeFinished(bool won)
        {
            if (activeChallenge != null) FinishRepair(won);
        }

        public void StartRepair()
        {
            if (!enabled || !started || sessionWon || lives <= 0 || HasRound) return;
            round = new TimingRound(new[]
            {
                Mathf.Clamp(firstTargetWidth, 0.03f, 0.95f),
                Mathf.Clamp(secondTargetWidth, 0.03f, firstTargetWidth),
                Mathf.Clamp(thirdTargetWidth, 0.03f, Mathf.Min(firstTargetWidth, secondTargetWidth))
            }, Mathf.Max(0.1f, cursorSpeed), Mathf.Max(1f, secondsPerStage));
            round.Start(Random.Range(0.35f, 0.65f));
            resultShown = false;
            repairPanel.SetActive(true);
            hitButton.interactable = true;
            RefreshRound();
            RefreshHome();
        }

        public void AttemptHit()
        {
            if (round == null || round.State != TimingState.Playing) return;
            round.Hit();
            if (round.State == TimingState.BetweenStages)
            {
                feedbackRemaining = Mathf.Max(0.1f, hitFeedbackSeconds);
                hitButton.interactable = false;
            }
            RefreshRound();
            if (round.State == TimingState.Won || round.State == TimingState.Lost) FinishRepair();
        }

        private void FinishRepair()
        {
            if (round != null) FinishRepair(round.State == TimingState.Won);
        }

        private void OnContactRepairFinished(bool won)
        {
            if (contactRepairActive) FinishRepair(won);
        }

        private void FinishRepair(bool won)
        {
            if (resultShown) return;
            resultShown = true;
            if (!won) lives = Mathf.Max(0, lives - 1);
            HideRepairs();
            careActions.CompleteRepair(won);
            RefreshHome();
            if (won) onRepairSucceeded.Invoke();
            else onRepairFailed.Invoke();
            if (lives == 0) onGameOver.Invoke();
        }

        public void ResetGame()
        {
            started = true;
            sessionWon = false;
            sessionTimeRemaining = Mathf.Max(1, sessionDuration);
            neglectedFor = 0;
            openingGraceRemaining = Mathf.Max(0, openingGraceSeconds);
            RefreshSessionTimer();
            HideRepairs();
            repairBag.Clear();
            lastRepair = null;
            careActions.ResetState();
            petStatus.ResetState();
            lives = Mathf.Clamp(startingLives, 1, hearts.Length);
            round = null;
            resultShown = false;
            repairPanel.SetActive(false);
            RefreshHome();
        }

        private void RefreshRound()
        {
            float half = round.TargetWidth * 0.5f;
            target.anchorMin = new Vector2(round.TargetCenter - half, 0);
            target.anchorMax = new Vector2(round.TargetCenter + half, 1);
            target.offsetMin = new Vector2(0, 4);
            target.offsetMax = new Vector2(0, -4);
            cursor.anchorMin = new Vector2(round.Cursor, 0);
            cursor.anchorMax = new Vector2(round.Cursor, 1);
            cursor.anchoredPosition = Vector2.zero;
            for (int i = 0; i < stageDots.Length; i++)
                stageDots[i].color = i < round.Hits ? new Color32(185, 208, 155, 255) : new Color32(79, 97, 92, 255);
            timeFill.anchorMax = new Vector2(Mathf.Clamp01(round.TimeRemaining / Mathf.Max(1f, secondsPerStage)), 1);
        }

        private void RefreshStats()
        {
            for (int i = 0; i < statBars.Length; i++)
            {
                float value = petStatus.GetValue((PetStat)i);
                statBars[i].value = value;
                statLabels[i].text = $"{StatNames[i]} {value:0}%";
            }
            if (started && !sessionWon && lives > 0 && petStatus.GetValue(PetStat.Satiety) >= 100 &&
                petStatus.GetValue(PetStat.Cleanliness) >= 100 && petStatus.GetValue(PetStat.Joy) >= 100)
            {
                sessionWon = true;
                resultShown = true;
                HideRepairs();
                RefreshHome();
                onGameWon.Invoke();
            }
        }

        private void RefreshHome()
        {
            startScreen.SetActive(!started);
            if (winScreen != null) winScreen.SetActive(started && sessionWon);
            if (sessionTimerFrame != null) sessionTimerFrame.SetActive(started && !sessionWon && lives > 0);
            gameOverScreen.SetActive(started && lives <= 0);
            petStatus.SetDecayEnabled(started && !sessionWon && lives > 0);
            careActions.SetVoiceActive(started && !sessionWon && lives > 0);
            careActions.SetAvailable(started && !sessionWon && lives > 0 && !HasRound);
            if (healthyPetSprite != null)
                petImage.sprite = (careActions.HasBrokenButtons || lives <= 0) && brokenPetSprite != null ? brokenPetSprite : healthyPetSprite;
            for (int i = 0; i < hearts.Length; i++) hearts[i].Filled = i < lives;
            statusLabel.text = lives <= 0 ? "Питомцу нужна новая попытка" : "";
            restartButton.gameObject.SetActive(lives <= 0);
            petGroup.alpha = lives <= 0 ? 0.35f : 1f;
        }

        private void OnDestroy()
        {
            if (petStatus != null) petStatus.Changed -= RefreshStats;
            if (contactMatching != null) contactMatching.OnGameFinished -= OnContactRepairFinished;
            if (keyHold != null) keyHold.OnGameFinished -= OnChallengeFinished;
            if (boardCleaning != null) boardCleaning.OnGameFinished -= OnChallengeFinished;
            if (restartButton != null) restartButton.onClick.RemoveListener(ResetGame);
            if (startButton != null) startButton.onClick.RemoveListener(ResetGame);
            if (winRetryButton != null) winRetryButton.onClick.RemoveListener(ResetGame);
        }
    }
}
