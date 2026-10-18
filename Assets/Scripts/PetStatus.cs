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
        [SerializeField, Min(0)] private float satietyLossPerMinute = 108;
        [SerializeField, Min(0)] private float cleanlinessLossPerMinute = 72;
        [SerializeField, Min(0)] private float joyLossPerMinute = 90;
        [SerializeField, Min(0)] private float fullNeedHoldSeconds = 3;
        private readonly float[] fullNeedHold = new float[3];
        private bool decayEnabled = true;

        private readonly float[] values = new float[4];
        public event Action Changed;

        private void Awake() => ResetState();
        private void Update() => Tick(Time.deltaTime);

        public void SetDecayEnabled(bool value) => decayEnabled = value;

        public void Tick(float seconds)
        {
            if (!decayEnabled || seconds <= 0) return;
            Decay(0, satietyLossPerMinute, seconds);
            Decay(1, cleanlinessLossPerMinute, seconds);
            Decay(2, joyLossPerMinute, seconds);
        }

        private void Decay(int index, float lossPerMinute, float seconds)
        {
            float held = Mathf.Min(seconds, fullNeedHold[index]);
            fullNeedHold[index] -= held;
            if (seconds > held)
                SetValue((PetStat)index, values[index] - Mathf.Max(0, lossPerMinute) * (seconds - held) / 60f);
        }

        public float GetValue(PetStat stat) => values[(int)stat];

        public void SetValue(PetStat stat, float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                throw new ArgumentOutOfRangeException(nameof(value));
            value = Mathf.Clamp(value, 0, 100);
            if (Mathf.Approximately(values[(int)stat], value)) return;
            values[(int)stat] = value;
            if ((int)stat < 3) fullNeedHold[(int)stat] = value >= 100 ? fullNeedHoldSeconds : 0;
            Changed?.Invoke();
        }

        public void ResetState()
        {
            System.Array.Clear(fullNeedHold, 0, fullNeedHold.Length);
            values[0] = Mathf.Clamp(startingSatiety, 0, 100);
            values[1] = Mathf.Clamp(startingCleanliness, 0, 100);
            values[2] = Mathf.Clamp(startingJoy, 0, 100);
            values[3] = Mathf.Clamp(startingBreakdown, 0, 100);
            Changed?.Invoke();
        }
    }
}
