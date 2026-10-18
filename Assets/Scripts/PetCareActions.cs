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

        [Header("Button wear")]
        [SerializeField, Min(2)] private int pressesWhenHealthy = 6;
        [SerializeField, Min(1)] private int pressesWhenWorn = 3;
        [SerializeField, Range(0, 20)] private float wearPerAction = 3;
        [SerializeField] private Color brokenColor = new Color32(204, 110, 94, 255);

        private readonly int[] presses = new int[3];
        private readonly bool[] broken = new bool[3];
        private Color[] normalColors;
        private int pendingAction = -1;
        private bool available = true;
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

        public void SetAvailable(bool value)
        {
            available = value;
            controls.SetActive(value);
            foreach (var button in Buttons) button.interactable = value;
        }

        public void ResetState()
        {
            if (normalColors == null)
            {
                normalColors = new Color[3];
                var buttons = Buttons;
                for (int i = 0; i < 3; i++) normalColors[i] = buttons[i].targetGraphic.color;
            }
            pendingAction = -1;
            for (int i = 0; i < 3; i++)
            {
                presses[i] = 0;
                broken[i] = false;
                Buttons[i].targetGraphic.color = normalColors[i];
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
            Buttons[index].targetGraphic.color = normalColors[index];
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
                Buttons[index].targetGraphic.color = brokenColor;
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
        }
    }
}
