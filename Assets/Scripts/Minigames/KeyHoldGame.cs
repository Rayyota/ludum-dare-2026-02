using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Tamagotchi.Minigames
{
    public sealed class KeyHoldGame : RepairChallenge
    {
        [SerializeField] private Image[] keyImages;
        private static readonly int[] ComfortableCombinations = { 3, 9, 6, 12, 10, 11, 14 };
        private readonly List<int> combinations = new List<int>();
        private int requiredMask;
        private int previousCombination = -1;

        protected override void Prepare()
        {
            if (combinations.Count == 0)
            {
                for (int i = 0; i < ComfortableCombinations.Length; i++) combinations.Add(i);
                for (int i = combinations.Count - 1; i > 0; i--)
                {
                    int other = Random.Range(0, i + 1);
                    (combinations[i], combinations[other]) = (combinations[other], combinations[i]);
                }
                if (combinations[combinations.Count - 1] == previousCombination)
                    (combinations[0], combinations[combinations.Count - 1]) = (combinations[combinations.Count - 1], combinations[0]);
            }
            int index = combinations[combinations.Count - 1];
            combinations.RemoveAt(combinations.Count - 1);
            previousCombination = index;
            requiredMask = ComfortableCombinations[index];
            int count = 0;
            for (int i = 0; i < 4; i++) if ((requiredMask & (1 << i)) != 0) count++;
            int position = 0;
            for (int i = 0; i < keyImages.Length; i++)
            {
                bool required = (requiredMask & (1 << i)) != 0;
                keyImages[i].gameObject.SetActive(required);
                if (required)
                    keyImages[i].rectTransform.anchoredPosition = new Vector2((position++ - (count - 1) * 0.5f) * 116, -84);
            }
        }

        protected override void Step(float seconds)
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            int held = (keyboard.wKey.isPressed ? 1 : 0) | (keyboard.aKey.isPressed ? 2 : 0) |
                (keyboard.sKey.isPressed ? 4 : 0) | (keyboard.dKey.isPressed ? 8 : 0);
            CheckKeys(held);
        }

        private void CheckKeys(int held)
        {
            if (IsRunning && (held & requiredMask) == requiredMask) Finish(true);
        }
    }
}
