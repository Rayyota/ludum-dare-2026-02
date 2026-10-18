using System;
using UnityEngine;
using UnityEngine.UI;

namespace Tamagotchi.Minigames
{
    public abstract class RepairChallenge : MonoBehaviour
    {
        [SerializeField, Min(1)] private float durationSeconds = 12;
        [SerializeField] private Text timerText;
        private float remaining;
        public bool IsRunning { get; private set; }
        public event Action<bool> OnGameFinished;

        public bool BeginGame()
        {
            remaining = Mathf.Max(1, durationSeconds);
            IsRunning = true;
            Prepare();
            RefreshTimer();
            return true;
        }

        public void CancelGame() => IsRunning = false;

        protected virtual void Update()
        {
            if (IsRunning) Step(Time.deltaTime);
            AdvanceTime(Time.deltaTime);
        }

        public void AdvanceTime(float seconds)
        {
            if (!IsRunning || seconds <= 0) return;
            remaining = Mathf.Max(0, remaining - seconds);
            RefreshTimer();
            if (remaining <= 0) Finish(false);
        }

        private void RefreshTimer()
        {
            if (timerText == null) return;
            int seconds = Mathf.CeilToInt(remaining);
            timerText.text = $"{seconds / 60:00}:{seconds % 60:00}";
        }

        protected void Finish(bool won)
        {
            if (!IsRunning) return;
            IsRunning = false;
            OnGameFinished?.Invoke(won);
        }

        protected abstract void Prepare();
        protected virtual void Step(float seconds) { }
    }
}
