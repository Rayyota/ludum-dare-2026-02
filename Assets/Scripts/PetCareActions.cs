using Tamagotchi.Minigames;
using UnityEngine;
using UnityEngine.UI;

namespace Tamagotchi
{
    public sealed class PetCareActions : MonoBehaviour
    {
        [SerializeField] private PetStatus petStatus;
        [SerializeField] private TimingMinigame repairGame;
        [SerializeField] private GameObject controls;
        [SerializeField] private Button feedButton;
        [SerializeField] private Button washButton;
        [SerializeField] private Button playButton;
        [SerializeField, Range(1, 100)] private float feedAmount = 8;
        [SerializeField, Range(1, 100)] private float washAmount = 8;
        [SerializeField, Range(1, 100)] private float playAmount = 8;

        [Header("Audio")]
        [SerializeField] private AudioSource petVoice;

        [Header("Button wear")]
        [SerializeField, Min(2)] private int pressesWhenHealthy = 8;
        [SerializeField, Min(1)] private int pressesWhenWorn = 4;
        [SerializeField, Range(0, 20)] private float wearPerAction = 1.25f;

        private readonly int[] presses = new int[3];
        private readonly bool[] broken = new bool[3];
        private int pendingAction = -1;
        private bool available = true;
        private bool voiceActive;
        private float meowDelay;
        private Button[] Buttons => new[] { feedButton, washButton, playButton };
        public bool HasBrokenButtons => System.Array.Exists(broken, value => value);

        public void Feed() => Use(0);
        public void Wash() => Use(1);
        public void Play() => Use(2);

        public int CurrentPressLimit
        {
            get
            {
                int healthy = Mathf.Max(2, pressesWhenHealthy);
                int worn = Mathf.Clamp(pressesWhenWorn, 1, healthy);
                return Mathf.RoundToInt(Mathf.Lerp(healthy, worn, petStatus.GetValue(PetStat.Breakdown) / 100f));
            }
        }

        public void SetVoiceActive(bool value)
        {
            if (voiceActive == value) return;
            voiceActive = value;
            meowDelay = Random.Range(3f, 5f);
            if (!value && petVoice != null) petVoice.Stop();
        }

        private void Update()
        {
            if (!voiceActive || petVoice == null) return;
            meowDelay -= Time.deltaTime;
            if (meowDelay <= 0 && !petVoice.isPlaying) PlayMeow();
        }

        private void PlayMeow()
        {
            if (!voiceActive || petVoice == null || petVoice.isPlaying) return;
            petVoice.Play();
            meowDelay = Random.Range(3f, 5f);
        }

        public void SetAvailable(bool value)
        {
            available = value;
            controls.SetActive(value);
            foreach (var button in Buttons) button.interactable = value;
        }

        public void ResetState()
        {
            if (petVoice != null) petVoice.Stop();
            pendingAction = -1;
            for (int i = 0; i < 3; i++)
            {
                presses[i] = 0;
                broken[i] = false;
            }
        }

        public void CompleteRepair(bool succeeded)
        {
            if (pendingAction < 0) return;
            int index = pendingAction;
            pendingAction = -1;
            if (!succeeded) return;
            broken[index] = false;
            presses[index] = 0;
            ApplyCare(index);
        }

        private void Use(int index)
        {
            if (!available || !isActiveAndEnabled || petStatus == null || repairGame == null) return;
            if (!broken[index])
            {
                presses[index]++;
                if (presses[index] < CurrentPressLimit)
                {
                    ApplyCare(index);
                    return;
                }
                broken[index] = true;
            }
            pendingAction = index;
            repairGame.RequestRepair();
        }

        private void ApplyCare(int index)
        {
            var stat = (PetStat)index;
            float amount = index == 0 ? feedAmount : index == 1 ? washAmount : playAmount;
            petStatus.SetValue(stat, petStatus.GetValue(stat) + Mathf.Clamp(amount, 0, 100));
            petStatus.SetValue(PetStat.Breakdown, petStatus.GetValue(PetStat.Breakdown) + Mathf.Max(0, wearPerAction));
            PlayMeow();
        }
    }
}
