using System;

namespace Tamagotchi.Minigames
{
    public enum TimingState { Ready, Playing, BetweenStages, Won, Lost }

    public sealed class TimingRound
    {
        private readonly float[] widths;
        private readonly float speed;
        private readonly float stageDuration;
        private float travel;

        public TimingState State { get; private set; } = TimingState.Ready;
        public int Hits { get; private set; }
        public int RequiredHits => widths.Length;
        public float Cursor { get; private set; }
        public float TargetCenter { get; private set; }
        public float TargetWidth => widths[Math.Min(Hits, widths.Length - 1)];
        public float TimeRemaining { get; private set; }
        public bool TimedOut { get; private set; }

        public TimingRound(float[] targetWidths, float cursorSpeed, float secondsPerStage)
        {
            if (targetWidths == null || targetWidths.Length == 0)
                throw new ArgumentException("At least one target width is required.");
            foreach (float width in targetWidths)
                if (float.IsNaN(width) || width <= 0 || width > 1)
                    throw new ArgumentOutOfRangeException(nameof(targetWidths));
            if (float.IsNaN(cursorSpeed) || float.IsInfinity(cursorSpeed) || cursorSpeed <= 0)
                throw new ArgumentOutOfRangeException(nameof(cursorSpeed));
            if (float.IsNaN(secondsPerStage) || float.IsInfinity(secondsPerStage) || secondsPerStage <= 0)
                throw new ArgumentOutOfRangeException(nameof(secondsPerStage));
            widths = (float[])targetWidths.Clone();
            speed = cursorSpeed;
            stageDuration = secondsPerStage;
        }

        public void Start(float targetPosition)
        {
            Hits = 0;
            TimedOut = false;
            BeginStage(targetPosition);
        }

        public void Tick(float deltaTime)
        {
            if (State != TimingState.Playing || deltaTime <= 0) return;
            TimeRemaining = Math.Max(0, TimeRemaining - deltaTime);
            if (TimeRemaining <= 0)
            {
                TimedOut = true;
                State = TimingState.Lost;
                return;
            }
            travel = (travel + deltaTime * speed) % 2f;
            Cursor = travel <= 1f ? travel : 2f - travel;
        }

        public void Hit()
        {
            if (State != TimingState.Playing) return;
            if (Math.Abs(Cursor - TargetCenter) > TargetWidth * 0.5f)
            {
                State = TimingState.Lost;
                return;
            }
            Hits++;
            State = Hits == RequiredHits ? TimingState.Won : TimingState.BetweenStages;
        }

        public void NextStage(float targetPosition)
        {
            if (State == TimingState.BetweenStages) BeginStage(targetPosition);
        }

        private void BeginStage(float targetPosition)
        {
            float half = TargetWidth * 0.5f;
            TargetCenter = Math.Max(half, Math.Min(1f - half, targetPosition));
            travel = 0;
            Cursor = 0;
            TimeRemaining = stageDuration;
            State = TimingState.Playing;
        }
    }
}
