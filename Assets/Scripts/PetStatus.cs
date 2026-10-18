using System;
using UnityEngine;

namespace Tamagotchi
{
    public enum PetStat { Satiety, Cleanliness, Joy, Breakdown }

    public sealed class PetStatus : MonoBehaviour
    {
        [SerializeField, Range(0, 100)] private float startingSatiety;
        [SerializeField, Range(0, 100)] private float startingCleanliness;
        [SerializeField, Range(0, 100)] private float startingJoy;
        [SerializeField, Range(0, 100)] private float startingBreakdown;

        [Header("Need loss per minute")]
        [SerializeField, Min(0)] private float satietyLossPerMinute = 180;
        [SerializeField, Min(0)] private float cleanlinessLossPerMinute = 120;
        [SerializeField, Min(0)] private float joyLossPerMinute = 150;
        private bool decayEnabled = true;

        private readonly float[] values = new float[4];
        public event Action Changed;

        private void Awake() => ResetState();
        private void Update() => Tick(Time.deltaTime);

        public void SetDecayEnabled(bool value) => decayEnabled = value;

        public void Tick(float seconds)
        {
            if (!decayEnabled || seconds <= 0) return;
            SetValue(PetStat.Satiety, values[0] - Mathf.Max(0, satietyLossPerMinute) * seconds / 60f);
            SetValue(PetStat.Cleanliness, values[1] - Mathf.Max(0, cleanlinessLossPerMinute) * seconds / 60f);
            SetValue(PetStat.Joy, values[2] - Mathf.Max(0, joyLossPerMinute) * seconds / 60f);
        }

        public float GetValue(PetStat stat) => values[(int)stat];

        public void SetValue(PetStat stat, float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                throw new ArgumentOutOfRangeException(nameof(value));
            value = Mathf.Clamp(value, 0, 100);
            if (Mathf.Approximately(values[(int)stat], value)) return;
            values[(int)stat] = value;
            Changed?.Invoke();
        }

        public void ResetState()
        {
            values[0] = Mathf.Clamp(startingSatiety, 0, 100);
            values[1] = Mathf.Clamp(startingCleanliness, 0, 100);
            values[2] = Mathf.Clamp(startingJoy, 0, 100);
            values[3] = Mathf.Clamp(startingBreakdown, 0, 100);
            Changed?.Invoke();
        }
    }
}
