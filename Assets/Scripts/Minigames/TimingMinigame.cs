using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Tamagotchi.Minigames
{
    public sealed class TimingMinigame : MonoBehaviour
    {
        [Header("Timing puzzle")]
        [SerializeField, Range(0.1f, 0.8f)] private float firstTargetWidth = 0.46f;
        [SerializeField, Range(0.05f, 0.5f)] private float secondTargetWidth = 0.28f;
        [SerializeField, Range(0.03f, 0.3f)] private float thirdTargetWidth = 0.14f;
        [SerializeField, Min(0.1f)] private float cursorSpeed = 1f;
        [SerializeField, Min(1f)] private float secondsPerStage = 3f;
        [SerializeField, Min(0.1f)] private float hitFeedbackSeconds = 0.18f;

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

        [Header("Integration hooks")]
        [SerializeField] private UnityEvent onRepairSucceeded = new UnityEvent();
        [SerializeField] private UnityEvent onRepairFailed = new UnityEvent();
        [SerializeField] private UnityEvent onGameOver = new UnityEvent();

        private static readonly string[] StatNames = { "Сытость", "Чистота", "Радость", "Поломка" };
        private TimingRound round;
        private int lives;
        private float feedbackRemaining;
        private bool resultShown;
        private bool started;
        private bool HasRound => round != null;

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
            restartButton.onClick.AddListener(ResetGame);
            startButton.onClick.AddListener(ResetGame);
            petImage.enabled = petImage.sprite != null;
            placeholder.SetActive(petImage.sprite == null);
            ResetGame();
            started = false;
            RefreshHome();
        }

        private void Update()
        {
            if (!started || lives <= 0) return;
            if (HasRound)
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

        public void RequestRepair()
        {
            if (!started || lives <= 0 || HasRound) return;
            StartRepair();
        }

        public void StartRepair()
        {
            if (!enabled || !started || lives <= 0 || HasRound) return;
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
            if (!HasRound || round.State != TimingState.Playing) return;
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
            if (resultShown) return;
            resultShown = true;
            bool won = round.State == TimingState.Won;
            if (!won) lives = Mathf.Max(0, lives - 1);
            round = null;
            repairPanel.SetActive(false);
            careActions.CompleteRepair(won);
            RefreshHome();
            if (won) onRepairSucceeded.Invoke();
            else onRepairFailed.Invoke();
            if (lives == 0) onGameOver.Invoke();
        }

        public void ResetGame()
        {
            started = true;
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
        }

        private void RefreshHome()
        {
            startScreen.SetActive(!started);
            gameOverScreen.SetActive(started && lives <= 0);
            petStatus.SetDecayEnabled(started && lives > 0);
            careActions.SetAvailable(started && lives > 0 && !HasRound);
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
            if (restartButton != null) restartButton.onClick.RemoveListener(ResetGame);
            if (startButton != null) startButton.onClick.RemoveListener(ResetGame);
        }
    }
}
