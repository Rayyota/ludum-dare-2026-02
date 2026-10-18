using NUnit.Framework;
using Tamagotchi.Minigames;

namespace Tamagotchi.Tests
{
    public class TimingRoundTests
    {
        private static TimingRound Create() => new TimingRound(new[] { 0.5f, 0.3f, 0.1f }, 1f, 5f);

        [Test]
        public void ThreeHitsShrinkTheTargetAndWin()
        {
            var round = Create();
            round.Start(0.5f);
            foreach (float width in new[] { 0.5f, 0.3f, 0.1f })
            {
                Assert.That(round.TargetWidth, Is.EqualTo(width));
                round.Tick(0.5f);
                round.Hit();
                if (round.Hits < 3)
                {
                    Assert.That(round.State, Is.EqualTo(TimingState.BetweenStages));
                    round.NextStage(0.5f);
                }
            }
            Assert.That(round.State, Is.EqualTo(TimingState.Won));
            Assert.That(round.Hits, Is.EqualTo(3));
        }

        [Test]
        public void MissEndsAttemptAndExtraClicksDoNotChangeResult()
        {
            var round = Create();
            round.Start(0.5f);
            round.Hit();
            round.Hit();
            round.Tick(1f);
            Assert.That(round.State, Is.EqualTo(TimingState.Lost));
            Assert.That(round.Hits, Is.Zero);
            Assert.That(round.TimedOut, Is.False);
        }

        [Test]
        public void TimeoutFailsWithoutClick()
        {
            var round = Create();
            round.Start(0.5f);
            round.Tick(5f);
            Assert.That(round.State, Is.EqualTo(TimingState.Lost));
            Assert.That(round.TimedOut, Is.True);
        }

        [Test]
        public void FeedbackIgnoresClicksAndDoesNotConsumeTime()
        {
            var round = Create();
            round.Start(0.5f);
            round.Tick(0.5f);
            round.Hit();
            round.Tick(10f);
            round.Hit();
            Assert.That(round.Hits, Is.EqualTo(1));
            Assert.That(round.State, Is.EqualTo(TimingState.BetweenStages));
            round.NextStage(0.5f);
            Assert.That(round.TimeRemaining, Is.EqualTo(5f));
            Assert.That(round.Cursor, Is.Zero);
        }

        [Test]
        public void CursorReflectsAtBothEnds()
        {
            var round = Create();
            round.Start(0.5f);
            round.Tick(1.25f);
            Assert.That(round.Cursor, Is.EqualTo(0.75f).Within(0.001f));
            round.Tick(1f);
            Assert.That(round.Cursor, Is.EqualTo(0.25f).Within(0.001f));
        }

        [Test]
        public void TargetStaysInsideTrack()
        {
            var round = Create();
            round.Start(-1f);
            Assert.That(round.TargetCenter, Is.EqualTo(0.25f));
            round.Start(2f);
            Assert.That(round.TargetCenter, Is.EqualTo(0.75f));
        }

        [Test]
        public void RestartResetsFailedAttempt()
        {
            var round = Create();
            round.Start(0.5f);
            round.Tick(5f);
            round.Start(0.5f);
            Assert.That(round.State, Is.EqualTo(TimingState.Playing));
            Assert.That(round.TimedOut, Is.False);
            Assert.That(round.Hits, Is.Zero);
            Assert.That(round.TargetWidth, Is.EqualTo(0.5f));
        }

        [Test]
        public void HitOnTargetBoundaryIsAccepted()
        {
            var round = Create();
            round.Start(0.5f);
            round.Tick(0.25f);
            round.Hit();
            Assert.That(round.Hits, Is.EqualTo(1));
        }
    }
}
